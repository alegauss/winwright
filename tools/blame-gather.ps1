#Requires -Version 5.1
<#
.SYNOPSIS
  Gather what a hung guest run's blame collector left, where the host can fetch it. WW406, WW607.

.DESCRIPTION
  Runs in the guest after a red run. vmrun copies by exact path and cannot glob, and the collector
  writes under a directory named for a GUID, by file names carrying a pid and a minute, so the guest
  finds the files and puts them where the host knows to look.

  WW406 brought back the largest dump. WW607: WinTimeline (WT168) hung with two dumps written, the
  application's and the test host's, and the largest was the test host's, so the one that said
  whether the application had deadlocked stayed in the guest. Now every dump comes back:

    blame.dmp           the test host's dump, which the host keeps as hangdump.dmp and reads
    blame-also-N.dmp    every other dump, under the cap, which the host keeps under its own name
    blame-dumps.txt     one line per other dump: what it was sent as, its own name, its size, and
                        'kept' or 'left' where the cap left it in the guest
    blame-sequence.xml  the newest sequence file
    blame.txt           one line saying what was gathered

  The collector writes each file twice, in place and under an In\<machine> folder it copies to, and
  two of the same file is not two readings, so a dump is taken once by its name.

  A script of its own, as carry.ps1 is, so the suite drives the one the guest runs.
#>
[CmdletBinding()]
param(
    # Define the function and do nothing else, so the suite can drive it.
    [switch] $DefineOnly,

    # Where the collector wrote, searched whole.
    [string] $Results,

    # Where the host fetches from.
    [string] $Sync,

    # How many bytes of other dumps one run brings back. The test host's comes back whatever its size.
    [long] $CapBytes = 2GB
)

<#
.SYNOPSIS
  Gather into -Sync what the collector left under -Results.
#>
function Invoke-BlameGather {
    param(
        [Parameter(Mandatory)] [string] $Results,
        [Parameter(Mandatory)] [string] $Sync,
        [long] $CapBytes = 2GB
    )

    $ErrorActionPreference = 'Stop'
    # Only what this gather writes: this script sits in the same folder, under a name that starts the
    # same way.
    foreach ($stale in @(Get-ChildItem -LiteralPath $Sync -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -in @('blame.dmp', 'blame-dumps.txt', 'blame-sequence.xml', 'blame.txt') -or $_.Name -like 'blame-also-*.dmp' })) {
        Remove-Item -LiteralPath $stale.FullName -Force
    }

    $said = @()

    # One of each name, the largest where the two copies differ.
    $dumps = @(Get-ChildItem -LiteralPath $Results -Recurse -File -Filter '*.dmp' -ErrorAction SilentlyContinue |
        Sort-Object Length -Descending |
        Group-Object Name |
        ForEach-Object { $_.Group[0] } |
        Sort-Object Length -Descending)

    # The test host's by its name, and the largest where none is named so - which is what WW406 took.
    $host_ = @($dumps | Where-Object { $_.Name -like 'testhost*' }) + @($dumps) | Select-Object -First 1
    if ($host_) {
        Copy-Item -LiteralPath $host_.FullName -Destination (Join-Path $Sync 'blame.dmp') -Force
        $said += "$($host_.Name) ($([math]::Round($host_.Length / 1MB, 1)) MB)"
    }

    $lines = @()
    $spent = 0L
    $index = 0
    foreach ($other in @($dumps | Where-Object { $host_ -and $_.FullName -ne $host_.FullName })) {
        $index++
        $sent = "blame-also-$index.dmp"
        if ($spent + $other.Length -gt $CapBytes) {
            $lines += "$sent|$($other.Name)|$($other.Length)|left"
            continue
        }

        Copy-Item -LiteralPath $other.FullName -Destination (Join-Path $Sync $sent) -Force
        $spent += $other.Length
        $lines += "$sent|$($other.Name)|$($other.Length)|kept"
        $said += "$($other.Name) ($([math]::Round($other.Length / 1MB, 1)) MB)"
    }

    if ($lines.Count -gt 0) {
        $lines | Set-Content -LiteralPath (Join-Path $Sync 'blame-dumps.txt') -Encoding ascii
    }

    $order = Get-ChildItem -LiteralPath $Results -Recurse -File -Filter 'Sequence_*.xml' -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($order) {
        Copy-Item -LiteralPath $order.FullName -Destination (Join-Path $Sync 'blame-sequence.xml') -Force
        $said += $order.Name
    }

    if ($said.Count -eq 0) { $said = @('nothing') }
    ($said -join '; ') | Set-Content -LiteralPath (Join-Path $Sync 'blame.txt') -Encoding ascii
}

<#
.SYNOPSIS
  What a dump is, by its file name: the test host, or the process it names. WW607.
#>
function Get-DumpOwner {
    param([Parameter(Mandatory)] [string] $Name)

    if ($Name -like 'testhost*') { return 'the test host' }
    $process = ($Name -split '_')[0]
    return "$process, a process the run drove"
}

if (-not $DefineOnly) {
    Invoke-BlameGather -Results $Results -Sync $Sync -CapBytes $CapBytes
}
