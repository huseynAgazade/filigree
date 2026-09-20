# Filigree

![build](https://github.com/huseynAgazade/filigree/actions/workflows/build.yml/badge.svg)

ETW sensors for Windows telemetry Sysmon does not emit.

Sysmon covers process creation, network connections, image loads, and DNS. It
does not cover LDAP. Windows already emits this data through ETW — Filigree
subscribes to those providers, normalizes the events, adds process context, and
writes them somewhere a SOC can query.

## What the LDAP module gives you

Which process on this machine asked Active Directory what question.

    {
      "SchemaVersion": 1,
      "UtcTime": "2026-09-08T21:13:37.027Z",
      "Hostname": "WKSTN-01",
      "Module": "ldap",
      "EventType": "LdapSearch",
      "Provider": "Microsoft-Windows-LDAP-Client",
      "ProviderEventId": 30,
      "ProcessId": 2688,
      "ThreadId": 10832,
      "ProcessGuid": "{769b0e78-e41e-4f0c-800a-00007d7132e2}",
      "Image": "C:\\Users\\analyst\\Desktop\\SharpHound.exe",
      "ldap": {
        "SearchFilter": "(&(samAccountType=805306368)(servicePrincipalName=*))",
        "DistinguishedName": "DC=contoso,DC=com",
        "AttributeList": "sAMAccountName;servicePrincipalName",
        "ScopeOfSearch": 2
      }
    }

The search filter is the intent, in plain text. A network sensor sees a TCP
connection and cannot tell you the process. A domain controller log sees a
query from an IP and cannot tell you the process either. The client-side ETW
event carries both halves at once.

## What it does not do

Filigree collects and writes. It does not alert, correlate, or block. Detection
rules under `detections/sigma/` are for your SIEM to run, not for Filigree.

Three coverage gaps, stated plainly:

**Library scope.** The instrumentation lives in `wldap32.dll`. Tools that use it
are visible — `dsquery`, `ldp.exe`, ADUC, PowerShell `[adsisearcher]`,
`System.DirectoryServices`, SharpHound, Rubeus. Tools that implement LDAP
directly over a socket are not: Java/JNDI, Python `ldap3`, and any Impacket or
`bloodhound-python` run from a Linux host against your DC. Outlook is not a
source; it does not use LDAP for directory access.

**Deployment scope.** Only hosts running Filigree.

**Integrity.** `Microsoft-Windows-LDAP-Client` is a user-mode provider. The
instrumentation runs inside the monitored process, so anyone with code execution
there can patch `EtwEventWrite` and the events stop silently. Stopping the trace
session is easier still and needs no exploitation. This is telemetry, not a
security boundary. Pair it with domain-controller-side logging.

## Requirements

- Windows x64
- .NET 10 runtime
- Administrator (ETW real-time sessions are privileged)

## Build and run

    dotnet build --configuration Release
    dotnet run --project src/Filigree.Host -- config/filigree.yml

Elevated. Config path is optional; without it Filigree looks for `filigree.yml`
beside the executable.

Copy `config/filigree.example.yml` to `config/filigree.yml` and edit.

## Output

Three sinks, all optional, configured in YAML:

| Sink | Destination |
|---|---|
| console | stdout, indented JSON |
| jsonl | rolling file, one compact JSON object per line |
| eventLog | Windows Event Log, log name `Filigree` |

The Event Log sink is the one most deployments want — existing Winlogbeat or WEF
pipelines pick it up alongside Sysmon with a one-line config change. It creates
its event source on first elevated run.

    Get-EventLog -LogName Filigree -Newest 5 | Select-Object -ExpandProperty Message

## Configuration

    ldap:
      enabled: true
      excludeRootDse: true
      excludeImages: []
      excludeFilters: []

`excludeRootDse` suppresses the capability probes client libraries issue during
connection setup — empty base DN, base scope. These are not application
searches, and on a busy host they are the bulk of the volume. On by default.

`excludeImages` and `excludeFilters` are case-insensitive substring matches for
tuning out known-good callers.

Because this provider maps keywords to events one-to-one, Filigree enables only
keyword `0x1` (search). The other 29 event types are never generated rather than
generated and discarded — filtering happens in the kernel.

## Detections

Five Sigma rules under `detections/sigma/ldap/`: SPN enumeration, AS-REP
roastable accounts, delegation enumeration, broad directory enumeration, and
LDAP from an unusual process path.

**All are `status: experimental` and none have been validated against real
attacker tooling.** They were written from published filter signatures, not
from observed traffic. Validation needs an Active Directory domain controller;
the development lab uses AD LDS, which lacks the AD schema attributes these
rules match. See `detections/sigma/ldap/README.md` for what would promote them.

Convert with the supplied pipeline so payload fields resolve:

    sigma convert -t <backend> -p detections/sigma/filigree-ldap-pipeline.yml detections/sigma/ldap/

## Known limitations

- Process enrichment is best-effort. `Image` and `ProcessGuid` are omitted when
  a process exits before its start notification is processed, and the fallback
  lookup can return the wrong process after PID recycling. See
  `docs/architecture.md`.
- The Event Log sink uses the classic `System.Diagnostics.EventLog` API rather
  than a manifest-registered channel. Slower per write. See `tools/eventlog/`.
- One module. RPC and CLR are planned, not started.

## Documentation

| File | Contents |
|---|---|
| `docs/schema.md` | Normalized event schema, v1 |
| `docs/architecture.md` | Pipeline, enrichment design, platform constraints |
| `docs/providers/ldap-client.md` | Provider findings, with evidence and claim status |
| `tests/fixtures/ldap/README.md` | How the test capture was produced |

`docs/providers/ldap-client.md` marks every claim as verified from manifest,
verified from live capture, or unverified. Findings on this provider were
established on Windows 10.0.26200 and recorded there rather than taken from
third-party writeups.

## Testing

    dotnet test

Tests replay a captured `.etl` fixture, so they need no administrator, no LDAP
server, and no domain. This is also what lets CI run them.

## How this was built

Written with an AI coding assistant. The premise is a real telemetry gap: Sysmon covers
process creation, network connections, image loads and DNS, but not LDAP — while Windows
already emits it through the `Microsoft-Windows-LDAP-Client` ETW provider. Filigree
subscribes, normalizes, and adds the process context that makes the event useful, so a
SharpHound-shaped search filter is attributable to a specific PID.

---

## License

Apache-2.0
