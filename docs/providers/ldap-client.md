# Microsoft-Windows-LDAP-Client

**Provider GUID:** `099614a5-5dd7-4788-8bc9-e29f43db28fc`
**Instrumentation lives in:** `%SystemRoot%\system32\wldap32.dll`
**Registered manifest:** `ldap-client-manifest.xml` (dumped from Windows 10.0.26200)

## Claim status

Every claim below is marked with how it was established. Do not promote a claim
to VERIFIED without recording the evidence.

| Claim | Status | Evidence |
|---|---|---|
| Provider GUID is `099614a5-...` | VERIFIED (manifest) | manifest root element |
| Instrumentation lives in wldap32.dll | VERIFIED (manifest) | `resourceFileName` attribute |
| Event 30 is gated by keyword `search` (0x1) | VERIFIED (manifest) | `<event value="30" keywords="0x8000000000000001">` |
| Event 1 is also gated by keyword `search` (0x1) | VERIFIED (manifest) | `<event value="1" keywords="0x8000000000000001">` |
| Keyword-to-event mapping is 1:1 for events 1-29, 31 | VERIFIED (manifest) | bit order matches keyword table |
| Provider declares NO event templates | VERIFIED (manifest) | zero `template=` attributes provider-wide |
| Event 30 payload contains a search filter | UNVERIFIED | no template; needs live capture |
| Event 30 payload field names | UNVERIFIED | needs live capture |
| Difference between event 1 and event 30 | UNVERIFIED | needs live capture |
| Provider fires without enabling the Debug channel | UNVERIFIED | needs live capture |
| Provider fires without per-process registry key | UNVERIFIED | needs live capture |

## Keyword table

30 keywords, mirroring LDAP protocol vocabulary. Full list in the manifest.
The one that matters: `search` = `0x1`.

Microsoft documents these as tracelog flags; `bind` = `0x80000` matches the
DEBUG_BIND example in their LDAP-over-ETW troubleshooting docs.

## Design consequence

Because keyword-to-event is 1:1, **keyword selection acts as event selection**.
Enabling only `0x1` yields events 1 and 30 and nothing else. The other 29 event
types are never generated, not generated-then-filtered. This is unusual and
favourable: most providers force broad subscription followed by userland
filtering.

Filigree therefore enables keyword `0x1` only.

## Scope: what this module sees

Any process that loads wldap32.dll and issues a search. The ETW event header
carries the emitting PID, which is how Filigree attributes the query to a process.

Expected in scope: dsquery, ldp.exe, ADUC, PowerShell `[adsisearcher]`,
System.DirectoryServices, SharpHound, Rubeus.

## Blind spots

- Tools implementing LDAP directly over a socket, bypassing wldap32
- Impacket or other non-Windows tooling run from a Linux host against a DC
  (no Windows client, therefore no client-side event)
- Anything running on a host without Filigree deployed

This module complements domain-controller-side logging. It does not replace it.

## Open questions for live capture

1. Does event 30 decode to named fields via TDH, or is the payload an opaque
   blob / preformatted string?
2. What does event 1 contain, and how does it differ from event 30?
3. Is the search filter truncated at any length?
4. Is `ScopeOfSearch` (if present) a decodable enum?
5. Does the provider fire with the Debug channel disabled?
6. Is the `HKLM\System\CurrentControlSet\Services\ldap\Tracing\<ProcessName>`
   registry key required?
