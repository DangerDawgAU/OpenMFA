using System.Diagnostics;
using System.Text;

namespace OpenMFA.SmartCard.MyEid;

/// <summary>
/// MyEID 4.5 smart card operations for Windows smart card logon.
/// Uses OpenSC's PKCS#15 tools (pkcs15-init, pkcs15-tool) which are officially supported.
/// </summary>
public class MyEidCard : IDisposable
{
    private readonly string _pkcs15InitPath;
    private readonly string _pkcs15ToolPath;
    private readonly uint? _readerNumber;
    private Action<string>? _logger;
    private bool _disposed;

    public MyEidCard(uint? readerNumber = null)
    {
        _pkcs15InitPath = FindOpenScTool("pkcs15-init");
        _pkcs15ToolPath = FindOpenScTool("pkcs15-tool");
        _readerNumber = readerNumber;
    }

    /// <summary>
    /// Set logging callback for command output
    /// </summary>
    public void SetLogger(Action<string> logger) => _logger = logger;

    #region Windows Logon Essential Operations

    /// <summary>
    /// Initialize card for Windows logon use.
    /// This erases the card and creates a PKCS#15 structure with MyEID profile.
    /// </summary>
    public async Task InitializeAsync(
        string userPin = "1111",
        string userPuk = "111111",
        string soPin = "12345678",
        string soPuk = "12345678",
        CancellationToken ct = default)
    {
        // Erase card (try without SO-PIN first for blank cards)
        try
        {
            await RunCommandAsync(_pkcs15InitPath, BuildArgs("--erase-card --no-so-pin"), ct);
        }
        catch
        {
            // Card already initialized, needs SO-PIN
            _logger?.Invoke("  Card requires SO-PIN, retrying...");
            await RunCommandAsync(_pkcs15InitPath, BuildArgs($"--erase-card --so-pin {soPin}"), ct);
        }

        // Create PKCS#15 structure
        var args = $"--create-pkcs15 --profile myeid --pin {userPin} --puk {userPuk} --so-pin {soPin} --so-puk {soPuk}";
        await RunCommandAsync(_pkcs15InitPath, BuildArgs(args), ct);

        // Create user PIN context (required before key generation)
        _logger?.Invoke("Creating user PIN context...");
        var pinArgs = $"--store-pin --label \"User PIN\" --auth-id 01 --pin {userPin} --puk {userPuk} --so-pin {soPin}";
        await RunCommandAsync(_pkcs15InitPath, BuildArgs(pinArgs), ct);
    }

    /// <summary>
    /// Generate RSA key pair in authentication slot (9A) for Windows logon.
    /// Windows requires RSA 2048-bit minimum.
    /// </summary>
    public async Task GenerateAuthenticationKeyAsync(
        string pin = "1111",
        int keySize = 2048,
        CancellationToken ct = default)
    {
        if (keySize != 2048 && keySize != 3072 && keySize != 4096)
            throw new ArgumentException("Key size must be 2048, 3072, or 4096 bits for Windows logon", nameof(keySize));

        // Generate in slot 01 (maps to PIV slot 9A - authentication)
        var args = $"--generate-key rsa/{keySize} --auth-id 01 --id 01 --label \"PIV AUTH\" --pin {pin}";
        await RunCommandAsync(_pkcs15InitPath, BuildArgs(args), ct);
    }

    /// <summary>
    /// Store certificate in authentication slot (9A).
    /// Certificate must have Smart Card Logon EKU (1.3.6.1.4.1.311.20.2.2) for Windows.
    /// </summary>
    public async Task StoreCertificateAsync(
        byte[] certificateData,
        string pin = "1111",
        CancellationToken ct = default)
    {
        // Write to temporary file (OpenSC tools require file input)
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempFile, certificateData, ct);
            var args = $"--store-certificate \"{tempFile}\" --id 01 --label \"User Certificate\" --pin {pin}";
            await RunCommandAsync(_pkcs15InitPath, BuildArgs(args), ct);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Read certificate from authentication slot (9A)
    /// </summary>
    public async Task<byte[]> ReadCertificateAsync(CancellationToken ct = default)
    {
        var output = await RunCommandAsync(_pkcs15ToolPath, BuildArgs("--read-certificate 01"), ct);

        // pkcs15-tool outputs DER format directly to stdout
        // Convert the output text to bytes (it's base64 or hex encoded)
        if (string.IsNullOrWhiteSpace(output))
            return Array.Empty<byte>();

        // The tool outputs binary data, so we need to run it differently
        return await ReadCertificateBinaryAsync(ct);
    }

    /// <summary>
    /// List all objects on card (for verification)
    /// </summary>
    public async Task<string> ListObjectsAsync(CancellationToken ct = default)
    {
        return await RunCommandAsync(_pkcs15ToolPath, BuildArgs("--dump"), ct);
    }

