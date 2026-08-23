# Run the suite on a desk that is not the operator's.
#
# The suite creates real windows, takes the foreground and synthesises input, so for the two and a
# half minutes a full run lasts the machine belongs to it and not to whoever is sitting at it. That
# is not a defect to work around: it is the reason WW157 exists. This script moves the run to a
# guest and leaves the host alone.
#
# What it refuses to do is arrange the desk. It starts a VM that is already configured and stops
# there - no provisioning, no installing, no second copy of the machine's setup living in this
# repository. If the guest lacks dotnet or git, it says so and exits; it does not fix it.
#
# Secrets are read from the environment and never written down. They are still passed to vmrun on
# its command line, where the host's process list can see them, and there is no vmrun spelling that
# avoids it - so treat the guest as disposable, which is what an OOBE-bypass account already says.

[CmdletBinding()]
param(
    # Debug or Release, handed straight to run-tests.cmd inside the guest.
    [string] $Configuration = 'Debug',

    # The VM. Encrypted, because a Windows 11 guest with a vTPM has to be, which is why -vp exists.
    [string] $Vmx = $(if ($env:WINWRIGHT_VMX) { $env:WINWRIGHT_VMX } else { 'D:\VMwares\Windows 11 x64.vmx' }),

    # The VM's own encryption password. Not the Windows login - a different secret entirely.
    [string] $VmPassword = $env:WINWRIGHT_VM_VP,

    [string] $GuestUser = $(if ($env:WINWRIGHT_VM_USER) { $env:WINWRIGHT_VM_USER } else { 'oobe' }),
    [string] $GuestPassword = $env:WINWRIGHT_VM_PASS,

    # Where the tree lands in the guest. Cloned on the first run, updated on every one after.
    [string] $GuestRepo = $(if ($env:WINWRIGHT_VM_REPO) { $env:WINWRIGHT_VM_REPO } else { 'C:\src\winwright' }),

    # Leave the guest's tree at HEAD and do not carry uncommitted work across.
    [switch] $CommittedOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

$GuestSync = 'C:\winwright-sync'
$RepoRoot = Split-Path -Parent $PSScriptRoot

function Refuse([string] $what) {
    Write-Host "run-tests-vm: $what" -ForegroundColor Red
    exit 3
}

# vmrun's own words for a failure are on stdout and its exit code is 255 for everything, so both are
# captured and the caller decides. Nothing here treats a non-zero as fatal on the caller's behalf.
function Invoke-Vmrun {
    param([string[]] $Arguments, [switch] $Quiet)

    $all = @('-T', 'ws', '-vp', $VmPassword, '-gu', $GuestUser, '-gp', $GuestPassword) + $Arguments
    $said = & $script:Vmrun @all 2>&1
    $code = $LASTEXITCODE
    if (-not $Quiet -and $said) { $said | ForEach-Object { Write-Host "  $_" } }
    return [pscustomobject]@{ Code = $code; Said = ($said -join "`n") }
}

# --- preflight: everything this needs, named before anything is started ------------------------

$candidates = @(
    'C:\Program Files (x86)\VMware\VMware Workstation\vmrun.exe',
    'C:\Program Files\VMware\VMware Workstation\vmrun.exe'
)
$script:Vmrun = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $script:Vmrun) { Refuse "vmrun.exe was not found. Looked in: $($candidates -join '; ')" }

if (-not (Test-Path $Vmx)) { Refuse "no VM at '$Vmx'. Set WINWRIGHT_VMX to the .vmx you mean." }

$missing = @()
if (-not $VmPassword) { $missing += "WINWRIGHT_VM_VP (the VM's encryption password - vmx.encryptionType is not 'none', so vmrun cannot open the config without it)" }
if (-not $GuestPassword) { $missing += "WINWRIGHT_VM_PASS (the guest Windows password for '$GuestUser')" }
if ($missing.Count -gt 0) {
    Write-Host "run-tests-vm: this run has no desk to move to, because it was not told how to reach one." -ForegroundColor Red
    $missing | ForEach-Object { Write-Host "  absent  $_" -ForegroundColor Red }
    exit 3
}

Write-Host "run-tests-vm: $Vmx" -ForegroundColor Cyan

