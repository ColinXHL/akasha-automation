# Source derivation

Parts of the future automatic pickup and automatic dialogue implementation will be derived from BetterGI, licensed under GPL-3.0.

The initial source snapshot is pinned for provenance and future selective synchronization:

- Upstream repository: `https://github.com/babalae/better-genshin-impact.git`
- Upstream branch: `origin/main`
- Source commit: `0eb90304c4e4fa1f5cee2a4cbf68de6c8200ec94`
- BetterGI version: `0.62.1-alpha.2`
- Intended extraction scope: capture, recognition, OCR, input, automatic pickup, and automatic dialogue dependencies
- Runtime asset baseline: BetterGI `0.62.0`, release commit `92b8beab53da3a1f86d625914c10d180fb05b0cd`
- Runtime artifact: `BetterGI_v0.62.0.7z`, official release URL and SHA-256 `11ccb62b7580dfdf15950300415cbde57181e5352dd817040bef2f9bc58bbb89`
- Upstream synchronization policy: selective, release-gated synchronization of AutoPick, AutoSkip, required recognition infrastructure, templates, configuration data, and models

## Imported runtime configuration

The following files were copied byte-for-byte from an installed BetterGI `0.62.0` distribution on 2026-07-14. Their source and target relative paths are identical below `Assets`:

| BetterGI path | SHA-256 | Entries | Unique entries |
|---|---|---:|---:|
| `Assets/Config/Pick/default_pick_black_lists.json` | `1129650653eed1ec7e81676b3f616895feb9433ab616efc98ac360232c7e7ea9` | 4914 | 4891 |
| `Assets/Config/Skip/default_pause_options.json` | `212962f57e0bb0c04d9c3af062be53ddd929573f0399bc29b4476ec646f2ef65` | 66 | 61 |
| `Assets/Config/Skip/pause_options.json` | `fcc7d1e985862f0e3b0cc59cad7312642f7e96a318a73fc7646c093701a08b5b` | 5 | 5 |
| `Assets/Config/Skip/select_options.json` | `8585ca3368566a6efe15ef52a816494ac2469470d7ac3b806d3d329cb4b36e88` | 1 | 1 |

The authoritative machine-readable mapping is `upstream/bettergi/manifest.json`; `upstream/bettergi/hashes.json` is the package integrity inventory. No content changes were made to these four files. On 2026-07-14, the official release archive size and SHA-256 were verified, then all four declared files were selectively extracted from its `BetterGI/` archive root and matched the committed files byte-for-byte.

Future extraction work must add exact copied source files, models, copyright notices, material changes, and synchronization decisions here.
