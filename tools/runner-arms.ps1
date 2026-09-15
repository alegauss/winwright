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
  could drive its own cases; pointed at a scratch tree whose command stands in for a suite that lost
  its test host, they drive the runner's failure path instead. No suite is built and nothing of this
  repository is carried, so what it costs is the runner's own overhead: the desk probe, the carry of
  two files, and the gather.

  Under a -Name of its own, so the guest folders it syncs are never the ones run-tests-vm.cmd uses
  for this repository. A scratch run that went wrong leaves C:\src\winwright-arms behind and nothing
  else.

  Three answers, and they are the runner's own three kinds of ending:
    0  every arm driven here came back as the runner says it does
    1  an arm was reached and came back wrong - the runner's defect
    2  the runner stopped before reaching the arms (a locked desk, no VM), so nothing was observed

.PARAMETER Name
  What the guest calls the scratch tree. Distinct from this repository's name on purpose.
#>
[CmdletBinding()]
param(
    [string] $Name = 'winwright-arms'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$runner = Join-Path $PSScriptRoot 'run-tests-vm.ps1'
$scratch = Join-Path ([IO.Path]::GetTempPath()) $Name

# Not 3, and that is the one number it must not be: the runner's Refuse exits 3, so a scratch run
# exiting 3 would read the same as a runner that never started it.
$verdict = 7

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
Set-Content -LiteralPath (Join-Path $scratch 'red.ps1') -Value $red -Encoding ascii

# A repository and nothing more: the runner carries what `git ls-files` lists, untracked files
# included, so an initialised tree with no commit is a tree it can carry.
Push-Location $scratch
try {
    & git init -q
    if ($LASTEXITCODE -ne 0) { throw "git would not initialise $scratch" }
}
finally { Pop-Location }

Write-Host "runner-arms  $scratch  ->  C:\src\$Name" -ForegroundColor Cyan

# A process of its own, because the runner ends in `exit` and speaks through Write-Host: in this
# process the first would end the caller and the second would not be captured.
#
# Continue for the length of the call. Windows PowerShell wraps each stderr line of a native command
# in an error record, and under Stop the first one - a vmrun warning, say - would end this script with
# the runner still halfway through the guest.
$ErrorActionPreference = 'Continue'
$said = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $runner `
    -Tree $scratch -Name $Name `
    -Run 'powershell -NoProfile -ExecutionPolicy Bypass -File red.ps1' `
    -ResultsIn 'out' -Bring 'first.txt' 2>&1 | ForEach-Object { "$_" }
$code = $LASTEXITCODE
$ErrorActionPreference = 'Stop'

foreach ($line in $said) { Write-Host "  | $line" }
Write-Host ''

# The runner reached its last line or it did not. Short of that nothing below was exercised, and
# reporting the arms as broken would blame the runner for a desk it was refused.
#
# Every sentence read out of the runner below is spelled whole, so RunnerArmsTests can hold it to
# the runner's own spelling: a runner that reworded its last line would otherwise read here as one
# that never got there, and a broken arm would be reported as nothing observed.
$ended = @($said | Where-Object {
    $_ -eq 'run-tests-vm: the guest run passed.' -or $_ -match '^run-tests-vm: the guest run exited -?\d+\.$'
})
if ($ended.Count -eq 0) {
    Write-Host "runner-arms: the runner stopped before the arms (exit $code), so none of them was observed." -ForegroundColor Yellow
    exit 2
}

$results = Join-Path $scratch 'TestResults\vm'
$broke = New-Object System.Collections.ArrayList

function Held {
    param([Parameter(Mandatory)] [string] $Arm, [Parameter(Mandatory)] [bool] $Holds, [string] $Instead)
    if ($Holds) { Write-Host "  held   $Arm" -ForegroundColor Green }
    else {
        Write-Host "  BROKE  $Arm - $Instead" -ForegroundColor Red
        $null = $broke.Add($Arm)
    }
}

Held 'a red guest run is the runner''s exit code' ($code -eq $verdict) "it exited $code"

Held 'a red guest run says so in its last line' `
    ($ended[0] -eq "run-tests-vm: the guest run exited $verdict.") "it said '$($ended[0])'"

Held 'the first of -Bring is fetched on a red run' `
    (Test-Path -LiteralPath (Join-Path $results 'first.txt')) "no first.txt under $results"

$blame = @($said | Where-Object { $_.StartsWith('  blame       ') })
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

Write-Host ''
Write-Host '  not driven here, and why:'
Write-Host '    the bound giving up on a guest, and the held tree it leaves for the next sync (WW420''s remainder)'
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

Write-Host 'runner-arms: every arm driven here came back as the runner says it does.' -ForegroundColor Green
exit 0
