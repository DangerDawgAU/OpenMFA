using System.Diagnostics;
using System.Text;

namespace OpenMFA.SmartCard.OpenSC;

/// <summary>
/// Wrapper for OpenSC command-line tools
/// Provides a simple interface to pkcs11-tool, pkcs15-tool, and opensc-tool
/// </summary>
public class OpenScTools
{
    private readonly string _pkcs11ToolPath;
    private readonly string _pkcs15ToolPath;
    private readonly string _pkcs15InitPath;
    private readonly string _openscToolPath;

    public OpenScTools()
    {
        _pkcs11ToolPath = FindTool("pkcs11-tool");
        _pkcs15ToolPath = FindTool("pkcs15-tool");
        _pkcs15InitPath = FindTool("pkcs15-init");
        _openscToolPath = FindTool("opensc-tool");
    }

    /// <summary>
    /// List all card readers
    /// </summary>
    public async Task<List<string>> ListReadersAsync(CancellationToken ct = default)
    {
        var output = await RunCommandAsync(_openscToolPath, "--list-readers", ct);
        var readers = new List<string>();

        foreach (var line in output.Split('\n'))
        {
            if (line.Contains("Reader") && !line.Contains("Slot"))
            {
                var parts = line.Split(':');
                if (parts.Length > 1)
                {
                    readers.Add(parts[1].Trim());
                }
            }
        }

        return readers;
    }

