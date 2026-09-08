using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

namespace Filigree.Core;

public sealed class EtwSession : IDisposable
{
    private readonly TraceEventSession _session;
    private bool _disposed;

    public EtwSession(string sessionName)
    {
        TraceEventSession.GetActiveSession(sessionName)?.Stop();
        _session = new TraceEventSession(sessionName);
    }

    public void EnableProvider(Guid providerGuid, ulong keywords)
        => _session.EnableProvider(providerGuid, TraceEventLevel.Verbose, keywords);

    public void Subscribe(Action<TraceEvent> handler)
        => _session.Source.Dynamic.All += handler;

    /// <summary>Feeds a ProcessCache from Microsoft-Windows-Kernel-Process.</summary>
    public void TrackProcesses(ProcessCache cache)
    {
        _session.EnableProvider(
            new Guid("22fb2cd6-0e7b-422b-a0c7-2fad1fd0e716"),
            TraceEventLevel.Informational,
            0x10);

        _session.Source.Kernel.ProcessStart += e =>
            cache.OnStart(e.ProcessID, e.ImageFileName, e.TimeStamp.ToUniversalTime(), e.CommandLine);

        _session.Source.Kernel.ProcessStop += e => cache.OnExit(e.ProcessID);
    }

    public void Process() => _session.Source.Process();
    public void Stop() => _session.Stop();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _session.Dispose();
    }
}
