#Requires -Version 5.1
<#
.SYNOPSIS
  Drive the runner's own failure arms in the guest, against a scratch tree. WW420.

.DESCRIPTION
  The fixture exists because a refusal nobody can provoke is a refusal that will quietly stop
  working. The runner has arms of its own that fire only on a run that has already gone wrong, and
  what the suite holds about them is their source: DeskProbeTests reads run-tests-vm.ps1 and runs
  the gather it generates against a hand-built tree. That catches an arm that was deleted. It says
  nothing about the host half - the vmrun call that runs the gather in the guest, the copies back
  under the names a reader asks for, the line a red run prints - which is the half WW406 could only
  prove by hand, with a scratch command and a fake dump.

  This is that proof, kept. WW227 gave the runner -Tree, -Name, -Run and -ResultsIn so an adopter
  could drive its own cases; pointed at a scratch tree whose command stands in for a suite that went
  wrong, they drive the runner's failure paths instead. No suite is built and nothing of this
  repository is carried, so what it costs is the runner's own overhead and the time the arms take.

  Three runs, in an order that is the design:
    the bound  a command that outlives -Bound 1, from an executable under the tree, so the refusal
               has a holder to name
    the held   a second run straight after, whose sync meets that command still holding the tree
    the red    once the command has ended: a run that loses its test host, whose gather keeps the
               collector's files - and which syncs at all only if the held tree came free

  Under a -Name of its own, so the guest folders it syncs are never the ones run-tests-vm.cmd uses
  for this repository. A scratch run that went wrong leaves C:\src\winwright-arms behind and nothing
  else.

  Three answers, and they are the runner's own three kinds of ending:
    0  every arm driven here came back as the runner says it does
    1  an arm was reached and came back wrong - the runner's defect
    2  an arm was never reached (a locked desk, no VM, a tree that came free early), so it was not
       observed; nothing came back wrong

.PARAMETER Name
  What the guest calls the scratch tree. Distinct from this repository's name on purpose.

.PARAMETER HoldSeconds
  How long the bound's command holds the tree. It has to outlast the bound, the reading the bound
  takes and the second run's way to its sync; four minutes is twice what that was measured at.
