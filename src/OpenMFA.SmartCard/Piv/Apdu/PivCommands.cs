namespace OpenMFA.SmartCard.Piv.Apdu;

/// <summary>
/// Factory for creating PIV APDU commands per NIST SP 800-73-4
/// </summary>
public static class PivCommands
{
    // PIV Applet AID: A000000308000010000100
    private static readonly byte[] PivAppletAid = { 0xA0, 0x00, 0x00, 0x03, 0x08, 0x00, 0x00, 0x10, 0x00, 0x01, 0x00 };

    /// <summary>
    /// SELECT command to select the PIV applet
    /// </summary>
    public static ApduCommand Select()
    {
        return new ApduCommand(
            cla: 0x00,
            ins: 0xA4,  // SELECT
            p1: 0x04,   // Select by AID
            p2: 0x00,
            data: PivAppletAid
        );
    }

    /// <summary>
    /// GET DATA command to read data from a PIV data object
    /// </summary>
    public static ApduCommand GetData(byte[] dataObjectId)
    {
        // Build TLV: 5C [len] [data object ID]
        var data = new byte[2 + dataObjectId.Length];
        data[0] = 0x5C;
        data[1] = (byte)dataObjectId.Length;
        Array.Copy(dataObjectId, 0, data, 2, dataObjectId.Length);

        return new ApduCommand(
            cla: 0x00,
            ins: 0xCB,  // GET DATA
            p1: 0x3F,
            p2: 0xFF,
            data: data,
            le: 0x00    // Expect full response
        );
    }

    /// <summary>
    /// PUT DATA command to write data to a PIV data object
    /// </summary>
    public static ApduCommand PutData(byte[] dataObjectId, byte[] dataToWrite)
    {
        // Build TLV: 5C [len] [data object ID] 53 [len] [data]
        var idTlv = new byte[2 + dataObjectId.Length];
        idTlv[0] = 0x5C;
        idTlv[1] = (byte)dataObjectId.Length;
        Array.Copy(dataObjectId, 0, idTlv, 2, dataObjectId.Length);

        var dataTlv = BuildTlv(0x53, dataToWrite);
        var data = new byte[idTlv.Length + dataTlv.Length];
        Array.Copy(idTlv, 0, data, 0, idTlv.Length);
        Array.Copy(dataTlv, 0, data, idTlv.Length, dataTlv.Length);

        return new ApduCommand(
            cla: 0x00,
            ins: 0xDB,  // PUT DATA
            p1: 0x3F,
            p2: 0xFF,
            data: data
        );
    }

    /// <summary>
    /// VERIFY command to verify PIN
    /// </summary>
    public static ApduCommand VerifyPin(string pin)
    {
        var pinBytes = System.Text.Encoding.ASCII.GetBytes(pin);
        var paddedPin = new byte[8];
        Array.Copy(pinBytes, paddedPin, Math.Min(pinBytes.Length, 8));

        // Pad remaining bytes with 0xFF
        for (int i = pinBytes.Length; i < 8; i++)
        {
            paddedPin[i] = 0xFF;
        }

        return new ApduCommand(
            cla: 0x00,
            ins: 0x20,  // VERIFY
            p1: 0x00,
            p2: 0x80,   // PIV Card Application PIN
            data: paddedPin
        );
    }

    /// <summary>
    /// CHANGE REFERENCE DATA command to set/change PIN
    /// </summary>
    public static ApduCommand ChangePin(string oldPin, string newPin)
    {
        var oldPinBytes = System.Text.Encoding.ASCII.GetBytes(oldPin);
        var newPinBytes = System.Text.Encoding.ASCII.GetBytes(newPin);

        var data = new byte[16];

        // Old PIN (8 bytes, padded with 0xFF)
        Array.Copy(oldPinBytes, data, Math.Min(oldPinBytes.Length, 8));
        for (int i = oldPinBytes.Length; i < 8; i++)
        {
            data[i] = 0xFF;
        }

        // New PIN (8 bytes, padded with 0xFF)
        Array.Copy(newPinBytes, 0, data, 8, Math.Min(newPinBytes.Length, 8));
        for (int i = 8 + newPinBytes.Length; i < 16; i++)
        {
            data[i] = 0xFF;
        }

        return new ApduCommand(
            cla: 0x00,
            ins: 0x24,  // CHANGE REFERENCE DATA
            p1: 0x00,
            p2: 0x80,   // PIV Card Application PIN
            data: data
        );
    }

    /// <summary>
    /// GENERATE ASYMMETRIC KEY PAIR command
    /// </summary>
    public static ApduCommand GenerateKeyPair(byte keySlot, byte algorithmId)
    {
        // Build template: AC 03 80 01 [algorithm]
        var template = new byte[] { 0xAC, 0x03, 0x80, 0x01, algorithmId };

        return new ApduCommand(
            cla: 0x00,
            ins: 0x47,  // GENERATE ASYMMETRIC KEY PAIR
            p1: 0x00,
            p2: keySlot,
            data: template,
            le: 0x00
        );
    }

    /// <summary>
    /// GET RESPONSE command to retrieve additional data
    /// </summary>
    public static ApduCommand GetResponse(byte length)
    {
        return new ApduCommand(
            cla: 0x00,
            ins: 0xC0,  // GET RESPONSE
            p1: 0x00,
            p2: 0x00,
            le: length
        );
    }

    /// <summary>
    /// Helper to build a TLV (Tag-Length-Value) structure
    /// </summary>
    private static byte[] BuildTlv(byte tag, byte[] value)
    {
        var length = value.Length;

        if (length < 128)
        {
            // Short form
            var tlv = new byte[2 + length];
            tlv[0] = tag;
            tlv[1] = (byte)length;
            Array.Copy(value, 0, tlv, 2, length);
            return tlv;
        }
        else if (length < 256)
        {
            // Long form (1 byte length)
            var tlv = new byte[3 + length];
            tlv[0] = tag;
            tlv[1] = 0x81;
            tlv[2] = (byte)length;
            Array.Copy(value, 0, tlv, 3, length);
            return tlv;
        }
        else
        {
            // Long form (2 byte length)
            var tlv = new byte[4 + length];
            tlv[0] = tag;
            tlv[1] = 0x82;
            tlv[2] = (byte)(length >> 8);
            tlv[3] = (byte)(length & 0xFF);
            Array.Copy(value, 0, tlv, 4, length);
            return tlv;
        }
    }
}