# --- the guest is running, and its tools answer -------------------------------------------------

$tools = Invoke-Vmrun -Arguments @('checkToolsState', $Vmx) -Quiet
if ($tools.Code -ne 0) { Refuse "vmrun would not open the VM: $($tools.Said)" }

if ($tools.Said -notmatch 'running') {
    Write-Host "  the VM is not running ($($tools.Said.Trim())); starting it with its console visible" -ForegroundColor Yellow
    $started = Invoke-Vmrun -Arguments @('start', $Vmx, 'gui')
    if ($started.Code -ne 0) { Refuse "the VM would not start: $($started.Said)" }

    # Waited for rather than assumed: a guest whose tools have not answered yet accepts no guest
    # operation at all, and the failure that produces names the operation instead of the wait.
    $deadline = (Get-Date).AddMinutes(5)
    do {
        Start-Sleep -Seconds 5
        $tools = Invoke-Vmrun -Arguments @('checkToolsState', $Vmx) -Quiet
    } while ($tools.Said -notmatch 'running' -and (Get-Date) -lt $deadline)

    if ($tools.Said -notmatch 'running') { Refuse "VMware Tools never answered in the guest after five minutes" }
}
Write-Host "  tools: $($tools.Said.Trim())"

# --- the tree the guest will test, carried as two files and never a share -----------------------

$stage = Join-Path ([System.IO.Path]::GetTempPath()) 'winwright-vm'
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage | Out-Null

Push-Location $RepoRoot
try {
    $branch = (& git rev-parse --abbrev-ref HEAD).Trim()
    if ($branch -eq 'HEAD') { Refuse "this tree is on a detached HEAD, and the guest is synced by branch name" }

    $bundle = Join-Path $stage 'winwright.bundle'
    & git bundle create $bundle $branch --quiet
    if ($LASTEXITCODE -ne 0) { Refuse "git bundle refused this tree" }

    $patch = Join-Path $stage 'worktree.patch'
    if ($CommittedOnly) {
        Set-Content -Path $patch -Value '' -Encoding ascii -NoNewline
        Write-Host "  carrying $branch at HEAD only (-CommittedOnly)"
    }
    else {
        # --binary, because a tree with a changed image or a .trx in it is a tree the guest must
        # reproduce exactly. A patch that dropped those would test something nobody has.
        & git diff HEAD --binary | Set-Content -Path $patch -Encoding ascii
        $lines = (Get-Content $patch | Measure-Object -Line).Lines
        Write-Host "  carrying $branch plus $lines lines of uncommitted change"
    }

    # Named, never carried. An untracked file is invisible to `git diff HEAD`, so a run that said
    # nothing about it would be testing a tree the operator does not have in front of them.
    $untracked = & git ls-files --others --exclude-standard
    if ($untracked) {
        Write-Host "  not carried, because they are untracked here:" -ForegroundColor Yellow
        $untracked | ForEach-Object { Write-Host "    $_" -ForegroundColor Yellow }
    }
}
finally { Pop-Location }

# --- the two scripts the guest runs, generated here so nothing is quoted through vmrun ----------

$syncCmd = Join-Path $stage 'vm-sync.cmd'
@"
@echo off
rem Generated by tools\run-tests-vm.ps1. Overwritten every run; do not edit in the guest.
where git >nul 2>&1 || (echo GUEST-MISSING git & exit /b 90)
where dotnet >nul 2>&1 || (echo GUEST-MISSING dotnet & exit /b 91)

if not exist "$GuestRepo\.git" (
    echo cloning into $GuestRepo
    git clone -b $branch "$GuestSync\winwright.bundle" "$GuestRepo" || exit /b 92
) else (
    git -C "$GuestRepo" fetch --force "$GuestSync\winwright.bundle" "$branch:refs/heads/$branch-vm" || exit /b 93
    git -C "$GuestRepo" checkout --force -B $branch "refs/heads/$branch-vm" || exit /b 94
)

rem -fd and never -fdx: the ignored build output is what keeps the next run incremental, and a
rem clean that took it would spend a full rebuild to remove nothing that changes a result.
git -C "$GuestRepo" reset --hard || exit /b 95
git -C "$GuestRepo" clean -fd || exit /b 96

