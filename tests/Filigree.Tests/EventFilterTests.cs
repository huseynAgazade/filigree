using Filigree.Core;

namespace Filigree.Tests;

public class EventFilterTests
{
    private static FiligreeEvent Make(string image, string searchFilter) => new()
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
            ["ldap"] = new Dictionary<string, object> { ["SearchFilter"] = searchFilter }
        },
    };

    [Fact]
    public void EmitsByDefault()
    {
        var f = new EventFilter(new ModuleConfig());
        Assert.True(f.ShouldEmit(Make(@"C:\x.exe", "(objectClass=user)")));
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
        var f = new EventFilter(new ModuleConfig { ExcludeFilters = { "supportedCapabilities" } });
        Assert.False(f.ShouldEmit(Make(@"C:\x.exe", "(objectclass=*)")) is false);
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
        Assert.True(f.ShouldEmit(Make(null!, "(x=y)")));
    }
}
