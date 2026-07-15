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
