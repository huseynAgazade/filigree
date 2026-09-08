$server = "localhost"
$port   = 389
$baseDn = "CN=filigree,DC=filigree,DC=local"   # <-- replace with your actual partition DN

Add-Type -AssemblyName System.DirectoryServices.Protocols

$id   = [System.DirectoryServices.Protocols.LdapDirectoryIdentifier]::new($server, $port)
$conn = [System.DirectoryServices.Protocols.LdapConnection]::new($id)
$conn.AuthType = "Negotiate"
$conn.SessionOptions.ProtocolVersion = 3
$conn.Bind()
Write-Host "[+] Bound to $server`:$port"

$filters = @(
    "(objectClass=*)",
    "(objectClass=user)",
    "(cn=FILIGREEMARKERALPHA)",
    "(&(objectClass=user)(cn=FILIGREEMARKERBRAVO))"
)

foreach ($f in $filters) {
    $req = [System.DirectoryServices.Protocols.SearchRequest]::new(
        $baseDn, $f, [System.DirectoryServices.Protocols.SearchScope]::Subtree)
    try   { $null = $conn.SendRequest($req); Write-Host "[+] $f" }
    catch { Write-Host "[!] $f -> $($_.Exception.Message)" }
    Start-Sleep -Milliseconds 300
}

Write-Host "[+] This process PID: $PID"
