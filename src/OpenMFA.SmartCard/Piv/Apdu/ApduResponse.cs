namespace OpenMFA.SmartCard.Piv.Apdu;

/// <summary>
/// Represents an APDU response
/// Format: [Data] SW1 SW2
/// </summary>
public class ApduResponse
{
    public byte[] Data { get; }
    public byte SW1 { get; }
    public byte SW2 { get; }

    public ApduResponse(byte[] response)
    {
        if (response.Length < 2)
        {
            throw new ArgumentException("Response must be at least 2 bytes (SW1 SW2)", nameof(response));
        }

        SW1 = response[^2];
        SW2 = response[^1];

        if (response.Length > 2)
        {
            Data = new byte[response.Length - 2];
            Array.Copy(response, 0, Data, 0, Data.Length);
        }
        else
        {
            Data = Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Returns the status word (SW1 SW2) as a 16-bit integer
    /// </summary>
    public ushort StatusWord => (ushort)((SW1 << 8) | SW2);

    /// <summary>
    /// Checks if the response indicates success (90 00)
    /// </summary>
    public bool IsSuccess => SW1 == 0x90 && SW2 == 0x00;

    /// <summary>
    /// Gets a human-readable status message
    /// </summary>
    public string StatusMessage => StatusWord switch
    {
        0x9000 => "Success",
        0x6300 => "Verification failed",
        0x6983 => "Authentication method blocked",
        0x6984 => "Referenced data invalidated",
        0x6A80 => "Incorrect parameters in data field",
        0x6A81 => "Function not supported",
        0x6A82 => "File or application not found",
        0x6A84 => "Not enough memory",
        0x6A86 => "Incorrect P1 P2",
        0x6A88 => "Referenced data not found",
        0x6D00 => "Instruction not supported",
        0x6E00 => "Class not supported",
        0x6F00 => "Unknown error",
        _ when (SW1 == 0x63 && (SW2 & 0xC0) == 0xC0) => $"Verification failed, {SW2 & 0x0F} retries left",
        _ when (SW1 == 0x61) => $"{SW2} bytes available",
        _ when (SW1 == 0x6C) => $"Wrong Le, correct value is {SW2}",
        _ => $"Unknown status: {SW1:X2} {SW2:X2}"
    };

    /// <summary>
    /// Throws an exception if the response is not successful
    /// </summary>
    public void ThrowIfError()
    {
        if (!IsSuccess)
        {
            throw new CardException($"Card operation failed: {StatusMessage} (SW={StatusWord:X4})");
        }
    }

    public override string ToString()
    {
        var dataHex = Data.Length > 0 ? BitConverter.ToString(Data).Replace("-", " ") + " " : "";
        return $"{dataHex}{SW1:X2} {SW2:X2} ({StatusMessage})";
    }
}

public class CardException : Exception
{
    public CardException(string message) : base(message) { }
    public CardException(string message, Exception innerException) : base(message, innerException) { }
}
