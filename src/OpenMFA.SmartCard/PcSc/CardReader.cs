using OpenMFA.SmartCard.Piv.Apdu;
using OpenMFA.SmartCard.PcSc.Native;
using System.Runtime.InteropServices;

namespace OpenMFA.SmartCard.PcSc;

/// <summary>
/// Smart card reader implementation
/// </summary>
internal class CardReader : ICardReader
{
    private readonly IntPtr _cardHandle;
    private bool _disposed;

    public string ReaderName { get; }
    public uint ActiveProtocol { get; }

    internal CardReader(IntPtr cardHandle, string readerName, uint activeProtocol)
    {
        _cardHandle = cardHandle;
        ReaderName = readerName;
        ActiveProtocol = activeProtocol;
    }

    public async Task<ApduResponse> TransmitAsync(ApduCommand command, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            EnsureNotDisposed();

            var sendBuffer = command.ToBytes();
            var receiveBuffer = new byte[256 + 2]; // Max APDU response + SW1 SW2
            uint receiveLength = (uint)receiveBuffer.Length;

            // Determine which protocol structure to use
            var pioSendPci = GetPioSendPci();

            var result = PcScNative.SCardTransmit(
                _cardHandle,
                pioSendPci,
                sendBuffer,
                (uint)sendBuffer.Length,
                IntPtr.Zero,
                receiveBuffer,
                ref receiveLength);

            if (result != PcScNative.SCARD_S_SUCCESS)
            {
                throw new PcScException($"Failed to transmit APDU: {PcScNative.GetErrorMessage(result)}");
            }

            // Trim receive buffer to actual received length
            var responseBytes = new byte[receiveLength];
            Array.Copy(receiveBuffer, responseBytes, receiveLength);

            return new ApduResponse(responseBytes);
        }, cancellationToken);
    }

    public async Task<byte[]> GetAtrAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            EnsureNotDisposed();

            // For now, return empty ATR - full implementation would require SCardStatus
            return Array.Empty<byte>();
        }, cancellationToken);
    }

    private unsafe IntPtr GetPioSendPci()
    {
        // Create a PCI structure based on active protocol
        var pci = new PcScNative.SCARD_IO_REQUEST
        {
            dwProtocol = ActiveProtocol,
            cbPciLength = (uint)sizeof(PcScNative.SCARD_IO_REQUEST)
        };

        // Allocate unmanaged memory for the structure
        var pciPtr = Marshal.AllocHGlobal(sizeof(PcScNative.SCARD_IO_REQUEST));
        Marshal.StructureToPtr(pci, pciPtr, false);

        return pciPtr;
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CardReader));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_cardHandle != IntPtr.Zero)
        {
            PcScNative.SCardDisconnect(_cardHandle, PcScNative.SCARD_LEAVE_CARD);
        }

        _disposed = true;
    }
}
