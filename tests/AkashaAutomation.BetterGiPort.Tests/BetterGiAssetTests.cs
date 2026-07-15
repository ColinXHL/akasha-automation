using System.Text.Json;
using System.Diagnostics;
using AkashaAutomation.BetterGiPort.Assets;

namespace AkashaAutomation.BetterGiPort.Tests;

public class BetterGiAssetTests
{
    private static readonly AssetExpectation[] Expectations =
    [
        new(BetterGiAssetPaths.DefaultPickBlacklist, "1129650653eed1ec7e81676b3f616895feb9433ab616efc98ac360232c7e7ea9", 4914, 4891),
        new(BetterGiAssetPaths.DefaultPauseOptions, "212962f57e0bb0c04d9c3af062be53ddd929573f0399bc29b4476ec646f2ef65", 66, 61),
        new(BetterGiAssetPaths.PauseOptions, "fcc7d1e985862f0e3b0cc59cad7312642f7e96a318a73fc7646c093701a08b5b", 5, 5),
        new(BetterGiAssetPaths.SelectOptions, "8585ca3368566a6efe15ef52a816494ac2469470d7ac3b806d3d329cb4b36e88", 1, 1),
    ];

    [Fact]
    public void ImportedAssets_ShouldMatchPinnedHashesAndListCounts()
    {
        var repositoryRoot = FindRepositoryRoot();
        foreach (var expectation in Expectations)
        {
            var path = Path.Combine(repositoryRoot, "src", "AkashaAutomation.BetterGiPort", expectation.RelativePath);

            BetterGiAssetIntegrity.VerifySha256(path, expectation.Sha256);
            var values = BetterGiJsonList.Load(path);

            Assert.Equal(expectation.Count, values.Count);
            Assert.Equal(expectation.UniqueCount, values.Distinct(StringComparer.Ordinal).Count());
        }
    }

    [Fact]
    public void ImportedAssets_ShouldBeCopiedToBuildOutput()
    {
        foreach (var expectation in Expectations)
        {
            var path = Path.Combine(AppContext.BaseDirectory, expectation.RelativePath);
            BetterGiAssetIntegrity.VerifySha256(path, expectation.Sha256);
        }
    }

