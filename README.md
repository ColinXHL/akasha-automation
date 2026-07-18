# Akasha Automation

> [!IMPORTANT]
> This repository is archived. Development, issues, and namespaced Releases moved to
> [AkashaPlugins/plugins/akasha-genshin-automation](https://github.com/ColinXHL/akasha-plugins/tree/main/plugins/akasha-genshin-automation).
> Existing AkashaNavigator installations are adopted by the official AkashaPlugins catalog
> without replacing user configuration or enabling automatic updates.

Akasha Automation is the out-of-process game automation companion for AkashaNavigator.

The historical repository was intentionally independent from AkashaNavigator:

- AkashaNavigator owns the generic plugin and companion-process hosting API.
- The Akasha plugin owns settings, hotkeys, and status presentation.
- `AkashaAutomation.Worker` owns capture, recognition, OCR, scheduling, and input.

The initial feature scope is automatic pickup and automatic dialogue derived from a pinned BetterGI source snapshot and matching runtime assets. The port is maintained behind an Akasha adapter boundary and may selectively adopt relevant BetterGI fixes at explicit release checkpoints; it does not continuously mirror upstream `main`.

When AkashaNavigator starts the production Worker, it passes the plugin data root in
`AKASHA_PLUGIN_DATA_DIR`. If `pick-blacklist/current.json` below that root is present
and valid, AutoPick uses it instead of the packaged BetterGI default blacklist.
Missing or invalid remote data falls back to the packaged list; user exact blacklist
entries are always merged afterward. The Worker reads this file only at startup.

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

## Release package

Build and verify the installable plugin ZIP:

```powershell
.\scripts\Publish-Plugin.ps1
```

The script runs the test suite, publishes the framework-dependent `win-x64` Worker, includes the pinned BetterGI assets and legal notices, creates `package-manifest.json`, then extracts and verifies the finished ZIP. Outputs are written to `artifacts/release/`.

New Akasha Automation versions are released from the GPL-3.0 AkashaPlugins repository and installed through the official catalog.

The historical workflows remain only as migration provenance. Do not create new tags
or Releases in this archived repository.

Real input is enabled only by the production Worker under AkashaNavigator companion supervision. The permanent DevHost remains observe-only, while the separate administrator-only LiveTestHost is a local acceptance tool with foreground enforcement and a global emergency stop.
