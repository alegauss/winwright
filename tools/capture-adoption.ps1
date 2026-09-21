<#
  One adoption's build, captured from a run rather than remembered. WW493.

  Every code block in the documentation area was typed by whoever wrote the page. That is normal,
  and it is also the thing this project refuses everywhere else: a figure true on the day it was
  typed and silent about the day it stopped being. A walkthrough misleads at the first message
  whose wording changed, and the refusals are exactly where somebody decides a tool is not worth
  the trouble.

  So this builds a throwaway repository laid out the way an adopting one is - the application's
  own project at the root, the driving project underneath it - and runs the build twice. Once
  without the line that stops the application's globs reaching down, which is how an adoption
  fails for most people the first time; once with it. What the page shows is what these two runs
  printed.

  It writes `site/docs/src/data/adoption.captured.json`, which is COMMITTED rather than generated
  at build time. The site builds on a runner with no .NET, so a capture produced there is a
  capture that cannot exist - and a capture nobody can regenerate is the paragraph this replaces.
  `capture-adoption.cmd` is how it is regenerated; `site/scripts/adoption.test.mjs` is what holds
  it to this tree.

  No package reference and no desk. Both halves of the adoption that need one - the restore from
  a packed feed, and a case run against a real window - are what WW493 still owes; this is the
  half an adopter meets first and it answers in seconds.
#>
[CmdletBinding()]
param(
    # Where to build the throwaway repository. Deleted and rebuilt on every run.
    [string] $At = (Join-Path ([System.IO.Path]::GetTempPath()) 'winwright-adoption'),

    # Where to write the capture, relative to the repository root.
    [string] $Into = 'site/docs/src/data/adoption.captured.json')

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

# The output goes onto a page written in English, and the SDK answers in whatever language the
# machine that ran it is in. A capture taken on this repository's own desk came back in Portuguese
# - correct, reproducible, and unreadable to most of the people the page is for. So the language is
# part of what is captured rather than a property of whoever captured it.
$spoke = $env:DOTNET_CLI_UI_LANGUAGE
$env:DOTNET_CLI_UI_LANGUAGE = 'en'

# The line the repair is about, read out of this repository's own adopter rather than typed here.
# A page teaching a line nobody uses is worse than no page: `Adopter.csproj` carries it because
# removing it is what breaks the sample, so it is the one spelling worth publishing.
$adopter = Join-Path $root 'samples/Adopter/Adopter.csproj'
$excludes = (Get-Content -LiteralPath $adopter | Where-Object { $_ -match '<DefaultItemExcludes>' }) `
    | Select-Object -First 1
if (-not $excludes) {
    throw "samples/Adopter/Adopter.csproj no longer carries a DefaultItemExcludes line to publish"
}

$excludes = $excludes.Trim()

if (Test-Path -LiteralPath $At) { Remove-Item -LiteralPath $At -Recurse -Force }
New-Item -ItemType Directory -Path (Join-Path $At 'driving') -Force | Out-Null

# The shape, and nothing else in it. An application project at the repository root with WPF on,
# and a driving project beneath it - which is the layout every default glob the SDK applies
# reaches down through.
$app = @'
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>

</Project>
'@

$driving = @'
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
  </PropertyGroup>

</Project>
'@

$written = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText((Join-Path $At 'YourApp.csproj'), $app, $written)
[System.IO.File]::WriteAllText((Join-Path $At 'Window.cs'), "namespace YourApp;`r`n`r`npublic static class Shipped;`r`n", $written)
[System.IO.File]::WriteAllText((Join-Path $At 'driving/Driving.csproj'), $driving, $written)
[System.IO.File]::WriteAllText((Join-Path $At 'driving/Drives.cs'), "namespace YourApp.Driving;`r`n`r`npublic static class Drives;`r`n", $written)

<#
  .SYNOPSIS
  Run one command in the throwaway repository and keep everything it said.

  .DESCRIPTION
  Both streams, because a build failure says what it says on whichever one it chooses and a
  capture missing half of it is a capture of a run nobody had. The paths are rewritten to the
  repository they are relative to: an absolute temp path on the machine that captured is the one
  detail on the page that could not possibly be a reader's.
#>
function Ran {
    param([Parameter(Mandatory)] [string] $What, [Parameter(Mandatory)] [string[]] $Argv)

    Push-Location $At
    try {
        $said = & dotnet @Argv 2>&1 | ForEach-Object { "$_" }
        $code = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }

    $text = ($said -join "`n").Replace($At, '<your repository>').Replace($At.Replace('\', '/'), '<your repository>')

    return [ordered]@{
        what    = $What
        command = "dotnet $($Argv -join ' ')"
        code    = $code
        said    = $text.TrimEnd()
    }
}

$steps = @()

# The driving project first, which is the ordering that matters and the one an adopter takes
# without thinking: its build writes the generated attributes the application then compiles a
# second copy of. Built alone it is perfectly fine, which is why the failure arrives later and
# about the wrong project.
$steps += Ran 'building the driving project on its own' @('build', 'driving/Driving.csproj', '--nologo')
$steps += Ran 'building the application, with nothing stopping its globs' @('build', 'YourApp.csproj', '--nologo')

# The repair, in the application's own project file.
$repaired = $app -replace '(?m)^(\s*)<UseWPF>true</UseWPF>', "`$1<UseWPF>true</UseWPF>`r`n`$1$excludes"
[System.IO.File]::WriteAllText((Join-Path $At 'YourApp.csproj'), $repaired, $written)

$steps += Ran 'building it again, with the one line that stops them' @('build', 'YourApp.csproj', '--nologo')

$env:DOTNET_CLI_UI_LANGUAGE = $spoke

$capture = [ordered]@{
    shape    = [ordered]@{
        application = 'YourApp.csproj'
        driving     = 'driving/Driving.csproj'
    }
    excludes = $excludes
    sdk      = (& dotnet --version).Trim()
    steps    = $steps
}

$out = Join-Path $root $Into
New-Item -ItemType Directory -Path (Split-Path -Parent $out) -Force | Out-Null
[System.IO.File]::WriteAllText($out, ($capture | ConvertTo-Json -Depth 6) + "`n", $written)

Write-Output "capture-adoption: $($steps.Count) step(s), exit codes $(($steps | ForEach-Object { $_.code }) -join ', ') -> $Into"
