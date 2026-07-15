using OpenCvSharp;

namespace AkashaAutomation.Core.Ocr;

public interface IPaddleOcrSession : IDisposable
{
    OcrResult Recognize(Mat image, CancellationToken cancellationToken = default);
}

public interface IPaddleOcrSessionFactory
{
    IPaddleOcrSession Create(PaddleOcrModelOptions options);
}
