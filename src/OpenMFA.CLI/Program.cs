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
                "detect" => await DetectCommandOpenSc.Execute(args[1..]),
                "info" => await InfoCommandOpenSc.Execute(args[1..]),
                "read" => await ReadCommandOpenSc.Execute(args[1..]),
                "write" => await WriteCommandOpenSc.Execute(args[1..]),
                "delete" => await DeleteCommandOpenSc.Execute(args[1..]),
                "generate-key" => await GenerateKeyCommandOpenSc.Execute(args[1..]),
                "list" => await ListCommandOpenSc.Execute(args[1..]),
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
