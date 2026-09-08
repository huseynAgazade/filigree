namespace Filigree.Core;

/// <summary>Destination for normalized events. Implementations must be thread-safe.</summary>
public interface IEventSink : IDisposable
{
    void Write(FiligreeEvent evt);
}
