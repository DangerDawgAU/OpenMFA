using OpenMFA.SmartCard.PcSc.Native;
using System.Text;

namespace OpenMFA.SmartCard.PcSc;

/// <summary>
/// PC/SC smart card context implementation
/// </summary>
public class PcScContext : IPcScContext
{
    private IntPtr _context;
    private bool _disposed;

    public PcScContext()
    {
        var result = PcScNative.SCardEstablishContext(
            PcScNative.SCARD_SCOPE_SYSTEM,
            IntPtr.Zero,
            IntPtr.Zero,
            out _context);

        if (result != PcScNative.SCARD_S_SUCCESS)
        {
            throw new PcScException($"Failed to establish PC/SC context: {PcScNative.GetErrorMessage(result)}");
        }
    }

    public async Task<IReadOnlyList<string>> ListReadersAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run<IReadOnlyList<string>>(() =>
        {
            EnsureNotDisposed();

            // First call to get the required buffer size
            uint readersLength = 0;
            var result = PcScNative.SCardListReaders(_context, null, null, ref readersLength);

            if (result == PcScNative.SCARD_E_NO_READERS_AVAILABLE)
            {
                return Array.Empty<string>();
            }

            if (result != PcScNative.SCARD_S_SUCCESS)
            {
                throw new PcScException($"Failed to list readers: {PcScNative.GetErrorMessage(result)}");
            }

            // Allocate buffer and get reader names
            var readersBuffer = new byte[readersLength];
            result = PcScNative.SCardListReaders(_context, null, readersBuffer, ref readersLength);

            if (result != PcScNative.SCARD_S_SUCCESS)
            {
                throw new PcScException($"Failed to list readers: {PcScNative.GetErrorMessage(result)}");
            }

            // Parse multi-string (null-separated strings, double-null terminated)
            return ParseMultiString(readersBuffer);
        }, cancellationToken);
    }

    public async Task<ICardReader> ConnectAsync(string readerName, CancellationToken cancellationToken = default)
    {
        return await Task.Run<ICardReader>(() =>
        {
            EnsureNotDisposed();

            var result = PcScNative.SCardConnect(
                _context,
                readerName,
                PcScNative.SCARD_SHARE_SHARED,
                PcScNative.SCARD_PROTOCOL_ANY,
                out var cardHandle,
                out var activeProtocol);

            if (result != PcScNative.SCARD_S_SUCCESS)
            {
                throw new PcScException($"Failed to connect to card: {PcScNative.GetErrorMessage(result)}");
            }

            return (ICardReader)new CardReader(cardHandle, readerName, activeProtocol);
        }, cancellationToken);
    }

    private List<string> ParseMultiString(byte[] buffer)
    {
        var readers = new List<string>();
        var currentString = new StringBuilder();

        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] == 0)
            {
                if (currentString.Length > 0)
                {
                    readers.Add(currentString.ToString());
                    currentString.Clear();
                }
                else
                {
                    // Double null - end of list
                    break;
                }
            }
            else
            {
                currentString.Append((char)buffer[i]);
            }
        }

        return readers;
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PcScContext));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_context != IntPtr.Zero)
        {
            PcScNative.SCardReleaseContext(_context);
            _context = IntPtr.Zero;
        }

        _disposed = true;
    }
}

public class PcScException : Exception
{
    public PcScException(string message) : base(message) { }
    public PcScException(string message, Exception innerException) : base(message, innerException) { }
}
