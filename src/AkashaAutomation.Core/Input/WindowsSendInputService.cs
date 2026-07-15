using System.ComponentModel;
using System.Runtime.InteropServices;
using AkashaAutomation.Core.Abstractions;
using AkashaAutomation.Core.GameContext;

namespace AkashaAutomation.Core.Input;

public sealed class WindowsSendInputService : IInputService
{
    private readonly object _gate = new();
    private readonly HashSet<ushort> _pressedKeys = [];
    private bool _disposed;

    public ValueTask ExecuteAsync(
        InputActionGroup actions,
        GameContextSnapshot context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actions);
        ArgumentNullException.ThrowIfNull(context);
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.IsGameForeground || context.Window?.Handle != NativeMethods.GetForegroundWindow())
        {
            throw new InvalidOperationException("SendInput is allowed only while the located game window is foreground.");
        }

        lock (_gate)
        {
            foreach (var action in actions.Actions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Execute(action);
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask ReleaseAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            foreach (var key in _pressedKeys.ToArray())
            {
                SendKeyboard(key, keyUp: true);
            }

            _pressedKeys.Clear();
            SendMouse(NativeMethods.MouseEventLeftUp);
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await ReleaseAllAsync().ConfigureAwait(false);
        _disposed = true;
    }

    private void Execute(InputAction action)
    {
        switch (action.Kind)
        {
            case InputActionKind.KeyDown:
                SendKeyboard(action.VirtualKey, keyUp: false);
                _pressedKeys.Add(action.VirtualKey);
                break;
            case InputActionKind.KeyUp:
                SendKeyboard(action.VirtualKey, keyUp: true);
                _pressedKeys.Remove(action.VirtualKey);
                break;
            case InputActionKind.KeyPress:
                SendKeyboard(action.VirtualKey, keyUp: false);
                SendKeyboard(action.VirtualKey, keyUp: true);
                break;
            case InputActionKind.MouseMove:
                SendMouseMove(action.X, action.Y);
                break;
            case InputActionKind.MouseLeftClick:
                SendMouse(NativeMethods.MouseEventLeftDown);
                SendMouse(NativeMethods.MouseEventLeftUp);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action.Kind, "Unknown input action.");
        }
    }

    private static void SendKeyboard(ushort virtualKey, bool keyUp)
    {
        if (virtualKey == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(virtualKey));
        }

        var input = NativeInput.Keyboard(virtualKey, keyUp ? NativeMethods.KeyEventKeyUp : 0);
        Send([input]);
    }

    private static void SendMouse(uint flags)
    {
        var input = NativeInput.Mouse(0, 0, flags);
        Send([input]);
    }

    private static void SendMouseMove(int x, int y)
    {
        var width = NativeMethods.GetSystemMetrics(0);
        var height = NativeMethods.GetSystemMetrics(1);
        if (width <= 1 || height <= 1)
        {
            throw new InvalidOperationException("The primary display dimensions are unavailable.");
        }

        var absoluteX = (int)Math.Clamp(Math.Round(x * 65535d / (width - 1)), 0, 65535);
        var absoluteY = (int)Math.Clamp(Math.Round(y * 65535d / (height - 1)), 0, 65535);
        var input = NativeInput.Mouse(
            absoluteX,
            absoluteY,
            NativeMethods.MouseEventMove | NativeMethods.MouseEventAbsolute);
        Send([input]);
    }

    private static void Send(NativeInput[] inputs)
    {
        var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeInput>());
        if (sent != inputs.Length)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "SendInput did not submit every requested input.");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeInput
    {
        internal uint Type;
        internal NativeInputUnion Data;

        internal static NativeInput Keyboard(ushort virtualKey, uint flags) =>
            new()
            {
                Type = NativeMethods.InputKeyboard,
                Data = new NativeInputUnion
                {
                    Keyboard = new NativeKeyboardInput { VirtualKey = virtualKey, Flags = flags },
                },
            };

        internal static NativeInput Mouse(int x, int y, uint flags) =>
            new()
            {
                Type = NativeMethods.InputMouse,
                Data = new NativeInputUnion
                {
                    Mouse = new NativeMouseInput { X = x, Y = y, Flags = flags },
                },
            };
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct NativeInputUnion
    {
        [FieldOffset(0)] internal NativeMouseInput Mouse;
        [FieldOffset(0)] internal NativeKeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMouseInput
    {
        internal int X;
        internal int Y;
        internal uint MouseData;
        internal uint Flags;
        internal uint Time;
        internal nuint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeKeyboardInput
    {
        internal ushort VirtualKey;
        internal ushort ScanCode;
        internal uint Flags;
        internal uint Time;
        internal nuint ExtraInfo;
    }

    private static class NativeMethods
    {
        internal const uint InputMouse = 0;
        internal const uint InputKeyboard = 1;
        internal const uint KeyEventKeyUp = 0x0002;
        internal const uint MouseEventMove = 0x0001;
        internal const uint MouseEventLeftDown = 0x0002;
        internal const uint MouseEventLeftUp = 0x0004;
        internal const uint MouseEventAbsolute = 0x8000;

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint SendInput(uint inputCount, NativeInput[] inputs, int inputSize);

        [DllImport("user32.dll")]
        internal static extern nint GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern int GetSystemMetrics(int index);
    }
}
