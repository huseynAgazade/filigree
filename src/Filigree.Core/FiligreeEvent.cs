using System.Text.Json.Serialization;

namespace Filigree.Core;

/// <summary>
/// Normalized Filigree event. Envelope fields are common to every module;
/// module-specific data lives in Payload. See docs/schema.md.
/// </summary>
public sealed class FiligreeEvent
{
    public int SchemaVersion { get; init; } = 1;
    public required string UtcTime { get; init; }
    public required string Hostname { get; init; }
    public required string Module { get; init; }
    public required string EventType { get; init; }
    public required string Provider { get; init; }
    public required int ProviderEventId { get; init; }
    public required int ProcessId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ThreadId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProcessGuid { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Image { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? User { get; init; }

    [JsonExtensionData]
    public Dictionary<string, object> Payload { get; init; } = new();
}
