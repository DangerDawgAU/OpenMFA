using OpenMFA.SmartCard.PcSc;
using OpenMFA.SmartCard.Piv;

namespace OpenMFA.CLI;

public static class WriteCommand
{
    public static async Task<int> Execute(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: openmfa write <slot> <file>");
            Console.WriteLine("Example: openmfa write 9A cert.der");
            return 1;
        }

        var slotStr = args[0].ToUpperInvariant();
        var inputFile = args[1];

        if (!CardHelper.TryParseSlot(slotStr, out var slot))
        {
            Console.WriteLine($"Invalid slot: {slotStr}");
            Console.WriteLine("Valid slots: 9A, 9C, 9D, 9E");
            return 1;
        }

        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"File not found: {inputFile}");
            return 1;
        }

        var data = await File.ReadAllBytesAsync(inputFile);

        Console.WriteLine($"Writing {data.Length} bytes to slot {slotStr}...");

        using var context = new PcScContext();
        using var (reader, pivCard) = await CardHelper.ConnectToFirstPivCard(context);

        // Note: Writing to card may require PIN verification
        // For now, attempt direct write
        await pivCard.PutCertificateAsync(slot, data);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ Successfully wrote {data.Length} bytes to slot {slotStr}");
        Console.ResetColor();

        return 0;
    }
}
