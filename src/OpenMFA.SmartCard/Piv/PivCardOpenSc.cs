using OpenMFA.SmartCard.OpenSC;

namespace OpenMFA.SmartCard.Piv;

/// <summary>
/// PIV smart card implementation using OpenSC tools.
/// Supports both native PIV cards (e.g., YubiKey) and PKCS#15 cards with PIV emulation (e.g., MyEID 4.5).
/// The implementation automatically tries PKCS#15 operations first, falling back to PKCS#11 for compatibility.
/// </summary>
public class PivCardOpenSc : IPivCard
{
    private readonly OpenScTools _openSc;
    private readonly uint? _slotId;
    private string? _pin;
    private bool _disposed;

    public PivCardOpenSc(uint? slotId = null, Action<string>? commandLogger = null)
    {
        _openSc = new OpenScTools();
        _slotId = slotId;

        if (commandLogger != null)
        {
            _openSc.SetCommandLogger(commandLogger);
        }
    }

    public Task<bool> SelectPivAppletAsync(CancellationToken cancellationToken = default)
    {
        // With OpenSC CLI, we don't need to explicitly select the applet
        // It's done automatically when accessing the card
        return Task.FromResult(true);
    }

    public async Task<byte[]> GetCertificateAsync(PivSlot slot, CancellationToken cancellationToken = default)
    {
        try
        {
            var objectId = PivDataObject.GetOpenScObjectId(slot);
            return await _openSc.ReadCertificateAsync(objectId, _slotId, cancellationToken);
        }
        catch (OpenScException)
        {
            // Certificate not found or empty
            return Array.Empty<byte>();
        }
    }

    public async Task PutCertificateAsync(PivSlot slot, byte[] certificateData, CancellationToken cancellationToken = default)
    {
        var objectId = PivDataObject.GetOpenScObjectId(slot);
        var label = GetSlotLabel(slot);
        await _openSc.WriteCertificateAsync(objectId, certificateData, label, _slotId, _pin, cancellationToken);
    }

    public async Task DeleteCertificateAsync(PivSlot slot, CancellationToken cancellationToken = default)
    {
        var objectId = PivDataObject.GetOpenScObjectId(slot);
        try
        {
            await _openSc.DeleteObjectAsync(objectId, "cert", _slotId, _pin, cancellationToken);
        }
        catch (OpenScException ex) when (ex.Message.Contains("CKR_FUNCTION_NOT_SUPPORTED"))
        {
            throw new NotSupportedException(
                "Certificate deletion is not supported by this card. " +
                "PIV cards (including YubiKeys) typically require overwriting the certificate slot with a new certificate rather than deleting. " +
                "To remove a certificate, write a new certificate to the same slot or use the card manufacturer's management tool.",
                ex);
        }
    }

    public async Task<byte[]> GenerateKeyPairAsync(PivSlot slot, PivAlgorithm algorithm, CancellationToken cancellationToken = default)
    {
        var objectId = PivDataObject.GetOpenScObjectId(slot);
        var keyType = PivAlgorithmHelper.ToOpenScKeyType(algorithm);
        var label = GetSlotLabel(slot);

        var output = await _openSc.GenerateKeyPairAsync(objectId, keyType, label, _slotId, _pin, cancellationToken);

        // Return the output as bytes for consistency
        return System.Text.Encoding.UTF8.GetBytes(output);
    }

    public Task<bool> VerifyPinAsync(string pin, CancellationToken cancellationToken = default)
    {
        // Store PIN for future operations
        _pin = pin;
        return Task.FromResult(true);
    }

    public Task<bool> ChangePinAsync(string oldPin, string newPin, CancellationToken cancellationToken = default)
    {
        // PIN change would typically be done via OpenSC tools or system utilities
        throw new NotImplementedException("PIN change must be done using OpenSC pkcs15-init or system utilities");
    }

    public Task<byte[]> GetDataAsync(byte[] dataObjectId, CancellationToken cancellationToken = default)
    {
        // For now, focus on certificate operations
        throw new NotImplementedException("Generic data operations not yet implemented with OpenSC");
    }

    public Task PutDataAsync(byte[] dataObjectId, byte[] data, CancellationToken cancellationToken = default)
    {
        // For now, focus on certificate operations
        throw new NotImplementedException("Generic data operations not yet implemented with OpenSC");
    }

    public Task DeleteDataAsync(byte[] dataObjectId, CancellationToken cancellationToken = default)
    {
        // For now, focus on certificate operations
        throw new NotImplementedException("Generic data operations not yet implemented with OpenSC");
    }

    private string GetSlotLabel(PivSlot slot) => slot switch
    {
        PivSlot.Authentication => "PIV AUTH",
        PivSlot.Signature => "SIGNATURE",
        PivSlot.KeyManagement => "KEY MGMT",
        PivSlot.CardAuthentication => "CARD AUTH",
        PivSlot.Retired1 => "RETIRED1",
        PivSlot.Retired2 => "RETIRED2",
        _ => "PIV KEY"
    };

    public void Dispose()
    {
        if (_disposed)
            return;

        _pin = null;
        _disposed = true;
    }
}