    /// <summary>
    /// List PKCS#11 slots
    /// </summary>
    public async Task<List<Pkcs11SlotInfo>> ListSlotsAsync(CancellationToken ct = default)
    {
        var output = await RunCommandAsync(_pkcs11ToolPath, "--list-slots", ct);
        var slots = new List<Pkcs11SlotInfo>();

        Pkcs11SlotInfo? currentSlot = null;

        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("Slot "))
            {
                if (currentSlot != null)
                    slots.Add(currentSlot);

                currentSlot = new Pkcs11SlotInfo();
                var parts = trimmed.Split(' ');
                if (parts.Length > 1 && uint.TryParse(parts[1], out var slotId))
                {
                    currentSlot.SlotId = slotId;
                }
            }
            else if (currentSlot != null)
            {
                if (trimmed.Contains("token label"))
                {
                    var parts = trimmed.Split(':');
                    if (parts.Length > 1)
                    {
                        currentSlot.TokenLabel = parts[1].Trim();
                        // If we have a token label, a token is present
                        currentSlot.TokenPresent = !string.IsNullOrWhiteSpace(currentSlot.TokenLabel);
                    }
                }
            }
        }

        if (currentSlot != null)
            slots.Add(currentSlot);

        return slots;
    }

    /// <summary>
    /// List all objects on the card
    /// </summary>
    public async Task<List<Pkcs11Object>> ListObjectsAsync(uint? slotId = null, string? pin = null, CancellationToken ct = default)
    {
        var args = new List<string> { "--list-objects" };

        if (slotId.HasValue)
            args.AddRange(new[] { "--slot", slotId.Value.ToString() });

        if (!string.IsNullOrEmpty(pin))
            args.AddRange(new[] { "--login", "--pin", pin });

        var output = await RunCommandAsync(_pkcs11ToolPath, string.Join(" ", args), ct);
        var objects = new List<Pkcs11Object>();

        Pkcs11Object? currentObj = null;

        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("Certificate Object") || trimmed.StartsWith("Data object") ||
                trimmed.StartsWith("Public Key Object") || trimmed.StartsWith("Private Key Object"))
            {
                if (currentObj != null)
                    objects.Add(currentObj);

                currentObj = new Pkcs11Object
                {
                    Type = trimmed.Split(' ')[0] + " " + trimmed.Split(' ')[1]
                };
            }
            else if (currentObj != null)
            {
                if (trimmed.StartsWith("label:"))
                    currentObj.Label = trimmed.Substring(6).Trim();
                else if (trimmed.StartsWith("ID:"))
                    currentObj.Id = trimmed.Substring(3).Trim();
            }
        }

        if (currentObj != null)
            objects.Add(currentObj);

        return objects;
    }

    /// <summary>
    /// Read a certificate from the card
    /// </summary>
    public async Task<byte[]> ReadCertificateAsync(string objectId, uint? slotId = null, CancellationToken ct = default)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var args = new List<string>
            {
                "--read-object",
                "--type", "cert",
                "--id", objectId,
                "--output-file", tempFile
            };

            if (slotId.HasValue)
                args.AddRange(new[] { "--slot", slotId.Value.ToString() });

            await RunCommandAsync(_pkcs11ToolPath, string.Join(" ", args), ct);

            if (File.Exists(tempFile))
            {
                return await File.ReadAllBytesAsync(tempFile, ct);
            }

            return Array.Empty<byte>();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Write a certificate to the card
    /// </summary>
    public async Task WriteCertificateAsync(string objectId, byte[] certificateData, string? label = null, uint? slotId = null, string? pin = null, CancellationToken ct = default)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempFile, certificateData, ct);

            var args = new List<string>
            {
                "--write-object", tempFile,
                "--type", "cert",
                "--id", objectId
            };

            if (!string.IsNullOrEmpty(label))
                args.AddRange(new[] { "--label", label });

            if (slotId.HasValue)
                args.AddRange(new[] { "--slot", slotId.Value.ToString() });

            if (!string.IsNullOrEmpty(pin))
                args.AddRange(new[] { "--login", "--pin", pin });

            await RunCommandAsync(_pkcs11ToolPath, string.Join(" ", args), ct);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Delete an object from the card (tries pkcs15-init first for PKCS#15 cards like MyEID, falls back to pkcs11-tool)
    /// </summary>
    public async Task DeleteObjectAsync(string objectId, string type = "cert", uint? slotId = null, string? pin = null, CancellationToken ct = default)
    {
        // Try pkcs15-init first (for MyEID and other PKCS#15 cards)
        try
        {
            await DeleteObjectPkcs15Async(objectId, type, slotId, pin, ct);
            return;
        }
        catch (OpenScException ex) when (ex.Message.Contains("not supported") || ex.Message.Contains("not found"))
        {
            // Fall through to try pkcs11-tool
        }

        // Fallback to pkcs11-tool (for PIV cards like YubiKey)
        var args = new List<string>
        {
            "--delete-object",
            "--type", type,
            "--id", objectId
        };

        if (slotId.HasValue)
            args.AddRange(new[] { "--slot", slotId.Value.ToString() });

        if (!string.IsNullOrEmpty(pin))
            args.AddRange(new[] { "--login", "--pin", pin });

        await RunCommandAsync(_pkcs11ToolPath, string.Join(" ", args), ct);
    }

    /// <summary>
    /// Delete object using pkcs15-init (for PKCS#15 cards like MyEID)
    /// </summary>
    private async Task DeleteObjectPkcs15Async(string objectId, string type, uint? slotId = null, string? pin = null, CancellationToken ct = default)
    {
        var args = new List<string>
        {
            "--delete-objects", type,
            "--id", objectId
        };

        if (slotId.HasValue)
            args.AddRange(new[] { "--reader", slotId.Value.ToString() });

        if (!string.IsNullOrEmpty(pin))
            args.AddRange(new[] { "--pin", pin });

        await RunCommandAsync(_pkcs15InitPath, string.Join(" ", args), ct);
    }

    /// <summary>
    /// Generate a key pair on the card (tries pkcs15-init first for MyEID, falls back to pkcs11-tool)
    /// </summary>
    public async Task<string> GenerateKeyPairAsync(string objectId, string keyType = "RSA:2048", string? label = null, uint? slotId = null, string? pin = null, CancellationToken ct = default)
    {
        // Try pkcs15-init first (for MyEID and other PKCS#15 cards)
        try
        {
            return await GenerateKeyPairPkcs15Async(objectId, keyType, label, slotId, pin, ct);
        }
        catch (OpenScException ex) when (ex.Message.Contains("not supported") || ex.Message.Contains("not found"))
        {
            // Fall through to try pkcs11-tool
        }

        // Fallback to pkcs11-tool (for PIV cards)
        var args = new List<string>
        {
            "--keypairgen",
            "--key-type", keyType,
            "--id", objectId
        };

        if (!string.IsNullOrEmpty(label))
            args.AddRange(new[] { "--label", label });

        if (slotId.HasValue)
            args.AddRange(new[] { "--slot", slotId.Value.ToString() });

        if (!string.IsNullOrEmpty(pin))
            args.AddRange(new[] { "--login", "--pin", pin });

        return await RunCommandAsync(_pkcs11ToolPath, string.Join(" ", args), ct);
    }

    /// <summary>
    /// Generate key pair using pkcs15-init (for PKCS#15 cards like MyEID)
    /// </summary>
    private async Task<string> GenerateKeyPairPkcs15Async(string objectId, string keyType, string? label = null, uint? slotId = null, string? pin = null, CancellationToken ct = default)
    {
        // Convert keyType format from "RSA:2048" to "rsa/2048" for pkcs15-init
        var keyTypeConverted = keyType.ToLowerInvariant().Replace(":", "/");

        var args = new List<string>
        {
            "--generate-key", keyTypeConverted,
            "--auth-id", objectId,
            "--id", objectId
        };

        if (!string.IsNullOrEmpty(label))
            args.AddRange(new[] { "--label", label });

        if (slotId.HasValue)
            args.AddRange(new[] { "--reader", slotId.Value.ToString() });

        if (!string.IsNullOrEmpty(pin))
            args.AddRange(new[] { "--pin", pin });

        return await RunCommandAsync(_pkcs15InitPath, string.Join(" ", args), ct);
    }

    private async Task<string> RunCommandAsync(string command, string arguments, CancellationToken ct)
    {
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
                output.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            throw new OpenScException($"OpenSC tool failed with exit code {process.ExitCode}: {error}");
        }

        return output.ToString();
    }

    private string FindTool(string toolName)
    {
        // On Linux/macOS, tools are typically in PATH
        // On Windows, might need full path

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
}

public class Pkcs11SlotInfo
{
    public uint SlotId { get; set; }
    public string TokenLabel { get; set; } = string.Empty;
    public bool TokenPresent { get; set; }
}

public class Pkcs11Object
{
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
}

public class OpenScException : Exception
{
    public OpenScException(string message) : base(message) { }
    public OpenScException(string message, Exception innerException) : base(message, innerException) { }
}
