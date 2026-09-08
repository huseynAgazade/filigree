# Filigree Event Schema v1

Every Filigree module emits events in this shape. The envelope is common to all
modules; module-specific data is nested under a key named after the module.

## Design rules

1. **Envelope + payload.** Common fields flat at the top level, module data
   nested. Prevents field collisions between modules.
2. **Sysmon names where they overlap.** `UtcTime`, `ProcessId`, `ProcessGuid`,
   `Image`, `User` match Sysmon so existing pipelines need no retraining.
3. **Enrichment is best-effort.** ETW is asynchronous. A process may exit before
   Filigree resolves its image path or user. Enrichment fields are nullable and
   omitted when unavailable. Consumers MUST NOT assume they are present.
4. **Never silently drop provider data.** Unrecognised provider fields go into
   the payload unchanged rather than being discarded.
5. **Additive changes only within a version.** Adding a field is fine. Renaming,
   removing, or retyping requires incrementing `SchemaVersion`.
6. **UTC always.** ISO 8601, millisecond precision, `Z` suffix.

## Envelope

| Field | Type | Null? | Description |
|---|---|---|---|
| `SchemaVersion` | int | no | Currently `1`. |
| `UtcTime` | string | no | ETW event timestamp, ISO 8601 UTC, ms precision. |
| `Hostname` | string | no | Machine emitting the event. |
| `Module` | string | no | Emitting module: `ldap`, `rpc`, `clr`. |
| `EventType` | string | no | Module-scoped event name, e.g. `LdapSearch`. |
| `Provider` | string | no | ETW provider name. |
| `ProviderEventId` | int | no | Raw provider event ID, for traceability. |
| `ProcessId` | int | no | PID from the ETW event header. |
| `ThreadId` | int | yes | TID from the ETW event header. |
| `ProcessGuid` | string | yes | Sysmon-style process identity. Null if unresolvable. |
| `Image` | string | yes | Full path of the emitting process. Enrichment. |
| `User` | string | yes | `DOMAIN\user` of the emitting process. Enrichment. |

`ProviderEventId` lets an analyst trace any Filigree event back to the raw
Windows event it came from. Do not remove it.

## Payload

Nested under a key matching `Module`.

## LDAP payload — UNVERIFIED, DO NOT IMPLEMENT

The registered provider manifest on Windows 10.0.26200 declares **no event
templates**. No field names for event 30 have been verified on any machine.

Field names circulating in third-party writeups (`DistinguishedName`,
`SearchFilter`, `AttributeList`, `ScopeOfSearch`) are plausible but unconfirmed
by first-hand observation.

This section will be filled in from a live capture. See
`docs/providers/ldap-client.md` for the open questions.

## Null handling

Null enrichment fields are **omitted** from JSON rather than emitted as `null`,
to keep event size down at volume. Consumers must treat absent and null
identically.

## Versioning

`SchemaVersion` is an integer, incremented only on breaking changes. Any
breaking change requires a migration note here and a `BREAKING:` prefix on the
commit.
