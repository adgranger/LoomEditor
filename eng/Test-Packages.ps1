<#
.SYNOPSIS
    Packs the libraries, then publishes a throwaway app that references only those packages with Native AOT and smoke-tests it.
.DESCRIPTION
    Proves that the packages carry everything an app needs (dependencies, buildTransitive PRI workaround, Generic.xaml).
#>
param([string] $Version)
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    $packArgs = @{}
    if ($Version) { $packArgs.Version = $Version }
    & (Join-Path $PSScriptRoot 'Pack.ps1') @packArgs
    if ($LASTEXITCODE -ne 0) { throw 'eng/Pack.ps1 failed.' }

    $winui = @(Get-ChildItem 'artifacts/packages' -Filter 'LoomEditor.WinUI.*.nupkg')
    if ($winui.Count -ne 1) { throw "Expected one LoomEditor.WinUI nupkg, found $($winui.Count)." }
    $version = $winui[0].BaseName.Substring('LoomEditor.WinUI.'.Length)

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

    Write-Host "Package test passed: $version"
}
finally { Pop-Location }
