using AkashaAutomation.BetterGiPort.Compatibility.AutoPick;

namespace AkashaAutomation.DevHost;

public sealed record DevHostOptions(
    string PickKey,
    int IntervalMilliseconds,
    bool BlacklistEnabled,
    bool ShowAllFrames,
    IReadOnlyList<string> UserExactBlacklist,
    IReadOnlyList<string> UserFuzzyBlacklist,
    IReadOnlyList<string> UserWhitelist)
{
    public static DevHostOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var pickKey = "F";
        var interval = 100;
        var blacklistEnabled = true;
        var showAllFrames = false;
        var exactBlacklist = new List<string>();
        var fuzzyBlacklist = new List<string>();
        var whitelist = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "--pick-key":
                    pickKey = ReadValue(args, ref index, argument);
                    break;
                case "--interval-ms":
                    var value = ReadValue(args, ref index, argument);
                    if (!int.TryParse(value, out interval) || interval is < 25 or > 2000)
                    {
                        throw new ArgumentException("--interval-ms must be between 25 and 2000.");
                    }

                    break;
                case "--no-blacklist":
                    blacklistEnabled = false;
                    break;
                case "--show-all":
                    showAllFrames = true;
                    break;
                case "--exact-blacklist":
                    exactBlacklist.Add(ReadValue(args, ref index, argument));
                    break;
                case "--fuzzy-blacklist":
                    fuzzyBlacklist.Add(ReadValue(args, ref index, argument));
                    break;
                case "--whitelist":
                    whitelist.Add(ReadValue(args, ref index, argument));
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{argument}'.");
            }
        }

        return new DevHostOptions(
            BetterGiAutoPickRecognizer.NormalizePickKey(pickKey),
            interval,
            blacklistEnabled,
            showAllFrames,
            exactBlacklist,
            fuzzyBlacklist,
            whitelist);
    }

    public static string Usage => """
        AkashaAutomation.DevHost — AutoPick observe-only real-game host

        Usage:
          AkashaAutomation.DevHost.exe [options]

        Options:
          --pick-key E|F|G          Interaction key template and virtual key (default: F)
          --interval-ms 25..2000   Delay between frames (default: 100)
          --no-blacklist           Disable default and user blacklists
          --exact-blacklist TEXT   Add a user exact-blacklist entry; repeatable
          --fuzzy-blacklist TEXT   Add a user fuzzy-blacklist entry; repeatable
          --whitelist TEXT         Add a user whitelist entry; repeatable
          --show-all               Print every frame instead of changes only
          --help                   Show this help

        Safety:
          This executable is permanently observe-only and contains no real input service.
        """;

    private static string ReadValue(string[] args, ref int index, string argument)
    {
        if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
        {
            throw new ArgumentException($"{argument} requires a value.");
        }

        return args[index];
    }
}
