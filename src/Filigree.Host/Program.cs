using System.Text.Json;
using Filigree.Core;
using Filigree.Modules.Ldap;

Console.WriteLine("Filigree — LDAP module. Ctrl+C to stop.\n");

var json = new JsonSerializerOptions { WriteIndented = true };

using var session = new EtwSession("Filigree-Ldap");

session.Subscribe(data =>
{
    var evt = LdapSearchParser.Parse(data);
    if (evt is null) return;
    Console.WriteLine(JsonSerializer.Serialize(evt, json));
});

session.EnableProvider(LdapProvider.Guid, LdapProvider.KeywordSearch);
Console.CancelKeyPress += (_, e) => { e.Cancel = true; session.Stop(); };
session.Process();
