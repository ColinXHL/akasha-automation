using System.Text.Json;
using System.Diagnostics;
using AkashaAutomation.BetterGiPort.Assets;
using AkashaAutomation.BetterGiPort.Compatibility.Ocr;
using AkashaAutomation.Core.Capture;
using AkashaAutomation.Core.Ocr;
using AkashaAutomation.Core.Recognition;
using OpenCvSharp;

namespace AkashaAutomation.BetterGiPort.Tests;

public class BetterGiAssetTests
{
    private static readonly AssetExpectation[] Expectations =
    [
        new(BetterGiAssetPaths.DefaultPickBlacklist, "1129650653eed1ec7e81676b3f616895feb9433ab616efc98ac360232c7e7ea9", 4914, 4891),
        new(BetterGiAssetPaths.AutoPickKeyE, "09cc25ef17a7aab56f147f40f4a1373ae3bce06fc966929cc8d34ef85e61cd55"),
        new(BetterGiAssetPaths.AutoPickKeyF, "ce0100ebf90a4c98e6b34b5ee3777d973c9ae05322972d552fc718817e66271b"),
        new(BetterGiAssetPaths.AutoPickKeyG, "724edac6d0da519ac44d7a973db51990a95cf9b80005d1973caaaabb684543d7"),
        new(BetterGiAssetPaths.AutoPickKeyL, "51008048871d25dbb5713de10cadfa3516c11047eb0675994b997cdc18910e87"),
        new(BetterGiAssetPaths.AutoPickSettingsIcon, "3bc1a9010f337e6990aae0b88ef81b0ea5f4621465bdca45fb90dc0190b07537"),
        new(BetterGiAssetPaths.AutoPickChatIcon, "b4f03c5641447fc30f2a3a92ab189e0fbc55444985c9baeedd68d3d506e68505"),
        new(BetterGiAssetPaths.DefaultPauseOptions, "212962f57e0bb0c04d9c3af062be53ddd929573f0399bc29b4476ec646f2ef65", 66, 61),
        new(BetterGiAssetPaths.PauseOptions, "fcc7d1e985862f0e3b0cc59cad7312642f7e96a318a73fc7646c093701a08b5b", 5, 5),
        new(BetterGiAssetPaths.SelectOptions, "8585ca3368566a6efe15ef52a816494ac2469470d7ac3b806d3d329cb4b36e88", 1, 1),
        new(BetterGiAssetPaths.PaddleOcrReadme, "195c0939e6ec90e99e10153c22778d4e8f18574cfbc776937796e3adfb950981"),
        new(BetterGiAssetPaths.PaddleOcrTestImage, "583caa82c158da88cbeb0bdb209ada6a7658fd43df306c2a2aa846700a4de376"),
        new(BetterGiAssetPaths.PaddleOcrV4DetectionConfig, "7a71be98abcc1038fb0d10fad3efb58407fcd5ac4ac3fb45a5544c143bc4763e"),
        new(BetterGiAssetPaths.PaddleOcrV4DetectionModel, "c0f2e256776e81d9e38f49e7cc2a37864a326ee8097e84adf30a8e0ebcc0b24b"),
        new(BetterGiAssetPaths.PaddleOcrV4RecognitionConfig, "018c94645678dc492754754291705c4999f35c6e5be854a42b4f918fefd06ab4"),
        new(BetterGiAssetPaths.PaddleOcrV4RecognitionModel, "df79157f86aa181ee0daa43364203cfc892f98e2a1b425614a1c98e0b96d7393"),
    ];

    [Fact]
    public void ImportedAssets_ShouldMatchPinnedHashesAndListCounts()
    {
        var repositoryRoot = FindRepositoryRoot();
        foreach (var expectation in Expectations)
        {
            var path = Path.Combine(repositoryRoot, "src", "AkashaAutomation.BetterGiPort", expectation.RelativePath);

            BetterGiAssetIntegrity.VerifySha256(path, expectation.Sha256);
            if (expectation.Count is { } expectedCount && expectation.UniqueCount is { } expectedUniqueCount)
            {
                var values = BetterGiJsonList.Load(path);
                Assert.Equal(expectedCount, values.Count);
                Assert.Equal(expectedUniqueCount, values.Distinct(StringComparer.Ordinal).Count());
            }
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
    public async Task PaddleOcrV4_ShouldRecognizePinnedPreheatImageAndReleaseSessions()
    {
        var baseline = PaddleOcrEngine.ActiveSessions;
        var resolver = new RootedAssetPathResolver(AppContext.BaseDirectory);
        var options = BetterGiPaddleOcrAssets.CreateV4Options(resolver);
        await using var engine = new PaddleOcrEngine(options, new PaddleOnnxOcrSessionFactory());
        using var image = Cv2.ImRead(
            resolver.Resolve(BetterGiAssetPaths.PaddleOcrTestImage),
            ImreadModes.Color);
        using var frame = CapturedFrame.TakeOwnership(
            image.Clone(),
            1,
            DateTimeOffset.UnixEpoch,
            "bettergi-paddle-preheat");

        var result = await engine.RecognizeAsync(frame);

        Assert.False(string.IsNullOrWhiteSpace(result.Text));
        Assert.NotEmpty(result.Regions);
        Assert.Equal(baseline + 1, PaddleOcrEngine.ActiveSessions);

        await engine.DisposeAsync();
        Assert.Equal(baseline, PaddleOcrEngine.ActiveSessions);
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

    private sealed record AssetExpectation(
        string RelativePath,
        string Sha256,
        int? Count = null,
        int? UniqueCount = null);

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}
