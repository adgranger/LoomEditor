<#
.SYNOPSIS
    Extracts the section of one version from CHANGELOG.md (the body of the GitHub Release).
.DESCRIPTION
    Finds the line '## [<version>]' and takes everything up to the next '## [' heading or link reference definition.
    Writes the text to -OutFile (UTF-8 without BOM, LF, final newline) or to the success stream.
    A missing or empty section throws, unless -AllowMissing is set: then a warning is written and the output is empty.
.EXAMPLE
    ./eng/Get-ReleaseNotes.ps1 -Version 0.1.0-alpha.1 -OutFile notes.md
.EXAMPLE
    ./eng/Get-ReleaseNotes.ps1 -SelfTest
#>
param(
    [Parameter(Position = 0)][string] $Version,
    [string] $ChangelogPath = 'CHANGELOG.md',
    [string] $OutFile,
    [switch] $AllowMissing,
    [switch] $SelfTest
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-Section([string] $path, [string] $version, [bool] $allowMissing) {
    $lines = (Get-Content -LiteralPath $path -Raw) -split '\r?\n'
    $start = [array]::FindIndex($lines, [Predicate[string]] { param($l) $l -match "^## \[$([regex]::Escape($version))\]" })
    $section = @()
    if ($start -ge 0) {
        for ($i = $start + 1; $i -lt $lines.Length; $i++) {
            if ($lines[$i] -match '^## \[' -or $lines[$i] -match '^\[[^\]]+\]:\s') { break }
            $section += $lines[$i]
        }
    }
    $text = ($section -join "`n").Trim("`n", "`r", ' ', "`t")
    if ($text.Length -eq 0) {
        $message = "CHANGELOG.md has no (or an empty) section [$version]: rename [Unreleased] first."
        if (-not $allowMissing) { throw $message }
        Write-Warning $message
        return ''
    }
    $text
}

function Write-Notes([string] $text, [string] $outFile) {
    if ($outFile) {
        $content = if ($text.Length -eq 0) { '' } else { $text + "`n" }
        [System.IO.File]::WriteAllText([System.IO.Path]::GetFullPath($outFile, (Get-Location).Path), $content, [System.Text.UTF8Encoding]::new($false))
    }
    else {
        $text
    }
}

if ($SelfTest) {
    $dir = Join-Path ([System.IO.Path]::GetTempPath()) "relnotes-$([guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Path $dir | Out-Null
    try {
        $changelog = Join-Path $dir 'CHANGELOG.md'
        @'
# Changelog

## [Unreleased]

- pending

## [1.0.0] - 2030-01-01

### Added

- first
- second

## [1.0.0-rc.1]

## [0.9.0]

- old

[Unreleased]: https://example.invalid/compare/v1.0.0...HEAD
[1.0.0]: https://example.invalid/compare/v0.9.0...v1.0.0
'@ | Set-Content -LiteralPath $changelog

        $cases = 0
        function Assert-Throws([scriptblock] $block, [string] $what) {
            $threw = $false
            try { $null = & $block } catch { $threw = $true }
            if (-not $threw) { throw "Self-test failed: $what should throw." }
        }

        $expected = "### Added`n`n- first`n- second"
        $actual = (& $PSCommandPath -Version 1.0.0 -ChangelogPath $changelog) -join "`n"
        if ($actual -ne $expected) { throw "Self-test failed: 1.0.0 gave '$actual'." }
        $cases++
        Assert-Throws { & $PSCommandPath -Version 1.0.0-rc.1 -ChangelogPath $changelog } '1.0.0-rc.1 (empty)'
        $cases++
        $empty = & $PSCommandPath -Version 1.0.0-rc.1 -ChangelogPath $changelog -AllowMissing 3>$null
        if ($empty) { throw 'Self-test failed: 1.0.0-rc.1 -AllowMissing should be empty.' }
        $cases++
        Assert-Throws { & $PSCommandPath -Version 2.0.0 -ChangelogPath $changelog } '2.0.0 (missing)'
        $cases++
        $empty = & $PSCommandPath -Version 2.0.0 -ChangelogPath $changelog -AllowMissing 3>$null
        if ($empty) { throw 'Self-test failed: 2.0.0 -AllowMissing should be empty.' }
        $cases++
        Assert-Throws { & $PSCommandPath -Version Unreleased -ChangelogPath $changelog } 'Unreleased'
        $cases++

        $out = Join-Path $dir 'notes.md'
        $null = & $PSCommandPath -Version 1.0.0 -ChangelogPath $changelog -OutFile $out
        $bytes = [System.IO.File]::ReadAllBytes($out)
        if ([System.Text.Encoding]::UTF8.GetString($bytes) -ne "$expected`n") { throw 'Self-test failed: -OutFile content differs.' }
        if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF) { throw 'Self-test failed: -OutFile has a BOM.' }
        $cases++
        Write-Host "Self-test passed: $cases cases"
    }
    finally {
        Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
    }
    return
}

$resolved = & "$PSScriptRoot/Get-ReleaseVersion.ps1" $Version
Write-Notes (Get-Section $ChangelogPath $resolved $AllowMissing.IsPresent) $OutFile
