using OpenMFA.SmartCard.Piv.Apdu;

namespace OpenMFA.SmartCard.PcSc;

/// <summary>
/// Interface for smart card reader operations
/// </summary>
public interface ICardReader : IDisposable
{
    /// <summary>
    /// Name of the card reader
    /// </summary>
    string ReaderName { get; }

    /// <summary>
    /// Active protocol (T=0 or T=1)
    /// </summary>
    uint ActiveProtocol { get; }

    /// <summary>
    /// Transmits an APDU command to the card and receives the response
    /// </summary>
    Task<ApduResponse> TransmitAsync(ApduCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the Answer-To-Reset (ATR) from the card
    /// </summary>
    Task<byte[]> GetAtrAsync(CancellationToken cancellationToken = default);
}
