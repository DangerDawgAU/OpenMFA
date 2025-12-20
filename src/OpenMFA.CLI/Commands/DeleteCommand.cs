using OpenMFA.SmartCard.PcSc;
using OpenMFA.SmartCard.Piv;

namespace OpenMFA.CLI;

public static class DeleteCommand
{
    public static async Task<int> Execute(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: openmfa delete <slot>");
            Console.WriteLine("Example: openmfa delete 9A");
            return 1;
        }

        var slotStr = args[0].ToUpperInvariant();

        if (!CardHelper.TryParseSlot(slotStr, out var slot))
        {
            Console.WriteLine($"Invalid slot: {slotStr}");
            Console.WriteLine("Valid slots: 9A, 9C, 9D, 9E");
            return 1;
        }

        Console.WriteLine($"Deleting data from slot {slotStr}...");

        using var context = new PcScContext();
        using var (reader, pivCard) = await CardHelper.ConnectToFirstPivCard(context);

        await pivCard.DeleteCertificateAsync(slot);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ Successfully deleted data from slot {slotStr}");
        Console.ResetColor();

        return 0;
    }
}
