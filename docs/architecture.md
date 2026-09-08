# Architecture

## Pipeline

ETW session -> module parser -> normalized event -> sink

`Filigree.Core` owns sessions, the schema, and sinks. It knows nothing about any
specific provider. Each `Filigree.Modules.*` project owns one provider. `Filigree.Host`
wires them together.

## Process enrichment

ETW events carry PID only. No image path, no user, no hostname. Enrichment is
Filigree's job.

**Target design:** a process cache fed by `Microsoft-Windows-Kernel-Process`,
recording image path, command line and start time at process start, removing on
exit. Start time disambiguates recycled PIDs and is required to construct
`ProcessGuid`.

**v1 (commits 4-5):** naive `Process.GetProcessById` lookup. Returns null for
processes that already exited, and can return the *wrong* process after PID
recycling. `Image` and `User` are nullable in the schema for this reason.

**Commit 6:** replace with the cache. Built separately so enrichment bugs stay
isolated from consumer bugs.

TraceEvent's `data.ProcessName` is a fallback only. It resolves inconsistently
and yields a bare name rather than a full path.

## Elevation

Real-time ETW sessions require administrator. `Filigree.Host` ships with an
app.manifest requesting requireAdministrator before release.

## Platform

x64 only. ETW property parsing has known gaps across the WOW64 boundary.
