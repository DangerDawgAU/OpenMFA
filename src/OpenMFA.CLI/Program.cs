using OpenMFA.SmartCard.OpenSC;
using OpenMFA.SmartCard.Piv;

namespace OpenMFA.CLI;

class ProgramOpenSc
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                ShowHelp();
                return 0;
            }

            var command = args[0].ToLowerInvariant();

            return command switch
            {
                "detect" => await DetectCommand(args[1..]),
                "info" => await InfoCommand(args[1..]),
                "read" => await ReadCommand(args[1..]),
                "write" => await WriteCommand(args[1..]),
                "delete" => await DeleteCommand(args[1..]),
                "generate-key" => await GenerateKeyCommand(args[1..]),
                "list" => await ListCommand(args[1..]),
                "help" or "--help" or "-h" => ShowHelp(),
                _ => ShowUnknownCommand(command)
            };
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            if (ex.InnerException != null)
            {
                Console.WriteLine($"  Details: {ex.InnerException.Message}");
            }
            return 1;
        }
    }

    static async Task<int> DetectCommand(string[] args)
    {
        var context = new OpenScContext();

        Console.WriteLine("--- Card Readers ---");
        var readers = await context.ListReadersAsync();
        if (readers.Any())
        {
            foreach (var reader in readers)
            {
                Console.WriteLine($"Reader: {reader}");
            }
        }
        else
        {
            Console.WriteLine("No card readers found.");
        }

        Console.WriteLine("\n--- Available Slots ---");
        var slots = await context.GetSlotsAsync();
        if (slots.Any())
        {
            foreach (var slot in slots)
            {
                Console.WriteLine($"Slot {slot.SlotId}: {slot.TokenLabel}");
                if (slot.TokenPresent)
                {
                    Console.WriteLine($"  Token present: Yes");
                }
            }
        }
        else
        {
            Console.WriteLine("No slots found.");
        }

        return 0;
    }

    static async Task<int> InfoCommand(string[] args)
    {
        var context = new OpenScContext();
        var slot = await context.GetFirstSlotWithTokenAsync();

        if (slot == null)
        {
            Console.WriteLine("No card found. Please insert a PIV card.");
            return 1;
        }

        Console.WriteLine("--- Card Information ---");
        Console.WriteLine($"Slot ID: {slot.SlotId}");
        Console.WriteLine($"Token Label: {slot.TokenLabel}");
        Console.WriteLine($"Token Present: {slot.TokenPresent}");

        return 0;
    }

    static async Task<int> ReadCommand(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Error: Slot parameter required.");
            Console.WriteLine("Usage: openmfa read <slot> [output-file]");
            return 1;
        }

        var slotHex = args[0];
        if (!byte.TryParse(slotHex, System.Globalization.NumberStyles.HexNumber, null, out var slotByte))
        {
            Console.WriteLine($"Error: Invalid slot '{slotHex}'. Must be hex (9A, 9C, 9D, 9E).");
            return 1;
        }

        var slot = (PivSlot)slotByte;
        var outputFile = args.Length > 1 ? args[1] : $"cert_{slotHex}.der";

        var context = new OpenScContext();
        var slotInfo = await context.GetFirstSlotWithTokenAsync();

        if (slotInfo == null)
        {
            Console.WriteLine("No card found.");
            return 1;
        }

        using var pivCard = new PivCardOpenSc(slotInfo.SlotId);
        var certData = await pivCard.GetCertificateAsync(slot);

        if (certData == null || certData.Length == 0)
        {
            Console.WriteLine($"No certificate found in slot {slotHex}.");
            return 1;
        }

        await File.WriteAllBytesAsync(outputFile, certData);
        Console.WriteLine($"Certificate saved to: {outputFile}");
        Console.WriteLine($"Size: {certData.Length} bytes");

        return 0;
    }

    static async Task<int> WriteCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Slot and file parameters required.");
            Console.WriteLine("Usage: openmfa write <slot> <cert-file>");
            return 1;
        }

        var slotHex = args[0];
        if (!byte.TryParse(slotHex, System.Globalization.NumberStyles.HexNumber, null, out var slotByte))
        {
            Console.WriteLine($"Error: Invalid slot '{slotHex}'. Must be hex (9A, 9C, 9D, 9E).");
            return 1;
        }

        var slot = (PivSlot)slotByte;
        var certFile = args[1];

        if (!File.Exists(certFile))
        {
            Console.WriteLine($"Error: File not found: {certFile}");
            return 1;
        }

        var certData = await File.ReadAllBytesAsync(certFile);
        Console.WriteLine($"Certificate file size: {certData.Length} bytes");

        var context = new OpenScContext();
        var slotInfo = await context.GetFirstSlotWithTokenAsync();

        if (slotInfo == null)
        {
            Console.WriteLine("No card found.");
            return 1;
        }

        using var pivCard = new PivCardOpenSc(slotInfo.SlotId);
        await pivCard.PutCertificateAsync(slot, certData);

        Console.WriteLine($"Certificate written successfully to slot {slotHex}.");
        return 0;
    }

    static async Task<int> DeleteCommand(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Error: Slot parameter required.");
            Console.WriteLine("Usage: openmfa delete <slot>");
            return 1;
        }

        var slotHex = args[0];
        if (!byte.TryParse(slotHex, System.Globalization.NumberStyles.HexNumber, null, out var slotByte))
        {
            Console.WriteLine($"Error: Invalid slot '{slotHex}'. Must be hex (9A, 9C, 9D, 9E).");
            return 1;
        }

        var slot = (PivSlot)slotByte;

        var context = new OpenScContext();
        var slotInfo = await context.GetFirstSlotWithTokenAsync();

        if (slotInfo == null)
        {
            Console.WriteLine("No card found.");
            return 1;
        }

        using var pivCard = new PivCardOpenSc(slotInfo.SlotId);
        await pivCard.DeleteCertificateAsync(slot);

        Console.WriteLine($"Certificate deleted from slot {slotHex}.");
        return 0;
    }

    static async Task<int> GenerateKeyCommand(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Error: Slot parameter required.");
            Console.WriteLine("Usage: openmfa generate-key <slot> [algorithm]");
            return 1;
        }

        var slotHex = args[0];
        if (!byte.TryParse(slotHex, System.Globalization.NumberStyles.HexNumber, null, out var slotByte))
        {
            Console.WriteLine($"Error: Invalid slot '{slotHex}'. Must be hex (9A, 9C, 9D, 9E).");
            return 1;
        }

        var slot = (PivSlot)slotByte;
        var algorithm = PivAlgorithm.Rsa2048; // Default

        if (args.Length > 1)
        {
            algorithm = args[1].ToUpperInvariant() switch
            {
                "RSA1024" => PivAlgorithm.Rsa1024,
                "RSA2048" => PivAlgorithm.Rsa2048,
                "ECCP256" => PivAlgorithm.EccP256,
                "ECCP384" => PivAlgorithm.EccP384,
                _ => throw new ArgumentException($"Unknown algorithm: {args[1]}")
            };
        }

        Console.WriteLine($"Generating {algorithm} key pair in slot {slotHex}...");
        Console.WriteLine("This may take a few moments...");

        var context = new OpenScContext();
        var slotInfo = await context.GetFirstSlotWithTokenAsync();

        if (slotInfo == null)
        {
            Console.WriteLine("No card found.");
            return 1;
        }

        using var pivCard = new PivCardOpenSc(slotInfo.SlotId);
        var publicKey = await pivCard.GenerateKeyPairAsync(slot, algorithm);

        if (publicKey != null && publicKey.Length > 0)
        {
            Console.WriteLine("Key pair generated successfully!");
            Console.WriteLine($"Public key size: {publicKey.Length} bytes");
        }
        else
        {
            Console.WriteLine("Key generation completed.");
        }

        return 0;
    }

    static async Task<int> ListCommand(string[] args)
    {
        var context = new OpenScContext();
        var slotInfo = await context.GetFirstSlotWithTokenAsync();

        if (slotInfo == null)
        {
            Console.WriteLine("No card found.");
            return 1;
        }

        using var pivCard = new PivCardOpenSc(slotInfo.SlotId);

        Console.WriteLine("--- Certificates on Card ---");
        var slots = new[] { PivSlot.Authentication, PivSlot.Signature, PivSlot.KeyManagement, PivSlot.CardAuthentication };

        foreach (var slot in slots)
        {
            try
            {
                var certData = await pivCard.GetCertificateAsync(slot);
                if (certData != null && certData.Length > 0)
                {
                    Console.WriteLine($"Slot {slot:X}: Certificate found ({certData.Length} bytes)");
                }
                else
                {
                    Console.WriteLine($"Slot {slot:X}: No certificate");
                }
            }
            catch
            {
                Console.WriteLine($"Slot {slot:X}: No certificate");
            }
        }

        return 0;
    }

    static int ShowHelp()
    {
        Console.WriteLine("OpenMFA - PIV Smart Card Operations (using OpenSC)");
        Console.WriteLine();
        Console.WriteLine("Usage: openmfa <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  detect              Detect card readers and PIV cards");
        Console.WriteLine("  info                Show card information");
        Console.WriteLine("  read <slot>         Read certificate from card slot");
        Console.WriteLine("  write <slot> <file> Write certificate to card slot");
        Console.WriteLine("  delete <slot>       Delete certificate from card slot");
        Console.WriteLine("  generate-key <slot> [algo] Generate key pair on card");
        Console.WriteLine("  list                List all certificates on card");
        Console.WriteLine("  help                Show this help message");
        Console.WriteLine();
        Console.WriteLine("Slots:");
        Console.WriteLine("  9A  PIV Authentication (Windows/Linux login)");
        Console.WriteLine("  9C  Digital Signature");
        Console.WriteLine("  9D  Key Management");
        Console.WriteLine("  9E  Card Authentication");
        Console.WriteLine();
        Console.WriteLine("Algorithms:");
        Console.WriteLine("  RSA1024   RSA 1024-bit key");
        Console.WriteLine("  RSA2048   RSA 2048-bit key (default)");
        Console.WriteLine("  ECCP256   ECC P-256");
        Console.WriteLine("  ECCP384   ECC P-384");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  openmfa detect");
        Console.WriteLine("  openmfa generate-key 9A RSA2048");
        Console.WriteLine("  openmfa write 9A cert.der");
        Console.WriteLine("  openmfa read 9A output.der");
        Console.WriteLine("  openmfa delete 9A");
        Console.WriteLine();
        Console.WriteLine("Requirements:");
        Console.WriteLine("  - OpenSC must be installed");
        Console.WriteLine("  - PC/SC daemon must be running");
        Console.WriteLine("  - PIV-compatible smart card");
        return 0;
    }

    static int ShowUnknownCommand(string command)
    {
        Console.WriteLine($"Unknown command: {command}");
        Console.WriteLine("Run 'openmfa help' for usage information.");
        return 1;
    }
}
