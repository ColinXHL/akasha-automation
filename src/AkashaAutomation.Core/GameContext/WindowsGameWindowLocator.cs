using System.Diagnostics;
using System.Runtime.InteropServices;
using AkashaAutomation.Core.Abstractions;
using AkashaAutomation.Core.Capture;

namespace AkashaAutomation.Core.GameContext;

public sealed class WindowsGameWindowLocator : IGameWindowLocator
{
    public static readonly IReadOnlyList<string> DefaultProcessNames = ["GenshinImpact", "YuanShen"];

    private readonly IReadOnlyList<string> _processNames;

    public WindowsGameWindowLocator(IEnumerable<string>? processNames = null)
    {
        _processNames = (processNames ?? DefaultProcessNames)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (_processNames.Count == 0)
        {
            throw new ArgumentException("At least one game process name is required.", nameof(processNames));
        }
    }

    public ValueTask<GameWindowInfo?> LocateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var foreground = NativeMethods.GetForegroundWindow();
        foreach (var processName in _processNames)
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                using (process)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var handle = process.MainWindowHandle;
                        if (handle == nint.Zero || !NativeMethods.IsWindowVisible(handle) || NativeMethods.IsIconic(handle))
                        {
                            continue;
                        }

                        if (!NativeMethods.GetClientRect(handle, out var rect) || rect.Width <= 0 || rect.Height <= 0)
                        {
                            continue;
                        }

                        return ValueTask.FromResult<GameWindowInfo?>(
                            new GameWindowInfo(
                                handle,
                                process.Id,
                                process.ProcessName,
                                process.MainWindowTitle,
                                new CaptureSize(rect.Width, rect.Height),
                                handle == foreground));
                    }
                    catch (InvalidOperationException)
                    {
                        // The process exited while it was being inspected.
                    }
                }
            }
        }

        return ValueTask.FromResult<GameWindowInfo?>(null);
    }

    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern nint GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindowVisible(nint window);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsIconic(nint window);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetClientRect(nint window, out NativeRect rect);

        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeRect
        {
            internal int Left;
            internal int Top;
            internal int Right;
            internal int Bottom;

            internal readonly int Width => Right - Left;
            internal readonly int Height => Bottom - Top;
        }
    }
}
