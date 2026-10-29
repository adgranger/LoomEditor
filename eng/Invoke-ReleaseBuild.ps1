<#
.SYNOPSIS
    Runs every release gate: restore, Release build (0 warnings), all tests, package checks.
.DESCRIPTION
    Stops at the first failure. Nothing is published or tagged; packages end up in artifacts/packages.
    Run from anywhere; the script works from the repository root.
.EXAMPLE
    ./eng/Invoke-ReleaseBuild.ps1 -Version v0.1.0-alpha.1
#>
param(
    [Parameter(Mandatory)][string] $Version
)
$ErrorActionPreference = 'Stop'

# Step 0 runs before anything is built.
$v = & "$PSScriptRoot/Get-ReleaseVersion.ps1" $Version

Push-Location (Split-Path -Parent $PSScriptRoot)
try {
    Write-Host '== restore =='
    dotnet restore LoomEditor.slnx
    if ($LASTEXITCODE -ne 0) { throw 'restore failed' }

    Write-Host '== build =='
    dotnet build LoomEditor.slnx -c Release --no-restore "-p:Version=$v" -warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'build failed' }

    Write-Host '== test =='
    dotnet test --solution LoomEditor.slnx -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'test failed' }

    Write-Host '== package checks =='
    & "$PSScriptRoot/Test-Packages.ps1" -Version $v
    if ($LASTEXITCODE -ne 0) { throw 'package checks failed' }

    Write-Host "Release build passed: $v; packages are in artifacts/packages."
}
finally {
    Pop-Location
}
