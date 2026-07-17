[CmdletBinding()]
param(
    [ValidateSet("Release")]
    [string]$Configuration = "Release",
    [ValidateSet("win-x64")]
    [string]$Runtime = "win-x64",
    [string]$OutputDirectory = "artifacts\release",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$pluginTemplate = Join-Path $repository "plugin\akasha-genshin-automation"
$workerProject = Join-Path $repository "src\AkashaAutomation.Worker\AkashaAutomation.Worker.csproj"
$solution = Join-Path $repository "AkashaAutomation.sln"
$manifestPath = Join-Path $pluginTemplate "plugin.json"
$outputRoot = if ([IO.Path]::IsPathRooted($OutputDirectory)) {
    [IO.Path]::GetFullPath($OutputDirectory)
} else {
    [IO.Path]::GetFullPath((Join-Path $repository $OutputDirectory))
}

foreach ($requiredPath in @($pluginTemplate, $workerProject, $solution, $manifestPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required release input was not found: $requiredPath"
    }
}

$pluginManifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$pluginId = [string]$pluginManifest.id
$pluginVersion = [string]$pluginManifest.version
if ($pluginId -notmatch '^[A-Za-z0-9._-]+$' -or [string]::IsNullOrWhiteSpace($pluginVersion)) {
    throw "plugin.json contains an invalid id or version."
}

$archiveName = "$pluginId-v$pluginVersion.zip"
$archivePath = [IO.Path]::GetFullPath((Join-Path $outputRoot $archiveName))
$checksumPath = "$archivePath.sha256"
$workingRoot = Join-Path ([IO.Path]::GetTempPath()) "AkashaAutomation.Release.$([Guid]::NewGuid().ToString('N'))"
$stagingRoot = Join-Path $workingRoot "staging"
$packageRoot = Join-Path $stagingRoot $pluginId
$verificationRoot = Join-Path $workingRoot "verify"

function Assert-ContainedPath {
    param(
        [Parameter(Mandatory)]
        [string]$Root,
        [Parameter(Mandatory)]
        [string]$Candidate
    )

    $resolvedRoot = [IO.Path]::GetFullPath($Root).TrimEnd(
        [IO.Path]::DirectorySeparatorChar,
        [IO.Path]::AltDirectorySeparatorChar)
    $resolvedCandidate = [IO.Path]::GetFullPath($Candidate)
    $rootPrefix = $resolvedRoot + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedCandidate.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path escapes its expected root: $resolvedCandidate"
    }
}

