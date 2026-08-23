#Requires -Version 5.1
<#
.SYNOPSIS
  Run the suite on a desk that is not the operator's.

.DESCRIPTION
  The suite creates real windows, takes the foreground and synthesises input, so for the two and a
  half minutes a full run lasts the machine belongs to it and not to whoever is sitting at it. That
  is not an inconvenience to route around: it is the whole of WW157. This moves the run to a guest
  under VMware Workstation and leaves the host to its owner.

  It arranges nothing. The guest is expected to have a desktop session and a .NET SDK; where it does
  not, this says which and stops. Provisioning a machine from a test script is how two machines drift
  apart while both look configured.

  Most of the mechanics here were paid for once already, in freewilly's scripts\vm.ps1, and are
  repeated rather than rediscovered: vmrun never relays guest output, so output is redirected to a
  file in the guest and copied out; vmrun never adopts the guest's exit code, so the guest writes it
  down; and a command is delivered as a batch file run with no arguments, because `cmd /c "..."`
  passes quoting through two layers that both rewrite it - measured there as `cmd /c exit 0` coming
  back as exit 1.

  One thing is deliberately different from freewilly. It runs everything without -interactive, since
  a console preflight needs no desk. This needs one, so -interactive -activeWindow is on every run
  and a guest sitting at the lock screen is a refusal rather than a red suite.

.PARAMETER Configuration
  Debug or Release, handed to run-tests.cmd inside the guest.

.PARAMETER Screenshot
  Capture the guest's screen when the run finishes. A run whose desk drew nothing is WW42's session,
  and a picture is the one answer no exit code carries.

.PARAMETER CommittedOnly
  Carry HEAD alone. By default the guest gets the working tree - uncommitted and untracked included -
  because testing a tree nobody has in front of them is the failure this whole project is about.

.NOTES
  Secrets are never parameters and never printed. KEY=VALUE lines in a file, searched in order:

    1. whatever WINWRIGHT_VM_ENV points at
    2. d:\tmp\winwright-vm.env
    3. winwright-vm.env in the repository root

  The repository-root spelling is covered by the `*.env` line in .gitignore. It is still the weaker
  of the two: `git add -f` stages it anyway and `git clean -xdf` deletes it without asking, so the
  out-of-tree location is the default on purpose.

    WINWRIGHT_VMX              full path to the .vmx
    WINWRIGHT_VM_PASSWORD      the VM *encryption* password. A Windows 11 guest needs a TPM, a TPM
                               means an encrypted VM, and vmrun opens nothing without this. It is
                               not the Windows login, and confusing the two costs an afternoon.
    WINWRIGHT_GUEST_USER       an account inside the guest
    WINWRIGHT_GUEST_PASSWORD   its password - a local account with a non-empty one, since vmrun
                               cannot authenticate a Microsoft account signed in with Hello or a PIN
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [switch] $Screenshot,

    [switch] $CommittedOnly,

    [string] $Vmx
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:RepoRoot = Split-Path -Parent $PSScriptRoot
$script:GuestSync = 'C:\winwright-sync'
$script:GuestRepo = 'C:\src\winwright'

$script:EnvFileCandidates = @(
    'd:\tmp\winwright-vm.env',
    (Join-Path $script:RepoRoot 'winwright-vm.env')
)

function Refuse {
    param([Parameter(Mandatory)] [string] $What, [string] $Remedy)
    Write-Host ''
    Write-Host "run-tests-vm: $What" -ForegroundColor Red
    if ($Remedy) { Write-Host "         -> $Remedy" -ForegroundColor Yellow }
    exit 3
}

function Import-EnvFile {
    if ($env:WINWRIGHT_VM_ENV) {
        # Honoured even when absent: a typo in it reads as "that file is not there" rather than
        # silently falling through to another file and reaching a VM nobody named.
        if (Test-Path -LiteralPath $env:WINWRIGHT_VM_ENV) { $path = $env:WINWRIGHT_VM_ENV } else { return $null }
    }
    else {
        $path = $script:EnvFileCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    }
    if (-not $path) { return $null }

    foreach ($line in Get-Content -LiteralPath $path) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith('#')) { continue }
        $split = $trimmed.IndexOf('=')
        if ($split -lt 1) { continue }
        $name = $trimmed.Substring(0, $split).Trim()
        # Only ever fills a blank, so a variable set for this shell wins and a one-off override
        # needs no edit to the file.
        if (-not [Environment]::GetEnvironmentVariable($name)) {
            Set-Item -Path "env:$name" -Value $trimmed.Substring($split + 1).Trim()
        }
    }
    return $path
}

