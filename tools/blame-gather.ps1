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
    blame-crash-N.wer   WW623: every Windows Error Reporting report written in the guest since the
                        run's tree was made, which is what a process that crashed leaves
    blame-crashes.txt   one line per report: what it was sent as, and what it says crashed, with
                        what code, in which module, at which offset
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
    [long] $CapBytes = 2GB,

    # WW623. The run's tree in the guest. Its creation is when this run began on the guest's own
    # clock, and a crash report written since is this run's. Without it no report is gathered.
    [string] $Tree
)

# WW623. Where Windows Error Reporting keeps what it wrote: the user's own and the machine's, each
# in the queue a report waits in and the archive it goes to once sent.
$script:WerRoots = @(
    (Join-Path $env:LOCALAPPDATA 'Microsoft\Windows\WER\ReportQueue'),
    (Join-Path $env:LOCALAPPDATA 'Microsoft\Windows\WER\ReportArchive'),
    (Join-Path $env:ProgramData 'Microsoft\Windows\WER\ReportQueue'),
    (Join-Path $env:ProgramData 'Microsoft\Windows\WER\ReportArchive')
)

<#
.SYNOPSIS
  Gather into -Sync what the collector left under -Results.
#>
function Invoke-BlameGather {
    param(
        [Parameter(Mandatory)] [string] $Results,
        [Parameter(Mandatory)] [string] $Sync,
        [long] $CapBytes = 2GB,

        # WW623. Crash reports written at or after this are gathered; none where it is not given.
        [Nullable[datetime]] $Since = $null,

        # Where they are looked for. The machine's own by default; a case names its own.
        [string[]] $WerRoots = $script:WerRoots
    )

    $ErrorActionPreference = 'Stop'
    # Only what this gather writes: this script sits in the same folder, under a name that starts the
    # same way.
    foreach ($stale in @(Get-ChildItem -LiteralPath $Sync -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -in @('blame.dmp', 'blame-dumps.txt', 'blame-sequence.xml', 'blame.txt', 'blame-crashes.txt') -or $_.Name -like 'blame-also-*.dmp' -or $_.Name -like 'blame-crash-*.wer' })) {
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

    # WW623. What crashed, where the collector's dumps say only what hung: a fail-fast leaves no dump
    # the collector writes and an exit code the case reports, and the report is the one place the
    # faulting module is.
    if ($null -ne $Since) {
        $crashes = @()
        $index = 0
        foreach ($report in @(Get-CrashReports -Since $Since -WerRoots $WerRoots)) {
            $index++
            $sent = "blame-crash-$index.wer"
            Copy-Item -LiteralPath $report.FullName -Destination (Join-Path $Sync $sent) -Force
            $crashes += "$sent|$(Get-CrashSummary -Report $report.FullName)"
        }

        if ($crashes.Count -gt 0) {
            $crashes | Set-Content -LiteralPath (Join-Path $Sync 'blame-crashes.txt') -Encoding ascii
            $said += "$($crashes.Count) crash report(s)"
        }
    }

    if ($said.Count -eq 0) { $said = @('nothing') }
    ($said -join '; ') | Set-Content -LiteralPath (Join-Path $Sync 'blame.txt') -Encoding ascii
}

<#
.SYNOPSIS
  Every Report.wer written under the roots at or after -Since, oldest first. WW623.
#>
function Get-CrashReports {
    param([Parameter(Mandatory)] [datetime] $Since, [string[]] $WerRoots)

    return @($WerRoots |
        Where-Object { $_ -and (Test-Path -LiteralPath $_) } |
        ForEach-Object { Get-ChildItem -LiteralPath $_ -Recurse -File -Filter 'Report.wer' -ErrorAction SilentlyContinue } |
        Where-Object { $_.LastWriteTime -ge $Since } |
        Sort-Object LastWriteTime)
}

<#
.SYNOPSIS
  What one report says crashed, in one line: the executable, and what faulted where. WW623.

.DESCRIPTION
  Read by position and never by the signature's names, which Windows writes in the guest's own
  language: a pt-BR guest names every one "Assinatura do Problema 0N". What each position means is
  fixed per event type, so the type decides the reading:

    APPCRASH        3 module, 4 its version, 6 exception code, 7 offset
    BEX, BEX64      3 module, 4 its version, 6 offset, 7 exception code (a fail-fast, 0xC0000409, is one)
    CLR20r3         3 assembly, 4 its version, 6 method token, 7 IL offset, 8 the exception's type

  Any other type is said with its values as they stand, rather than as nothing.
#>
function Get-CrashSummary {
    param([Parameter(Mandatory)] [string] $Report)

    # Windows writes these as UTF-16 with a byte order mark; Get-Content reads the mark and decodes.
    $fields = @{}
    $sig = @{}
    foreach ($line in (Get-Content -LiteralPath $Report)) {
        $split = $line.IndexOf('=')
        if ($split -lt 1) { continue }
        $key = $line.Substring(0, $split)
        $value = $line.Substring($split + 1)
        if ($key -match '^Sig\[(\d+)\]\.Value$') { $sig[[int]$Matches[1]] = $value; continue }
        $fields[$key] = $value
    }

    $app = if ($fields['AppPath']) { Split-Path -Leaf $fields['AppPath'] } elseif ($sig[0]) { $sig[0] } else { 'an application' }
    $kind = if ($fields['EventType']) { $fields['EventType'] } else { 'a report' }
    $in = if ($sig[3]) { " in $($sig[3])$(if ($sig[4]) { " $($sig[4])" })" } else { '' }

    switch -Regex ($kind) {
        '^APPCRASH$' { return "$app ($kind): exception $($sig[6])$in at offset $($sig[7])" }
        '^BEX(64)?$' { return "$app ($kind): exception $($sig[7])$in at offset $($sig[6])" }
        '^CLR20r3$' { return "$app ($kind): $($sig[8])$in, method $($sig[6]) at IL offset $($sig[7])" }
        default {
            $values = @($sig.Keys | Sort-Object | ForEach-Object { $sig[$_] }) -join ', '
            return "$app ($kind): $values"
        }
    }
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
    $since = if ($Tree -and (Test-Path -LiteralPath $Tree)) { (Get-Item -LiteralPath $Tree).CreationTime } else { $null }
    Invoke-BlameGather -Results $Results -Sync $Sync -CapBytes $CapBytes -Since $since
}
