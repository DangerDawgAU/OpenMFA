using OpenMFA.SmartCard.PcSc;
using OpenMFA.SmartCard.Piv;

namespace OpenMFA.CLI;

public static class InfoCommand
{
    public static async Task<int> Execute(string[] args)
    {
        using var context = new PcScContext();
        using var (reader, pivCard) = await CardHelper.ConnectToFirstPivCard(context);

        Console.WriteLine("PIV Card Information");
        Console.WriteLine("===================");
        Console.WriteLine();
        Console.WriteLine($"Reader: {reader.ReaderName}");
        Console.WriteLine($"Protocol: T={(reader.ActiveProtocol == 1 ? "0" : "1")}");
        Console.WriteLine();

        // Check which slots have certificates
        Console.WriteLine("Certificate Slots:");
        await CheckSlot(pivCard, PivSlot.Authentication, "PIV Authentication (9A)");
        await CheckSlot(pivCard, PivSlot.Signature, "Digital Signature (9C)");
        await CheckSlot(pivCard, PivSlot.KeyManagement, "Key Management (9D)");
        await CheckSlot(pivCard, PivSlot.CardAuthentication, "Card Authentication (9E)");

        return 0;
    }

    private static async Task CheckSlot(IPivCard pivCard, PivSlot slot, string description)
    {
        try
        {
            var cert = await pivCard.GetCertificateAsync(slot);

            if (cert.Length > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✓ {description}");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"    Size: {cert.Length} bytes");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  - {description} (empty)");
                Console.ResetColor();
            }
        }
        catch
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  - {description} (empty)");
            Console.ResetColor();
        }
    }
}
