<#
.SYNOPSIS
    Packs the libraries, then publishes throwaway apps that reference only those packages with Native AOT and smoke-tests them:
    the WinUI app (Windows only) and the headless Avalonia app (every system, for the runtime of the machine).
.DESCRIPTION
    Proves that the packages carry everything an app needs (dependencies, buildTransitive PRI workaround, Generic.xaml).
#>
param([string] $Version, [switch] $RequireAllNatives)
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    $packArgs = @{}
    if ($Version) { $packArgs.Version = $Version }
    if ($RequireAllNatives) { $packArgs.RequireAllNatives = $true }
    & (Join-Path $PSScriptRoot 'Pack.ps1') @packArgs
    if ($LASTEXITCODE -ne 0) { throw 'eng/Pack.ps1 failed.' }

    $core = @(Get-ChildItem 'artifacts/packages' -Filter 'LoomEditor.Core.*.nupkg')
    if ($core.Count -ne 1) { throw "Expected one LoomEditor.Core nupkg, found $($core.Count)." }
    $version = $core[0].BaseName.Substring('LoomEditor.Core.'.Length)

    # The WinUI app needs Windows (its target framework is a Windows one).
    if ($IsWindows) {
        # Same version between packs: never reuse an old restore or build output.
        Remove-Item 'artifacts/package-test' -Recurse -Force -ErrorAction SilentlyContinue
        $app = 'eng/package-test/LoomEditor.PackageTest'
        Remove-Item "$app/bin", "$app/obj" -Recurse -Force -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Force 'artifacts/package-test' | Out-Null
        $log = 'artifacts/package-test/publish.log'

        dotnet publish $app -c Release -r win-x64 -warnaserror --packages artifacts/package-test/nuget "-p:LoomEditorVersion=$version" 2>&1 | Tee-Object -FilePath $log
        if ($LASTEXITCODE -ne 0) { throw "The AOT publish of the package test app failed with exit code $LASTEXITCODE." }
        $warnings = @(Select-String -Path $log -Pattern 'warning (IL|AOT|CS|CA|NETSDK)\d+')
        if ($warnings.Count -gt 0) { $warnings; throw 'The AOT publish of the package test app produced warnings.' }

        & (Join-Path $PSScriptRoot 'Test-AotApp.ps1') $app -NoPublish
        if ($LASTEXITCODE -ne 0) { throw 'The AOT smoke test of the package test app failed.' }
    }

    # The headless Avalonia app: published for the runtime of this machine, run, and its exit code is the result.
    $rid = if ($IsWindows) { 'win-x64' } elseif ($IsMacOS) { 'osx-arm64' } else { 'linux-x64' }
    $avaloniaApp = 'eng/package-test/LoomEditor.PackageTest.Avalonia'
    Remove-Item "$avaloniaApp/bin", "$avaloniaApp/obj" -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force 'artifacts/package-test' | Out-Null
    Remove-Item 'artifacts/package-test/nuget-avalonia' -Recurse -Force -ErrorAction SilentlyContinue
    $avaloniaLog = 'artifacts/package-test/publish-avalonia.log'
    dotnet publish $avaloniaApp -c Release -r $rid -warnaserror --packages artifacts/package-test/nuget-avalonia "-p:LoomEditorVersion=$version" 2>&1 | Tee-Object -FilePath $avaloniaLog
    if ($LASTEXITCODE -ne 0) { throw "The AOT publish of the Avalonia package test app failed with exit code $LASTEXITCODE." }
    $warnings = @(Select-String -Path $avaloniaLog -Pattern 'warning (IL|AOT|CS|CA|NETSDK)\d+')
    if ($warnings.Count -gt 0) { $warnings; throw 'The AOT publish of the Avalonia package test app produced warnings.' }
    $exe = Join-Path $avaloniaApp "bin/Release/net11.0/$rid/publish/LoomEditor.PackageTest.Avalonia$(if ($IsWindows) { '.exe' })"
    if (-not (Test-Path $exe)) { throw "The published Avalonia package test app is missing: $exe" }
    & $exe
    if ($LASTEXITCODE -ne 0) { throw "The Avalonia package test app failed with exit code $LASTEXITCODE." }

    Write-Host "Package test passed: $version"
}
finally { Pop-Location }
