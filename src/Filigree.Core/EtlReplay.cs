using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;

namespace Filigree.Core;

/// <summary>
/// Replays a captured .etl file through the same handler path as a live session.
/// Lets parsers be tested without admin rights or a live provider.
/// </summary>
public static class EtlReplay
{
    public static void Process(string etlPath, Action<TraceEvent> handler)
    {
        using var source = new ETWTraceEventSource(etlPath);
        source.Dynamic.All += handler;
        source.Process();
    }
}