function Find-VmRun {
    $candidates = @(
        "${env:ProgramFiles(x86)}\VMware\VMware Workstation\vmrun.exe",
        "$env:ProgramFiles\VMware\VMware Workstation\vmrun.exe",
        "$env:ProgramFiles\VMware\VMware VIX\vmrun.exe"
    )
    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) { return $candidate }
    }
    $onPath = Get-Command 'vmrun.exe' -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }
    return $null
}

function Invoke-VmRun {
    <#
      Authentication flags are assembled here and nowhere else, so no call site can put one in a log
      line. They still reach vmrun's own command line, where the host's process list can read them,
      and vmrun offers no spelling that avoids it - which is a reason for the guest to be one that
      can be thrown away, not a reason to pretend otherwise.
    #>
    param([Parameter(Mandatory)] [string[]] $Arguments, [switch] $Guest)

    $argv = New-Object System.Collections.ArrayList
    $null = $argv.Add('-T'); $null = $argv.Add('ws')
    if ($env:WINWRIGHT_VM_PASSWORD) { $null = $argv.Add('-vp'); $null = $argv.Add($env:WINWRIGHT_VM_PASSWORD) }
    if ($Guest) {
        $null = $argv.Add('-gu'); $null = $argv.Add($env:WINWRIGHT_GUEST_USER)
        $null = $argv.Add('-gp'); $null = $argv.Add($env:WINWRIGHT_GUEST_PASSWORD)
    }
    foreach ($argument in $Arguments) { $null = $argv.Add($argument) }

    $output = & $script:VmRun @argv 2>&1
    return [pscustomobject]@{
        ExitCode = $LASTEXITCODE
        Output   = ($output | Out-String).Trim()
        Ok       = ($LASTEXITCODE -eq 0)
    }
}

function Read-ConsoleText {
    <#
      Decodes guest output by what is in it rather than by what wrote it. dotnet writes UTF-8 and
      several Windows tools write UTF-16LE, and reading either with the wrong one is not a crash: it
      is a NUL after every character, which reads as data and reaches a report.
    #>
    param([Parameter(Mandatory)] [string] $Path)

    $bytes = [IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -eq 0) { return '' }
    if ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) {
        return [Text.Encoding]::Unicode.GetString($bytes, 2, $bytes.Length - 2)
    }
    $zeroes = 0
    for ($i = 1; $i -lt $bytes.Length; $i += 2) { if ($bytes[$i] -eq 0) { $zeroes++ } }
    if (($zeroes * 4) -gt $bytes.Length) { return [Text.Encoding]::Unicode.GetString($bytes) }
    return [Text.Encoding]::UTF8.GetString($bytes)
}

# --- what this needs, named before anything is started ------------------------------------------

$envFile = Import-EnvFile
$script:VmRun = Find-VmRun
if (-not $script:VmRun) {
    Refuse 'vmrun.exe was not found on this host' 'Install VMware Workstation, or put vmrun.exe on PATH.'
}

if ($Vmx) { $env:WINWRIGHT_VMX = $Vmx }
if (-not $env:WINWRIGHT_VMX) {
    Refuse 'no .vmx is configured' "Set WINWRIGHT_VMX, or put it in $($script:EnvFileCandidates[0])."
}
if (-not (Test-Path -LiteralPath $env:WINWRIGHT_VMX)) {
    Refuse "WINWRIGHT_VMX points at $env:WINWRIGHT_VMX, which does not exist" 'A path that is not there is a typo, not a stopped VM.'
}
if (-not $env:WINWRIGHT_GUEST_USER -or -not $env:WINWRIGHT_GUEST_PASSWORD) {
    Refuse 'no guest credentials are configured' 'Set WINWRIGHT_GUEST_USER and WINWRIGHT_GUEST_PASSWORD.'
}

$vmxPath = $env:WINWRIGHT_VMX
Write-Host "run-tests-vm  $vmxPath" -ForegroundColor Cyan
if ($envFile) { Write-Host "  settings    $envFile" }

