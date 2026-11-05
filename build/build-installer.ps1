<#
.SYNOPSIS
    Builds the Screenshot Maker installer (Setup.exe) with Velopack.

.DESCRIPTION
    Code signing is enabled when a PFX certificate is provided via -CertificatePath or the
    SIGN_CERT_PATH environment variable; its password comes from SIGN_CERT_PASSWORD.

.EXAMPLE
    ./build/build-installer.ps1
    Builds the version from ScreenshotMaker.csproj into artifacts/releases (unsigned).

.EXAMPLE
    $env:SIGN_CERT_PASSWORD = '<password>'; ./build/build-installer.ps1 -CertificatePath C:\certs\codesign.pfx
    Builds a signed installer.

.EXAMPLE
    $env:GITHUB_TOKEN = '<token>'; ./build/build-installer.ps1 -Version 2.1.0 -Publish
    Builds 2.1.0 with delta updates and attaches it to the GitHub release v2.1.0 (creating it if needed).
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$CertificatePath = $env:SIGN_CERT_PATH,
    [switch]$Publish
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'ScreenshotMaker.csproj'
$publishDir = Join-Path $root 'artifacts/publish'
$releasesDir = Join-Path $root 'artifacts/releases'
$repoUrl = 'https://github.com/radimbig2/ScreenshotMaker'
$timestampUrl = 'http://timestamp.digicert.com'

function Invoke-Step([string]$Name, [scriptblock]$Command) {
    Write-Host "==> $Name" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Name failed (exit code $LASTEXITCODE)." }
}

if (-not $Version) {
    $Version = ([xml](Get-Content $project)).Project.PropertyGroup.Version |
        Where-Object { $_ } | Select-Object -First 1
}

if ($Publish -and -not $env:GITHUB_TOKEN) {
    throw 'Set the GITHUB_TOKEN environment variable to publish a release.'
}

$signArgs = @()
if ($CertificatePath) {
    if (-not (Test-Path $CertificatePath)) { throw "Certificate not found: $CertificatePath" }
    $signParams = "/f `"$CertificatePath`" /fd sha256 /tr $timestampUrl /td sha256"
    if ($env:SIGN_CERT_PASSWORD) { $signParams += " /p `"$env:SIGN_CERT_PASSWORD`"" }
    $signArgs = @('--signParams', $signParams)
    Write-Host 'Code signing: enabled' -ForegroundColor Green
}
else {
    Write-Warning 'Code signing: disabled (no certificate provided). Windows SmartScreen will warn users.'
}

Push-Location $root
try {
    Invoke-Step 'Restore tools' { dotnet tool restore }

    if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }

    Invoke-Step "Publish $Version" {
        dotnet publish $project -c Release -r win-x64 --self-contained -p:Version=$Version -o $publishDir
    }

    if (-not $Publish -and (Test-Path $releasesDir)) {
        Remove-Item -Recurse -Force $releasesDir
    }

    if ($Publish) {
        Write-Host '==> Download previous release (for delta updates)' -ForegroundColor Cyan
        dotnet vpk download github --repoUrl $repoUrl --outputDir $releasesDir --token $env:GITHUB_TOKEN
        if ($LASTEXITCODE -ne 0) { Write-Warning 'No previous release downloaded; building a full package only.' }
    }

    Invoke-Step 'Pack installer' {
        dotnet vpk pack `
            --packId ScreenshotMaker `
            --packVersion $Version `
            --packDir $publishDir `
            --mainExe ScreenshotMaker.exe `
            --packTitle 'Screenshot Maker' `
            --packAuthors radimbig `
            --icon Assets/app.ico `
            --outputDir $releasesDir `
            @signArgs
    }

    if ($Publish) {
        Invoke-Step 'Upload to GitHub release' {
            dotnet vpk upload github `
                --repoUrl $repoUrl `
                --outputDir $releasesDir `
                --token $env:GITHUB_TOKEN `
                --releaseName "Screenshot Maker $Version" `
                --tag "v$Version" `
                --merge `
                --publish
        }
    }

    Write-Host "Done. Installer: $(Join-Path $releasesDir 'ScreenshotMaker-win-Setup.exe')" -ForegroundColor Green
}
finally {
    Pop-Location
}
