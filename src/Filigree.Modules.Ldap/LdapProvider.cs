namespace Filigree.Modules.Ldap;

public static class LdapProvider
{
    /// <summary>Microsoft-Windows-LDAP-Client. Verified from registered manifest.</summary>
    public static readonly Guid Guid = new("099614a5-5dd7-4788-8bc9-e29f43db28fc");

    public const string Name = "Microsoft-Windows-LDAP-Client";

    /// <summary>
    /// Keyword "search". Gates events 1 and 30. Because this provider maps
    /// keywords to events 1:1, enabling only this keyword means the other 29
    /// event types are never generated — filtering happens in the kernel.
    /// </summary>
    public const ulong KeywordSearch = 0x1;

    /// <summary>
    /// Search event. Verified by live capture on Windows 10.0.26200.
    /// Event 1 shares the same keyword but was not observed to fire.
    /// </summary>
    public const int EventIdSearch = 30;
}
