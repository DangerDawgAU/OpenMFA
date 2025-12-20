namespace OpenMFA.SmartCard.PcSc;

/// <summary>
/// Interface for PC/SC smart card context
/// </summary>
public interface IPcScContext : IDisposable
{
    /// <summary>
    /// Lists all available smart card readers
    /// </summary>
    Task<IReadOnlyList<string>> ListReadersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects to a smart card in the specified reader
    /// </summary>
    Task<ICardReader> ConnectAsync(string readerName, CancellationToken cancellationToken = default);
}
