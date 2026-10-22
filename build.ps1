<#
.SYNOPSIS
    Builds the native CursorLock.exe and the MSI installer into .\artifacts.

.EXAMPLE
    .\build.ps1 -Version 1.2.3
#>
param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'
$publish = Join-Path $artifacts 'publish'

# Visual Studio 2026 (v18) vcvarsall.bat calls vswhere.exe without a full path.
$vsInstaller = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
if (Test-Path $vsInstaller) {
    $env:PATH = "$vsInstaller;$env:PATH"
}

function Invoke-Checked([scriptblock] $Command) {
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "Command failed with exit code $LASTEXITCODE" }
}

Write-Host "Publishing CursorLock $Version (NativeAOT)..."
Invoke-Checked { dotnet publish (Join-Path $root 'CursorLock\CursorLock.csproj') -c Release -p:Version=$Version -o $publish }

$exe = Join-Path $publish 'CursorLock.exe'
$installerProject = Join-Path $root 'installer\CursorLock.Installer.wixproj'

Write-Host 'Building the MSI installer...'
Invoke-Checked {
    dotnet build $installerProject -c Release "-p:ProductVersion=$Version" "-p:SourceExe=$exe" -o $artifacts
}

Copy-Item $exe (Join-Path $artifacts "CursorLock-$Version-portable.exe") -Force

Get-ChildItem $artifacts -Filter "CursorLock-$Version*" -File |
    Get-FileHash -Algorithm SHA256 |
    ForEach-Object { "$($_.Hash.ToLowerInvariant())  $(Split-Path $_.Path -Leaf)" } |
    Set-Content (Join-Path $artifacts 'SHA256SUMS.txt') -Encoding ascii

Write-Host "Done:"
Get-ChildItem $artifacts -File | ForEach-Object { "  $($_.Name)" }
