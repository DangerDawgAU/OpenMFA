using OpenMFA.SmartCard.PcSc;
using OpenMFA.SmartCard.Piv;

namespace OpenMFA.CLI;

public static class CardHelper
{
    public static async Task<(ICardReader, IPivCard)> ConnectToFirstPivCard(IPcScContext context)
    {
        var readers = await context.ListReadersAsync();

        if (readers.Count == 0)
        {
            throw new Exception("No card readers found");
        }

        foreach (var readerName in readers)
        {
            try
            {
                var reader = await context.ConnectAsync(readerName);
                var pivCard = new PivCard(reader);

                if (await pivCard.SelectPivAppletAsync())
                {
                    return (reader, pivCard);
                }

                reader.Dispose();
                pivCard.Dispose();
            }
            catch (PcScException)
            {
                continue;
            }
        }

        throw new Exception("No PIV card found in any reader");
    }

    public static bool TryParseSlot(string slotStr, out PivSlot slot)
    {
        slot = slotStr.ToUpperInvariant() switch
        {
            "9A" => PivSlot.Authentication,
            "9C" => PivSlot.Signature,
            "9D" => PivSlot.KeyManagement,
            "9E" => PivSlot.CardAuthentication,
            "82" => PivSlot.Retired1,
            "83" => PivSlot.Retired2,
            _ => (PivSlot)0
        };

        return (byte)slot != 0;
    }

    public static bool TryParseAlgorithm(string algoStr, out PivAlgorithm algorithm)
    {
        algorithm = algoStr.ToUpperInvariant() switch
        {
            "RSA1024" => PivAlgorithm.Rsa1024,
            "RSA2048" => PivAlgorithm.Rsa2048,
            "ECCP256" or "ECC256" or "P256" => PivAlgorithm.EccP256,
            "ECCP384" or "ECC384" or "P384" => PivAlgorithm.EccP384,
            _ => (PivAlgorithm)0
        };

        return (byte)algorithm != 0;
    }
}