function Get-PackageFileRecords {
    param(
        [Parameter(Mandatory)]
        [string]$Root
    )

    return @(
        Get-ChildItem -LiteralPath $Root -Recurse -File |
            Where-Object { $_.Name -ne "package-manifest.json" } |
            Sort-Object FullName |
            ForEach-Object {
                $relativePath = [IO.Path]::GetRelativePath($Root, $_.FullName).Replace('\', '/')
                [ordered]@{
                    path = $relativePath
                    size = $_.Length
                    sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
                }
            }
    )
}

function Assert-PackagePayload {
    param(
        [Parameter(Mandatory)]
        [string]$Root
    )

    $packageManifestPath = Join-Path $Root "package-manifest.json"
    $pluginManifestPath = Join-Path $Root "plugin.json"
    if (-not (Test-Path -LiteralPath $pluginManifestPath -PathType Leaf)) {
        throw "Published package is missing plugin.json."
    }
    if (-not (Test-Path -LiteralPath (Join-Path $Root "main.js") -PathType Leaf)) {
        throw "Published package is missing main.js."
    }
    if (-not (Test-Path -LiteralPath (Join-Path $Root "worker\$Runtime\AkashaAutomation.Worker.exe") -PathType Leaf)) {
        throw "Published package is missing AkashaAutomation.Worker.exe."
    }
    if (-not (Test-Path -LiteralPath (Join-Path $Root "worker\$Runtime\Assets\Config\Pick\default_pick_black_lists.json") -PathType Leaf)) {
        throw "Published package is missing the built-in AutoPick blacklist."
    }
    if (-not (Test-Path -LiteralPath (Join-Path $Root "worker\$Runtime\Assets\Model\PaddleOCR") -PathType Container)) {
        throw "Published package is missing PaddleOCR models."
    }

    foreach ($legalFile in @("LICENSE", "DERIVATION.md", "THIRD_PARTY_NOTICES.md")) {
        if (-not (Test-Path -LiteralPath (Join-Path $Root $legalFile) -PathType Leaf)) {
            throw "Published package is missing $legalFile."
        }
    }

    $forbiddenFiles = @(
        Get-ChildItem -LiteralPath $Root -Recurse -File |
            Where-Object {
                $_.Extension -in @(".pdb", ".log") -or
                $_.Name -in @("config.json", "library.json") -or
                $_.FullName -match '[\\/](testdata|logs?)[\\/]'
            }
    )
    if ($forbiddenFiles.Count -gt 0) {
        throw "Published package contains forbidden files: $($forbiddenFiles.FullName -join ', ')"
    }

    if (-not (Test-Path -LiteralPath $packageManifestPath -PathType Leaf)) {
        throw "Published package is missing package-manifest.json."
    }

    $packageManifest = Get-Content -LiteralPath $packageManifestPath -Raw | ConvertFrom-Json
    if ($packageManifest.schemaVersion -ne 1 -or
        $packageManifest.plugin.id -ne $pluginId -or
        $packageManifest.plugin.version -ne $pluginVersion -or
        $packageManifest.runtime -ne $Runtime) {
        throw "package-manifest.json metadata does not match plugin.json."
    }

    $declared = @{}
    foreach ($file in $packageManifest.files) {
        $relativePath = ([string]$file.path).Replace('/', [IO.Path]::DirectorySeparatorChar)
        $fullPath = [IO.Path]::GetFullPath((Join-Path $Root $relativePath))
        Assert-ContainedPath -Root $Root -Candidate $fullPath
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            throw "Manifest file is missing: $($file.path)"
        }
        if ($declared.ContainsKey([string]$file.path)) {
            throw "Manifest contains a duplicate path: $($file.path)"
        }

        $declared[[string]$file.path] = $true
        $actualFile = Get-Item -LiteralPath $fullPath
        $actualHash = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actualFile.Length -ne [long]$file.size -or
            $actualHash -ne ([string]$file.sha256).ToLowerInvariant()) {
            throw "Manifest verification failed: $($file.path)"
        }
    }

    $actualPaths = @(
        Get-ChildItem -LiteralPath $Root -Recurse -File |
            Where-Object { $_.Name -ne "package-manifest.json" } |
            ForEach-Object { [IO.Path]::GetRelativePath($Root, $_.FullName).Replace('\', '/') }
    )
    if ($actualPaths.Count -ne $declared.Count -or
        @($actualPaths | Where-Object { -not $declared.ContainsKey($_) }).Count -gt 0) {
        throw "Package contents do not match package-manifest.json."
    }
}

try {
    if (-not $SkipTests) {
        dotnet test $solution --configuration $Configuration --nologo
        if ($LASTEXITCODE -ne 0) {
            throw "Tests failed with exit code $LASTEXITCODE."
        }
    }

    New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null

    Get-ChildItem -LiteralPath $pluginTemplate -Force |
        Where-Object { $_.Name -ne "worker" } |
        ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination $packageRoot -Recurse -Force
        }

    $workerOutput = Join-Path $packageRoot "worker\$Runtime"
    dotnet publish $workerProject `
        --configuration $Configuration `
        --runtime $Runtime `
        --self-contained false `
        --output $workerOutput `
        --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Worker publish failed with exit code $LASTEXITCODE."
    }

    Get-ChildItem -LiteralPath $packageRoot -Recurse -File |
        Where-Object { $_.Name -eq ".gitkeep" -or $_.Extension -eq ".pdb" } |
        Remove-Item -Force

    foreach ($legalFile in @("LICENSE", "DERIVATION.md", "THIRD_PARTY_NOTICES.md")) {
        Copy-Item -LiteralPath (Join-Path $repository $legalFile) -Destination (Join-Path $packageRoot $legalFile) -Force
    }

    $fileRecords = Get-PackageFileRecords -Root $packageRoot
    $packageManifest = [ordered]@{
        schemaVersion = 1
        plugin = [ordered]@{
            id = $pluginId
            version = $pluginVersion
        }
        runtime = $Runtime
        files = $fileRecords
    }
    $packageManifest |
        ConvertTo-Json -Depth 6 |
        Set-Content -LiteralPath (Join-Path $packageRoot "package-manifest.json") -Encoding utf8NoBOM

    Assert-PackagePayload -Root $packageRoot

    New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
    foreach ($existingOutput in @($archivePath, $checksumPath)) {
        Assert-ContainedPath -Root $outputRoot -Candidate $existingOutput
        if (Test-Path -LiteralPath $existingOutput) {
            Remove-Item -LiteralPath $existingOutput -Force
        }
    }

    [IO.Compression.ZipFile]::CreateFromDirectory(
        $stagingRoot,
        $archivePath,
        [IO.Compression.CompressionLevel]::Optimal,
        $false)

    [IO.Compression.ZipFile]::ExtractToDirectory($archivePath, $verificationRoot)
    $verifiedPackageRoot = Join-Path $verificationRoot $pluginId
    Assert-PackagePayload -Root $verifiedPackageRoot

    $archiveHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    "$archiveHash *$archiveName" |
        Set-Content -LiteralPath $checksumPath -Encoding ascii -NoNewline

    $archiveSizeMiB = [Math]::Round((Get-Item -LiteralPath $archivePath).Length / 1MB, 2)
    Write-Output "Plugin package created: $archivePath"
    Write-Output "SHA-256 file created: $checksumPath"
    Write-Output "Package size: $archiveSizeMiB MiB"
} finally {
    if (Test-Path -LiteralPath $workingRoot) {
        $resolvedTempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd(
            [IO.Path]::DirectorySeparatorChar,
            [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
        $resolvedWorkingRoot = [IO.Path]::GetFullPath($workingRoot)
        if ($resolvedWorkingRoot.StartsWith($resolvedTempRoot, [StringComparison]::OrdinalIgnoreCase) -and
            [IO.Path]::GetFileName($resolvedWorkingRoot).StartsWith(
                "AkashaAutomation.Release.",
                [StringComparison]::Ordinal)) {
            Remove-Item -LiteralPath $resolvedWorkingRoot -Recurse -Force
        }
    }
}
