# Third-party notices

This inventory must be completed before the first distributable plugin package is produced.

## BetterGI

Akasha Automation currently includes four unmodified configuration list files, the minimal PP-OCRv4 runtime model set, and six AutoPick recognition templates from BetterGI `0.62.0`:

- `Assets/Config/Pick/default_pick_black_lists.json`
- `Assets/Config/Skip/default_pause_options.json`
- `Assets/Config/Skip/pause_options.json`
- `Assets/Config/Skip/select_options.json`
- `Assets/Model/PaddleOCR/README.md`
- `Assets/Model/PaddleOCR/test_pp_ocr.png`
- PP-OCRv4 mobile detection `inference.yml` and `slim.onnx`
- PP-OCRv4 mobile recognition `inference.yml` and `slim.onnx`
- `Assets/Recognition/AutoPick/1920x1080/E.png`
- `Assets/Recognition/AutoPick/1920x1080/F.png`
- `Assets/Recognition/AutoPick/1920x1080/G.png`
- `Assets/Recognition/AutoPick/1920x1080/L.png`
- `Assets/Recognition/AutoPick/1920x1080/icon_settings.png`
- `Assets/Recognition/AutoPick/1920x1080/icon_option.png`

Phase 4 also includes translated AutoPick behavior derived from BetterGI's `AutoPickTrigger`, `AutoPickConfig`, `TextRectExtractor`, `AutoPickAssets` and recognition declarations. BetterGI is copyright its contributors and licensed under GPL-3.0. The upstream repository is `https://github.com/babalae/better-genshin-impact`. Source and release pins, file mappings, hashes, local translation decisions, and list statistics are recorded in `DERIVATION.md` and `upstream/bettergi/`.

The model README identifies PaddleOCR inference models converted through Paddle2ONNX. Their upstream license texts and the exact conversion provenance must be included in the Phase 7 release license review; the BetterGI archive and every copied file are already pinned by SHA-256.

## Runtime libraries introduced in Phase 3

- OpenCvSharp4 `4.11.0.20250507` — Apache-2.0 as declared by the NuGet package; native OpenCV notices must accompany the release.
- Microsoft.ML.OnnxRuntime `1.21.0` — MIT, copyright Microsoft Corporation.
- YamlDotNet `16.3.0` — MIT.
- SharpDX.Direct3D11 `4.2.0` and its SharpDX dependencies — used only by the Windows Graphics Capture adapter; include their upstream license notice in the release package.

Remaining expected categories include:

- Fischless.GameCapture.
- Fischless.WindowsInput.
- Windows interop libraries.

No BetterGI AutoSkip feature source, Yap model, or VAD model has been copied yet. Each must be added to this notice as it is imported.
