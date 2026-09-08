using Filigree.Core;
using Filigree.Modules.Ldap;

var selfPid = Environment.ProcessId;
var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "filigree-ldap.jsonl");

Console.WriteLine($"Filigree — LDAP module (pid {selfPid})");
Console.WriteLine($"Writing to {logPath}");
Console.WriteLine("Ctrl+C to stop.\n");

var cache = new ProcessCache();
cache.Seed();

var parser = new LdapSearchParser(cache);

using var sink = new CompositeSink(
    new ConsoleSink(indented: true),
    new JsonlSink(logPath));

using var session = new EtwSession("Filigree-Ldap");
session.TrackProcesses(cache);

session.Subscribe(data =>
{
    var evt = parser.Parse(data);
    if (evt is null) return;
    if (evt.ProcessId == selfPid) return;   // never log our own activity
    sink.Write(evt);
});

session.EnableProvider(LdapProvider.Guid, LdapProvider.KeywordSearch);
Console.CancelKeyPress += (_, e) => { e.Cancel = true; session.Stop(); };
session.Process();
