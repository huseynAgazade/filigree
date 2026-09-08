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

        if (config.ExcludeFilters.Count > 0
            && evt.Payload.TryGetValue("ldap", out var raw)
            && raw is Dictionary<string, object> ldap
            && ldap.TryGetValue("SearchFilter", out var f))
        {
            var filter = f.ToString() ?? string.Empty;
            foreach (var pattern in config.ExcludeFilters)
                if (filter.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return false;
        }

        return true;
    }
}
