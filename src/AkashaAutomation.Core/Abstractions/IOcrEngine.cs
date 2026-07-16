using AkashaAutomation.Core.Capture;
using AkashaAutomation.Core.Ocr;

namespace AkashaAutomation.Core.Abstractions;

public interface IOcrEngine : IAsyncDisposable
{
    ValueTask<OcrResult> RecognizeAsync(
        CapturedFrame frame,
        RegionOfInterest? region = null,
        CancellationToken cancellationToken = default);

    ValueTask<OcrResult> RecognizeSingleLineAsync(
        CapturedFrame frame,
        RegionOfInterest region,
        CancellationToken cancellationToken = default) =>
        RecognizeAsync(frame, region, cancellationToken);
}
