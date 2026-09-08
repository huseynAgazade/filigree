using Filigree.Core;
using Filigree.Modules.Ldap;

var selfPid = Environment.ProcessId;
var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "filigree-ldap.jsonl");

Console.WriteLine($"Filigree — LDAP module (pid {selfPid})");

var cache = new ProcessCache();
cache.Seed();

var parser = new LdapSearchParser(cache);

var eventLog = new EventLogSink();
Console.WriteLine(eventLog.Available
    ? "Event Log sink: Filigree/Operational"
    : "Event Log sink: unavailable (provider not registered, see tools/eventlog)");
Console.WriteLine($"JSONL sink: {logPath}");
Console.WriteLine("Ctrl+C to stop.\n");

using var sink = new CompositeSink(
    new ConsoleSink(indented: true),
    new JsonlSink(logPath),
    eventLog);

using var session = new EtwSession("Filigree-Ldap");
session.TrackProcesses(cache);

session.Subscribe(data =>
{
    var evt = parser.Parse(data);
    if (evt is null) return;
    if (evt.ProcessId == selfPid) return;
    sink.Write(evt);
});

session.EnableProvider(LdapProvider.Guid, LdapProvider.KeywordSearch);
Console.CancelKeyPress += (_, e) => { e.Cancel = true; session.Stop(); };
session.Process();
