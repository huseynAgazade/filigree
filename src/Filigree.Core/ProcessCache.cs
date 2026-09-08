using System.Collections.Concurrent;
using System.Diagnostics;

namespace Filigree.Core;

public sealed record ProcessInfo(int Pid, string? Image, DateTime StartTime, string? CommandLine);

/// <summary>
/// PID -> process identity, fed by Microsoft-Windows-Kernel-Process events.
/// Entries survive process exit for a grace period so short-lived processes can
/// still be resolved. StartTime disambiguates recycled PIDs and is the basis
/// for ProcessGuid. See docs/architecture.md.
/// </summary>
public sealed class ProcessCache
{
    private readonly ConcurrentDictionary<int, ProcessInfo> _live = new();
    private readonly ConcurrentDictionary<int, (ProcessInfo Info, DateTime ExitedAt)> _recent = new();
    private readonly TimeSpan _grace = TimeSpan.FromMinutes(5);
    private readonly Guid _machineGuid = MachineGuid();

    public void OnStart(int pid, string? image, DateTime startTime, string? cmdLine)
        => _live[pid] = new ProcessInfo(pid, image, startTime, cmdLine);

    public void OnExit(int pid)
    {
        if (_live.TryRemove(pid, out var info))
            _recent[pid] = (info, DateTime.UtcNow);
        Prune();
    }

    public ProcessInfo? Lookup(int pid)
    {
        if (_live.TryGetValue(pid, out var live)) return live;
        if (_recent.TryGetValue(pid, out var recent)) return recent.Info;
        return Fallback(pid);
    }

    /// <summary>Sysmon-style identity: machine GUID + PID + start time.</summary>
    public string? ProcessGuid(ProcessInfo? info)
    {
        if (info is null) return null;
        Span<byte> b = stackalloc byte[16];
        _machineGuid.TryWriteBytes(b);
        BitConverter.TryWriteBytes(b[8..], info.Pid);
        BitConverter.TryWriteBytes(b[12..], (int)(info.StartTime.Ticks / TimeSpan.TicksPerSecond));
        return new Guid(b).ToString("B");
    }

    /// <summary>Seeds the cache with processes that existed before the session started.</summary>
    public void Seed()
    {
        foreach (var p in Process.GetProcesses())
        {
            try { OnStart(p.Id, p.MainModule?.FileName, p.StartTime.ToUniversalTime(), null); }
            catch { }
            finally { p.Dispose(); }
        }
    }

    private ProcessInfo? Fallback(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            var info = new ProcessInfo(pid, p.MainModule?.FileName, p.StartTime.ToUniversalTime(), null);
            _live[pid] = info;
            return info;
        }
        catch { return null; }
    }

    private void Prune()
    {
        var cutoff = DateTime.UtcNow - _grace;
        foreach (var kv in _recent)
            if (kv.Value.ExitedAt < cutoff) _recent.TryRemove(kv.Key, out _);
    }

    private static Guid MachineGuid()
    {
        try
        {
            using var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            if (k?.GetValue("MachineGuid") is string s && Guid.TryParse(s, out var g)) return g;
        }
        catch { }
        return Guid.Empty;
    }
}
