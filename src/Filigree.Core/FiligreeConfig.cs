namespace Filigree.Core;

public sealed class FiligreeConfig
{
    public string SessionName { get; set; } = "Filigree";
    public SinkConfig Sinks { get; set; } = new();
    public ModuleConfig Ldap { get; set; } = new();
    public ProcessConfig Process { get; set; } = new();
}

public sealed class SinkConfig
{
    public bool Console { get; set; } = true;
    public bool ConsoleIndented { get; set; } = true;
    public bool Jsonl { get; set; } = true;
    public string JsonlPath { get; set; } = "logs/filigree.jsonl";
    public long JsonlMaxBytes { get; set; } = 64L * 1024 * 1024;
    public int JsonlMaxFiles { get; set; } = 5;
    public bool EventLog { get; set; } = true;
}

public sealed class ModuleConfig
{
    public bool Enabled { get; set; } = true;

    /// <summary>Image paths to suppress. Case-insensitive substring match.</summary>
    public List<string> ExcludeImages { get; set; } = new();

    /// <summary>Search filters to suppress. Case-insensitive substring match.</summary>
    public List<string> ExcludeFilters { get; set; } = new();
}

public sealed class ProcessConfig
{
    public bool TrackProcesses { get; set; } = true;
    public int CacheGraceMinutes { get; set; } = 5;
}
