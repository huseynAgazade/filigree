using Filigree.Core;
using Filigree.Modules.Ldap;

var configPath = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "filigree.yml");

Console.WriteLine($"[filigree] config path: {Path.GetFullPath(configPath)}");
Console.WriteLine($"[filigree] config exists: {File.Exists(configPath)}");

var config  = ConfigLoader.Load(configPath);
var selfPid = Environment.ProcessId;

Console.WriteLine($"[filigree] excludeImages: {config.Ldap.ExcludeImages.Count}");
Console.WriteLine($"[filigree] excludeFilters: {config.Ldap.ExcludeFilters.Count} -> {string.Join(", ", config.Ldap.ExcludeFilters)}");
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
    if (!filter.ShouldEmit(evt))
    {
        Console.WriteLine("[filigree] suppressed by filter");
        return;
    }
    sink.Write(evt);
});

session.EnableProvider(LdapProvider.Guid, LdapProvider.KeywordSearch);
Console.CancelKeyPress += (_, e) => { e.Cancel = true; session.Stop(); };
session.Process();
