namespace OpenMFA.SmartCard.Piv.Apdu;

/// <summary>
/// Represents an APDU command following ISO 7816-4 specification
/// Format: CLA INS P1 P2 [Lc Data] [Le]
/// </summary>
public class ApduCommand
{
    public byte Cla { get; set; }
    public byte Ins { get; set; }
    public byte P1 { get; set; }
    public byte P2 { get; set; }
    public byte[]? Data { get; set; }
    public byte? Le { get; set; }

    public ApduCommand(byte cla, byte ins, byte p1, byte p2, byte[]? data = null, byte? le = null)
    {
        Cla = cla;
        Ins = ins;
        P1 = p1;
        P2 = p2;
        Data = data;
        Le = le;
    }

    /// <summary>
    /// Converts the APDU command to a byte array for transmission
    /// </summary>
    public byte[] ToBytes()
    {
        var hasData = Data != null && Data.Length > 0;
        var hasLe = Le.HasValue;

        // Calculate total length
        var length = 4; // CLA, INS, P1, P2
        if (hasData)
        {
            length += 1 + Data!.Length; // Lc + Data
        }
        if (hasLe)
        {
            length += 1; // Le
        }

        var buffer = new byte[length];
        var offset = 0;

        // Header
        buffer[offset++] = Cla;
        buffer[offset++] = Ins;
        buffer[offset++] = P1;
        buffer[offset++] = P2;

        // Data
        if (hasData)
        {
            buffer[offset++] = (byte)Data!.Length; // Lc
            Array.Copy(Data, 0, buffer, offset, Data.Length);
            offset += Data.Length;
        }

        // Expected response length
        if (hasLe)
        {
            buffer[offset] = Le!.Value;
        }

        return buffer;
    }

    public override string ToString()
    {
        var bytes = ToBytes();
        return BitConverter.ToString(bytes).Replace("-", " ");
    }
}
