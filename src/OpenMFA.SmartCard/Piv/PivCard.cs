using OpenMFA.SmartCard.PcSc;
using OpenMFA.SmartCard.Piv.Apdu;

namespace OpenMFA.SmartCard.Piv;

/// <summary>
/// PIV smart card implementation per NIST SP 800-73-4
/// </summary>
public class PivCard : IPivCard
{
    private readonly ICardReader _reader;
    private bool _disposed;
    private bool _appletSelected;

    public PivCard(ICardReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public async Task<bool> SelectPivAppletAsync(CancellationToken cancellationToken = default)
    {
        var command = PivCommands.Select();
        var response = await _reader.TransmitAsync(command, cancellationToken);

        _appletSelected = response.IsSuccess;
        return _appletSelected;
    }

    public async Task<byte[]> GetDataAsync(byte[] dataObjectId, CancellationToken cancellationToken = default)
    {
        EnsureAppletSelected();

        var command = PivCommands.GetData(dataObjectId);
        var response = await _reader.TransmitAsync(command, cancellationToken);

        if (!response.IsSuccess)
        {
            // Data not found is acceptable for some operations
            if (response.StatusWord == 0x6A82 || response.StatusWord == 0x6A88)
            {
                return Array.Empty<byte>();
            }

            throw new CardException($"Failed to get data: {response.StatusMessage}");
        }

        // Parse TLV response to extract actual data
        // Response format: 53 [len] [data]
        return ParseTlvData(response.Data, 0x53);
    }

    public async Task PutDataAsync(byte[] dataObjectId, byte[] data, CancellationToken cancellationToken = default)
    {
        EnsureAppletSelected();

        var command = PivCommands.PutData(dataObjectId, data);
        var response = await _reader.TransmitAsync(command, cancellationToken);

        if (!response.IsSuccess)
        {
            throw new CardException($"Failed to put data: {response.StatusMessage}");
        }
    }

    public async Task DeleteDataAsync(byte[] dataObjectId, CancellationToken cancellationToken = default)
    {
        // Delete by writing empty data
        await PutDataAsync(dataObjectId, Array.Empty<byte>(), cancellationToken);
    }

    public async Task<byte[]> GenerateKeyPairAsync(PivSlot slot, PivAlgorithm algorithm, CancellationToken cancellationToken = default)
    {
        EnsureAppletSelected();

        var command = PivCommands.GenerateKeyPair((byte)slot, (byte)algorithm);
        var response = await _reader.TransmitAsync(command, cancellationToken);

        if (!response.IsSuccess)
        {
            throw new CardException($"Failed to generate key pair: {response.StatusMessage}");
        }

        // Return the raw response containing the public key
        // Full TLV parsing would be needed for production use
        return response.Data;
    }

    public async Task<bool> VerifyPinAsync(string pin, CancellationToken cancellationToken = default)
    {
        EnsureAppletSelected();

        if (string.IsNullOrEmpty(pin) || pin.Length < 6 || pin.Length > 8)
        {
            throw new ArgumentException("PIN must be 6-8 characters", nameof(pin));
        }

        var command = PivCommands.VerifyPin(pin);
        var response = await _reader.TransmitAsync(command, cancellationToken);

        return response.IsSuccess;
    }

    public async Task<bool> ChangePinAsync(string oldPin, string newPin, CancellationToken cancellationToken = default)
    {
        EnsureAppletSelected();

        if (string.IsNullOrEmpty(newPin) || newPin.Length < 6 || newPin.Length > 8)
        {
            throw new ArgumentException("New PIN must be 6-8 characters", nameof(newPin));
        }

        var command = PivCommands.ChangePin(oldPin, newPin);
        var response = await _reader.TransmitAsync(command, cancellationToken);

        return response.IsSuccess;
    }

    public async Task<byte[]> GetCertificateAsync(PivSlot slot, CancellationToken cancellationToken = default)
    {
        var dataObjectId = PivDataObject.GetCertificateObject(slot);
        return await GetDataAsync(dataObjectId, cancellationToken);
    }

    public async Task PutCertificateAsync(PivSlot slot, byte[] certificateData, CancellationToken cancellationToken = default)
    {
        var dataObjectId = PivDataObject.GetCertificateObject(slot);
        await PutDataAsync(dataObjectId, certificateData, cancellationToken);
    }

    public async Task DeleteCertificateAsync(PivSlot slot, CancellationToken cancellationToken = default)
    {
        var dataObjectId = PivDataObject.GetCertificateObject(slot);
        await DeleteDataAsync(dataObjectId, cancellationToken);
    }

    private void EnsureAppletSelected()
    {
        if (!_appletSelected)
        {
            throw new InvalidOperationException("PIV applet not selected. Call SelectPivAppletAsync first.");
        }
    }

    private byte[] ParseTlvData(byte[] tlvData, byte expectedTag)
    {
        if (tlvData.Length < 2)
        {
            return Array.Empty<byte>();
        }

        // Simple TLV parser - production would need full BER-TLV support
        int offset = 0;

        while (offset < tlvData.Length)
        {
            var tag = tlvData[offset++];

            if (offset >= tlvData.Length)
                break;

            // Parse length
            int length = tlvData[offset++];
            if ((length & 0x80) != 0)
            {
                // Long form length
                var numLengthBytes = length & 0x7F;
                length = 0;
                for (int i = 0; i < numLengthBytes; i++)
                {
                    if (offset >= tlvData.Length)
                        break;
                    length = (length << 8) | tlvData[offset++];
                }
            }

            // Extract value
            if (tag == expectedTag && offset + length <= tlvData.Length)
            {
                var value = new byte[length];
                Array.Copy(tlvData, offset, value, 0, length);
                return value;
            }

            offset += length;
        }

        return Array.Empty<byte>();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Reader is owned by the caller, don't dispose it
        _disposed = true;
    }
}
