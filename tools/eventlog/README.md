# Event Log channel — NOT CURRENTLY USED

`Filigree.man` describes a manifest-registered ETW channel
(`Filigree/Operational`). This is the intended long-term design but is **not**
what the current sink does.

## Current implementation

`EventLogSink` uses `System.Diagnostics.EventLog`, which creates a classic
event log source at runtime. No manifest, no resource DLL, no Windows SDK.
Creating the source requires administrator once; writing afterwards does not.

## Why the manifest is still here

The classic API is slower per write and produces a classic log rather than a
modern channel. Migrating means using `EventSource` with an explicit provider
GUID plus manifest registration via `wevtutil im`, and shipping a resource DLL
built with `mc.exe`. Deferred — the classic sink is adequate at current volume.
