using System.Diagnostics;
using System.Text;

namespace OpenMFA.SmartCard.OpenSC;

/// <summary>
/// MyEID 4.5 specific operations using OpenSC tools.
/// Provides full functionality for Aventra MyEID PKI cards including:
/// - Card initialization and erasure
/// - PIN/PUK management
/// - Key generation (RSA 512-4096, ECC P-256/P-384/P-521)
/// - Certificate storage and management
/// - Data object storage
/// - Card finalization
/// </summary>
public class MyEidOperations
{
    private readonly string _pkcs15InitPath;
    private readonly string _pkcs15ToolPath;
    private readonly string _openscToolPath;
    private readonly uint? _readerNumber;
    private Action<string>? _commandLogger;

    public MyEidOperations(uint? readerNumber = null)
    {
        _pkcs15InitPath = FindTool("pkcs15-init");
        _pkcs15ToolPath = FindTool("pkcs15-tool");
        _openscToolPath = FindTool("opensc-tool");
        _readerNumber = readerNumber;
    }

    /// <summary>
    /// Set a callback to log commands as they are executed
    /// </summary>
    public void SetCommandLogger(Action<string> logger)
    {
        _commandLogger = logger;
    }

    #region Card Initialization

    /// <summary>
    /// Erase and initialize a MyEID card with PKCS#15 structure
    /// WARNING: This will erase ALL data on the card!
    /// </summary>
    public async Task<bool> InitializeCardAsync(
        string userPin = "1111",
        string userPuk = "1111",
        string soPin = "12345678",
        string soPuk = "12345678",
        CancellationToken ct = default)
    {
        try
        {
            // Try erasing card without SO-PIN first (for uninitialized cards)
            var eraseArgs = $"--erase-card --no-so-pin";
            if (_readerNumber.HasValue)
                eraseArgs += $" --reader {_readerNumber}";

            try
            {
                await RunPkcs15InitAsync(eraseArgs, ct);
            }
            catch (OpenScException ex) when (ex.Message.Contains("Authentication") ||
                                              ex.Message.Contains("security") ||
                                              ex.Message.Contains("PIN"))
            {
                // Card is already initialized and requires SO-PIN for erasure
                _commandLogger?.Invoke("  ! Card requires SO-PIN for erasure, retrying with SO-PIN...");

                var eraseArgsWithPin = $"--erase-card --so-pin {soPin}";
                if (_readerNumber.HasValue)
                    eraseArgsWithPin += $" --reader {_readerNumber}";

                await RunPkcs15InitAsync(eraseArgsWithPin, ct);
            }

            // Create PKCS#15 structure with MyEID profile
            var args = $"--create-pkcs15 --profile myeid --pin {userPin} --puk {userPuk} --so-pin {soPin} --so-puk {soPuk}";
            if (_readerNumber.HasValue)
                args += $" --reader {_readerNumber}";

            await RunPkcs15InitAsync(args, ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Erase the card completely, removing all data and returning to factory state
    /// WARNING: This is irreversible! All keys, certificates, and data will be lost.
    /// Optionally provide SO-PIN if card is already initialized, or use --no-so-pin for blank cards
    /// </summary>
    public async Task<bool> EraseCardAsync(string? soPin = null, CancellationToken ct = default)
    {
        try
        {
            var args = "--erase-card";

            if (_readerNumber.HasValue)
                args += $" --reader {_readerNumber}";

            // If SO-PIN provided, use it for authentication
            if (!string.IsNullOrEmpty(soPin))
            {
                args += $" --so-pin {soPin}";
            }
            else
            {
                // For blank/corrupted cards or when SO-PIN is unknown
                args += " --no-so-pin";
            }

            await RunPkcs15InitAsync(args, ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Finalize card initialization (locks certain operations)
    /// </summary>
    public async Task FinalizeCardAsync(CancellationToken ct = default)
    {
        var args = "--finalize";
        if (_readerNumber.HasValue)
            args += $" --reader {_readerNumber}";

        await RunPkcs15InitAsync(args, ct);
    }

    #endregion

    #region PIN Management

    /// <summary>
    /// Store a new PIN on the card
    /// </summary>
    public async Task StorePinAsync(
        string authId,
        string label,
        string pin,
        string puk,
        string soPin,
        CancellationToken ct = default)
    {
        var args = $"--store-pin --auth-id {authId} --label \"{label}\" --pin {pin} --puk {puk} --so-pin {soPin}";
        if (_readerNumber.HasValue)
            args += $" --reader {_readerNumber}";

        await RunPkcs15InitAsync(args, ct);
    }

    /// <summary>
    /// List all PINs on the card
    /// </summary>
    public async Task<string> ListPinsAsync(CancellationToken ct = default)
    {
        var args = "--list-pins";
        if (_readerNumber.HasValue)
            args += $" --reader {_readerNumber}";

        return await RunPkcs15ToolAsync(args, ct);
    }

    #endregion

    #region Key Generation

    /// <summary>
    /// Generate an RSA key pair on the card
    /// </summary>
    public async Task<string> GenerateRsaKeyAsync(
        int keySize,
        string authId,
        string objectId,
        string label,
        string pin,
        string? outputFile = null,
        CancellationToken ct = default)
    {
        var args = $"--generate-key rsa/{keySize} --auth-id {authId} --id {objectId} --label \"{label}\" --pin {pin}";

        if (!string.IsNullOrEmpty(outputFile))
            args += $" --output-file \"{outputFile}\"";

        if (_readerNumber.HasValue)
            args += $" --reader {_readerNumber}";

        return await RunPkcs15InitAsync(args, ct);
    }

    /// <summary>
    /// Generate an ECC key pair on the card
    /// </summary>
    public async Task<string> GenerateEccKeyAsync(
        string curve, // prime256v1, secp384r1, secp521r1
        string authId,
        string objectId,
        string label,
        string pin,
        string? outputFile = null,
        CancellationToken ct = default)
    {
        var args = $"--generate-key ec/{curve} --auth-id {authId} --id {objectId} --label \"{label}\" --pin {pin}";

        if (!string.IsNullOrEmpty(outputFile))
            args += $" --output-file \"{outputFile}\"";

        if (_readerNumber.HasValue)
            args += $" --reader {_readerNumber}";

        return await RunPkcs15InitAsync(args, ct);
    }

    /// <summary>
    /// List all keys on the card
    /// </summary>
    public async Task<string> ListKeysAsync(CancellationToken ct = default)
    {
        var args = "--list-keys";
        if (_readerNumber.HasValue)
            args += $" --reader {_readerNumber}";

        return await RunPkcs15ToolAsync(args, ct);
    }

    /// <summary>
    /// List public keys on the card
    /// </summary>
    public async Task<string> ListPublicKeysAsync(CancellationToken ct = default)
    {
        var args = "--list-public-keys";
        if (_readerNumber.HasValue)
            args += $" --reader {_readerNumber}";

        return await RunPkcs15ToolAsync(args, ct);
    }

    #endregion

    #region Certificate Management

    /// <summary>
    /// Store a certificate on the card
    /// </summary>
    public async Task StoreCertificateAsync(
        string certFile,
        string objectId,
        string label,
        string pin,
        bool isAuthority = false,
        CancellationToken ct = default)
    {
        var args = $"--store-certificate \"{certFile}\" --id {objectId} --label \"{label}\" --pin {pin}";

        if (isAuthority)
            args += " --authority";

        if (_readerNumber.HasValue)
            args += $" --reader {_readerNumber}";

        await RunPkcs15InitAsync(args, ct);
    }

    /// <summary>
    /// Update an existing certificate on the card
    /// </summary>
    public async Task UpdateCertificateAsync(
        string certFile,
        string objectId,
        string pin,
        CancellationToken ct = default)
    {
        var args = $"--update-certificate \"{certFile}\" --id {objectId} --pin {pin}";

        if (_readerNumber.HasValue)
            args += $" --reader {_readerNumber}";

        await RunPkcs15InitAsync(args, ct);
    }

    /// <summary>
    /// Read a certificate from the card
    /// </summary>
    public async Task<byte[]> ReadCertificateAsync(string objectId, CancellationToken ct = default)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var args = $"--read-certificate {objectId} --output \"{tempFile}\"";
            if (_readerNumber.HasValue)
                args += $" --reader {_readerNumber}";

            await RunPkcs15ToolAsync(args, ct);

            if (File.Exists(tempFile))
                return await File.ReadAllBytesAsync(tempFile, ct);

            return Array.Empty<byte>();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <summary>
    /// List all certificates on the card
    /// </summary>
    public async Task<string> ListCertificatesAsync(CancellationToken ct = default)
    {
        var args = "--list-certificates";
        if (_readerNumber.HasValue)
            args += $" --reader {_readerNumber}";

        return await RunPkcs15ToolAsync(args, ct);
    }

    /// <summary>
    /// Delete a certificate from the card
    /// </summary>
    public async Task DeleteCertificateAsync(string objectId, string pin, CancellationToken ct = default)
    {
        var args = $"--delete-objects cert --id {objectId} --pin {pin}";
        if (_readerNumber.HasValue)
            args += $" --reader {_readerNumber}";

        await RunPkcs15InitAsync(args, ct);
    }

    #endregion

    #region Data Objects

    /// <summary>
    /// Store a data object on the card
    /// </summary>
    public async Task StoreDataObjectAsync(
        string dataFile,
        string applicationName,
        string applicationId,
        string label,
        string pin,
        CancellationToken ct = default)
    {
        var args = $"--store-data \"{dataFile}\" --application-name \"{applicationName}\" --application-id {applicationId} --label \"{label}\" --pin {pin}";

        if (_readerNumber.HasValue)
            args += $" --reader {_readerNumber}";

        await RunPkcs15InitAsync(args, ct);
    }

    /// <summary>
    /// Read a data object from the card
    /// </summary>
    public async Task<byte[]> ReadDataObjectAsync(string label, CancellationToken ct = default)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var args = $"--read-data-object \"{label}\" --output \"{tempFile}\"";
            if (_readerNumber.HasValue)
                args += $" --reader {_readerNumber}";

            await RunPkcs15ToolAsync(args, ct);

            if (File.Exists(tempFile))
                return await File.ReadAllBytesAsync(tempFile, ct);

            return Array.Empty<byte>();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <summary>
    /// List all data objects on the card
    /// </summary>
    public async Task<string> ListDataObjectsAsync(CancellationToken ct = default)
    {
        var args = "--list-data-objects";
        if (_readerNumber.HasValue)
            args += $" --reader {_readerNumber}";

        return await RunPkcs15ToolAsync(args, ct);
    }

    #endregion

    #region Card Information

    /// <summary>
    /// Get card information
    /// </summary>
    public async Task<string> GetCardInfoAsync(CancellationToken ct = default)
    {
        var args = "--list-info";
        if (_readerNumber.HasValue)
            args += $" --reader {_readerNumber}";

        return await RunPkcs15ToolAsync(args, ct);
    }

    /// <summary>
    /// Dump all card objects
    /// </summary>
    public async Task<string> DumpCardAsync(CancellationToken ct = default)
    {
        var args = "--dump";
        if (_readerNumber.HasValue)
            args += $" --reader {_readerNumber}";

        return await RunPkcs15ToolAsync(args, ct);
    }

    /// <summary>
    /// Get card serial number
    /// </summary>
    public async Task<string> GetSerialNumberAsync(CancellationToken ct = default)
    {
        var args = "--serial";
        if (_readerNumber.HasValue)
            args += $" -r {_readerNumber}";

        return (await RunOpenScToolAsync(args, ct)).Trim();
    }

    /// <summary>
    /// Get card name
    /// </summary>
    public async Task<string> GetCardNameAsync(CancellationToken ct = default)
    {
        var args = "--name";
        if (_readerNumber.HasValue)
            args += $" -r {_readerNumber}";

        return (await RunOpenScToolAsync(args, ct)).Trim();
    }

    #endregion

    #region Helper Methods

    private async Task<string> RunPkcs15InitAsync(string arguments, CancellationToken ct)
    {
        return await RunCommandAsync(_pkcs15InitPath, arguments, ct);
    }

    private async Task<string> RunPkcs15ToolAsync(string arguments, CancellationToken ct)
    {
        return await RunCommandAsync(_pkcs15ToolPath, arguments, ct);
    }

    private async Task<string> RunOpenScToolAsync(string arguments, CancellationToken ct)
    {
        return await RunCommandAsync(_openscToolPath, arguments, ct);
    }

    private async Task<string> RunCommandAsync(string command, string arguments, CancellationToken ct)
    {
        // Log the command being executed (mask PIN values for security)
        var commandName = Path.GetFileNameWithoutExtension(command);
        var maskedArgs = MaskSensitiveData(arguments);
        _commandLogger?.Invoke($"$ {commandName} {maskedArgs}");

        using var process = new Process();
        process.StartInfo.FileName = command;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                // Log output in real-time for debugging
                _commandLogger?.Invoke($"  > {e.Data}");
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                error.AppendLine(e.Data);
                // Log errors in real-time
                _commandLogger?.Invoke($"  ! {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Add timeout handling (2 minutes default)
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            // Timeout occurred
            _commandLogger?.Invoke($"  ✗ Command timed out after 2 minutes");
            try { process.Kill(); } catch { }
            throw new OpenScException($"OpenSC command timed out after 2 minutes");
        }

        // Log command completion
        if (process.ExitCode == 0)
        {
            _commandLogger?.Invoke($"  ✓ Command completed (exit code: 0)");
        }
        else
        {
            _commandLogger?.Invoke($"  ✗ Command failed (exit code: {process.ExitCode})");
        }

        if (process.ExitCode != 0)
        {
            var errorMsg = error.ToString();
            if (!string.IsNullOrWhiteSpace(errorMsg))
                throw new OpenScException($"OpenSC command failed with exit code {process.ExitCode}: {errorMsg}");
        }

        return output.ToString();
    }

    private string FindTool(string toolName)
    {
        if (OperatingSystem.IsWindows())
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var possiblePaths = new[]
            {
                Path.Combine(programFiles, "OpenSC Project", "OpenSC", "tools", $"{toolName}.exe"),
                Path.Combine(programFiles, "OpenSC", "bin", $"{toolName}.exe"),
                $"{toolName}.exe"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }

            return $"{toolName}.exe"; // Assume in PATH
        }

        return toolName; // On Linux/macOS, assume in PATH
    }

    /// <summary>
    /// Mask sensitive data (PINs, PUKs) in command arguments for logging
    /// </summary>
    private string MaskSensitiveData(string arguments)
    {
        var result = arguments;

        // Mask PIN values (--pin, --so-pin, --puk, --so-puk)
        result = System.Text.RegularExpressions.Regex.Replace(
            result,
            @"(--(so-)?pin|--((so-)?puk))\s+\S+",
            "$1 ****",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return result;
    }

    #endregion
}
