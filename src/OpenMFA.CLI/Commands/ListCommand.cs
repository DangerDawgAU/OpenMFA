using OpenMFA.SmartCard.PcSc;
using OpenMFA.SmartCard.Piv;

namespace OpenMFA.CLI;

public static class ListCommand
{
    public static async Task<int> Execute(string[] args)
    {
        using var context = new PcScContext();
        using var (reader, pivCard) = await CardHelper.ConnectToFirstPivCard(context);

        Console.WriteLine("PIV Card Data Objects");
        Console.WriteLine("====================");
        Console.WriteLine();

        var slots = new[]
        {
            (PivSlot.Authentication, "9A", "PIV Authentication (for login)"),
            (PivSlot.Signature, "9C", "Digital Signature"),
            (PivSlot.KeyManagement, "9D", "Key Management (encryption)"),
            (PivSlot.CardAuthentication, "9E", "Card Authentication (physical access)"),
            (PivSlot.Retired1, "82", "Retired Key 1"),
            (PivSlot.Retired2, "83", "Retired Key 2")
        };

        foreach (var (slot, slotId, description) in slots)
        {
            try
            {
                var data = await pivCard.GetCertificateAsync(slot);

                Console.Write($"[{slotId}] {description,-40} ");

                if (data.Length > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ ({data.Length} bytes)");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("(empty)");
                    Console.ResetColor();
                }
            }
            catch
            {
                Console.Write($"[{slotId}] {description,-40} ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("(empty)");
                Console.ResetColor();
            }
        }

        Console.WriteLine();
        Console.WriteLine("Note: Only certificate slots are shown.");
        Console.WriteLine("Use 'openmfa read <slot>' to view certificate data.");

        return 0;
    }
}
