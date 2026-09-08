using Filigree.Core;
using Microsoft.Diagnostics.Tracing;

namespace Filigree.Modules.Ldap;

public sealed class LdapSearchParser(ProcessCache cache)
{
    private static readonly string Host = Environment.MachineName;

    public FiligreeEvent? Parse(TraceEvent data)
    {
        if ((int)data.ID != LdapProvider.EventIdSearch) return null;

        var proc = cache.Lookup(data.ProcessID);

        var payload = new Dictionary<string, object>
        {
            ["SearchFilter"]      = Str(data, "SearchFilter"),
            ["DistinguishedName"] = Str(data, "DistinguishedName"),
            ["AttributeList"]     = Str(data, "AttributeList"),
            ["ScopeOfSearch"]     = Scope(data),
        };

        return new FiligreeEvent
        {
            UtcTime         = data.TimeStamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            Hostname        = Host,
            Module          = "ldap",
            EventType       = "LdapSearch",
            Provider        = LdapProvider.Name,
            ProviderEventId = (int)data.ID,
            ProcessId       = data.ProcessID,
            ThreadId        = data.ThreadID,
            ProcessGuid     = cache.ProcessGuid(proc),
            Image           = proc?.Image,
            Payload         = new Dictionary<string, object> { ["ldap"] = payload },
        };
    }

    private static string Str(TraceEvent data, string name)
        => data.PayloadByName(name)?.ToString()?.Trim() ?? string.Empty;

    private static int Scope(TraceEvent data)
    {
        var raw = data.PayloadByName("ScopeOfSearch");
        return raw switch
        {
            null     => -1,
            int i    => i,
            uint u   => (int)u,
            long l   => (int)l,
            string s => int.TryParse(s.Trim(), out var v) ? v : -1,
            _        => int.TryParse(raw.ToString()?.Trim(), out var v2) ? v2 : -1,
        };
    }
}
