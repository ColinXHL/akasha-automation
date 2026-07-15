namespace AkashaAutomation.Core.GameContext;

public sealed record GameContextSnapshot(
    DateTimeOffset ObservedAtUtc,
    GameWindowInfo? Window)
{
    public bool HasGameWindow => Window is not null;

    public bool IsGameForeground => Window?.IsForeground == true;
}
