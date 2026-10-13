<#
.SYNOPSIS
    Adds every new public symbol of a project to its PublicAPI.Unshipped.txt.

.DESCRIPTION
    The PublicApiAnalyzers report RS0016 ("symbol is not part of the declared public API") when you add
    a public member. Visual Studio / Rider can fix it with a code fix; this script does the same from the
    command line. Review the diff of PublicAPI.Unshipped.txt before committing: every line is a promise
    to the users of the NuGet package.

.EXAMPLE
    ./eng/Update-PublicApi.ps1 src/LoomEditor.Core
#>
param(
    [Parameter(Mandatory)] [string] $ProjectDirectory
)

$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_UI_LANGUAGE = 'en'

$project = Get-ChildItem -Path $ProjectDirectory -Filter *.csproj | Select-Object -First 1
if (-not $project) { throw "No .csproj found in '$ProjectDirectory'." }

$output = dotnet build $project.FullName -c Debug --no-incremental -p:TreatWarningsAsErrors=false 2>&1
$symbols = $output |
    Select-String -Pattern "RS0016: Symbol '(.+?)' is not part of the declared public API" |
    ForEach-Object { $_.Matches[0].Groups[1].Value } |
    Sort-Object -Unique

$unshipped = Join-Path $project.DirectoryName 'PublicAPI.Unshipped.txt'
$existing = if (Test-Path $unshipped) { Get-Content $unshipped } else { @('#nullable enable') }
$header = $existing | Where-Object { $_ -like '#*' }
$lines = @($existing | Where-Object { $_ -notlike '#*' -and $_ -ne '' }) + @($symbols) | Sort-Object -Unique

Set-Content -Path $unshipped -Value (@($header) + $lines) -Encoding utf8NoBOM
Write-Host "Added $(@($symbols).Count) symbol(s) to $unshipped"
