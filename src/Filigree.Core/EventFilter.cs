namespace Filigree.Core;

/// <summary>Applies module exclusion rules to a parsed event.</summary>
public sealed class EventFilter(ModuleConfig config)
{
    public bool ShouldEmit(FiligreeEvent evt)
    {
        if (evt.Image is not null)
            foreach (var pattern in config.ExcludeImages)
                if (evt.Image.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return false;

        if (!evt.Payload.TryGetValue("ldap", out var raw) || raw is not Dictionary<string, object> ldap)
            return true;

        var searchFilter = ldap.TryGetValue("SearchFilter", out var f) ? f.ToString() ?? "" : "";

        foreach (var pattern in config.ExcludeFilters)
            if (searchFilter.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return false;

        // RootDSE probes: empty base DN with base scope. Issued by client
        // libraries during connection setup, not by application code.
        // See docs/providers/ldap-client.md.
        if (config.ExcludeRootDse)
        {
            var dn = ldap.TryGetValue("DistinguishedName", out var d) ? d.ToString() ?? "" : "";
            var scope = ldap.TryGetValue("ScopeOfSearch", out var s) && s is int i ? i : -1;
            if (string.IsNullOrEmpty(dn) && scope == 0) return false;
        }

        return true;
    }
}
