using Filigree.Core;
using Filigree.Modules.Ldap;

namespace Filigree.Tests;

public class LdapSearchParserTests
{
    private const string Fixture = "fixtures/ldap/ldap-search-adlds-26200.etl";

    private static List<FiligreeEvent> ParseFixture()
    {
        var cache  = new ProcessCache();
        var parser = new LdapSearchParser(cache);
        var events = new List<FiligreeEvent>();

        EtlReplay.Process(Fixture, data =>
        {
            var evt = parser.Parse(data);
            if (evt is not null) events.Add(evt);
        });

        return events;
    }

    [Fact]
    public void FixtureExists()
    {
        Assert.True(File.Exists(Fixture), $"Fixture missing: {Path.GetFullPath(Fixture)}");
    }

    [Fact]
    public void ParsesSixSearchEvents()
    {
        Assert.Equal(6, ParseFixture().Count);
    }

    [Fact]
    public void EnvelopeIsPopulated()
    {
        var e = ParseFixture().First();
        Assert.Equal(1, e.SchemaVersion);
        Assert.Equal("ldap", e.Module);
        Assert.Equal("LdapSearch", e.EventType);
        Assert.Equal("Microsoft-Windows-LDAP-Client", e.Provider);
        Assert.Equal(30, e.ProviderEventId);
        Assert.True(e.ProcessId > 0);
        Assert.EndsWith("Z", e.UtcTime);
    }

    [Fact]
    public void MarkerFiltersAreCaptured()
    {
        var filters = ParseFixture()
            .Select(e => ((Dictionary<string, object>)e.Payload["ldap"])["SearchFilter"].ToString())
            .ToList();

        Assert.Contains(filters, f => f!.Contains("FILIGREEMARKERALPHA"));
        Assert.Contains(filters, f => f!.Contains("FILIGREEMARKERBRAVO"));
    }

    [Fact]
    public void ScopeDecodesToBaseAndSubtree()
    {
        var scopes = ParseFixture()
            .Select(e => (int)((Dictionary<string, object>)e.Payload["ldap"])["ScopeOfSearch"])
            .ToList();

        Assert.DoesNotContain(-1, scopes);
        Assert.Contains(0, scopes);   // RootDSE lookups, base scope
        Assert.Contains(2, scopes);   // application searches, subtree
    }

    [Fact]
    public void RootDseQueriesHaveEmptyDnAndNamedAttributes()
    {
        var rootDse = ParseFixture()
            .Select(e => (Dictionary<string, object>)e.Payload["ldap"])
            .Where(p => string.IsNullOrEmpty(p["DistinguishedName"].ToString()))
            .ToList();

        Assert.Equal(2, rootDse.Count);
        Assert.All(rootDse, p => Assert.NotEmpty(p["AttributeList"].ToString()!));
    }
}