    /// <summary>
    /// Verify PIN (test card access)
    /// </summary>
    public async Task<bool> VerifyPinAsync(string pin, CancellationToken ct = default)
    {
        try
        {
            var args = $"--verify-pin --pin {pin}";
            await RunCommandAsync(_pkcs15ToolPath, BuildArgs(args), ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Erase card completely (factory reset)
    /// </summary>
    public async Task EraseAsync(string? soPin = null, CancellationToken ct = default)
    {
        var args = "--erase-card";

        if (string.IsNullOrEmpty(soPin))
        {
            args += " --no-so-pin";
            await RunCommandAsync(_pkcs15InitPath, BuildArgs(args), ct);
        }
        else
        {
            // For initialized cards, auto-respond to SO-PIN prompts
            args += $" --so-pin {soPin}";
            await RunCommandWithPinAsync(_pkcs15InitPath, BuildArgs(args), soPin, ct);
        }
    }

    #endregion

    #region Helper Methods

    private string BuildArgs(string args)
    {
        return _readerNumber.HasValue ? $"{args} --reader {_readerNumber}" : args;
    }

    private async Task<string> RunCommandAsync(string command, string arguments, CancellationToken ct)
    {
        var commandName = Path.GetFileNameWithoutExtension(command);
        _logger?.Invoke($"$ {commandName} {MaskPins(arguments)}");

        using var process = new Process();
        process.StartInfo.FileName = command;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                _logger?.Invoke($"  > {e.Data}");
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                error.AppendLine(e.Data);
                _logger?.Invoke($"  ! {e.Data}");
            }
        };

        process.Start();
        process.StandardInput.Close(); // Close stdin to prevent hanging
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger?.Invoke("  ✗ Command timed out");
            try { process.Kill(); } catch { }
            throw new TimeoutException("OpenSC command timed out after 3 minutes");
        }

        if (process.ExitCode == 0)
        {
            _logger?.Invoke("  ✓ Success");
        }
        else
        {
            _logger?.Invoke($"  ✗ Failed (exit code: {process.ExitCode})");
            var errorMsg = error.ToString().Trim();
            if (!string.IsNullOrEmpty(errorMsg))
                throw new InvalidOperationException($"Command failed: {errorMsg}");
            throw new InvalidOperationException($"Command failed with exit code {process.ExitCode}");
        }

        return output.ToString();
    }

    private async Task<byte[]> ReadCertificateBinaryAsync(CancellationToken ct)
    {
        // For binary output, we need to capture raw bytes
        using var process = new Process();
        process.StartInfo.FileName = _pkcs15ToolPath;
        process.StartInfo.Arguments = BuildArgs("--read-certificate 01");
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        process.StandardInput.Close();

        using var ms = new MemoryStream();
        await process.StandardOutput.BaseStream.CopyToAsync(ms, ct);
        await process.WaitForExitAsync(ct);

        return process.ExitCode == 0 ? ms.ToArray() : Array.Empty<byte>();
    }

    private async Task<string> RunCommandWithPinAsync(string command, string arguments, string pin, CancellationToken ct)
    {
        var commandName = Path.GetFileNameWithoutExtension(command);
        _logger?.Invoke($"$ {commandName} {MaskPins(arguments)}");

        using var process = new Process();
        process.StartInfo.FileName = command;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        var output = new StringBuilder();
        var error = new StringBuilder();
        bool pinSent = false;

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                _logger?.Invoke($"  > {e.Data}");

                // Auto-respond to PIN prompts
                if (!pinSent && e.Data.Contains("PIN") && e.Data.Contains("required"))
                {
                    try
                    {
                        process.StandardInput.WriteLine(pin);
                        process.StandardInput.Flush();
                        pinSent = true;
                        _logger?.Invoke($"  > [PIN provided automatically]");
                    }
                    catch { }
                }
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                error.AppendLine(e.Data);
                _logger?.Invoke($"  ! {e.Data}");

                // Also check error stream for PIN prompts
                if (!pinSent && e.Data.Contains("enter PIN"))
                {
                    try
                    {
                        process.StandardInput.WriteLine(pin);
                        process.StandardInput.Flush();
                        pinSent = true;
                        _logger?.Invoke($"  ! [PIN provided automatically]");
                    }
                    catch { }
                }
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger?.Invoke("  ✗ Command timed out");
            try { process.Kill(); } catch { }
            throw new TimeoutException("OpenSC command timed out after 3 minutes");
        }

        if (process.ExitCode == 0)
        {
            _logger?.Invoke("  ✓ Success");
        }
        else
        {
            _logger?.Invoke($"  ✗ Failed (exit code: {process.ExitCode})");
            var errorMsg = error.ToString().Trim();
            if (!string.IsNullOrEmpty(errorMsg))
                throw new InvalidOperationException($"Command failed: {errorMsg}");
            throw new InvalidOperationException($"Command failed with exit code {process.ExitCode}");
        }

        return output.ToString();
    }

    private static string FindOpenScTool(string toolName)
    {
        var openScPath = @"C:\Program Files\OpenSC Project\OpenSC\tools";

        if (OperatingSystem.IsWindows())
        {
            var exePath = Path.Combine(openScPath, $"{toolName}.exe");
            if (File.Exists(exePath))
                return exePath;
            return $"{toolName}.exe"; // Assume in PATH
        }

        return toolName; // Linux/macOS - assume in PATH
    }

    private static string MaskPins(string args)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            args,
            @"(--(so-)?p(in|uk))\s+\S+",
            "$1 ****",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
