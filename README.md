# Akasha Automation

Akasha Automation is the out-of-process game automation companion for AkashaNavigator.

The repository is intentionally independent from AkashaNavigator:

- AkashaNavigator owns the generic plugin and companion-process hosting API.
- The Akasha plugin owns settings, hotkeys, and status presentation.
- `AkashaAutomation.Worker` owns capture, recognition, OCR, scheduling, and input.

The initial feature scope is automatic pickup and automatic dialogue derived from a pinned BetterGI source snapshot and matching runtime assets. The port is maintained behind an Akasha adapter boundary and may selectively adopt relevant BetterGI fixes at explicit release checkpoints; it does not continuously mirror upstream `main`.

See [docs/design.md](docs/design.md) for the architecture, [docs/implementation-plan.md](docs/implementation-plan.md) for the staged implementation plan, and [docs/companion-protocol.md](docs/companion-protocol.md) for the Worker interoperability contract.

## Build

```powershell
dotnet build
dotnet test
```

## BetterGI assets

Pinned BetterGI runtime configuration is committed under `AkashaAutomation.BetterGiPort` and is copied into Worker build and publish output. Normal builds never read a BetterGI installation and never download a BetterGI release.

To deliberately refresh the pinned files from a verified unpacked distribution or archive:

```powershell
.\scripts\Import-BetterGiAssets.ps1 -Source 'C:\Program Files\BetterGI'
.\scripts\Import-BetterGiAssets.ps1 -Source '.\BetterGI_v0.62.0.7z' -VerifyOnly
```

The import fails if an artifact hash, file hash, JSON structure, entry count, path boundary, or link check differs from `upstream/bettergi/manifest.json`.

## Repository status

Phase 0, the Phase 1 Companion Echo vertical slice, and Phase 2 Worker hosting are complete. The Worker now uses .NET Generic Host as its composition root and includes a guarded lifecycle state machine, latched emergency stop, bounded command queue, idempotent ordered shutdown, stable subsystem status, and structured rolling logs. AkashaNavigator validates the fixed companion manifest, confirms the high-risk permission, supervises a per-plugin process in a Job Object, exposes the restricted JavaScript API, and stops the Worker before plugin teardown or file replacement. The plugin skeleton starts the Worker and verifies Echo on load.

Run the real cross-repository smoke test from this repository when AkashaNavigator is in the sibling directory:

```powershell
.\scripts\Test-NavigatorCompanion.ps1
```

Phase 3 capture abstractions, recognition, OCR, virtual time, input arbitration, and frame replay remain unimplemented. No real input implementation is registered or enabled.