    [Fact]
    public void HashVerification_ShouldRejectModifiedAsset()
    {
        var path = Path.Combine(Path.GetTempPath(), $"akasha-bettergi-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "[]");
            var expectedHash = BetterGiAssetIntegrity.ComputeSha256(path);
            File.AppendAllText(path, Environment.NewLine);

            Assert.Throws<InvalidDataException>(() => BetterGiAssetIntegrity.VerifySha256(path, expectedHash));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void UpstreamManifests_ShouldDescribeExactlyTheImportedAssets()
    {
        var repositoryRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(repositoryRoot, "upstream", "bettergi", "manifest.json");
        var hashesPath = Path.Combine(repositoryRoot, "upstream", "bettergi", "hashes.json");
        using var manifest = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
        using var hashes = JsonDocument.Parse(File.ReadAllBytes(hashesPath));

        var manifestAssets = manifest.RootElement.GetProperty("assets").EnumerateArray()
            .ToDictionary(
                asset => asset.GetProperty("targetPath").GetString()!,
                asset => asset.GetProperty("sha256").GetString()!,
                StringComparer.Ordinal);
        var hashAssets = hashes.RootElement.GetProperty("assets").EnumerateArray()
            .ToDictionary(
                asset => asset.GetProperty("path").GetString()!,
                asset => asset.GetProperty("sha256").GetString()!,
                StringComparer.Ordinal);
        var expectedAssets = Expectations.ToDictionary(
            expectation => $"src/AkashaAutomation.BetterGiPort/{expectation.RelativePath}",
            expectation => expectation.Sha256,
            StringComparer.Ordinal);
        var actualAssets = GetManagedAssetPaths(repositoryRoot);

        Assert.Equal(expectedAssets.OrderBy(asset => asset.Key), manifestAssets.OrderBy(asset => asset.Key));
        Assert.Equal(expectedAssets.OrderBy(asset => asset.Key), hashAssets.OrderBy(asset => asset.Key));
        Assert.Equal(expectedAssets.Keys.Order(StringComparer.Ordinal), actualAssets);

        foreach (var asset in manifestAssets)
        {
            Assert.Equal(asset.Value, hashAssets[asset.Key], ignoreCase: true);
            var actualPath = Path.Combine(repositoryRoot, asset.Key.Replace('/', Path.DirectorySeparatorChar));
            Assert.Equal(asset.Value, BetterGiAssetIntegrity.ComputeSha256(actualPath), ignoreCase: true);
        }
    }

    [Fact]
    public void OfficialReleaseArtifact_ShouldBePinned()
    {
        var repositoryRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(repositoryRoot, "upstream", "bettergi", "manifest.json");
        using var manifest = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
        var artifact = manifest.RootElement.GetProperty("runtimeArtifact");

        Assert.Equal("0.62.0", artifact.GetProperty("version").GetString());
        Assert.Equal("official-release-verified", artifact.GetProperty("status").GetString());
        Assert.Equal("92b8beab53da3a1f86d625914c10d180fb05b0cd", artifact.GetProperty("releaseCommit").GetString());
        Assert.Equal(424052950, artifact.GetProperty("size").GetInt64());
        Assert.Equal("BetterGI", artifact.GetProperty("archiveRoot").GetString());
        Assert.Equal("11ccb62b7580dfdf15950300415cbde57181e5352dd817040bef2f9bc58bbb89", artifact.GetProperty("sha256").GetString());
        Assert.Equal(
            "https://github.com/babalae/better-genshin-impact/releases/download/0.62.0/BetterGI_v0.62.0.7z",
            artifact.GetProperty("downloadUrl").GetString());
    }

    [Fact]
    public void ImportScript_ShouldRejectUndeclaredFilesInVerifyModeAndRemoveThemDuringImport()
    {
        var repositoryRoot = FindRepositoryRoot();
        var scriptPath = Path.Combine(repositoryRoot, "scripts", "Import-BetterGiAssets.ps1");
        var temporaryRoot = Path.Combine(Path.GetTempPath(), $"akasha-import-test-{Guid.NewGuid():N}");
        var sourceRoot = Path.Combine(temporaryRoot, "source");
        var sourceAsset = Path.Combine(sourceRoot, "Assets", "Config", "Pick", "default.json");
        var targetAsset = Path.Combine(
            temporaryRoot,
            "src",
            "AkashaAutomation.BetterGiPort",
            "Assets",
            "Config",
            "Pick",
            "default.json");
        var undeclaredAsset = Path.Combine(Path.GetDirectoryName(targetAsset)!, "stale.json");
        var manifestPath = Path.Combine(temporaryRoot, "upstream", "bettergi", "manifest.json");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(sourceAsset)!);
            Directory.CreateDirectory(Path.GetDirectoryName(targetAsset)!);
            Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
            File.WriteAllText(sourceAsset, "[\"one\"]");
            File.Copy(sourceAsset, targetAsset);
            File.WriteAllText(undeclaredAsset, "[]");
            var sha256 = BetterGiAssetIntegrity.ComputeSha256(sourceAsset);
            File.WriteAllText(
                manifestPath,
                JsonSerializer.Serialize(new
                {
                    schemaVersion = 1,
                    runtimeArtifact = new { sha256 = "" },
                    assets = new[]
                    {
                        new
                        {
                            sourcePath = "Assets/Config/Pick/default.json",
                            targetPath = "src/AkashaAutomation.BetterGiPort/Assets/Config/Pick/default.json",
                            sha256,
                            kind = "json-string-list",
                            count = 1,
                            uniqueCount = 1
                        }
                    }
                }));

            var verify = RunImportScript(scriptPath, sourceRoot, manifestPath, verifyOnly: true);
            Assert.NotEqual(0, verify.ExitCode);
            Assert.Contains("not declared", verify.Error, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(undeclaredAsset));

            var import = RunImportScript(scriptPath, sourceRoot, manifestPath, verifyOnly: false);
            Assert.Equal(0, import.ExitCode);
            Assert.False(File.Exists(undeclaredAsset));
            Assert.Contains("Removed", import.Output, StringComparison.Ordinal);
            Assert.Contains("1", import.Output, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(temporaryRoot))
                Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "AkashaAutomation.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("AkashaAutomation repository root not found.");
    }

    private static string[] GetManagedAssetPaths(string repositoryRoot)
    {
        var projectRoot = Path.Combine(repositoryRoot, "src", "AkashaAutomation.BetterGiPort");
        var managedRoots = new[]
        {
            Path.Combine(projectRoot, "Assets", "Config"),
            Path.Combine(projectRoot, "Assets", "Recognition"),
            Path.Combine(projectRoot, "Assets", "Model")
        };

        return managedRoots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static ProcessResult RunImportScript(
        string scriptPath,
        string sourceRoot,
        string manifestPath,
        bool verifyOnly)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "pwsh.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add("-Source");
        startInfo.ArgumentList.Add(sourceRoot);
        startInfo.ArgumentList.Add("-ManifestPath");
        startInfo.ArgumentList.Add(manifestPath);
        if (verifyOnly)
            startInfo.ArgumentList.Add("-VerifyOnly");

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start pwsh.exe.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new ProcessResult(process.ExitCode, output, error);
    }

    private sealed record AssetExpectation(string RelativePath, string Sha256, int Count, int UniqueCount);

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}
