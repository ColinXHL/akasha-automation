namespace AkashaAutomation.Core.Input;

public enum InputActionKind
{
    KeyDown,
    KeyUp,
    KeyPress,
    MouseMove,
    MouseLeftClick,
}

public sealed record InputAction(
    InputActionKind Kind,
    ushort VirtualKey = 0,
    int X = 0,
    int Y = 0)
{
    public static InputAction KeyPress(ushort virtualKey) =>
        new(InputActionKind.KeyPress, virtualKey);

    public static InputAction KeyDown(ushort virtualKey) =>
        new(InputActionKind.KeyDown, virtualKey);

    public static InputAction KeyUp(ushort virtualKey) =>
        new(InputActionKind.KeyUp, virtualKey);

    public static InputAction MouseMove(int x, int y) =>
        new(InputActionKind.MouseMove, X: x, Y: y);

    public static InputAction MouseLeftClick() => new(InputActionKind.MouseLeftClick);
}
