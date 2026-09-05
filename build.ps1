<#
.SYNOPSIS
    Builds the OLED Sleeper release artifacts: the dual-architecture installer and the portable x64 zip.

.DESCRIPTION
    Publishes the two framework-dependent payloads the installer packs, publishes the self-contained
    portable build, compiles the Inno Setup script and zips the portable output. Every artifact carries
    the same version, which MinVer derives from the nearest v* tag.

    Both the CI release workflow and a local build run this script, so the two cannot drift.

.PARAMETER OutputDirectory
    Where the finished artifacts are collected. Defaults to 'artifacts' in the repository root.

.PARAMETER IsccPath
    Full path to ISCC.exe. Only needed when Inno Setup is not in one of the usual locations or on PATH.

.PARAMETER SkipPortable
    Builds only the installer. Useful when iterating on the .iss, since the portable publish is the slow half.

.EXAMPLE
    .\build.ps1
#>
[CmdletBinding()]
param(
    [string] $OutputDirectory,
    [string] $IsccPath,
    [switch] $SkipPortable
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'OLED-Sleeper\OLED-Sleeper.csproj'
$installerDirectory = Join-Path $repositoryRoot 'installer'
$scriptPath = Join-Path $installerDirectory 'OLED-Sleeper.iss'
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repositoryRoot 'artifacts' }

# Publish output is staged under the artifacts folder; the shipped files sit at its root.
$stagingDirectory = Join-Path $OutputDirectory 'publish'

function Write-Step {
    param([string] $Message)
    Write-Host ''
    Write-Host "==> $Message" -ForegroundColor Cyan
}

<#
.SYNOPSIS
    Locates ISCC.exe, the Inno Setup command-line compiler.
#>
function Resolve-Iscc {
    param([string] $Explicit)

    if ($Explicit) {
        if (Test-Path -LiteralPath $Explicit) { return (Resolve-Path -LiteralPath $Explicit).Path }
        throw "ISCC.exe was not found at the supplied path: $Explicit"
    }

    $onPath = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }

    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )
    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) { return $candidate }
    }

    throw @"
Inno Setup's compiler (ISCC.exe) was not found on PATH or in any of the usual locations.

Install it with:

    winget install JRSoftware.InnoSetup

Then re-run this script, or pass the path directly:

    .\build.ps1 -IsccPath 'C:\Path\To\ISCC.exe'
"@
}

<#
.SYNOPSIS
    Reads the version MinVer derives from the nearest v* tag.
#>
function Get-BuildVersion {
    # MinVer's targets arrive with the package, so a clean checkout has to restore before the target exists.
    & dotnet restore $projectPath --nologo | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed while preparing to read the version.' }

    $output = & dotnet msbuild $projectPath -t:MinVer -getProperty:Version -v:quiet -nologo
    if ($LASTEXITCODE -ne 0) { throw 'Could not read the version from the project.' }

    $version = ($output | Out-String).Trim()
    if (-not $version) { throw 'The project reported an empty version.' }
    return $version
}

<#
.SYNOPSIS
    Publishes one runtime identifier into the given folder.
#>
function Publish-Payload {
    param(
        [string] $Runtime,
        [string] $Destination,
        [switch] $Portable
    )

    if (Test-Path -LiteralPath $Destination) { Remove-Item -LiteralPath $Destination -Recurse -Force }

    $arguments = @(
        'publish', $projectPath,
        '-c', 'Release',
        '-r', $Runtime,
        '-o', $Destination,
        '--nologo'
    )

    if ($Portable) {
        # Self-contained so the zip needs no .NET runtime, and single-file so the payload is the exe plus
        # the five WPF native DLLs that must stay beside it.
        $arguments += @(
            '--self-contained', 'true',
            '-p:Portable=true',
            '-p:PublishSingleFile=true',
            '-p:EnableCompressionInSingleFile=true',
            '-p:DebugType=none'
        )
    }
    else {
        # The installer fetches the .NET 8 Desktop runtime itself, and an installed copy must never be
        # portable: it would try to write its state inside Program Files.
        $arguments += @('--self-contained', 'false')
    }

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "Publishing $Runtime failed." }
}

# ---------------------------------------------------------------------------------------------------

$iscc = Resolve-Iscc -Explicit $IsccPath

Write-Step 'Reading version'
$version = Get-BuildVersion
Write-Host "Version: $version"

Write-Step 'Clearing previously shipped files'
# Anything left from an earlier run would otherwise be listed in this run's checksum file.
if (Test-Path -LiteralPath $OutputDirectory) {
    Get-ChildItem -LiteralPath $OutputDirectory -File | Remove-Item -Force
}

Write-Step 'Publishing installer payloads'
Publish-Payload -Runtime 'win-x64' -Destination (Join-Path $stagingDirectory 'x64')
Publish-Payload -Runtime 'win-x86' -Destination (Join-Path $stagingDirectory 'x86')

Write-Step 'Compiling the installer'
& $iscc "/DAppVersion=$version" $scriptPath
if ($LASTEXITCODE -ne 0) { throw 'The Inno Setup compile failed.' }

$setupPath = Join-Path $OutputDirectory "OLED-Sleeper-$version-Setup.exe"
if (-not (Test-Path -LiteralPath $setupPath)) { throw "The installer was not produced at $setupPath." }

if (-not $SkipPortable) {
    Write-Step 'Publishing the portable build'
    $portableX64 = Join-Path $stagingDirectory 'portable'
    Publish-Payload -Runtime 'win-x64' -Destination $portableX64 -Portable

    Write-Step 'Zipping the portable build'
    $zipPath = Join-Path $OutputDirectory "OLED-Sleeper-$version-Portable-x64.zip"
    if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
    # The exe sits at the zip root, so extracting anywhere gives a working folder.
    Compress-Archive -Path (Join-Path $portableX64 '*') -DestinationPath $zipPath
}

Write-Step 'Writing checksums'
$checksumPath = Join-Path $OutputDirectory "OLED-Sleeper-$version-SHA256SUMS.txt"
if (Test-Path -LiteralPath $checksumPath) { Remove-Item -LiteralPath $checksumPath -Force }
$lines = Get-ChildItem -LiteralPath $OutputDirectory -File |
    Where-Object { $_.Extension -in '.exe', '.zip' } |
    Sort-Object Name |
    ForEach-Object { "$((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLower())  $($_.Name)" }
Set-Content -LiteralPath $checksumPath -Value $lines -Encoding utf8

Write-Step 'Done'
Get-ChildItem -LiteralPath $OutputDirectory -File | ForEach-Object {
    Write-Host ("  {0,-46} {1,8:N1} MB" -f $_.Name, ($_.Length / 1MB))
}
