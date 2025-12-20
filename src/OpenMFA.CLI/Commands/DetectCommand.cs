using OpenMFA.SmartCard.PcSc;
using OpenMFA.SmartCard.Piv;
using OpenMFA.SmartCard.Piv.Apdu;

namespace OpenMFA.CLI;

public static class DetectCommand
{
    public static async Task<int> Execute(string[] args)
    {
        Console.WriteLine("Detecting smart card readers...");
        Console.WriteLine();

        using var context = new PcScContext();
        var readers = await context.ListReadersAsync();

        if (readers.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No card readers found.");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Possible causes:");
            Console.WriteLine("  - No card reader connected");
            Console.WriteLine("  - PC/SC service not running");
            Console.WriteLine("    Linux: sudo systemctl start pcscd");
            Console.WriteLine("    Windows: Smart Card service should start automatically");
            return 1;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Found {readers.Count} reader(s):");
        Console.ResetColor();
        Console.WriteLine();

        for (int i = 0; i < readers.Count; i++)
        {
            Console.WriteLine($"[{i + 1}] {readers[i]}");

            try
            {
                using var reader = await context.ConnectAsync(readers[i]);
                using var pivCard = new PivCard(reader);

                var selected = await pivCard.SelectPivAppletAsync();

                if (selected)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("    ✓ PIV card detected");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("    ⚠ Card present but not a PIV card");
                    Console.ResetColor();
                }
            }
            catch (PcScException ex) when (ex.Message.Contains("No smart card"))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("    (No card inserted)");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"    ✗ Error: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        return 0;
    }
}
