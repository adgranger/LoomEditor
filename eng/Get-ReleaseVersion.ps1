<#
.SYNOPSIS
    Turns a release tag (v0.1.0-alpha.1) into a package version (0.1.0-alpha.1).
.DESCRIPTION
    Accepts SemVer 2 without build metadata, with or without the leading 'v'. Writes the version to the success stream.
    With -GitHubOutput, also appends version=<version> and prerelease=<true|false> to the file named by $env:GITHUB_OUTPUT.
.EXAMPLE
    ./eng/Get-ReleaseVersion.ps1 v0.1.0-alpha.1
.EXAMPLE
    ./eng/Get-ReleaseVersion.ps1 -SelfTest
#>
param(
    [Parameter(Position = 0)][string] $TagOrVersion,
    [switch] $GitHubOutput,
    [switch] $SelfTest
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$pattern = '^v?(?<core>(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*))(?<pre>-(?:0|[1-9]\d*|\d*[A-Za-z-][0-9A-Za-z-]*)(?:\.(?:0|[1-9]\d*|\d*[A-Za-z-][0-9A-Za-z-]*))*)?$'

function Get-Version([string] $value) {
    if ($value -cnotmatch $pattern) {
        throw "'$value' is not a release version (expected vMAJOR.MINOR.PATCH[-prerelease], e.g. v0.1.0-alpha.1)."
    }
    $Matches.core + $(if ($Matches.ContainsKey('pre')) { $Matches.pre } else { '' })
}

if ($SelfTest) {
    $valid = [ordered]@{
        'v0.1.0' = '0.1.0'; 'v0.1.0-alpha.1' = '0.1.0-alpha.1'; '1.2.3-rc.1' = '1.2.3-rc.1'
        'v10.20.30' = '10.20.30'; 'v1.0.0-0' = '1.0.0-0'; 'v1.0.0-x-y.7' = '1.0.0-x-y.7'
    }
    $invalid = '', 'v1', 'v1.2', 'v01.2.3', 'v1.2.3-', 'v1.2.3+build', 'v1.2.3-alpha..1', 'v1.2.3-01', 'vfoo', 'v1.2.3.4', 'refs/tags/v1.2.3'
    $cases = 0
    foreach ($k in $valid.Keys) {
        $actual = Get-Version $k
        if ($actual -ne $valid[$k]) { throw "Self-test failed: '$k' gave '$actual', expected '$($valid[$k])'." }
        $cases++
    }
    foreach ($k in $invalid) {
        $threw = $false
        try { $null = Get-Version $k } catch { $threw = $true }
        if (-not $threw) { throw "Self-test failed: '$k' should be rejected." }
        $cases++
    }

    $tmp = New-TemporaryFile
    $saved = $env:GITHUB_OUTPUT
    try {
        $env:GITHUB_OUTPUT = $tmp.FullName
        foreach ($c in @(@('v1.2.3', 'false'), @('v1.2.3-rc.1', 'true'))) {
            Set-Content $tmp.FullName ''
            $null = & $PSCommandPath $c[0] -GitHubOutput
            $text = (Get-Content $tmp.FullName -Raw)
            $expected = "version=$((Get-Version $c[0]))", "prerelease=$($c[1])"
            foreach ($line in $expected) {
                if ($text -notmatch "(?m)^$([regex]::Escape($line))\r?$") { throw "Self-test failed: GITHUB_OUTPUT for '$($c[0])' misses '$line'." }
            }
            $cases++
        }
        $env:GITHUB_OUTPUT = ''
        $threw = $false
        try { $null = & $PSCommandPath 'v1.2.3' -GitHubOutput } catch { $threw = $true }
        if (-not $threw) { throw 'Self-test failed: -GitHubOutput without GITHUB_OUTPUT should throw.' }
        $cases++
    }
    finally {
        $env:GITHUB_OUTPUT = $saved
        Remove-Item $tmp.FullName -Force -ErrorAction SilentlyContinue
    }
    Write-Host "Self-test passed: $cases cases"
    return
}

$version = Get-Version $TagOrVersion
if ($GitHubOutput) {
    if ([string]::IsNullOrEmpty($env:GITHUB_OUTPUT)) { throw '-GitHubOutput needs the GITHUB_OUTPUT environment variable.' }
    $pre = if ($version.Contains('-')) { 'true' } else { 'false' }
    Add-Content -Path $env:GITHUB_OUTPUT -Value "version=$version", "prerelease=$pre"
}
$version
