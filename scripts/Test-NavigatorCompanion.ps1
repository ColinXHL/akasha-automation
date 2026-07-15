[CmdletBinding()]
param(
    [string]$NavigatorRepository = (Join-Path $PSScriptRoot "..\..\AkashaNavigator"),
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$automationRepository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$navigatorRepository = [IO.Path]::GetFullPath($NavigatorRepository)
$navigatorProject = Join-Path $navigatorRepository "AkashaNavigator\AkashaNavigator.csproj"
$workerProject = Join-Path $automationRepository "src\AkashaAutomation.Worker\AkashaAutomation.Worker.csproj"
$pluginTemplate = Join-Path $automationRepository "plugin\akasha-genshin-automation"
$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ("akasha-companion-smoke-" + [Guid]::NewGuid().ToString("N"))
$pluginRoot = Join-Path $temporaryRoot "plugin"
$workerOutput = Join-Path $pluginRoot "worker\win-x64"
$harnessRoot = Join-Path $temporaryRoot "harness"

if (-not (Test-Path -LiteralPath $navigatorProject -PathType Leaf)) {
    throw "AkashaNavigator project was not found: $navigatorProject"
}

try {
    New-Item -ItemType Directory -Path $workerOutput, $harnessRoot -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $pluginTemplate "plugin.json") -Destination $pluginRoot
    Copy-Item -LiteralPath (Join-Path $pluginTemplate "main.js") -Destination $pluginRoot

    dotnet publish $workerProject `
        --configuration $Configuration `
        --runtime win-x64 `
        --self-contained false `
        --output $workerOutput `
        --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Worker publish failed with exit code $LASTEXITCODE."
    }

    $escapedNavigatorProject = [Security.SecurityElement]::Escape($navigatorProject)
    $projectFile = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$escapedNavigatorProject" />
  </ItemGroup>
</Project>
"@
    Set-Content -LiteralPath (Join-Path $harnessRoot "CompanionSmoke.csproj") -Value $projectFile -Encoding utf8

    $programFile = @'
using System.Text.Json;
using AkashaNavigator.Models.Plugin;
using AkashaNavigator.Services;

if (args.Length != 1)
{
    throw new ArgumentException("Expected the temporary plugin root as the only argument.");
}

var pluginRoot = Path.GetFullPath(args[0]);
var manifestResult = PluginManifest.LoadFromFile(Path.Combine(pluginRoot, "plugin.json"));
if (!manifestResult.IsSuccess || manifestResult.Manifest?.Companion == null)
{
    throw new InvalidDataException($"Plugin manifest validation failed: {manifestResult.ErrorMessage}");
}

var pluginId = manifestResult.Manifest.Id!;
var manifest = manifestResult.Manifest.Companion;

using var manager = new CompanionProcessManager(new LogService(Path.Combine(pluginRoot, "logs")));
var first = await manager.StartAsync(pluginId, pluginRoot, manifest);
if (!first.Running || first.ProcessId is null)
{
    throw new InvalidOperationException("The companion did not reach the running state.");
}

var second = await manager.StartAsync(pluginId, pluginRoot, manifest);
if (second.ProcessId != first.ProcessId)
{
    throw new InvalidOperationException("Single-instance start created a second process.");
}

var request = JsonSerializer.SerializeToElement(new { message = "navigator-worker-echo" });
var response = await manager.InvokeAsync(pluginId, "worker.echo", request);
if (response is null || response.Value.GetProperty("message").GetString() != "navigator-worker-echo")
{
    throw new InvalidOperationException("Echo response did not match the request.");
}

var workerStatus = await manager.InvokeAsync(pluginId, "worker.getStatus", null);
if (workerStatus is null ||
    workerStatus.Value.GetProperty("state").GetString() != "ready" ||
    workerStatus.Value.GetProperty("gameWindow").GetProperty("state").GetString() != "not_found" ||
    workerStatus.Value.GetProperty("realInputEnabled").GetBoolean())
{
    throw new InvalidOperationException("Worker status did not describe the safe no-window Ready state.");
}

await manager.StopAsync(pluginId);
if (manager.GetStatus(pluginId).Running)
{
    throw new InvalidOperationException("The companion was still running after stop.");
}

Console.WriteLine($"PASS process={first.ProcessId} echo=navigator-worker-echo status=ready/no-window stopped=true");
'@
    Set-Content -LiteralPath (Join-Path $harnessRoot "Program.cs") -Value $programFile -Encoding utf8

    dotnet run `
        --project (Join-Path $harnessRoot "CompanionSmoke.csproj") `
        --configuration $Configuration `
        --no-launch-profile `
        -- $pluginRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Companion smoke test failed with exit code $LASTEXITCODE."
    }
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
