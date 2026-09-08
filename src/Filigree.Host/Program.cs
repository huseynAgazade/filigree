using Filigree.Core;
using Filigree.Modules.Ldap;

var configPath = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "filigree.yml");

var config  = ConfigLoader.Load(configPath);
var selfPid = Environment.ProcessId;

Console.WriteLine($"Filigree (pid {selfPid}) — session '{config.SessionName}'");

if (!config.Ldap.Enabled)
{
    Console.WriteLine("No modules enabled. Exiting.");
    return;
}

var cache = new ProcessCache();
if (config.Process.TrackProcesses) cache.Seed();

var parser = new LdapSearchParser(cache);
var filter = new EventFilter(config.Ldap);

var sinks = new List<IEventSink>();
if (config.Sinks.Console) sinks.Add(new ConsoleSink(config.Sinks.ConsoleIndented));
if (config.Sinks.Jsonl)
{
    var path = Path.IsPathRooted(config.Sinks.JsonlPath)
        ? config.Sinks.JsonlPath
        : Path.Combine(AppContext.BaseDirectory, config.Sinks.JsonlPath);
    sinks.Add(new JsonlSink(path, config.Sinks.JsonlMaxBytes, config.Sinks.JsonlMaxFiles));
    Console.WriteLine($"JSONL sink: {path}");
}
if (config.Sinks.EventLog)
{
    var el = new EventLogSink();
    Console.WriteLine($"Event Log sink: {(el.Available ? "Filigree" : "unavailable")}");
    sinks.Add(el);
}

Console.WriteLine("Ctrl+C to stop.\n");

using var sink = new CompositeSink(sinks.ToArray());
using var session = new EtwSession(config.SessionName);

if (config.Process.TrackProcesses) session.TrackProcesses(cache);

session.Subscribe(data =>
{
    var evt = parser.Parse(data);
    if (evt is null) return;
    if (evt.ProcessId == selfPid) return;
    if (!filter.ShouldEmit(evt)) return;
    sink.Write(evt);
});

session.EnableProvider(LdapProvider.Guid, LdapProvider.KeywordSearch);
Console.CancelKeyPress += (_, e) => { e.Cancel = true; session.Stop(); };
session.Process();
