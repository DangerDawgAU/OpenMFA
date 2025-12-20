namespace OpenMFA.SmartCard.OpenSC;

/// <summary>
/// Context for OpenSC operations - simple wrapper around OpenScTools
/// </summary>
public class OpenScContext : IDisposable
{
    private readonly OpenScTools _tools;
    private bool _disposed;

    public OpenScContext()
    {
        _tools = new OpenScTools();
    }

    /// <summary>
    /// List all available slots with tokens (cards)
    /// </summary>
    public async Task<IReadOnlyList<Pkcs11SlotInfo>> GetSlotsAsync(CancellationToken cancellationToken = default)
    {
        return await _tools.ListSlotsAsync(cancellationToken);
    }

    /// <summary>
    /// List all card readers
    /// </summary>
    public async Task<IReadOnlyList<string>> ListReadersAsync(CancellationToken cancellationToken = default)
    {
        return await _tools.ListReadersAsync(cancellationToken);
    }

    /// <summary>
    /// Get the first available slot with a token
    /// </summary>
    public async Task<Pkcs11SlotInfo?> GetFirstSlotWithTokenAsync(CancellationToken cancellationToken = default)
    {
        var slots = await GetSlotsAsync(cancellationToken);
        return slots.FirstOrDefault(s => s.TokenPresent);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }
}
