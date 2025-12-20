using OpenMFA.SmartCard.PcSc;
using OpenMFA.SmartCard.Piv;

namespace OpenMFA.CLI;

class Program
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
                "detect" => await DetectCommand.Execute(args[1..]),
                "info" => await InfoCommand.Execute(args[1..]),
                "read" => await ReadCommand.Execute(args[1..]),
                "write" => await WriteCommand.Execute(args[1..]),
                "delete" => await DeleteCommand.Execute(args[1..]),
                "generate-key" => await GenerateKeyCommand.Execute(args[1..]),
                "list" => await ListCommand.Execute(args[1..]),
                "help" or "--help" or "-h" => ShowHelp(),
                _ => ShowUnknownCommand(command)
            };
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    static int ShowHelp()
    {
        Console.WriteLine("OpenMFA - PIV Smart Card Operations");
        Console.WriteLine();
        Console.WriteLine("Usage: openmfa <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  detect              Detect card readers and cards");
        Console.WriteLine("  info                Show card information");
        Console.WriteLine("  read <slot>         Read data from card slot");
        Console.WriteLine("  write <slot> <file> Write data to card slot");
        Console.WriteLine("  delete <slot>       Delete data from card slot");
        Console.WriteLine("  generate-key <slot> [algo] Generate key pair on card");
        Console.WriteLine("  list                List all data objects on card");
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
        Console.WriteLine("  openmfa read 9A");
        Console.WriteLine("  openmfa delete 9A");
        return 0;
    }

    static int ShowUnknownCommand(string command)
    {
        Console.WriteLine($"Unknown command: {command}");
        Console.WriteLine("Run 'openmfa help' for usage information.");
        return 1;
    }
}
