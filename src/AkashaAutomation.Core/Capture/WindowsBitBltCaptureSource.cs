using System.ComponentModel;
using System.Runtime.InteropServices;
using AkashaAutomation.Core.Abstractions;
using OpenCvSharp;

namespace AkashaAutomation.Core.Capture;

public sealed class WindowsBitBltCaptureSource(
    IGameWindowLocator windowLocator,
    IClock clock) : ICaptureSource
{
    private long _sequence;
    private bool _disposed;

    public async ValueTask<CapturedFrame?> CaptureAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var window = await windowLocator.LocateAsync(cancellationToken).ConfigureAwait(false);
        if (window is null)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var image = CaptureClientArea(window.Handle, window.ClientSize);
        return CapturedFrame.TakeOwnership(
            image,
            Interlocked.Increment(ref _sequence),
            clock.UtcNow,
            $"bitblt:{window.ProcessId}");
    }

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        return ValueTask.CompletedTask;
    }

    private static Mat CaptureClientArea(nint windowHandle, CaptureSize clientSize)
    {
        var origin = new NativePoint();
        if (!NativeMethods.ClientToScreen(windowHandle, ref origin))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to locate the game client area on screen.");
        }

        var screenDc = NativeMethods.GetDC(nint.Zero);
        if (screenDc == nint.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to acquire the desktop device context.");
        }

        nint memoryDc = nint.Zero;
        nint bitmap = nint.Zero;
        nint previousBitmap = nint.Zero;
        try
        {
            memoryDc = NativeMethods.CreateCompatibleDC(screenDc);
            if (memoryDc == nint.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to create a capture device context.");
            }

            bitmap = NativeMethods.CreateCompatibleBitmap(screenDc, clientSize.Width, clientSize.Height);
            if (bitmap == nint.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to create a capture bitmap.");
            }

            previousBitmap = NativeMethods.SelectObject(memoryDc, bitmap);
            if (previousBitmap == nint.Zero || previousBitmap == new nint(-1))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to select the capture bitmap.");
            }

            if (!NativeMethods.BitBlt(
                    memoryDc,
                    0,
                    0,
                    clientSize.Width,
                    clientSize.Height,
                    screenDc,
                    origin.X,
                    origin.Y,
                    NativeMethods.SourceCopy | NativeMethods.CaptureLayeredWindows))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to capture the game client area.");
            }

            NativeMethods.SelectObject(memoryDc, previousBitmap);
            previousBitmap = nint.Zero;

            using var bgra = new Mat(clientSize.Height, clientSize.Width, MatType.CV_8UC4);
            var bitmapInfo = NativeBitmapInfo.Create(clientSize.Width, clientSize.Height);
            var rows = NativeMethods.GetDIBits(
                memoryDc,
                bitmap,
                0,
                (uint)clientSize.Height,
                bgra.Data,
                ref bitmapInfo,
                NativeMethods.DibRgbColors);
            if (rows != clientSize.Height)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to read the captured game bitmap.");
            }

            return bgra.CvtColor(ColorConversionCodes.BGRA2BGR);
        }
        finally
        {
            if (previousBitmap != nint.Zero && memoryDc != nint.Zero)
            {
                NativeMethods.SelectObject(memoryDc, previousBitmap);
            }

            if (bitmap != nint.Zero)
            {
                NativeMethods.DeleteObject(bitmap);
            }

            if (memoryDc != nint.Zero)
            {
                NativeMethods.DeleteDC(memoryDc);
            }

            NativeMethods.ReleaseDC(nint.Zero, screenDc);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        internal int X;
        internal int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeBitmapInfoHeader
    {
        internal uint Size;
        internal int Width;
        internal int Height;
        internal ushort Planes;
        internal ushort BitCount;
        internal uint Compression;
        internal uint SizeImage;
        internal int XPixelsPerMeter;
        internal int YPixelsPerMeter;
        internal uint ColorsUsed;
        internal uint ColorsImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeBitmapInfo
    {
        internal NativeBitmapInfoHeader Header;
        internal uint Colors;

        internal static NativeBitmapInfo Create(int width, int height) =>
            new()
            {
                Header = new NativeBitmapInfoHeader
                {
                    Size = (uint)Marshal.SizeOf<NativeBitmapInfoHeader>(),
                    Width = width,
                    Height = -height,
                    Planes = 1,
                    BitCount = 32,
                    Compression = NativeMethods.BitmapCompressionRgb,
                },
            };
    }

    private static class NativeMethods
    {
        internal const uint SourceCopy = 0x00CC0020;
        internal const uint CaptureLayeredWindows = 0x40000000;
        internal const uint DibRgbColors = 0;
        internal const uint BitmapCompressionRgb = 0;

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern nint GetDC(nint window);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int ReleaseDC(nint window, nint deviceContext);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ClientToScreen(nint window, ref NativePoint point);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern nint CreateCompatibleDC(nint deviceContext);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern nint CreateCompatibleBitmap(nint deviceContext, int width, int height);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern nint SelectObject(nint deviceContext, nint value);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(nint value);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteDC(nint deviceContext);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BitBlt(
            nint destination,
            int destinationX,
            int destinationY,
            int width,
            int height,
            nint source,
            int sourceX,
            int sourceY,
            uint operation);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern int GetDIBits(
            nint deviceContext,
            nint bitmap,
            uint startScan,
            uint scanLines,
            nint bits,
            ref NativeBitmapInfo bitmapInfo,
            uint usage);
    }
}
