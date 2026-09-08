using System.Text.Json;

namespace Filigree.Core;

/// <summary>Human-readable output for development. Indented; not for volume.</summary>
public sealed class ConsoleSink(bool indented = true) : IEventSink
{
    private readonly JsonSerializerOptions _opts = new() { WriteIndented = indented };
    private readonly Lock _gate = new();

    public void Write(FiligreeEvent evt)
    {
        var line = JsonSerializer.Serialize(evt, _opts);
        lock (_gate) Console.WriteLine(line);
    }

    public void Dispose() { }
}
