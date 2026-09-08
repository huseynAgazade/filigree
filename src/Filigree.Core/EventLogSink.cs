using System.Diagnostics;
using System.Text.Json;

namespace Filigree.Core;

/// <summary>
/// Writes events to a Windows Event Log source as a single JSON string.
/// Creating the source requires administrator once; after that any run can write.
/// No-ops if the source is unavailable so the host still runs without it.
/// </summary>
public sealed class EventLogSink : IEventSink
{
    private const string SourceName = "Filigree";
    private const string LogName = "Filigree";
    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = false };

    private readonly EventLog? _log;

    public bool Available => _log is not null;

    public EventLogSink()
    {
        try
        {
            if (!EventLog.SourceExists(SourceName))
                EventLog.CreateEventSource(new EventSourceCreationData(SourceName, LogName));

            _log = new EventLog(LogName) { Source = SourceName };
        }
        catch
        {
            _log = null;
        }
    }

    public void Write(FiligreeEvent evt)
    {
        if (_log is null) return;
        try { _log.WriteEntry(JsonSerializer.Serialize(evt, Opts), EventLogEntryType.Information, 1); }
        catch { }
    }

    public void Dispose() => _log?.Dispose();
}
