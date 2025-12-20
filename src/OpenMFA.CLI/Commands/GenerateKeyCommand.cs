using OpenMFA.SmartCard.PcSc;
using OpenMFA.SmartCard.Piv;

namespace OpenMFA.CLI;

public static class GenerateKeyCommand
{
    public static async Task<int> Execute(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: openmfa generate-key <slot> [algorithm]");
            Console.WriteLine("Example: openmfa generate-key 9A RSA2048");
            Console.WriteLine();
            Console.WriteLine("Algorithms: RSA1024, RSA2048 (default), ECCP256, ECCP384");
            return 1;
        }

        var slotStr = args[0].ToUpperInvariant();
        var algoStr = args.Length > 1 ? args[1] : "RSA2048";

        if (!CardHelper.TryParseSlot(slotStr, out var slot))
        {
            Console.WriteLine($"Invalid slot: {slotStr}");
            Console.WriteLine("Valid slots: 9A, 9C, 9D, 9E");
            return 1;
        }

        if (!CardHelper.TryParseAlgorithm(algoStr, out var algorithm))
        {
            Console.WriteLine($"Invalid algorithm: {algoStr}");
            Console.WriteLine("Valid algorithms: RSA1024, RSA2048, ECCP256, ECCP384");
            return 1;
        }

        Console.WriteLine($"Generating {algoStr} key pair in slot {slotStr}...");
        Console.WriteLine("This may take 10-30 seconds...");

        using var context = new PcScContext();
        using var (reader, pivCard) = await CardHelper.ConnectToFirstPivCard(context);

        var publicKey = await pivCard.GenerateKeyPairAsync(slot, algorithm);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ Successfully generated {algoStr} key pair in slot {slotStr}");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine($"Public key data: {publicKey.Length} bytes");

        // Display first 64 bytes of public key
        Console.WriteLine("Public key (first 64 bytes, hex):");
        var displayBytes = Math.Min(64, publicKey.Length);
        for (int i = 0; i < displayBytes; i++)
        {
            Console.Write($"{publicKey[i]:X2} ");
            if ((i + 1) % 16 == 0)
                Console.WriteLine();
        }
        if (displayBytes % 16 != 0)
            Console.WriteLine();

        return 0;
    }
}
