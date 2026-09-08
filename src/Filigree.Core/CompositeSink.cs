namespace Filigree.Core;

/// <summary>Writes to every configured sink. One sink failing must not stop the others.</summary>
public sealed class CompositeSink(params IEventSink[] sinks) : IEventSink
{
    public void Write(FiligreeEvent evt)
    {
        foreach (var sink in sinks)
        {
            try { sink.Write(evt); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[filigree] sink {sink.GetType().Name} failed: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        foreach (var sink in sinks)
        {
            try { sink.Dispose(); } catch { }
        }
    }
}