for %%A in ("$GuestSync\worktree.patch") do if %%~zA GTR 0 (
    echo applying uncommitted change
    git -C "$GuestRepo" apply --binary "$GuestSync\worktree.patch" || exit /b 97
)

git -C "$GuestRepo" rev-parse --short HEAD
exit /b 0
"@ | Set-Content -Path $syncCmd -Encoding ascii

$runCmd = Join-Path $stage 'vm-run.cmd'
@"
@echo off
rem Generated by tools\run-tests-vm.ps1. The exit code is written to a file rather than left to
rem vmrun, which reports every failure as 255 and would make a red suite and an unreachable guest
rem the same number.
if not exist "$GuestRepo\TestResults" mkdir "$GuestRepo\TestResults"
cd /d "$GuestRepo"
call "$GuestRepo\run-tests.cmd" $Configuration > "$GuestRepo\TestResults\vm-run.log" 2>&1
echo %ERRORLEVEL%> "$GuestRepo\TestResults\vm-exit.txt"
exit /b 0
"@ | Set-Content -Path $runCmd -Encoding ascii

# --- into the guest -----------------------------------------------------------------------------

Invoke-Vmrun -Arguments @('createDirectoryInGuest', $Vmx, $GuestSync) -Quiet | Out-Null
foreach ($file in @('winwright.bundle', 'worktree.patch', 'vm-sync.cmd', 'vm-run.cmd')) {
    $sent = Invoke-Vmrun -Arguments @('copyFileFromHostToGuest', $Vmx, (Join-Path $stage $file), "$GuestSync\$file")
    if ($sent.Code -ne 0) { Refuse "could not copy $file into the guest" }
}

Write-Host "  syncing the guest tree" -ForegroundColor Cyan
$synced = Invoke-Vmrun -Arguments @('runProgramInGuest', $Vmx, '-interactive', '-activeWindow', 'C:\Windows\System32\cmd.exe', '/c', "$GuestSync\vm-sync.cmd")
if ($synced.Code -ne 0) {
    if ($synced.Said -match 'GUEST-MISSING (\w+)') { Refuse "the guest has no $($Matches[1]) on its PATH. Install it in the VM once; this script does not provision." }
    Refuse "the guest tree would not sync: $($synced.Said)"
}

# --- the run itself -----------------------------------------------------------------------------

Write-Host "  running the suite in the guest ($Configuration). The host is yours." -ForegroundColor Cyan
$ran = Invoke-Vmrun -Arguments @('runProgramInGuest', $Vmx, '-interactive', '-activeWindow', 'C:\Windows\System32\cmd.exe', '/c', "$GuestSync\vm-run.cmd") -Quiet
if ($ran.Code -ne 0) { Refuse "the guest never finished the run: $($ran.Said)" }

# --- back to the host ---------------------------------------------------------------------------

$results = Join-Path $RepoRoot 'TestResults'
if (-not (Test-Path $results)) { New-Item -ItemType Directory -Path $results | Out-Null }

$exitFile = Join-Path $stage 'vm-exit.txt'
$back = Invoke-Vmrun -Arguments @('copyFileFromGuestToHost', $Vmx, "$GuestRepo\TestResults\vm-exit.txt", $exitFile)
if ($back.Code -ne 0) { Refuse "the guest wrote no exit code, so what the run did is unknown" }

foreach ($artefact in @('vm-run.log', 'winwright.trx', 'discovered.txt')) {
    $got = Invoke-Vmrun -Arguments @('copyFileFromGuestToHost', $Vmx, "$GuestRepo\TestResults\$artefact", (Join-Path $results $artefact)) -Quiet
    if ($got.Code -ne 0) { Write-Host "  no $artefact came back from the guest" -ForegroundColor Yellow }
}

$log = Join-Path $results 'vm-run.log'
if (Test-Path $log) { Get-Content $log | ForEach-Object { Write-Host $_ } }

$code = [int]((Get-Content $exitFile -Raw).Trim())
if ($code -eq 0) { Write-Host "run-tests-vm: the guest run passed." -ForegroundColor Green }
else { Write-Host "run-tests-vm: the guest run exited $code." -ForegroundColor Red }
exit $code
