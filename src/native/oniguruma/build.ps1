<#
.SYNOPSIS
    Builds the native Oniguruma library (onig.dll) for Windows x64 or ARM64.

.DESCRIPTION
    1. Downloads the pinned Oniguruma release and checks its SHA-256.
    2. Builds it with CMake + MSVC as a DLL, with the C runtime linked statically
       (so the DLL does not need the Visual C++ Redistributable).
    3. Copies onig.dll to artifacts/native/win-<arch>/.

    Requirements: Visual Studio 2022+ (or Build Tools) with the "Desktop development with C++" workload,
    plus the "MSVC ... ARM64 build tools" component for ARM64. CMake comes with Visual Studio.

.EXAMPLE
    ./src/native/oniguruma/build.ps1 -Architecture x64
    ./src/native/oniguruma/build.ps1 -Architecture ARM64
#>
param(
    [ValidateSet('x64', 'ARM64')] [string] $Architecture = 'x64'
)

$ErrorActionPreference = 'Stop'

# --- Pinned version. Change all three lines together (see docs/adr/0001-baseline.md). ---
$Version = '6.9.10'
$Url = "https://github.com/kkos/oniguruma/releases/download/v$Version/onig-$Version.tar.gz"
$Sha256 = '2A5CFC5AE259E4E97F86B68DFFFC152CDAFFE94E2060B770CB827238D769FC05'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '../../..')
$work = Join-Path $PSScriptRoot '.build'
$archive = Join-Path $work "onig-$Version.tar.gz"
$sources = Join-Path $work "onig-$Version"
$buildDir = Join-Path $work "build-$Architecture"
$rid = "win-$($Architecture.ToLowerInvariant())"
$output = Join-Path $repoRoot "artifacts/native/$rid"

function Find-CMake {
    $onPath = Get-Command cmake -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
    if (Test-Path $vswhere) {
        $vs = & $vswhere -latest -products * -property installationPath
        $candidate = Join-Path $vs 'Common7/IDE/CommonExtensions/Microsoft/CMake/CMake/bin/cmake.exe'
        if (Test-Path $candidate) { return $candidate }
    }
    throw 'CMake not found. Install Visual Studio with the C++ workload, or put cmake on the PATH.'
}

New-Item -ItemType Directory -Force -Path $work, $output | Out-Null

if (-not (Test-Path $archive)) {
    Write-Host "Downloading $Url"
    Invoke-WebRequest -Uri $Url -OutFile $archive
}

$actual = (Get-FileHash $archive -Algorithm SHA256).Hash
if ($actual -ne $Sha256) {
    Remove-Item $archive
    throw "SHA-256 mismatch for $archive. Expected $Sha256, got $actual."
}

if (-not (Test-Path $sources)) {
    # Use the tar.exe shipped with Windows: the GNU tar of Git Bash does not understand C:\ paths.
    & (Join-Path $env:SystemRoot 'System32/tar.exe') -xzf $archive -C $work
    if ($LASTEXITCODE -ne 0) { throw 'Extraction failed.' }
}

$cmake = Find-CMake
& $cmake -S $sources -B $buildDir -A $Architecture `
    -DBUILD_SHARED_LIBS=ON `
    -DENABLE_POSIX_API=OFF `
    -DENABLE_BINARY_COMPATIBLE_POSIX_API=OFF `
    -DCMAKE_POLICY_DEFAULT_CMP0091=NEW `
    -DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded
if ($LASTEXITCODE -ne 0) { throw 'CMake configure failed.' }

& $cmake --build $buildDir --config Release
if ($LASTEXITCODE -ne 0) { throw 'CMake build failed.' }

Copy-Item (Join-Path $buildDir 'Release/onig.dll') $output -Force
Copy-Item (Join-Path $sources 'COPYING') (Join-Path $output 'oniguruma-COPYING.txt') -Force
Write-Host "onig.dll ($Architecture) -> $output"
