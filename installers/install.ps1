#Requires -Version 5.1
<#
.SYNOPSIS
    byo CLI installer for Windows (PowerShell).

.DESCRIPTION
    Downloads the latest (or pinned) self-contained byo binary from GitHub Releases,
    verifies its SHA256 checksum, installs into a user-local directory, and adds that
    directory to the current user's PATH.

.EXAMPLE
    iwr -useb https://github.com/softwareworkercom/byo/releases/latest/download/install.ps1 | iex

.EXAMPLE
    $env:BYO_VERSION = '1.2.3'
    iwr -useb https://github.com/softwareworkercom/byo/releases/download/v1.2.3/install.ps1 | iex
#>
[CmdletBinding()]
param(
    [string] $Version    = $env:BYO_VERSION,
    [string] $InstallDir = $(if ($env:BYO_INSTALL_DIR) { $env:BYO_INSTALL_DIR } else { Join-Path $env:LOCALAPPDATA 'Programs\byo' })
)

$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'

$Repo    = 'softwareworkercom/byo'
$BinName = 'byo.exe'

function Write-Log  ([string]$Message) { Write-Host "==> $Message" }
function Write-Warn ([string]$Message) { Write-Host "warn: $Message" -ForegroundColor Yellow }

function Get-Rid {
    # OSArchitecture is a [System.Runtime.InteropServices.Architecture] enum; normalize to a
    # string so comparisons are reliable across PowerShell versions. Fall back to the
    # PROCESSOR_ARCHITECTURE environment variable if the runtime value is unavailable.
    $arch = [string][System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
    if (-not $arch) { $arch = $env:PROCESSOR_ARCHITECTURE }

    switch -Regex ($arch) {
        '^(X64|AMD64)$' { return 'win-x64' }
        default { throw "Unsupported architecture: '$arch'. Only win-x64 is published today." }
    }
}

function Resolve-Version {
    param([string] $Requested)
    if ($Requested -and $Requested -ne 'latest') {
        return $Requested.TrimStart('v')
    }
    # Follow the 'latest' redirect to discover the tag without using the GitHub API.
    $resp = Invoke-WebRequest -UseBasicParsing -MaximumRedirection 0 `
                -Uri "https://github.com/$Repo/releases/latest" `
                -ErrorAction SilentlyContinue
    $location = $resp.Headers['Location']
    if (-not $location) { throw "Could not resolve latest version from GitHub." }
    return ($location -split '/tag/v')[-1]
}

function Add-ToUserPath {
    param([string] $Directory)
    $userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
    $entries  = @()
    if ($userPath) { $entries = $userPath -split ';' | Where-Object { $_ -ne '' } }
    if ($entries -notcontains $Directory) {
        $newPath = if ($userPath) { "$userPath;$Directory" } else { $Directory }
        [Environment]::SetEnvironmentVariable('Path', $newPath, 'User')
        Write-Log "Added '$Directory' to user PATH (open a new shell to pick it up)."
    }
}

try {
    $rid     = Get-Rid
    $version = Resolve-Version -Requested $Version
    $asset   = "byo-$version-$rid.zip"
    $baseUrl = "https://github.com/$Repo/releases/download/v$version"
    $assetUrl   = "$baseUrl/$asset"
    $sumsUrl    = "$baseUrl/SHA256SUMS.txt"

    Write-Log "Platform : $rid"
    Write-Log "Version  : $version"
    Write-Log "Source   : $assetUrl"
    Write-Log "Target   : $InstallDir"

    $tmp = New-Item -ItemType Directory -Force -Path (Join-Path ([IO.Path]::GetTempPath()) ("byo-" + [Guid]::NewGuid()))
    try {
        $archivePath = Join-Path $tmp $asset
        $sumsPath    = Join-Path $tmp 'SHA256SUMS.txt'

        Write-Log "Downloading archive"
        Invoke-WebRequest -UseBasicParsing -Uri $assetUrl -OutFile $archivePath
        Write-Log "Downloading checksums"
        Invoke-WebRequest -UseBasicParsing -Uri $sumsUrl  -OutFile $sumsPath

        Write-Log "Verifying checksum"
        $expectedLine = Get-Content $sumsPath | Where-Object { $_ -match [regex]::Escape($asset) } | Select-Object -First 1
        if (-not $expectedLine) { throw "Could not find checksum for $asset in SHA256SUMS.txt" }
        $expected = ($expectedLine -split '\s+')[0].ToLower()
        $actual   = (Get-FileHash -Path $archivePath -Algorithm SHA256).Hash.ToLower()
        if ($expected -ne $actual) {
            throw "Checksum mismatch for ${asset}: expected $expected, got $actual"
        }

        Write-Log "Installing to $InstallDir"
        New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
        # Remove any prior install of just the binary first (best-effort)
        $targetBin = Join-Path $InstallDir $BinName
        if (Test-Path $targetBin) { Remove-Item -Force $targetBin }
        Expand-Archive -Force -Path $archivePath -DestinationPath $InstallDir

        Add-ToUserPath -Directory $InstallDir
        # Also update the current session's PATH so verification works immediately
        if (($env:Path -split ';') -notcontains $InstallDir) { $env:Path = "$env:Path;$InstallDir" }

        Write-Log "Verifying installation"
        & $targetBin --help | Out-Null
        Write-Log "Done. byo $version installed at $targetBin"
    }
    finally {
        Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
    }
}
catch {
    Write-Error $_
    exit 1
}
