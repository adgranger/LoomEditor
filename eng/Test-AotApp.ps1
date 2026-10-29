<#
.SYNOPSIS
Publishes a WinUI sample with Native AOT, starts it and fails if it does not stay alive with a window.
.PARAMETER Project
Project folder; its leaf name is the assembly name (e.g. samples/LoomEditor.Gallery).
#>
param(
    [Parameter(Mandatory, Position = 0)][string] $Project,
    [string] $Runtime = 'win-x64',
    [int] $Seconds = 10,
    [switch] $NoPublish,
    # Build first, then publish with --no-build (Avalonia: a recompiling publish drops the compiled XAML).
    [switch] $BuildFirst
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$name = Split-Path $Project -Leaf

if (-not $NoPublish) {
    $logDir = Join-Path (Get-Location) 'artifacts/aot-smoke'
    New-Item -ItemType Directory -Force $logDir | Out-Null
    $log = Join-Path $logDir "$name-publish.log"
    $publishArgs = @()
    if ($BuildFirst) {
        dotnet build $Project -c Release -r $Runtime -warnaserror 2>&1 | Tee-Object -FilePath $log
        if ($LASTEXITCODE -ne 0) { Write-Error "The build of $name failed with exit code $LASTEXITCODE."; exit 1 }
        $publishArgs = @('--no-build')
    }
    dotnet publish $Project -c Release -r $Runtime -warnaserror @publishArgs 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { Write-Error "The AOT publish of $name failed with exit code $LASTEXITCODE."; exit 1 }
    $warnings = @(Select-String -Path $log -Pattern 'warning (IL|AOT|CS|CA|NETSDK)\d+')
    if ($warnings.Count -gt 0) { $warnings; Write-Error "The AOT publish of $name produced warnings."; exit 1 }
}

$exes = @(Get-ChildItem -Path (Join-Path $Project "bin/Release/*/$Runtime/publish/$name.exe") -ErrorAction SilentlyContinue)
if ($exes.Count -ne 1) { Write-Error "Expected one published $name.exe for $Runtime under $Project, found $($exes.Count)."; exit 1 }
$exe = $exes[0].FullName

$crashLog = Join-Path ([IO.Path]::GetTempPath()) "$name.log"
Remove-Item $crashLog -ErrorAction SilentlyContinue

$process = Start-Process $exe -PassThru -WorkingDirectory (Split-Path $exe)
$children = @()
try {
    Start-Sleep -Seconds $Seconds
    $process.Refresh()

    if ($process.HasExited) { throw "The AOT app exited at startup with code $($process.ExitCode)." }
    if ($process.MainWindowHandle -eq 0) { throw 'The AOT app is running but has no window.' }
    $children = @(Get-CimInstance Win32_Process -Filter "ParentProcessId=$($process.Id)")
    if (Test-Path $crashLog) { Get-Content $crashLog; throw "The AOT app wrote its crash log: $crashLog" }

    Write-Host "AOT smoke test passed: $name ($Runtime), alive after $Seconds s."
}
catch {
    Write-Host "AOT smoke test FAILED: $($_.Exception.Message)"
    if (Test-Path $crashLog) { Get-Content $crashLog }
    exit 1
}
finally {
    foreach ($child in $children) { Stop-Process -Id $child.ProcessId -Force -ErrorAction SilentlyContinue }
    Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
}
