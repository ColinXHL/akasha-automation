[CmdletBinding()]
param(
    [string]$NavigatorRepository = (Join-Path $PSScriptRoot "..\..\AkashaNavigator"),
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$SkipNavigatorBuild
)

$ErrorActionPreference = "Stop"

$automationRepository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$navigatorRepository = [IO.Path]::GetFullPath($NavigatorRepository)
$navigatorProject = Join-Path $navigatorRepository "AkashaNavigator\AkashaNavigator.csproj"
$navigatorOutput = Join-Path $navigatorRepository "AkashaNavigator\bin\$Configuration\net8.0-windows"
$builtInPluginsRoot = [IO.Path]::GetFullPath((Join-Path $navigatorOutput "Repos\Plugins"))
$destination = [IO.Path]::GetFullPath((Join-Path $builtInPluginsRoot "akasha-genshin-automation"))
$pluginTemplate = Join-Path $automationRepository "plugin\akasha-genshin-automation"
$workerProject = Join-Path $automationRepository "src\AkashaAutomation.Worker\AkashaAutomation.Worker.csproj"
$workerOutput = Join-Path $destination "worker\win-x64"

if (-not (Test-Path -LiteralPath $navigatorProject -PathType Leaf)) {
    throw "AkashaNavigator project was not found: $navigatorProject"
}

if (-not (Test-Path -LiteralPath $pluginTemplate -PathType Container)) {
    throw "Plugin template was not found: $pluginTemplate"
}

$pluginsRootPrefix = $builtInPluginsRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $destination.StartsWith($pluginsRootPrefix, [StringComparison]::OrdinalIgnoreCase) -or
    [IO.Path]::GetFileName($destination) -ne "akasha-genshin-automation") {
    throw "Refusing to deploy outside the expected Navigator plugin output: $destination"
}

if (-not $SkipNavigatorBuild) {
    dotnet build $navigatorProject --configuration $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "AkashaNavigator build failed with exit code $LASTEXITCODE."
    }
}

if (Test-Path -LiteralPath $destination) {
    Remove-Item -LiteralPath $destination -Recurse -Force
}

New-Item -ItemType Directory -Path $destination -Force | Out-Null
Copy-Item -Path (Join-Path $pluginTemplate "*") -Destination $destination -Recurse -Force

dotnet publish $workerProject `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained false `
    --output $workerOutput `
    --nologo
if ($LASTEXITCODE -ne 0) {
    throw "Worker publish failed with exit code $LASTEXITCODE."
}

$navigatorExecutable = Join-Path $navigatorOutput "AkashaNavigator.exe"
Write-Output "Development plugin deployed: $destination"
Write-Output "Start Navigator: $navigatorExecutable"
Write-Output "Then install 'Akasha 原神自动化' from the plugin center and add it to the current Genshin Profile."
