<#
  One adoption's run, captured from a desk rather than remembered. WW493.

  `capture-adoption.ps1` captured the half that fails before anything runs. This is the other
  half: `samples/Walkthrough` is an application with a window, a case written against it, and the
  four calls an adopting project writes - and this builds all of it and keeps what the run said.

  It needs a desk, because the case creates a real window and reads it. Run it the way the suite
  is run:

      run-tests-vm.cmd -Run "capture-walkthrough.cmd"

  and the capture comes back in the working tree. On the machine somebody is sitting at it works
  too and takes the foreground for a second, which is why it is not part of any gate.

  It needs `pack-local.cmd` to have run, because the sample restores the engine from `packages/`
  the way an adopting repository restores it from a feed. That is the point rather than a chore:
  a capture taken against a project reference would prove this checkout, and what an adopter has
  is the package.
#>
[CmdletBinding()]
param(
    # Where to write the capture, relative to the repository root.
    [string] $Into = 'site/docs/src/data/walkthrough.captured.json',

    # Skip the pack, for a second capture against a feed that is already current.
    [switch] $NoPack)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$sample = Join-Path $root 'samples/Walkthrough'

# The same reason `capture-adoption.ps1` gives: the page is written in English and the SDK answers
# in whatever language the machine is in.
$spoke = $env:DOTNET_CLI_UI_LANGUAGE
$env:DOTNET_CLI_UI_LANGUAGE = 'en'

<#
  .SYNOPSIS
  Run one command and keep everything it said, with this machine's paths taken out.
#>
function Ran {
    param(
        [Parameter(Mandatory)] [string] $What,
        [Parameter(Mandatory)] [string] $In,
        [Parameter(Mandatory)] [string[]] $Argv)

    Push-Location $In
    try {
        $said = & dotnet @Argv 2>&1 | ForEach-Object { "$_" }
        $code = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }

    $text = ($said -join "`n")
    foreach ($path in @($sample, $root)) {
        $text = $text.Replace($path, '<your repository>').Replace($path.Replace('\', '/'), '<your repository>')
    }

    return [ordered]@{
        what    = $What
        command = "dotnet $($Argv -join ' ')"
        code    = $code
        said    = $text.TrimEnd()
    }
}

if (-not $NoPack) {
    Push-Location $root
    try {
        & "$root\pack-local.cmd" Release | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "pack-local.cmd failed, so the sample has no package to restore" }
    }
    finally {
        Pop-Location
    }
}

$steps = @()
$steps += Ran 'building the application under test' $sample @('build', 'app/Walkthrough.App.csproj', '--nologo')
$steps += Ran 'building the project that drives it' $sample @('build', 'driving/Walkthrough.Run.csproj', '--nologo')
$steps += Ran 'running every case this sample declares' $sample @('run', '--project', 'driving/Walkthrough.Run.csproj', '--no-build')

$capture = [ordered]@{
    declaration = (Get-Content -LiteralPath (Join-Path $sample 'winwright.json') -Raw).TrimEnd()
    case        = (Get-Content -LiteralPath (Join-Path $sample 'cases/walkthrough.cases.json') -Raw).TrimEnd()
    sdk         = (& dotnet --version).Trim()
    steps       = $steps
}

$env:DOTNET_CLI_UI_LANGUAGE = $spoke

$out = Join-Path $root $Into
New-Item -ItemType Directory -Path (Split-Path -Parent $out) -Force | Out-Null
[System.IO.File]::WriteAllText($out, ($capture | ConvertTo-Json -Depth 6) + "`n", [System.Text.UTF8Encoding]::new($false))

Write-Output "capture-walkthrough: $($steps.Count) step(s), exit codes $(($steps | ForEach-Object { $_.code }) -join ', ') -> $Into"
