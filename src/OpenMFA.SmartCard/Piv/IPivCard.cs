namespace OpenMFA.SmartCard.Piv;

/// <summary>
/// Interface for PIV smart card operations per NIST SP 800-73-4
/// </summary>
public interface IPivCard : IDisposable
{
    /// <summary>
    /// Selects the PIV applet on the card
    /// </summary>
    Task<bool> SelectPivAppletAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads data from a PIV data object
    /// </summary>
    Task<byte[]> GetDataAsync(byte[] dataObjectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes data to a PIV data object
    /// </summary>
    Task PutDataAsync(byte[] dataObjectId, byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes data from a PIV data object by writing empty data
    /// </summary>
    Task DeleteDataAsync(byte[] dataObjectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an asymmetric key pair on the card
    /// </summary>
    Task<byte[]> GenerateKeyPairAsync(PivSlot slot, PivAlgorithm algorithm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the PIN
    /// </summary>
    Task<bool> VerifyPinAsync(string pin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the PIN
    /// </summary>
    Task<bool> ChangePinAsync(string oldPin, string newPin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets certificate from a specific slot
    /// </summary>
    Task<byte[]> GetCertificateAsync(PivSlot slot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Puts certificate into a specific slot
    /// </summary>
    Task PutCertificateAsync(PivSlot slot, byte[] certificateData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes certificate from a specific slot
    /// </summary>
    Task DeleteCertificateAsync(PivSlot slot, CancellationToken cancellationToken = default);
}