# listSnapshots is the cheapest call that needs the VM actually opened, so it is what tells an
# absent encryption password from a wrong one. The two messages differ, and matching only the first
# sends a wrong password to the branch whose remedy says the problem is not a password.
$snapshots = Invoke-VmRun -Arguments @('listSnapshots', $vmxPath)
if (-not $snapshots.Ok) {
    $said = ($snapshots.Output -split "`r?`n" | Where-Object { $_.Trim() }) -join ' '
    if ($said -match 'password is required') {
        Refuse 'the VM is encrypted and no password was supplied' 'Set WINWRIGHT_VM_PASSWORD to the VM encryption password - not the guest login.'
    }
    if ($said -match 'ncorrect password') {
        Refuse 'the VM is encrypted and the password supplied was refused' 'WINWRIGHT_VM_PASSWORD is the password VMware asks for when opening the VM, not the Windows login inside it.'
    }
    Refuse "vmrun could not open the VM: $said"
}

$running = Invoke-VmRun -Arguments @('list')
if ($running.Output -notmatch [regex]::Escape([IO.Path]::GetFileName($vmxPath))) {
    Write-Host '  the guest is not running; starting it with its console visible' -ForegroundColor Yellow
    # gui and never nogui: this run needs a desk that draws, and a headless guest is the session
    # WW42 was measured on - everything present, nothing rendering.
    $start = Invoke-VmRun -Arguments @('start', $vmxPath, 'gui')
    if (-not $start.Ok) { Refuse "the guest would not start: $($start.Output)" }

    # Ten minutes, not five. freewilly measured an agent taking longer than five to come up after a
    # component install, and gave up on a machine that was fine.
    $deadline = (Get-Date).AddMinutes(10)
    do {
        Start-Sleep -Seconds 10
        $tools = Invoke-VmRun -Arguments @('checkToolsState', $vmxPath)
    } while ($tools.Output -notmatch 'running' -and (Get-Date) -lt $deadline)
    if ($tools.Output -notmatch 'running') { Refuse 'VMware Tools never answered in the guest within ten minutes' }
}

$tools = Invoke-VmRun -Arguments @('checkToolsState', $vmxPath)
if ($tools.Output -notmatch 'running') {
    Refuse "VMware Tools is '$($tools.Output.Trim())' in the guest" 'Install VMware Tools there. Without it vmrun can run nothing.'
}
Write-Host '  guest       running, tools answering'

# --- the tree the guest will test ---------------------------------------------------------------

$stage = Join-Path ([IO.Path]::GetTempPath()) 'winwright-vm'
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
$null = New-Item -ItemType Directory -Path $stage

Push-Location $script:RepoRoot
try {
    # -c and -o together, minus what .gitignore covers: tracked files plus the untracked ones that
    # are really part of the tree. A sync built from `git diff` alone carries neither an untracked
    # test file nor a new fixture, and a suite that never saw them is green about nothing.
    if ($CommittedOnly) { $files = @(& git ls-files -c) } else { $files = @(& git ls-files -c -o --exclude-standard) }
    if ($LASTEXITCODE -ne 0) { Refuse 'git would not list this tree' }
}
finally { Pop-Location }

$zip = Join-Path $stage 'source.zip'
Add-Type -AssemblyName System.IO.Compression.FileSystem | Out-Null
$archive = [IO.Compression.ZipFile]::Open($zip, 'Create')
try {
    foreach ($relative in $files) {
        $full = Join-Path $script:RepoRoot $relative
        if (Test-Path -LiteralPath $full -PathType Leaf) {
            $null = [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $full, $relative)
        }
    }
}
finally { $archive.Dispose() }

$size = [math]::Round((Get-Item -LiteralPath $zip).Length / 1MB, 1)
$carrying = if ($CommittedOnly) { 'HEAD only' } else { 'the working tree' }
Write-Host "  carrying    $($files.Count) files, $size MB ($carrying)"

# --- the scripts the guest runs, generated here so nothing is quoted through vmrun ---------------

