using Filigree.Core;

namespace Filigree.Tests;

public class EventFilterTests
{
    private static FiligreeEvent Make(string? image, string searchFilter, string dn = "DC=x", int scope = 2) => new()
    {
        UtcTime = "2026-01-01T00:00:00.000Z",
        Hostname = "TEST",
        Module = "ldap",
        EventType = "LdapSearch",
        Provider = "Microsoft-Windows-LDAP-Client",
        ProviderEventId = 30,
        ProcessId = 1,
        Image = image,
        Payload = new Dictionary<string, object>
        {
            ["ldap"] = new Dictionary<string, object>
            {
                ["SearchFilter"] = searchFilter,
                ["DistinguishedName"] = dn,
                ["ScopeOfSearch"] = scope,
            }
        },
    };

    [Fact]
    public void EmitsApplicationSearch()
    {
        var f = new EventFilter(new ModuleConfig());
        Assert.True(f.ShouldEmit(Make(@"C:\x.exe", "(objectClass=user)")));
    }

    [Fact]
    public void SuppressesRootDseProbe()
    {
        var f = new EventFilter(new ModuleConfig { ExcludeRootDse = true });
        Assert.False(f.ShouldEmit(Make(@"C:\x.exe", "(objectclass=*)", dn: "", scope: 0)));
    }

    [Fact]
    public void KeepsRootDseWhenDisabled()
    {
        var f = new EventFilter(new ModuleConfig { ExcludeRootDse = false });
        Assert.True(f.ShouldEmit(Make(@"C:\x.exe", "(objectclass=*)", dn: "", scope: 0)));
    }

    [Fact]
    public void EmptyDnWithSubtreeScopeIsNotRootDse()
    {
        var f = new EventFilter(new ModuleConfig { ExcludeRootDse = true });
        Assert.True(f.ShouldEmit(Make(@"C:\x.exe", "(objectClass=user)", dn: "", scope: 2)));
    }

    [Fact]
    public void SuppressesExcludedImage()
    {
        var f = new EventFilter(new ModuleConfig { ExcludeImages = { "svchost.exe" } });
        Assert.False(f.ShouldEmit(Make(@"C:\Windows\System32\svchost.exe", "(objectClass=user)")));
    }

    [Fact]
    public void SuppressesExcludedFilter()
    {
        var f = new EventFilter(new ModuleConfig { ExcludeFilters = { "objectClass=computer" } });
        Assert.False(f.ShouldEmit(Make(@"C:\x.exe", "(objectClass=computer)")));
        Assert.True(f.ShouldEmit(Make(@"C:\x.exe", "(objectClass=user)")));
    }

    [Fact]
    public void MatchIsCaseInsensitive()
    {
        var f = new EventFilter(new ModuleConfig { ExcludeImages = { "SVCHOST.EXE" } });
        Assert.False(f.ShouldEmit(Make(@"C:\Windows\System32\svchost.exe", "(x=y)")));
    }

    [Fact]
    public void NullImageDoesNotThrow()
    {
        var f = new EventFilter(new ModuleConfig { ExcludeImages = { "svchost.exe" } });
        Assert.True(f.ShouldEmit(Make(null, "(x=y)")));
    }
}
