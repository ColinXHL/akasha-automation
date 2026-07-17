using System.Text.Json;

namespace AkashaAutomation.Worker.IntegrationTests;

public sealed class PluginPackageContractTests
{
    [Fact]
    public void PluginManifest_DeclaresFixedCompanionExecutableAndProtocol()
    {
        var repositoryRoot = FindRepositoryRoot();
        var pluginRoot = Path.Combine(repositoryRoot, "plugin", "akasha-genshin-automation");
        var manifestPath = Path.Combine(pluginRoot, "plugin.json");
        var mainPath = Path.Combine(pluginRoot, "main.js");

        Assert.True(File.Exists(manifestPath), $"Plugin manifest was not found: {manifestPath}");
        Assert.True(File.Exists(mainPath), $"Plugin entry point was not found: {mainPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;
        var permissions = root.GetProperty("permissions")
            .EnumerateArray()
            .Select(value => value.GetString())
            .ToArray();
        var companion = root.GetProperty("companion");

        Assert.Contains("companion", permissions);
        Assert.Equal(
            "worker/win-x64/AkashaAutomation.Worker.exe",
            companion.GetProperty("executable").GetString());
        Assert.Equal(1, companion.GetProperty("protocolVersion").GetInt32());
        Assert.Equal("plugin", companion.GetProperty("lifetime").GetString());
        Assert.True(companion.GetProperty("singleInstance").GetBoolean());
    }

    [Fact]
    public void SettingsUi_DefaultsAndMainScript_ShouldStayInSyncWithManifest()
    {
        var repositoryRoot = FindRepositoryRoot();
        var pluginRoot = Path.Combine(repositoryRoot, "plugin", "akasha-genshin-automation");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(pluginRoot, "plugin.json")));
        using var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(pluginRoot, "settings_ui.json")));
        var defaults = manifest.RootElement.GetProperty("defaultConfig");
        var keyedItems = new List<(string Key, string DefaultJson)>();

        foreach (var section in settings.RootElement.GetProperty("sections").EnumerateArray())
        {
            CollectKeyedItems(section.GetProperty("items"), keyedItems);
        }

        Assert.Equal(keyedItems.Count, keyedItems.Select(item => item.Key).Distinct(StringComparer.Ordinal).Count());
        foreach (var item in keyedItems)
        {
            Assert.True(defaults.TryGetProperty(item.Key, out var manifestDefault), $"Missing manifest default: {item.Key}");
            Assert.Equal(item.DefaultJson, manifestDefault.GetRawText());
        }

        var script = File.ReadAllText(Path.Combine(pluginRoot, "main.js"));
        Assert.Contains("features.autoPick.setOptions", script, StringComparison.Ordinal);
        Assert.Contains("features.autoDialogue.setOptions", script, StringComparison.Ordinal);
        Assert.Contains("buildAutoPickOptions", script, StringComparison.Ordinal);
        Assert.Contains("buildAutoDialogueOptions", script, StringComparison.Ordinal);
    }

    private static void CollectKeyedItems(
        JsonElement items,
        ICollection<(string Key, string DefaultJson)> destination)
    {
        foreach (var item in items.EnumerateArray())
        {
            if (item.TryGetProperty("key", out var key))
            {
                destination.Add((key.GetString()!, item.GetProperty("default").GetRawText()));
            }

            if (item.TryGetProperty("items", out var children))
            {
                CollectKeyedItems(children, destination);
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "AkashaAutomation.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
               ?? throw new DirectoryNotFoundException("AkashaAutomation repository root not found.");
    }
}
