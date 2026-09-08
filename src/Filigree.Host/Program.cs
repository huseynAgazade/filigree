using Filigree.Core;
using Filigree.Modules.Ldap;

Console.WriteLine("Filigree — LDAP consumer (commit 4: raw dump)");
Console.WriteLine("Press Ctrl+C to stop.\n");

using var session = new EtwSession("Filigree-Ldap");

session.Subscribe(data =>
{
    if ((int)data.ID != LdapProvider.EventIdSearch) return;

    Console.WriteLine($"[{data.TimeStamp:HH:mm:ss.fff}] EventID={(int)data.ID} PID={data.ProcessID} TID={data.ThreadID}");
    foreach (var name in data.PayloadNames)
    {
        Console.WriteLine($"    {name} = {data.PayloadByName(name)}");
    }
    Console.WriteLine();
});

session.EnableProvider(LdapProvider.Guid, LdapProvider.KeywordSearch);

Console.CancelKeyPress += (_, e) => { e.Cancel = true; session.Stop(); };

session.Process();
Console.WriteLine("Session stopped.");
