# Akasha Automation

Akasha Automation is the out-of-process game automation companion for AkashaNavigator.

The repository is intentionally independent from AkashaNavigator:

- AkashaNavigator owns the generic plugin and companion-process hosting API.
- The Akasha plugin owns settings, hotkeys, and status presentation.
- `AkashaAutomation.Worker` owns capture, recognition, OCR, scheduling, and input.

The initial feature scope is automatic pickup and automatic dialogue derived from a pinned BetterGI source snapshot and matching runtime assets. The port is maintained behind an Akasha adapter boundary and may selectively adopt relevant BetterGI fixes at explicit release checkpoints; it does not continuously mirror upstream `main`.

See [docs/design.md](docs/design.md) for the architecture, [docs/implementation-plan.md](docs/implementation-plan.md) for the staged implementation plan, [docs/companion-protocol.md](docs/companion-protocol.md) for the Worker interoperability contract, and [docs/devhost.md](docs/devhost.md) for independent observe-only and real-input testing.

## Build

```powershell
dotnet build
dotnet test
```

## Independent real-game testing

Run the observe-only DevHost without AkashaNavigator:

```powershell
dotnet run --project .\src\AkashaAutomation.DevHost\AkashaAutomation.DevHost.csproj --configuration Release -- --pick-key F
```

The DevHost uses the real capture, template and PaddleOCR pipeline but contains no real input service. Press `Ctrl+C` to stop safely.

Observe AutoDialogue instead:

```powershell
dotnet run --project .\src\AkashaAutomation.DevHost\AkashaAutomation.DevHost.csproj --configuration Release -- --feature auto-dialogue --option-strategy first
```

After the observe-only checks pass, publish the separate `AkashaAutomation.LiveTestHost` and run it without arguments from an administrator terminal. It exposes only AutoPick and AutoDialogue switches, then runs both selected features on BetterGI's 50 ms cadence; `Ctrl+C` stops the active session and `Ctrl+Alt+F12` is the foreground-game emergency stop. Follow [docs/devhost.md](docs/devhost.md).

## BetterGI assets

Pinned BetterGI runtime configuration is committed under `AkashaAutomation.BetterGiPort` and is copied into Worker build and publish output. Normal builds never read a BetterGI installation and never download a BetterGI release.

To deliberately refresh the pinned files from a verified unpacked distribution or archive:

```powershell
.\scripts\Import-BetterGiAssets.ps1 -Source 'C:\Program Files\BetterGI'
.\scripts\Import-BetterGiAssets.ps1 -Source '.\BetterGI_v0.62.0.7z' -VerifyOnly
```

The import fails if an artifact hash, file hash, JSON structure, entry count, path boundary, or link check differs from `upstream/bettergi/manifest.json`.

## Repository status

Phase 0 through Phase 5 are complete. The Worker includes guarded lifecycle management, capture, PaddleOCR, template recognition, AutoPick and AutoDialogue rules, process-loopback Silero VAD with fixed-delay fallback, a single-frame scheduler, Input Arbiter and stable status reporting. AkashaNavigator supervises the companion process, while the separate DevHost allows either feature pipeline to be tested against the real game without Navigator.

Run the real cross-repository smoke test from this repository when AkashaNavigator is in the sibling directory:

```powershell
.\scripts\Test-NavigatorCompanion.ps1
```

Real input remains unregistered in both the production Worker and permanent observe-only DevHost. The separate administrator-only LiveTestHost is a local acceptance tool with foreground enforcement and a global emergency stop. Plugin settings and release packaging remain for later phases.
