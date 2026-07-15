using AkashaAutomation.Core.Capture;
using OpenCvSharp;

namespace AkashaAutomation.BetterGiPort.Upstream.AutoPick;

public static class BetterGiTextRectExtractor
{
    public static RegionOfInterest Refine(CapturedFrame frame, RegionOfInterest textRegion)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (!textRegion.FitsWithin(frame.Size))
        {
            throw new ArgumentOutOfRangeException(nameof(textRegion));
        }

        var width = frame.UseImage(source =>
        {
            using var text = new Mat(source, new Rect(textRegion.X, textRegion.Y, textRegion.Width, textRegion.Height));
            using var gray = new Mat();
            if (text.Channels() == 3)
            {
                Cv2.CvtColor(text, gray, ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                text.CopyTo(gray);
            }

            using var binary = new Mat();
            Cv2.Threshold(gray, binary, 160, 255, ThresholdTypes.Binary);
            using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
            Cv2.Erode(binary, binary, kernel, iterations: 1);
            Cv2.Dilate(binary, binary, kernel, iterations: 2);
            return ProjectWidth(binary);
        });

        if (width <= 5)
        {
            return textRegion;
        }

        return new RegionOfInterest(
            textRegion.X,
            textRegion.Y,
            Math.Min(textRegion.Width, width + 5),
            textRegion.Height);
    }

    private static int ProjectWidth(Mat binary)
    {
        using var projection = new Mat();
        Cv2.Reduce(binary, projection, 0, ReduceTypes.Sum, MatType.CV_32S);
        projection.GetArray(out int[] columnSums);
        var gapCount = 0;
        var lastNonEmpty = -1;
        for (var x = 0; x < columnSums.Length; x++)
        {
            if (columnSums[x] > 0)
            {
                lastNonEmpty = x;
                gapCount = 0;
                continue;
            }

            gapCount++;
            if (gapCount > 30)
            {
                break;
            }
        }

        return Math.Max(0, lastNonEmpty);
    }
}
