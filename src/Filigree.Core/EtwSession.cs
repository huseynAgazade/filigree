using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;

namespace Filigree.Core;

/// <summary>
/// Wraps a real-time ETW session. Disposing stops the session — ETW sessions
/// are kernel objects that outlive the process, so leaking one leaves it
/// running until reboot.
/// </summary>
public sealed class EtwSession : IDisposable
{
    private readonly TraceEventSession _session;
    private bool _disposed;

    public EtwSession(string sessionName)
    {
        // Stop any leftover session of the same name from a previous crashed run.
        TraceEventSession.GetActiveSession(sessionName)?.Stop();
        _session = new TraceEventSession(sessionName);
    }

    public void EnableProvider(Guid providerGuid, ulong keywords)
    {
        _session.EnableProvider(providerGuid, TraceEventLevel.Verbose, keywords);
    }

    public void Subscribe(Action<TraceEvent> handler)
    {
        _session.Source.Dynamic.All += handler;
    }

    /// <summary>Blocks until Stop() is called or the session ends.</summary>
    public void Process() => _session.Source.Process();

    public void Stop() => _session.Stop();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _session.Dispose();
    }
}
