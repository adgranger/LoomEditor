<#
.SYNOPSIS
    Packs the six LoomEditor libraries into artifacts/packages and checks the content of every package.
.DESCRIPTION
    Not published anywhere: publishing to nuget.org is a manual decision. Fails with the list of all problems found.
#>
param([string] $Version)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root 'artifacts/packages'
$projects = 'Core', 'TextMate', 'Themes', 'View', 'Rendering', 'WinUI'
$sourceLinkUrl = 'https://raw.githubusercontent.com/adgranger/LoomEditor/'

New-Item -ItemType Directory -Force $out | Out-Null
Get-ChildItem $out -Include '*.nupkg', '*.snupkg' -Recurse | Remove-Item -Force

$versionArgs = @(if ($Version) { "-p:Version=$Version" })

foreach ($p in $projects) {
    dotnet pack (Join-Path $root "src/LoomEditor.$p") -c Release -o $out -warnaserror @versionArgs
    if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed for LoomEditor.$p" }
}

$version = (dotnet msbuild (Join-Path $root 'src/LoomEditor.Core') -getProperty:PackageVersion @versionArgs).Trim()
if ($Version -and $version -ne $Version) { throw "PackageVersion is $version, expected $Version." }
$problems = [System.Collections.Generic.List[string]]::new()

# Expected dependency ids per package; LoomEditor ids must have the package version.
$dependencies = @{
    Core      = @()
    TextMate  = @('LoomEditor.Core')
    Themes    = @('LoomEditor.Core')
    View      = @('LoomEditor.Core', 'LoomEditor.TextMate', 'LoomEditor.Themes')
    Rendering = @('LoomEditor.View', 'Microsoft.WindowsAppSDK', 'Microsoft.Graphics.Win2D')
    WinUI     = @('LoomEditor.Rendering', 'Microsoft.WindowsAppSDK', 'Microsoft.Graphics.Win2D')
}

function Read-Entry($entry) {
    $reader = [System.IO.StreamReader]::new($entry.Open())
    try { $reader.ReadToEnd() } finally { $reader.Dispose() }
}

foreach ($ext in 'nupkg', 'snupkg') {
    $files = @(Get-ChildItem $out -Filter "*.$ext")
    if ($files.Count -ne 6) { $problems.Add("expected 6 .$ext files, found $($files.Count)") }
}