#>
[CmdletBinding()]
param(
    [string] $Name = 'winwright-arms',

    [int] $HoldSeconds = 240
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$runner = Join-Path $PSScriptRoot 'run-tests-vm.ps1'
$scratch = Join-Path ([IO.Path]::GetTempPath()) $Name
$guestTree = "C:\src\$Name"

# Not 3, and that is the one number it must not be: the runner's Refuse exits 3, so a scratch run
# exiting 3 would read the same as a runner that never started it.
$verdict = 7

# Every run names the same command, and what the command does is the file it runs. That is not
# tidiness. cmd reads a batch file from disk as it goes, the runner rewrites run.cmd on every run, and
# the bound's run.cmd is still executing when the next run replaces it - so a second run with a
# different -Run would hand the first one a line of its own from the middle of a different file. With
# one command the two files differ only in the run's name, which is the same width every time.
$command = 'powershell -NoProfile -ExecutionPolicy Bypass -File arm.ps1'

# Held from under the tree, because that is what the holder walk names: a process running from an
# executable there. A command that only had the tree as its directory would hold it just the same and
# be named by nothing, and the arm this proves is the one that says who.
$hold = @"
Copy-Item -LiteralPath (Join-Path `$env:SystemRoot 'System32\PING.EXE') -Destination 'held.exe' -Force
& .\held.exe -n $HoldSeconds 127.0.0.1 | Out-Null
exit 0
"@

# The collector's two files as a lost test host leaves them: a dump and a sequence under a directory
# named for a GUID, and the second copy of each under In\<machine>, the smaller dump and the older
# sequence. The gather is meant to keep the larger and the newer, so the pair is what proves it chose.
$red = @"
`$under = New-Item -ItemType Directory -Force -Path (Join-Path 'out' ([guid]::NewGuid().ToString()))
`$copied = New-Item -ItemType Directory -Force -Path (Join-Path 'out' 'oobe_MACHINE\In\MACHINE')

[IO.File]::WriteAllBytes((Join-Path `$under.FullName 'testhost_420_hangdump.dmp'), (New-Object byte[] 65536))
[IO.File]::WriteAllBytes((Join-Path `$copied.FullName 'testhost_420_hangdump.dmp'), (New-Object byte[] 1024))

`$older = Join-Path `$copied.FullName 'Sequence_older.xml'
Set-Content -LiteralPath `$older -Value '<TestSequence />' -Encoding ascii
(Get-Item -LiteralPath `$older).LastWriteTimeUtc = [DateTime]::UtcNow.AddMinutes(-10)

Set-Content -LiteralPath (Join-Path `$under.FullName 'Sequence_newest.xml') -Value '<TestSequence><Test Name="the last one" /></TestSequence>' -Encoding ascii
Set-Content -LiteralPath (Join-Path 'out' 'first.txt') -Value 'the first of -Bring, fetched on a red run as well' -Encoding ascii

exit $verdict
"@

if (Test-Path -LiteralPath $scratch) { Remove-Item -LiteralPath $scratch -Recurse -Force }
$null = New-Item -ItemType Directory -Path $scratch

# What the runner brings back lands under the tree it carried, and three runs carry this tree: the
# second would otherwise carry the first one's readings into the guest as though they were source.
Set-Content -LiteralPath (Join-Path $scratch '.gitignore') -Value 'TestResults/' -Encoding ascii

# A repository and nothing more: the runner carries what `git ls-files` lists, untracked files
# included, so an initialised tree with no commit is a tree it can carry.
Push-Location $scratch
try {
    & git init -q
    if ($LASTEXITCODE -ne 0) { throw "git would not initialise $scratch" }
}
finally { Pop-Location }

Write-Host "runner-arms  $scratch  ->  $guestTree" -ForegroundColor Cyan

function Invoke-Arm {
    <#
      One run of the runner with this arm's command in the tree, answered as what it said and what it
      exited with.

      A process of its own, because the runner ends in `exit` and speaks through Write-Host: in this
      process the first would end the caller and the second would not be captured. Continue for the
      length of the call, because Windows PowerShell wraps each stderr line of a native command in an
      error record, and under Stop the first one would end this script halfway through the guest.
    #>
    param([Parameter(Mandatory)] [string] $Arm, [Parameter(Mandatory)] [string] $Script, [int] $Bound = 0)

    Set-Content -LiteralPath (Join-Path $scratch 'arm.ps1') -Value $Script -Encoding ascii

    $asked = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $runner,
        '-Tree', $scratch, '-Name', $Name, '-Run', $command, '-ResultsIn', 'out', '-Bring', 'first.txt')
    if ($Bound -gt 0) { $asked += @('-Bound', $Bound) }

    Write-Host ''
    Write-Host "  $Arm" -ForegroundColor Cyan

    $ErrorActionPreference = 'Continue'
    $said = @(& powershell.exe @asked 2>&1 | ForEach-Object { "$_" })
    $code = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'

    foreach ($line in $said) { Write-Host "  | $line" }

    return [pscustomobject]@{ Said = $said; Code = $code }
}

$broke = New-Object System.Collections.ArrayList
$unseen = New-Object System.Collections.ArrayList

function Held {
    param([Parameter(Mandatory)] [string] $Arm, [Parameter(Mandatory)] [bool] $Holds, [string] $Instead)
    if ($Holds) { Write-Host "  held   $Arm" -ForegroundColor Green }
    else {
        Write-Host "  BROKE  $Arm - $Instead" -ForegroundColor Red
        $null = $broke.Add($Arm)
    }
}

function Unseen {
    param([Parameter(Mandatory)] [string] $Arm, [Parameter(Mandatory)] [string] $Because)
    Write-Host "  unseen $Arm - $Because" -ForegroundColor Yellow
    $null = $unseen.Add($Arm)
}

# Every sentence read out of the runner below is spelled whole, so RunnerArmsTests can hold it to the
# runner's own spelling: a runner that reworded a line would otherwise read here as one that never got
# there, and a broken arm would be reported as one nobody observed.
function Get-Ending {
    # Empty strings allowed, and they are most of it: the runner prints a blank line between every
    # stage, and a mandatory string array refuses the whole run's output for the first one.
    param([Parameter(Mandatory)] [AllowEmptyString()] [string[]] $Said)
    return @($Said | Where-Object {
        $_ -eq 'run-tests-vm: the guest run passed.' -or $_ -match '^run-tests-vm: the guest run exited -?\d+\.$'
    })
}

$holder = "runs from $guestTree\held.exe"

# --- the bound ------------------------------------------------------------------------------------

$began = Get-Date
$bound = Invoke-Arm -Arm 'the bound: a command that outlives -Bound 1' -Script $hold -Bound 1
Write-Host ''

$gaveUp = @($bound.Said | Where-Object { $_.StartsWith('run-tests-vm: the guest run did not answer within 1 minute(s).') })
if ($gaveUp.Count -eq 1) {
    Held 'the bound refuses a run that outlives it' ($bound.Code -eq 3) "it exited $($bound.Code)"
    Held 'the bound names what is still running from the tree' `
        ($gaveUp[0].Contains($holder)) "the refusal read '$($gaveUp[0])'"
}
elseif (@(Get-Ending -Said $bound.Said).Count -gt 0) {
    Held 'the bound refuses a run that outlives it' $false 'the runner waited the command out and reached its end'
}
else {
    Unseen 'the bound' "the runner stopped before it (exit $($bound.Code))"
}

# --- the held tree --------------------------------------------------------------------------------

# Straight after, with the bound's command still running: the tree it holds is what this sync meets.
# Given the same bound, so a sync that did not refuse costs a minute rather than the whole hold.
$second = Invoke-Arm -Arm 'the held tree: the next sync, with that command still running' -Script $hold -Bound 1
Write-Host ''

$refused = @($second.Said | Where-Object { $_.StartsWith('run-tests-vm: the guest tree is held open by ') })
if ($refused.Count -eq 1) {
    Held 'a sync meeting a held tree refuses' ($second.Code -eq 3) "it exited $($second.Code)"
    Held 'the sync names what holds the tree' ($refused[0].Contains($holder)) "the refusal read '$($refused[0])'"
}
else {
    Unseen 'the held tree' "the second run did not meet a held tree (exit $($second.Code)), so the command had ended or the runner stopped first"
}

# --- the red run ----------------------------------------------------------------------------------

# Once the command has ended, which is the hold counted from before the bound's run began plus the two
# minutes that run can take to get into the guest. Waited out rather than polled: the only door into
# the guest is a run, and a run started while the command is still going would be the held arm again
# with nobody asking for it.
$until = $began.AddSeconds($HoldSeconds + 120)
$left = [int]($until - (Get-Date)).TotalSeconds
if ($left -gt 0) {
    Write-Host ''
    Write-Host "  waiting    ${left}s for the held command to end"
    Start-Sleep -Seconds $left
}

# Asked twice where the first meets the tree still held. The wait above is an estimate of when the
# bound's run reached the guest, which this script cannot see; a guest slow to start that run is a
# late command and not a tree that stays held. Twice and not until it works, because a tree that is
# still held a minute past that is the finding.
$lost = $null
foreach ($try in 1..2) {
    $lost = Invoke-Arm -Arm 'the red run: a test host lost, after the tree came free' -Script $red
    Write-Host ''

    $stillHeld = @($lost.Said | Where-Object { $_.StartsWith('run-tests-vm: the guest tree is held open by ') }).Count -gt 0
    if (-not $stillHeld -or $try -eq 2) { break }

    Write-Host '  waiting    60s: the tree was still held, which is the estimate above being short or the finding'
    Start-Sleep -Seconds 60
}

$results = Join-Path $scratch 'TestResults\vm'
# Wrapped at the call, because a function returning one line hands back the line and not a list of one.
$ended = @(Get-Ending -Said $lost.Said)

if ($stillHeld) {
    Held 'a tree held by a command that ended is free for the next sync' $false `
        "it was still held $([int]((Get-Date) - $began).TotalSeconds)s after the bound's run began, against a hold of ${HoldSeconds}s"
}
elseif ($ended.Count -eq 0) {
    Unseen 'the red run' "the runner stopped before it (exit $($lost.Code))"
}
else {
    if ($refused.Count -eq 1) {
        Held 'a tree held by a command that ended is free for the next sync' $true ''
    }

    Held 'a red guest run is the runner''s exit code' ($lost.Code -eq $verdict) "it exited $($lost.Code)"

    Held 'a red guest run says so in its last line' `
        ($ended[0] -eq "run-tests-vm: the guest run exited $verdict.") "it said '$($ended[0])'"

    Held 'the first of -Bring is fetched on a red run' `
        (Test-Path -LiteralPath (Join-Path $results 'first.txt')) "no first.txt under $results"

    $blame = @($lost.Said | Where-Object { $_.StartsWith('  blame       ') })
    Held 'the gather says what it found, by the collector''s own names' `
        ($blame.Count -eq 1 -and $blame[0] -match 'testhost_420_hangdump\.dmp' -and $blame[0] -match 'Sequence_newest\.xml') `
        "the blame line read '$($blame -join ' / ')'"

    $dump = Join-Path $results 'hangdump.dmp'
    Held 'the larger dump comes back as hangdump.dmp' `
        ((Test-Path -LiteralPath $dump) -and (Get-Item -LiteralPath $dump).Length -eq 65536) `
        $(if (Test-Path -LiteralPath $dump) { "it is $((Get-Item -LiteralPath $dump).Length) bytes" } else { 'nothing came back' })

    $sequence = Join-Path $results 'sequence.xml'
    Held 'the newer sequence comes back as sequence.xml' `
        ((Test-Path -LiteralPath $sequence) -and ((Get-Content -LiteralPath $sequence -Raw) -match 'the last one')) `
        $(if (Test-Path -LiteralPath $sequence) { 'it is the older copy' } else { 'nothing came back' })
}

Write-Host ''
Write-Host '  not driven here, and why:'
Write-Host '    a guest with no .NET SDK, which cannot be provoked without taking the SDK away from the guest'

# Not asserted, and deliberately. The dump is zeroes, so "could not be read" is the right answer -
# but Show-Blame prints that same sentence when dotnet could not run the reader at all, and a check
# that passes for either reason is a green over a reading nobody took. WW441 is that sentence.
Write-Host '    the reader on the dump that came back, whose sentence is the same for an unreadable dump and a reader that never ran (WW441)'
Write-Host ''

if ($broke.Count -gt 0) {
    Write-Host "runner-arms: $($broke.Count) arm(s) came back wrong." -ForegroundColor Red
    exit 1
}

if ($unseen.Count -gt 0) {
    Write-Host "runner-arms: $($unseen.Count) arm(s) were never reached, so nothing here says whether they work." -ForegroundColor Yellow
    exit 2
}

Write-Host 'runner-arms: every arm driven here came back as the runner says it does.' -ForegroundColor Green
exit 0