# Wiped and expanded rather than updated in place. A full rebuild costs under a minute on a tree
# this size, and it buys the one thing an incremental guest cannot offer: no result can be left
# over from a file that is no longer in the tree.
@"
`$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath '$script:GuestRepo') { Remove-Item -LiteralPath '$script:GuestRepo' -Recurse -Force }
`$null = New-Item -ItemType Directory -Path '$script:GuestRepo' -Force
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory('$script:GuestSync\source.zip', '$script:GuestRepo')
`$dotnet = Join-Path `$env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath `$dotnet)) { Write-Output 'GUEST-MISSING dotnet'; exit 91 }
Write-Output ('sdk ' + (& `$dotnet --version))
"@ | Set-Content -LiteralPath (Join-Path $stage 'sync.ps1') -Encoding ascii

@"
@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "$script:GuestSync\sync.ps1" > "$script:GuestSync\sync.log" 2>&1
exit /b %ERRORLEVEL%
"@ | Set-Content -LiteralPath (Join-Path $stage 'sync.cmd') -Encoding ascii

# The exit code is written down rather than left to vmrun, which reports every failure as its own
# non-zero and would make a red suite and an unreachable guest the same number.
@"
@echo off
set "DOTNET_ROOT=%LOCALAPPDATA%\Microsoft\dotnet"
set "PATH=%DOTNET_ROOT%;%PATH%"
cd /d "$script:GuestRepo"
call "$script:GuestRepo\run-tests.cmd" $Configuration > "$script:GuestSync\vm-run.log" 2>&1
rem The redirect leads, and that is not a style choice. `echo %ERRORLEVEL%> file` is parsed by cmd
rem as `echo` redirected to handle %ERRORLEVEL% whenever the code is a single digit, so a run that
rem exited 1 wrote "ECHO is off." into the file and the host read the suite's verdict as a string.
> "$script:GuestSync\vm-exit.txt" echo %ERRORLEVEL%
exit /b 0
"@ | Set-Content -LiteralPath (Join-Path $stage 'run.cmd') -Encoding ascii

# --- into the guest ------------------------------------------------------------------------------

$null = Invoke-VmRun -Guest -Arguments @('createDirectoryInGuest', $vmxPath, $script:GuestSync)

# Deleted in the guest before the run, never merely overwritten after it. A run that writes nothing
# leaves the last one's files in place, the copy back succeeds, and the caller is handed a previous
# afternoon's trx as this run's result - which is worse than an error, because it looks like one.
foreach ($stale in @('vm-exit.txt', 'vm-run.log', 'sync.log', 'winwright.trx', 'discovered.txt')) {
    $null = Invoke-VmRun -Guest -Arguments @('deleteFileInGuest', $vmxPath, "$script:GuestSync\$stale")
}

foreach ($file in @('source.zip', 'sync.ps1', 'sync.cmd', 'run.cmd')) {
    $sent = Invoke-VmRun -Guest -Arguments @('copyFileFromHostToGuest', $vmxPath, (Join-Path $stage $file), "$script:GuestSync\$file")
    if (-not $sent.Ok) { Refuse "could not copy $file into the guest: $($sent.Output)" }
}

$synced = Invoke-VmRun -Guest -Arguments @('runProgramInGuest', $vmxPath, "$script:GuestSync\sync.cmd")
$syncLog = Join-Path $stage 'sync.log'
$null = Invoke-VmRun -Guest -Arguments @('copyFileFromGuestToHost', $vmxPath, "$script:GuestSync\sync.log", $syncLog)
$syncSaid = if (Test-Path -LiteralPath $syncLog) { (Read-ConsoleText $syncLog).Trim() } else { '' }

if (-not $synced.Ok) {
    if ($syncSaid -match 'GUEST-MISSING dotnet') {
        Refuse 'the guest has no .NET SDK' 'Install it once in the VM. This script does not provision a machine.'
    }
    Refuse "the guest tree would not sync: $syncSaid $($synced.Output)"
}
Write-Host "  guest tree  $syncSaid"

# --- the run ------------------------------------------------------------------------------------

Write-Host ''
Write-Host "  running the suite in the guest ($Configuration). The host is yours." -ForegroundColor Cyan

