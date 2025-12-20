using OpenMFA.SmartCard.PcSc;
using OpenMFA.SmartCard.Piv;

namespace OpenMFA.CLI;

public static class ReadCommand
{
    public static async Task<int> Execute(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: openmfa read <slot> [output-file]");
            Console.WriteLine("Example: openmfa read 9A cert.der");
            return 1;
        }

        var slotStr = args[0].ToUpperInvariant();
        var outputFile = args.Length > 1 ? args[1] : null;

        if (!CardHelper.TryParseSlot(slotStr, out var slot))
        {
            Console.WriteLine($"Invalid slot: {slotStr}");
            Console.WriteLine("Valid slots: 9A, 9C, 9D, 9E");
            return 1;
        }

        using var context = new PcScContext();
        using var (reader, pivCard) = await CardHelper.ConnectToFirstPivCard(context);

        Console.WriteLine($"Reading from slot {slotStr}...");

        var data = await pivCard.GetCertificateAsync(slot);

        if (data.Length == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Slot is empty (no certificate found)");
            Console.ResetColor();
            return 1;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ Read {data.Length} bytes from slot {slotStr}");
        Console.ResetColor();

        if (outputFile != null)
        {
            await File.WriteAllBytesAsync(outputFile, data);
            Console.WriteLine($"✓ Saved to {outputFile}");
        }
        else
        {
            // Display hex dump
            Console.WriteLine();
            Console.WriteLine("Data (hex):");
            DisplayHexDump(data);
        }

        return 0;
    }

    private static void DisplayHexDump(byte[] data)
    {
        const int bytesPerLine = 16;

        for (int i = 0; i < data.Length; i += bytesPerLine)
        {
            Console.Write($"{i:X8}  ");

            // Hex bytes
            for (int j = 0; j < bytesPerLine; j++)
            {
                if (i + j < data.Length)
                {
                    Console.Write($"{data[i + j]:X2} ");
                }
                else
                {
                    Console.Write("   ");
                }

                if (j == 7)
                    Console.Write(" ");
            }

            Console.Write(" ");

            // ASCII representation
            for (int j = 0; j < bytesPerLine && i + j < data.Length; j++)
            {
                var b = data[i + j];
                Console.Write(b >= 32 && b <= 126 ? (char)b : '.');
            }

            Console.WriteLine();
        }
    }
}