foreach ($p in $projects) {
    $id = "LoomEditor.$p"
    foreach ($ext in 'nupkg', 'snupkg') {
        $path = Join-Path $out "$id.$version.$ext"
        if (-not (Test-Path $path)) { $problems.Add("$id.$version.$ext is missing"); continue }
        $zip = [System.IO.Compression.ZipFile]::OpenRead($path)
        try {
            $names = @($zip.Entries | ForEach-Object FullName)
            $tfmDirs = @($names | Where-Object { $_ -match '^lib/[^/]+/' } | ForEach-Object { ($_ -split '/')[1] } | Sort-Object -Unique)
            if ($tfmDirs.Count -eq 0) { $problems.Add("$id.$ext has no lib/<tfm>/ folder"); continue }
            $tfm = $tfmDirs[0]
            $need = if ($ext -eq 'nupkg') {
                @('README.md', 'NOTICE.md', "lib/$tfm/$id.dll", "lib/$tfm/$id.xml")
            } else {
                @("lib/$tfm/$id.pdb")
            }
            foreach ($n in $need) {
                if ($n -notin $names) { $problems.Add("$id.$ext misses $n") }
            }

            if ($ext -eq 'snupkg') {
                $pdb = $zip.GetEntry("lib/$tfm/$id.pdb")
                if ($pdb) {
                    $ms = [System.IO.MemoryStream]::new()
                    $s = $pdb.Open(); $s.CopyTo($ms); $s.Dispose()
                    $text = [System.Text.Encoding]::Latin1.GetString($ms.ToArray())
                    if (-not $text.Contains($sourceLinkUrl)) { $problems.Add("$id.pdb has no SourceLink map ($sourceLinkUrl)") }
                }
                continue
            }

            $readmeEntry = $zip.GetEntry('README.md')
            if ($readmeEntry) {
                $readme = Read-Entry $readmeEntry
                if (-not $readme.Contains("dotnet add package $id")) { $problems.Add("$id.nupkg README.md: it does not contain 'dotnet add package $id'") }
                foreach ($m in [regex]::Matches($readme, '\]\((?!https?://|#|mailto:)[^)]+\)')) {
                    $problems.Add("$id.nupkg README.md: relative link $($m.Value) breaks on nuget.org")
                }
            }

            $nuspecEntry = $zip.Entries | Where-Object { $_.FullName -like '*.nuspec' } | Select-Object -First 1
            if (-not $nuspecEntry) { $problems.Add("$id.nupkg has no .nuspec"); continue }
            $nuspec = [xml](Read-Entry $nuspecEntry)
            $meta = $nuspec.package.metadata
            if ($meta.license.type -ne 'expression' -or $meta.license.'#text' -ne 'MIT') {
                $problems.Add("$id.nuspec: license is not the MIT expression")
            }
            if (-not $meta.repository -or $meta.repository.type -ne 'git' -or
                -not "$($meta.repository.url)".StartsWith('https://github.com/adgranger/LoomEditor') -or
                "$($meta.repository.commit)" -notmatch '^[0-9a-fA-F]{40}$') {
                $problems.Add("$id.nuspec: repository must be git, under https://github.com/adgranger/LoomEditor, with a 40-hex commit")
            }
            if ($meta.id -ne $id) { $problems.Add("$id.nuspec: id is '$($meta.id)'") }
            if ($meta.version -ne $version) { $problems.Add("$id.nuspec: version is '$($meta.version)', expected $version") }
            if ($meta.authors -ne 'LoomEditor contributors') { $problems.Add("$id.nuspec: authors is '$($meta.authors)'") }
            if ([string]::IsNullOrWhiteSpace($meta.copyright)) { $problems.Add("$id.nuspec: copyright is empty") }
            if ($meta.projectUrl -ne 'https://github.com/adgranger/LoomEditor') { $problems.Add("$id.nuspec: projectUrl is '$($meta.projectUrl)'") }
            $tags = @("$($meta.tags)" -split '\s+')
            if ('editor' -notin $tags -or 'monaco' -notin $tags) { $problems.Add("$id.nuspec: tags must contain editor and monaco") }
            if ("$($meta.description)".Length -lt 40 -or $meta.description -eq $id) { $problems.Add("$id.nuspec: description is too short or equals the id") }
            if ([string]::IsNullOrWhiteSpace($meta.releaseNotes)) { $problems.Add("$id.nuspec: releaseNotes is empty") }
            if ($meta.readme -ne 'README.md') { $problems.Add("$id.nuspec: readme is '$($meta.readme)', expected README.md") }
            if ($meta.requireLicenseAcceptance -eq 'true') { $problems.Add("$id.nuspec: requireLicenseAcceptance is not false") }

            $found = @{}
            foreach ($group in @($meta.dependencies.group)) {
                foreach ($d in @($group.dependency)) { if ($d) { $found[$d.id] = $d.version } }
            }
            foreach ($d in $dependencies[$p]) {
                if (-not $found.ContainsKey($d)) { $problems.Add("$id.nuspec: dependency $d is missing"); continue }
                if ($d -like 'LoomEditor.*' -and $found[$d] -notmatch "^\[?$([regex]::Escape($version))") {
                    $problems.Add("$id.nuspec: dependency $d is $($found[$d]), expected $version")
                }
            }
            foreach ($d in $found.Keys) {
                if ($d -notin $dependencies[$p]) { $problems.Add("$id.nuspec: unexpected dependency $d") }
            }

            if ($p -eq 'TextMate') {
                foreach ($n in 'runtimes/win-x64/native/onig.dll', 'runtimes/win-arm64/native/onig.dll') {
                    if ($n -notin $names) { $problems.Add("$id.nupkg misses $n") }
                }
            }

            if ($p -eq 'WinUI') {
                foreach ($n in 'buildTransitive/LoomEditor.WinUI.targets', "lib/$tfm/LoomEditor.WinUI.pri") {
                    if ($n -notin $names) { $problems.Add("$id.nupkg misses $n") }
                }
                if (-not ($names | Where-Object { $_ -like '*Themes/Generic.xaml' -or $_ -like '*Themes/Generic.xbf' })) {
                    $problems.Add("$id.nupkg misses Themes/Generic.xaml (or .xbf)")
                }
            }
        } finally { $zip.Dispose() }
    }
}

if ($problems.Count -gt 0) {
    $problems | ForEach-Object { Write-Error $_ -ErrorAction Continue }
    throw "$($problems.Count) package problem(s) found"
}

Write-Host "Packed and checked: $version"
Get-ChildItem $out | ForEach-Object { Write-Host $_.Name }