# -interactive, which freewilly's harness deliberately never uses: it needs a logged-in desktop
# session and refuses without one, and that refusal is the right answer here, because a suite
# synthesising input into a lock screen is not a suite that ran.
#
# And -activeWindow deliberately not, which cost a run to learn. It brings this batch's console to
# the foreground in the guest, and the suite then synthesises input at whatever holds it: a run died
# with 0xC000013A, STATUS_CONTROL_C_EXIT, which is a keystroke of the suite's own reaching the
# console hosting it. The fixtures take the foreground themselves; the console must never compete
# for it.
$ran = Invoke-VmRun -Guest -Arguments @('runProgramInGuest', $vmxPath, '-interactive', "$script:GuestSync\run.cmd")
if (-not $ran.Ok) {
    if ($ran.Output -match 'logged in interactively') {
        Refuse 'the guest has no interactive desktop session' 'Log in at the guest console once, and leave it unlocked. A locked desk renders nothing, which is the session WW42 was measured on.'
    }
    Refuse "the guest never finished the run: $($ran.Output)"
}

# --- back to the host -----------------------------------------------------------------------------

# TestResults\vm and never TestResults itself. Measured the hard way: the guest's trx landed on the
# host's, and the only thing in it saying which desk had produced the run was a computerName buried
# in the XML. Two machines writing one path is a result that cannot say where it came from, which is
# the same defect as a green that cannot say what it covered.
$results = Join-Path $script:RepoRoot 'TestResults\vm'
if (-not (Test-Path -LiteralPath $results)) { $null = New-Item -ItemType Directory -Path $results -Force }

$exitFile = Join-Path $stage 'vm-exit.txt'
$back = Invoke-VmRun -Guest -Arguments @('copyFileFromGuestToHost', $vmxPath, "$script:GuestSync\vm-exit.txt", $exitFile)
if (-not $back.Ok) {
    Refuse 'the guest wrote no exit code, so what the run did there is unknown' 'Look at the guest console; the run may still be on screen.'
}

$said = (Read-ConsoleText $exitFile).Trim()
if ($said -notmatch '^-?\d+$') {
    # Refused rather than coerced. Whatever this is, it is not a verdict, and guessing a number out
    # of it would report an outcome the guest never stated.
    Refuse "the guest wrote '$said' where its exit code should be" 'Read TestResults\vm\vm-run.log: the run happened, but what it concluded did not come back.'
}
$code = [int]$said

$logFile = Join-Path $results 'vm-run.log'
$null = Invoke-VmRun -Guest -Arguments @('copyFileFromGuestToHost', $vmxPath, "$script:GuestSync\vm-run.log", $logFile)

$got = Invoke-VmRun -Guest -Arguments @('copyFileFromGuestToHost', $vmxPath, "$script:GuestRepo\TestResults\winwright.trx", (Join-Path $results 'winwright.trx'))
if (-not $got.Ok) { Write-Host '  no winwright.trx came back from the guest' -ForegroundColor Yellow }

# Only chased on a green run, and that is a fact about this repository rather than about the guest.
# The roll call hangs off AfterTargets="VSTest", so a failed VSTest never reaches it and writes no
# discovered.txt - warning about its absence on a red run reports a missing check that was never
# due, which is noise on exactly the runs already carrying a real answer.
if ($code -eq 0) {
    $roll = Invoke-VmRun -Guest -Arguments @('copyFileFromGuestToHost', $vmxPath, "$script:GuestRepo\TestResults\discovered.txt", (Join-Path $results 'discovered.txt'))
    if (-not $roll.Ok) { Write-Host '  the run passed and no discovered.txt came back: the roll call did not take' -ForegroundColor Yellow }
}

if (Test-Path -LiteralPath $logFile) {
    Write-Host ''
    foreach ($line in ((Read-ConsoleText $logFile) -split "`r?`n")) { Write-Host $line }
}

if ($Screenshot) {
    $shot = Join-Path $results 'vm-desk.png'
    if (Test-Path -LiteralPath $shot) { Remove-Item -LiteralPath $shot -Force }
    # -Guest, because captureScreen is a guest operation: without a login vmrun answers "Anonymous
    # guest operations are not allowed on this virtual machine".
    $capture = Invoke-VmRun -Guest -Arguments @('captureScreen', $vmxPath, $shot)
    if ($capture.Ok) { Write-Host "  desk        $shot" }
    else { Write-Host "  captureScreen failed: $($capture.Output)" -ForegroundColor Yellow }
}

Write-Host ''
if ($code -eq 0) { Write-Host 'run-tests-vm: the guest run passed.' -ForegroundColor Green }
else { Write-Host "run-tests-vm: the guest run exited $code." -ForegroundColor Red }
exit $code
