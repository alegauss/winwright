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
  a console preflight needs no desk. This needs one, so -interactive is on every run and a guest
  sitting at the lock screen is a refusal rather than a red suite.

  -activeWindow is deliberately NOT, and this sentence used to say it was. It brings the program's
  console to the foreground in the guest, and the suite then synthesises input at whatever holds it:
  a run died with 0xC000013A, STATUS_CONTROL_C_EXIT, which is a keystroke of the suite's own reaching
  the console hosting it. WW311 needs the same thing for the opposite reason - a probe that took the
  foreground would find itself holding it and report the desk as waiting for an answer.

.PARAMETER Configuration
  Debug or Release, handed to run-tests.cmd inside the guest.

.PARAMETER Screenshot
  Capture the guest's screen when the run finishes. A run whose desk drew nothing is WW42's session,
  and a picture is the one answer no exit code carries.

.PARAMETER CommittedOnly
  Carry HEAD alone. By default the guest gets the working tree - uncommitted and untracked included -
  because testing a tree nobody has in front of them is the failure this whole project is about.

.PARAMETER Bound
  How many minutes the guest run may take before this stops waiting on it, sixty by default. WW386:
  the wait is bounded and the guest is not touched, so what a bound produces is a reading - the desk
  as the probe finds it and the log as far as the run got - rather than a command that has to be
  killed by hand.

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

    [string] $Vmx,

    # WW227. The five that make this an adopter's runner rather than this repository's.
    #
    # An adopting project had exactly one place to run its migrated cases: the desk somebody is
    # working at. That is the thing this project already knows better than — a host run of this suite
    # produced eight failures of which two were only the desk, and reported a negative control
    # passing because the host wrote a file faster than the guest could. Every one of those lessons
    # applied to every adopter and none of them had the runner that taught it.
    #
    # Defaults reproduce this repository's own run exactly, which is what makes the change testable:
    # the gate that validates it is the gate it is.

    # The tree to carry. Its git listing is what goes into the guest, and its TestResults is where
    # what comes back lands.
    [string] $Tree,

    # What the guest calls it: the sync folder, the tree's folder, and the stage on the host. Derived
    # from the tree's own leaf name, so two projects cannot collide in one guest.
    [string] $Name,

    # The command the guest runs from the tree's root. Not a project or a filter: an adopting
    # repository already has a way to run its own tests, and this types what a developer there types.
    [string] $Run,

    # Where that command leaves its results, relative to the tree in the guest.
    [string] $ResultsIn,

    # What to bring back out of it. The first is fetched always; the rest only on a green run, for
    # the reason the roll call is: a check that was never due is not a check that is missing.
    [string[]] $Bring,

    # WW386. How many minutes the guest run may take before this stops waiting on it.
    #
    # A bound on the wait and never a deadline for the suite: what it ends is this command, and what
    # is in the guest is left exactly where it is for somebody to look at. WW373 bounded a case and
    # everything around one inherited nothing - a guest that stops answering, a restore that hangs, a
    # testhost that dies without writing the exit file each leave this waiting with no bound, which
    # is the state `Start-Guest` refuses about `vmrun start` for the same reason. Ten silent minutes
    # and a wedge look alike from outside.
    #
    # Sixty, which is several times anything measured here - seven to fifteen minutes for the suite
    # and about one for the carry, so a whole run has never reached twenty. The margin is WW373's and
    # is the argument: a bound that decides a red is worse than no bound at all. A parameter because
    # an adopter's suite is not this one's, and `-Run` is where they already say so.
    [int] $Bound = 60
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:RepoRoot = Split-Path -Parent $PSScriptRoot
$script:Tree = if ($Tree) { (Resolve-Path -LiteralPath $Tree).Path } else { $script:RepoRoot }
$script:Name = if ($Name) { $Name } else { Split-Path -Leaf $script:Tree }
$script:GuestSync = "C:\$($script:Name)-sync"
$script:GuestRepo = "C:\src\$($script:Name)"

# WW150. The suite writes its listing and its results into a directory named for the run, so two
# runs on one machine cannot read each other's files. The default is a stamp the run picks for
# itself, which is right for a developer and useless here: the files are copied back out of the
# guest by exact path, and vmrun cannot glob. So this run names its own and tells the guest what it
# chose, which is also what stops two guest runs from colliding.
$script:RunName = 'vm-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff')

# WW412. How long a guest gets to finish logging in before having no session is a refusal. Three,
# argued the way the tools wait above argues ten: long enough that a machine which is fine is not
# called broken — the cold run this was measured on reached a desk in ninety seconds — and short,
# because a login that has not finished in three minutes is a desk to look at rather than wait on.
$script:SessionMinutes = 3

# What the guest runs, and where it leaves what it wrote. WW227: defaulted rather than required, so
# this repository's own invocation is unchanged and an adopter names the two things that differ.
$script:Run = if ($Run) { $Run } else { "run-tests.cmd $Configuration" }
$script:ResultsIn = if ($ResultsIn) { $ResultsIn } else { "TestResults\$script:RunName" }
$script:Bring = if ($Bring) { $Bring } else { @('winwright.trx', 'discovered.txt') }

# The VM's credentials and not the project's, so the search is not multiplied per tree: one guest,
# one file. The tree's own copy is second because a project may pin a different machine.
$script:EnvFileCandidates = @(
    'd:\tmp\winwright-vm.env',
    (Join-Path $script:Tree 'winwright-vm.env'),
    (Join-Path $script:RepoRoot 'winwright-vm.env')
) | Select-Object -Unique

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

function Get-VmRunArguments {
    <#
      Authentication flags are assembled here and nowhere else, so no call site can put one in a log
      line. They still reach vmrun's own command line, where the host's process list can read them,
      and vmrun offers no spelling that avoids it - which is a reason for the guest to be one that
      can be thrown away, not a reason to pretend otherwise.

      Separate from the call that waits for one, because one caller does not wait. Splitting it is
      what lets `Start-Guest` launch vmrun without a second spelling of the credentials.
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
    return $argv.ToArray()
}

function Invoke-VmRun {
    param([Parameter(Mandatory)] [string[]] $Arguments, [switch] $Guest)

    $argv = Get-VmRunArguments -Arguments $Arguments -Guest:$Guest
    $output = & $script:VmRun @argv 2>&1
    return [pscustomobject]@{
        ExitCode = $LASTEXITCODE
        Output   = ($output | Out-String).Trim()
        Ok       = ($LASTEXITCODE -eq 0)
    }
}

function Invoke-OnTheDesk {
    <#
      Run something in the guest's own desktop session, and refuse in one voice where there is none.

      WW314. Two calls need a session — a probe before anything is carried, and the run itself — and
      the sentence they share is WW42's: a suite synthesising input into a lock screen is not a suite
      that ran. Written once here because a rule spelled twice is a rule where the second copy goes
      on saying the old thing after the first one moves.

      -interactive, which freewilly's harness deliberately never uses: it needs a logged-in desktop
      session and refuses without one, and that refusal is the right answer.

      And -activeWindow deliberately not, which cost a run to learn. It brings the program's console
      to the foreground in the guest, and the suite then synthesises input at whatever holds it: a run
      died with 0xC000013A, STATUS_CONTROL_C_EXIT, which is a keystroke of the suite's own reaching
      the console hosting it. The fixtures take the foreground themselves; nothing else may compete.
    #>
    param(
        [Parameter(Mandatory)] [string] $Vmx,
        [Parameter(Mandatory)] [string[]] $Arguments,

        # WW386. How long this may take, in minutes, or nothing to block until it answers. The three
        # short calls - the session probe, the desk probe, the clearer - block: each is seconds, and
        # a bound on one of those would be a knob nobody turns. The run is the one that wants it.
        [int] $Minutes = 0,

        # Where the reading a bound produces is written, with --minutes. Named by the caller rather
        # than chosen here, because what a person opens after a run that would not end is the same
        # directory they open after one that did.
        [string] $Stage = '',

        # WW412. How long to let a guest finish logging in before its lack of a session is a
        # refusal. Zero for every caller but the first, and that is the whole design: by the time
        # anything else runs here a session has already been proved, so a retry there would be a
        # genuine refusal quietly waited out.
        [int] $SessionWithinMinutes = 0)

    $asked = @('runProgramInGuest', $Vmx, '-interactive') + $Arguments
    $waitingUntil = (Get-Date).AddMinutes($SessionWithinMinutes)
    $said = $false

    while ($true) {
        # Both arms end here, which is the point of the branch being inside this function: the
        # refusal below is the one voice WW314 wrote for a guest with no session, and a second
        # caller with a second copy of it is the copy that goes on saying the old thing.
        $ran = if ($Minutes -gt 0) {
            Wait-OnTheDesk -Vmx $Vmx -Arguments $asked -Minutes $Minutes -Stage $Stage
        }
        else {
            Invoke-VmRun -Guest -Arguments $asked
        }

        if ($ran.Ok -or $ran.Output -notmatch 'logged in interactively') { break }

        # WW412. A guest that has just been powered on has no session and has one a minute later,
        # and this refusal was true when it was made and false about the machine. Measured twice on
        # WW396's cold runs: tools answered in ten seconds, the session probe refused, and the same
        # command ninety seconds later carried the whole suite.
        #
        # The tools wait above spends ten minutes so that a machine which is fine is not called
        # broken. This is the same argument about the thing those tools exist to reach, and a
        # shorter number because a login that has not finished in three minutes is a desk somebody
        # should look at rather than one to keep waiting on.
        if ((Get-Date) -ge $waitingUntil) {
            Refuse 'the guest has no interactive desktop session' "Log in at the guest console once, and leave it unlocked. A locked desk renders nothing, which is the session WW42 was measured on. A guest that has just been powered on reaches one on its own, and this waited $SessionWithinMinutes minute(s) for it."
        }

        # Said once and not per go: a line every few seconds about a machine that is logging in
        # reads as a loop that has stuck, which is the opposite of what it is for.
        if (-not $said) {
            Write-Host "  desk        no session yet; waiting up to $SessionWithinMinutes minute(s) for the guest to finish logging in" -ForegroundColor Yellow
            $said = $true
        }

        Start-Sleep -Seconds 5
    }

    return $ran
}

function Wait-OnTheDesk {
    <#
      The same call, launched rather than blocked on, and given a bound. WW386.

      `Start-Guest`'s shape one call over, and for its reason: waited on directly this is unbounded,
      and a run that cannot end is worse than one that refuses - it gets killed by hand, which is the
      thing that leaves a guest tree the next sync cannot delete.

      What the bound ends is the wait. Nothing is stopped in the guest and nothing is cleaned up
      there, because a wedge is the one state worth looking at and a script that tidied it away would
      be the last thing to see it.

      That has a cost the refusal names, measured the first time this bound fired: what is still
      running holds the tree open, and the next run's sync refuses with `the process cannot access
      the file C:\src\winwright`. It is the same state a killed run leaves and it arrives from the
      other side - so the sentence says it, rather than a person meeting it one command later.

      It says the time out loud every minute, which is the other half. A run that prints nothing for
      quarter of an hour reads exactly like one that will never print again, and this file already
      spends ten lines arguing that about `vmrun start`.
    #>
    param(
        [Parameter(Mandatory)] [string] $Vmx,
        [Parameter(Mandatory)] [string[]] $Arguments,
        [Parameter(Mandatory)] [int] $Minutes,
        [string] $Stage = '')

    $argv = Get-VmRunArguments -Arguments $Arguments -Guest
    $running = Start-Job -ScriptBlock {
        param($Exe, $Argv)
        $said = & $Exe @Argv 2>&1
        [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = ($said | Out-String).Trim() }
    } -ArgumentList $script:VmRun, $argv

    $deadline = (Get-Date).AddMinutes($Minutes)
    $began = Get-Date
    $spoke = 0
    while ((Get-Date) -lt $deadline) {
        # Looked at often and spoken about rarely, which are two different intervals and were one in
        # the first version of this. Polling once a minute would add up to a minute to every run that
        # finished normally, for a line nobody needed; saying it every five seconds would bury the
        # run's own output under its clock.
        Start-Sleep -Seconds 5

        if ($running.State -eq 'Running') {
            $waited = [int]((Get-Date) - $began).TotalMinutes
            if ($waited -gt $spoke) {
                $spoke = $waited
                Write-Host "  waiting     ${waited}m of $Minutes; the guest has not answered yet"
            }

            continue
        }

        # Read as the last thing the job produced and only where it is the record the job promises:
        # an error written on the way there arrives on the same pipe and is not an exit code. That is
        # Start-Guest's own correction, and it applies here for the same reason.
        $said = @(Receive-Job -Job $running -ErrorAction SilentlyContinue) | Select-Object -Last 1
        Remove-Job -Job $running -Force -ErrorAction SilentlyContinue

        if ($said -and $said.PSObject.Properties['ExitCode']) {
            return [pscustomobject]@{
                ExitCode = $said.ExitCode
                Output   = $said.Output
                Ok       = ($said.ExitCode -eq 0)
            }
        }

        return [pscustomobject]@{
            ExitCode = 1
            Ok       = $false
            Output   = "the guest run ended without an exit code: $($said | Out-String)".Trim()
        }
    }

    # The bound, and everything below it is the reading rather than the refusal. A run told only that
    # an hour passed is WW371's refusal in another form - an operator sent to a console to find out
    # what this could have asked. The desk probe is already carried and already answers.
    Stop-Job -Job $running -ErrorAction SilentlyContinue
    Remove-Job -Job $running -Force -ErrorAction SilentlyContinue

    Write-Host ''
    Write-Host "  the guest has not answered in $Minutes minute(s); reading its desk" -ForegroundColor Yellow

    $desk = Read-GuestDesk -Vmx $Vmx -Stage $Stage
    $log = if ($Stage) { Join-Path $Stage 'vm-run.log' } else { '' }
    if ($log) {
        $null = Invoke-VmRun -Guest -Arguments @(
            'copyFileFromGuestToHost', $Vmx, "$script:GuestSync\vm-run.log", $log)
    }

    $far = if ($log -and (Test-Path -LiteralPath $log)) {
        $lines = (Read-ConsoleText $log) -split "`r?`n"
        "It had written $($lines.Count) line(s) to $log; the last is: $(($lines | Where-Object { $_.Trim() } | Select-Object -Last 1))"
    }
    else {
        'It had written no log this could fetch, so the run may not have reached the suite at all.'
    }

    Refuse (
        "the guest run did not answer within $Minutes minute(s). Its desk reads $($desk.State)" +
        "$(if ($desk.Process) { ": $($desk.Process) (pid $($desk.Pid), $($desk.Class))" } else { '' })" +
        " - $($desk.Detail). $far"
    ) ('Nothing was stopped in the guest, so look at its console - and note that whatever is still ' +
        'running there holds the tree: the next run refuses to sync until it ends. Raise -Bound if ' +
        'this suite really takes that long.')
}

function Read-GuestDesk {
    <#
      WW311, WW345. Carry the desk probe into the guest, run it on the desk there, and bring its
      one line back. What the states mean and why the reading is a reading rather than a repair is
      in the probe itself - `tools/desk-probe.ps1` - because that is the file that decides them,
      and a second copy of that argument here is the copy that would go on saying the old thing.

      What is this function's own is the carrying: the guest needs the file, the run needs a
      desktop session, and an answer that never came back is not a quiet desk.
    #>
    param([Parameter(Mandatory)] [string] $Vmx, [Parameter(Mandatory)] [string] $Stage)

    # WW345. The probe is a file beside this one rather than a here-string in it. It decides
    # whether a run happens at all and had been wrong twice, and nothing could run any of its
    # answers - a here-string has no caller but this function. Sent as it sits on disk, so the
    # classification the suite exercises and the one that refuses a run are one file.
    $probe = Join-Path $PSScriptRoot 'desk-probe.ps1'
    if (-not (Test-Path -LiteralPath $probe)) { Refuse "the desk probe is missing: $probe" }

    $null = Invoke-VmRun -Guest -Arguments @('createDirectoryInGuest', $Vmx, $script:GuestSync)
    $null = Invoke-VmRun -Guest -Arguments @('deleteFileInGuest', $Vmx, "$script:GuestSync\desk.txt")

    $sent = Invoke-VmRun -Guest -Arguments @(
        'copyFileFromHostToGuest', $Vmx, $probe, "$script:GuestSync\desk.ps1")
    if (-not $sent.Ok) { Refuse "could not copy the desk probe into the guest: $($sent.Output)" }

    # On the desk and not beside it: the foreground is the thing being read, and a program run
    # outside the session has none to read. WW314's helper is the one voice for a guest with no
    # session, and this is its second caller rather than a second copy of the refusal.
    $null = Invoke-OnTheDesk -Vmx $Vmx -Arguments @(
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
        '-NoProfile', '-ExecutionPolicy', 'Bypass',
        '-File', "$script:GuestSync\desk.ps1")

    $answerFile = Join-Path $Stage 'desk.txt'
    $null = Invoke-VmRun -Guest -Arguments @(
        'copyFileFromGuestToHost', $Vmx, "$script:GuestSync\desk.txt", $answerFile)

    # A probe that wrote nothing is not a quiet desk. It is a probe that did not run, and calling
    # that clear is the shape of green this whole project refuses.
    if (-not (Test-Path -LiteralPath $answerFile)) {
        return [pscustomobject]@{
            State = 'unread'; Process = ''; Pid = ''; Class = ''
            Detail = 'the desk probe wrote no answer in the guest'
        }
    }

    $said = (Read-ConsoleText $answerFile).Trim()
    $fields = $said -split '\|', 5
    if ($fields.Count -lt 5) {
        return [pscustomobject]@{
            State = 'unread'; Process = ''; Pid = ''; Class = ''
            Detail = "the desk probe answered something this cannot read: $said"
        }
    }

    return [pscustomobject]@{
        State = $fields[0]; Process = $fields[1]; Pid = $fields[2]; Class = $fields[3]; Detail = $fields[4]
    }
}

function Clear-GuestDesk {
    <#
      WW371. Put an ordinary window away, so a desk nobody is standing at can still be run on.

      What this is for is in `tools/desk-clear.ps1`, which decides what may be moved and why - the
      probe reads and never repairs, and the argument for keeping those apart is WW311's. Here is
      only the carrying, which is `Read-GuestDesk`'s shape one file over.

      What it answers is what the guest said it did, and never whether the desk is clear. The caller
      reads the desk again for that: a window that comes back is a window that refuses this run, and
      a repair trusted on its own report would be exactly the green this project refuses.
    #>
    param([Parameter(Mandatory)] [string] $Vmx, [Parameter(Mandatory)] [string] $Stage)

    $clearer = Join-Path $PSScriptRoot 'desk-clear.ps1'
    if (-not (Test-Path -LiteralPath $clearer)) { return "the desk clearer is missing: $clearer" }

    $null = Invoke-VmRun -Guest -Arguments @('deleteFileInGuest', $Vmx, "$script:GuestSync\cleared.txt")

    $sent = Invoke-VmRun -Guest -Arguments @(
        'copyFileFromHostToGuest', $Vmx, $clearer, "$script:GuestSync\clear.ps1")
    if (-not $sent.Ok) { return "could not copy the desk clearer into the guest: $($sent.Output)" }

    # On the desk, for the reason the probe is: a program run outside the session has no foreground
    # to put away, and one that reported success from out there would be reporting about no desk.
    $null = Invoke-OnTheDesk -Vmx $Vmx -Arguments @(
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
        '-NoProfile', '-ExecutionPolicy', 'Bypass',
        '-File', "$script:GuestSync\clear.ps1")

    $answerFile = Join-Path $Stage 'cleared.txt'
    $null = Invoke-VmRun -Guest -Arguments @(
        'copyFileFromGuestToHost', $Vmx, "$script:GuestSync\cleared.txt", $answerFile)

    if (-not (Test-Path -LiteralPath $answerFile)) { return 'the desk clearer wrote no answer in the guest' }

    return (Read-ConsoleText $answerFile).Trim()
}

function Start-Guest {
    <#
      Power the guest on without letting `vmrun start` decide how long this run lasts.

      That call blocks until VMware is satisfied, and there are states in which it never is: a .lck
      a killed Workstation left behind, a console sitting on the encryption prompt with nobody at
      it. Waited on directly it is unbounded, and a run that cannot end is worse than one that
      refuses - it gets killed by hand, which is the thing that leaves a tree the next sync cannot
      delete.

      So the start is launched and never waited on. What this run needs is not that call returning,
      it is VMware Tools answering inside the guest, and a guest that comes up while `start` is
      still thinking has come up. The poll below is the verdict; the start is only how it was asked.

      It also says the time out loud, every poll. Ten silent minutes and a wedge read identically
      from outside, and reading them apart is the whole reason this waits rather than blocks.

      WW396. The start writes to files of its own, and that is not tidiness. `vmrun start ... gui`
      launches VMware's own window, which outlives this script by design - it is the console a
      person watches - and it inherits the handles it was launched with. Started inside a job those
      are this process's, and this process's are the caller's: a run piped anywhere printed nothing
      for sixty-five minutes after the script itself had exited, because the write end of that pipe
      was open in a window nobody was waiting for and end-of-file arrives when somebody closes the
      VM.

      Redirecting the start's own output does not fix it, which was measured rather than assumed: a
      cold run with `-NoNewWindow -RedirectStandardOutput` hung exactly as it had, because a
      redirected launch still passes every other inheritable handle down and the caller's pipe is
      one of them. What breaks the chain is a launch that inherits nothing - which is what
      Start-Process does when it is given no redirection and no console to share.

      So the command is a file and the file is what runs, which is the shape this script already
      uses for everything it sends the guest: a generated script quotes nothing through anybody, and
      it can redirect vmrun's own words to files itself while this process hands it nothing.

      The improvement that filed this proposed the repair in the wrapper a person types, and that is
      the wrong place: the wrapper can only redirect everything, which would take the script's own
      output away from the caller as well. What has to be handed a handle of its own is the console,
      and the only line that can is the one that starts it.
    #>
    param([Parameter(Mandatory)] [string] $Vmx)

    # gui and never nogui: this run needs a desk that draws, and a headless guest is the session
    # WW42 was measured on - everything present, nothing rendering.
    $argv = Get-VmRunArguments -Arguments @('start', $Vmx, 'gui')

    $said = Join-Path ([IO.Path]::GetTempPath()) "$($script:Name)-start.log"
    $wrong = "$said.err"

    # Two rules, and both were learned by a cold run rather than reasoned about. A path with a space
    # in it arrives as two arguments unless it is quoted, and the .vmx has one on this machine. And
    # the encryption password is never written down: it goes as the name of the variable holding it,
    # which the child inherits and expands itself, so the secret is not on disk and is not quoted by
    # anybody - the second cold run answered `Incorrect password` when it was.
    $line = ($argv | ForEach-Object {
        if ($env:WINWRIGHT_VM_PASSWORD -and $_ -ceq $env:WINWRIGHT_VM_PASSWORD) { '$env:WINWRIGHT_VM_PASSWORD' }
        elseif ($_ -match '\s') { '"' + $_ + '"' }
        else { $_ }
    }) -join ' '

    $asking = Join-Path ([IO.Path]::GetTempPath()) "$($script:Name)-start.ps1"
    "& `"$($script:VmRun)`" $line 1> `"$said`" 2> `"$wrong`"; exit `$LASTEXITCODE" |
        Set-Content -LiteralPath $asking -Encoding ascii

    # No -NoNewWindow and no -Redirect*, which is the whole of the repair: either of those makes
    # Start-Process launch the child itself and pass it this process's handles. Without them it goes
    # through the shell, which inherits none, and the window it would show is hidden.
    $starting = Start-Process -FilePath 'powershell.exe' -PassThru -WindowStyle Hidden `
        -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$asking`""

    # Ten minutes, not five. freewilly measured an agent taking longer than five to come up after a
    # component install, and gave up on a machine that was fine.
    $deadline = (Get-Date).AddMinutes(10)
    $began = Get-Date
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 10

        $tools = Invoke-VmRun -Arguments @('checkToolsState', $Vmx)
        if ($tools.Output -match 'running') {
            return
        }

        # A start that came back non-zero is an answer, and waiting out the rest of the deadline for
        # a guest nobody is powering on is ten minutes spent on a question already settled. Read off
        # the process rather than a record it promised: an exit code is what it has, and what it
        # wrote is in the files beside it.
        if ($starting.HasExited -and $starting.ExitCode -ne 0) {
            Refuse "the guest would not start: $(Started $said $wrong)"
        }

        $waited = [int]((Get-Date) - $began).TotalSeconds
        $asked = if ($starting.HasExited) { 'vmrun start returned' } else { 'vmrun start has not returned yet' }
        $state = if ($tools.Output) { $tools.Output.Trim() } else { 'no answer' }
        Write-Host "  starting    ${waited}s waited; $asked, tools: $state"
    }

    Refuse 'VMware Tools never answered in the guest within ten minutes' 'Look at the VM console. A guest stopped at the encryption prompt or the boot menu is waiting for a person, not for this script.'
}

function Started {
    <#
      What the start wrote, out of the two files it was given. WW396.

      Both, and the error first: vmrun says why it refused on standard error and says nothing at all
      on the other, so a refusal read from stdout alone is a refusal with no sentence in it.
    #>
    param([Parameter(Mandatory)] [string] $Said, [Parameter(Mandatory)] [string] $Wrong)

    # Wrapped, because a pipeline that produced one string is that string under Set-StrictMode and
    # a string has no Count. Measured on the first cold run after WW396: the refusal this composes
    # threw instead of saying why the guest would not start.
    $lines = @(
        @($Wrong, $Said) |
            Where-Object { Test-Path -LiteralPath $_ } |
            ForEach-Object { (Read-ConsoleText $_).Trim() } |
            Where-Object { $_.Length -gt 0 })

    if ($lines.Count -eq 0) { return 'it wrote nothing at all' }
    return ($lines -join ' ')
}

function Read-ConsoleText {
    <#
      Decodes guest output by what is in it rather than by what wrote it. dotnet writes UTF-8 and
      several Windows tools write UTF-16LE, and reading either with the wrong one is not a crash: it
      is a NUL after every character, which reads as data and reaches a report.

      WW311. A byte order mark is the same defect one character long, and it was left in: Windows
      PowerShell's `Set-Content -Encoding utf8` writes one, `Trim()` does not remove it because it
      is not whitespace, and the first field of the first line comes back a character longer than
      it looks. It cost a run, which read `?clear` where the guest had written `clear` and refused
      an answer it had in hand. Stripped here rather than at the one caller that noticed, because
      every guest tool that writes UTF-8 from PowerShell writes it.
    #>
    param([Parameter(Mandatory)] [string] $Path)

    $bytes = [IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -eq 0) { return '' }
    if ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) {
        return [Text.Encoding]::Unicode.GetString($bytes, 2, $bytes.Length - 2)
    }
    $zeroes = 0
    for ($i = 1; $i -lt $bytes.Length; $i += 2) { if ($bytes[$i] -eq 0) { $zeroes++ } }
    $text = if (($zeroes * 4) -gt $bytes.Length) {
        [Text.Encoding]::Unicode.GetString($bytes)
    } else {
        [Text.Encoding]::UTF8.GetString($bytes)
    }

    return $text.TrimStart([char]0xFEFF)
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

# Named on every run and not only an adopter's. A runner that can carry two trees has to say which
# one it took, or a green is a green about whichever tree the caller believed they named.
Write-Host "  tree        $script:Tree  ->  $script:GuestRepo"
Write-Host "  running     $script:Run"

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
    Start-Guest -Vmx $vmxPath
}

$tools = Invoke-VmRun -Arguments @('checkToolsState', $vmxPath)
if ($tools.Output -notmatch 'running') {
    Refuse "VMware Tools is '$($tools.Output.Trim())' in the guest" 'Install VMware Tools there. Without it vmrun can run nothing.'
}
Write-Host '  guest       running, tools answering'

# WW314. Asked here and not where the suite starts, which is a zip, a copy, an extract and an SDK
# probe later. Tools answering is not a desk: the service side of the guest replies while the login
# screen is still up, and a guest that nobody has logged into is the likeliest state of one that
# just cold-booted — which WW305 made the ordinary way to reach it. The first cold start of a day
# found a desktop and the second did not, and paid the whole carry to say so.
#
# `cmd /c exit` and nothing else: the cheapest program that cannot run without a session, so the
# probe costs one process start and answers the one question it asks.
#
# WW412: and the one call here that waits for one. A guest this run may have powered on itself is
# still logging in, and the ten minutes spent above on the tools that reach the desk were spent so
# that a machine which is fine is not called broken.
$null = Invoke-OnTheDesk -Vmx $vmxPath -Arguments @('C:\Windows\System32\cmd.exe', '/c', 'exit') `
    -SessionWithinMinutes $script:SessionMinutes
Write-Host '  desk        a session is logged in'

# --- the tree the guest will test ---------------------------------------------------------------

$stage = Join-Path ([IO.Path]::GetTempPath()) "$($script:Name)-vm"
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
$null = New-Item -ItemType Directory -Path $stage

# WW311. Before the carry and after the session, because those are the two things this costs: it
# needs a desk to have a foreground at all, and it is worth nothing once the twenty minutes are
# already spent. The stage exists by here and the sync directory does not, so the probe makes it.
$desk = Read-GuestDesk -Vmx $vmxPath -Stage $stage

# WW388. The desks a run is willing to tidy before it starts, and the sentence the two readings had
# been missing between them.
#
# WW371 and WW375 landed an hour apart and answer one desk two ways. A minimised window holding the
# foreground is `stale` - not a question, so the run goes on - and it is also exactly what the
# clearer puts away: it has a minimise button, it is already down, and handing the foreground on is
# the half that repair exists for. Nothing decided between them and the order settled it by
# accident, so the Edge window WW371 was filed about was reported and stepped over.
#
# Both are tidied now, and the line is drawn at what the tidying is for rather than at how bad the
# desk looks. `asking` and `stale` are a window this run could put away and would rather not leave
# for a case to trip over: the run going on is right, and it is not the whole answer, because the
# first case to take the foreground is not always one that takes it - a case reading the foreground
# as a precondition excuses a check over a desk a second would have cleared.
#
# Everything else is reported and never touched, each for its own reason. `clear` has nothing to
# tidy. `busy` is a desk somebody let go of already, and the suite's own foreground handling is for
# exactly that. `shell` is the taskbar, which WW331 measured as not a question and WW330 stops a run
# leaving - and putting the shell away is taking the thing this project spends a whole rule giving
# back. `broken` and an unreadable answer are refusals: there is nothing there to tidy.
#
# What the two tidied arms do NOT share is what a failed tidy means. A question that survives it
# refuses the run, because a question is what nobody at a console can be sent to answer twice; a
# minimised window that survives it is the desk WW375 already said the run may go on with.
$script:Tidied = @('asking', 'stale')

switch ($desk.State) {
    'clear' {
        Write-Host '  foreground  clear'
    }
    'busy' {
        # Not a refusal. Something had the foreground and let go of it, which is the desk the
        # suite's own foreground handling exists for - and every excuse it produces is named in
        # the roll call rather than swallowed.
        Write-Host "  foreground  busy: $($desk.Detail)" -ForegroundColor Yellow
    }
    'shell' {
        # WW331. Not a refusal, and the difference is the whole task: the shell asks nothing, so
        # there is nothing at the guest console for a reader to go and answer. Said out loud anyway,
        # because a desk left with the taskbar selected is a run that did not put back what it took
        # — and the run that has just been refused for it is the wrong place to find that out.
        Write-Host (
            "  foreground  the shell is selected, not asking: $($desk.Process) " +
            "(pid $($desk.Pid), $($desk.Class)). Something left the desk on the taskbar; WW330 is " +
            'what stops a tray act doing it. The run goes on — the first case to take the ' +
            'foreground clears it.') -ForegroundColor Yellow
    }
    'stale' {
        # WW375. Not a refusal, and the difference is the whole reading: the window holding the desk
        # is minimised, so nobody can see it and nobody can answer it. Windows keeps a minimised
        # window as the foreground until something else claims it, which is how an ordinary desk
        # ends up here — measured on this guest, where an Edge window left focused was iconic for
        # all twelve looks and refused every run.
        #
        # WW388 put it away as well as saying it. The window is already down, so the whole of the
        # repair here is the half WW371 was actually filed about — handing the foreground on — and
        # this is the desk that motivated it: reported and stepped over, because `stale` is
        # classified before the arm that clears.
        #
        # Still not a refusal, and that is the difference from `asking` below. A minimised window
        # the clearer declines is a window somebody minimised without a minimise button, and WW375
        # already said a run may go on with it.
        Write-Host (
            "  foreground  a minimised window holds it, so there is nothing to answer: " +
            "$($desk.Process) (pid $($desk.Pid), $($desk.Class)) '$($desk.Detail)'. Putting it " +
            'away, because a desk nobody can read is one the first case would otherwise trip ' +
            'over.') -ForegroundColor Yellow

        $cleared = Clear-GuestDesk -Vmx $vmxPath -Stage $stage
        Write-Host "  clearing    $cleared"

        $desk = Read-GuestDesk -Vmx $vmxPath -Stage $stage

        # Read whole, for the reason the arm below reads its second look whole: a desk that came
        # back as something this run cannot drive is one to refuse on, and the arm it started in has
        # nothing to say about that. `stale` is in the list because staying stale is the answer
        # WW375 gave and this arm does not overturn it.
        if ($desk.State -notin @('clear', 'busy', 'shell', 'stale')) {
            Refuse (
                "the desk was tidied and now reads '$($desk.State)': $($desk.Detail)"
            ) 'Look at the guest console. The window was put away and what is there now is not a desk this run can drive.'
        }

        Write-Host "  foreground  tidied, and now reads $($desk.State)" -ForegroundColor Yellow
    }
    'asking' {
        # WW371. Tried once before refusing, and read again afterwards rather than believed.
        #
        # What this arm used to be was a refusal with one remedy — go and click it at the guest
        # console — and a session working a backlog has nobody there. Measured on WW358: a cold
        # start passed 1961 cases, and the next run refused because Edge had restored its session
        # at login and held the foreground for all twelve looks. The runner had manufactured the
        # desk that then refused it.
        #
        # The clearer moves a window a person could have minimised and leaves anything else alone,
        # which is where the line is: a modal prompt has no minimise button, and that is what makes
        # it a question. So the desk is read again and this refuses on what the second reading says.
        # WW311's rule is intact — waiting does not clear a question — and a question is still
        # exactly what refuses the run.
        Write-Host (
            "  foreground  waiting for an answer: $($desk.Process) (pid $($desk.Pid), " +
            "$($desk.Class)) '$($desk.Detail)'. Trying to put it away.") -ForegroundColor Yellow

        $cleared = Clear-GuestDesk -Vmx $vmxPath -Stage $stage
        Write-Host "  clearing    $cleared"

        $desk = Read-GuestDesk -Vmx $vmxPath -Stage $stage
        if ($desk.State -eq 'asking') {
            Refuse (
                "the guest's desk is waiting for an answer: $($desk.Process) (pid $($desk.Pid), " +
                "$($desk.Class)) '$($desk.Detail)' held the foreground for every look, and it is " +
                "still there after the run tried to put it away — $cleared"
            ) (
                'Answer it at the guest console and run again. Waiting does not clear a question, ' +
                'and killing its owner cost the tray the last time it was tried: the window belongs ' +
                'to ShellExperienceHost, and the run after that went red with no icon anywhere. ' +
                'This is the prompt that took three steps off the adoption keyboard case and ' +
                'reported them unchecked twenty minutes after the carry.'
            )
        }

        # The second reading is read whole and not only for `asking`. A desk that came back
        # `broken` or unreadable is a desk this run cannot use either, and falling past it because
        # the arm it landed in was chosen by the first reading is the kind of green that gets a
        # suite run against a session that renders nothing.
        if ($desk.State -notin @('clear', 'busy', 'shell')) {
            Refuse (
                "the desk was cleared and now reads '$($desk.State)': $($desk.Detail)"
            ) 'Look at the guest console. The window was put away and what is there now is not a desk this run can drive.'
        }

        Write-Host "  foreground  cleared, and now reads $($desk.State)" -ForegroundColor Yellow
    }
    'broken' {
        Refuse "the guest is logged in and has no shell: $($desk.Detail)" 'Sign out and back in at the guest console. A desk with no foreground renders nothing for a capture and takes no input.'
    }
    default {
        Refuse "the desk could not be read: $($desk.Detail)" 'The probe runs in the guest session and writes one line beside itself. Check the guest console is reachable.'
    }
}

Push-Location $script:Tree
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
        $full = Join-Path $script:Tree $relative
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

function Get-WhatHolds {
    # WW404. Windows refuses the delete with the directory's name and never with the name of whoever
    # has it open, and the one machine that can answer that is this one. Two readings, because a
    # suite in the guest is two kinds of process: the test host runs from under the tree, and the
    # runtime hosting it does not but has the tree's assemblies mapped in.
    `$held = @()
    foreach (`$one in Get-Process) {
        `$where = `$null
        try { `$where = `$one.Path } catch { }
        if (`$where -and `$where.StartsWith('$script:GuestRepo', 'OrdinalIgnoreCase')) {
            `$held += "`$(`$one.ProcessName) (`$(`$one.Id)) runs from `$where"
            continue
        }
        `$mapped = @()
        try { `$mapped = @(`$one.Modules | Where-Object { `$_.FileName -and `$_.FileName.StartsWith('$script:GuestRepo', 'OrdinalIgnoreCase') }) } catch { }
        if (`$mapped.Count -gt 0) { `$held += "`$(`$one.ProcessName) (`$(`$one.Id)) has `$(`$mapped[0].FileName) loaded" }
    }
    return `$held
}

if (Test-Path -LiteralPath '$script:GuestRepo') {
    try { Remove-Item -LiteralPath '$script:GuestRepo' -Recurse -Force }
    catch {
        Write-Output ('GUEST-HELD ' + (`$_.Exception.Message -replace '\s+', ' '))
        foreach (`$one in Get-WhatHolds) { Write-Output "GUEST-HOLDER `$one" }
        exit 92
    }
}
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

rem Node reuse off, and the build servers shut down at both ends. Measured: five dotnet processes
rem and a VBCSCompiler outlived their runs and accumulated across them, and on a guest of eight
rem cores they took a suite from 3m18s to 5m53s - which failed three timing cases that had nothing
rem wrong with them. This block's own criterion is that no process outlives the run that started
rem it, and the harness enforcing it was the one breaking it.
set "MSBUILDDISABLENODEREUSE=1"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"

rem WW150: the name this run's results go under, read by MSBuild as a property the way it reads
rem every environment variable. Set here rather than passed through run-tests.cmd, so the guest
rem types the same command a developer does.
set "RollCallRun=$script:RunName"

rem WW289: where a run's ledger of excused checks is kept so the next run can say its own count
rem beside it. Under the sync folder and never under the tree, because sync.ps1 deletes the tree
rem whole and re-extracts it - a history inside it is gone before anything can read it, and every
rem run would report that no earlier run existed. Read by MSBuild as a property, like RollCallRun
rem above.
set "RollCallRoot=$script:GuestSync\history"
dotnet build-server shutdown >nul 2>&1

cd /d "$script:GuestRepo"
call $script:Run > "$script:GuestSync\vm-run.log" 2>&1

rem Captured before the shutdown and not after. Anything run between the suite and the write takes
rem %ERRORLEVEL% with it, so the tidying up would have reported its own success as the suite's.
set "RC=%ERRORLEVEL%"
dotnet build-server shutdown >nul 2>&1

rem The redirect leads, and that is not a style choice. `echo %RC%> file` is parsed by cmd as
rem `echo` redirected to handle %RC% whenever the code is a single digit, so a run that exited 1
rem wrote "ECHO is off." into the file and the host read the suite's verdict as a string.
> "$script:GuestSync\vm-exit.txt" echo %RC%
exit /b 0
"@ | Set-Content -LiteralPath (Join-Path $stage 'run.cmd') -Encoding ascii

# WW406. What Blame leaves behind when the test host stops answering: a dump of every thread, and a
# sequence naming the case that was still running. Both are the evidence and both were lost three
# times, because they are written under the tree and the next run's sync deletes the tree whole.
#
# Gathered in the guest rather than fetched from it, because vmrun copies by exact path and cannot
# glob - and these are written under a directory named for a GUID, by a file name carrying the test
# host's pid and the minute it gave up. Nothing on the host can spell that. So the guest finds them
# and puts them where the host already knows to look.
#
# The largest dump and the newest sequence, one each. The collector writes both twice, once in place
# and once under an `In\<machine>` folder it copies to, and two of the same file is not two readings.
@"
`$ErrorActionPreference = 'Stop'
foreach (`$stale in @('blame.dmp', 'blame-sequence.xml', 'blame.txt')) {
    `$at = Join-Path '$script:GuestSync' `$stale
    if (Test-Path -LiteralPath `$at) { Remove-Item -LiteralPath `$at -Force }
}

`$said = @()

`$dump = Get-ChildItem -LiteralPath '$script:GuestRepo\$script:ResultsIn' -Recurse -File -Filter '*.dmp' -ErrorAction SilentlyContinue |
    Sort-Object Length -Descending | Select-Object -First 1
if (`$dump) {
    Copy-Item -LiteralPath `$dump.FullName -Destination (Join-Path '$script:GuestSync' 'blame.dmp') -Force
    `$said += "`$(`$dump.Name) (`$([math]::Round(`$dump.Length / 1MB, 1)) MB)"
}

`$order = Get-ChildItem -LiteralPath '$script:GuestRepo\$script:ResultsIn' -Recurse -File -Filter 'Sequence_*.xml' -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (`$order) {
    Copy-Item -LiteralPath `$order.FullName -Destination (Join-Path '$script:GuestSync' 'blame-sequence.xml') -Force
    `$said += `$order.Name
}

if (`$said.Count -eq 0) { `$said = @('nothing') }
(`$said -join '; ') | Set-Content -LiteralPath (Join-Path '$script:GuestSync' 'blame.txt') -Encoding ascii
"@ | Set-Content -LiteralPath (Join-Path $stage 'blame.ps1') -Encoding ascii

# --- into the guest ------------------------------------------------------------------------------

$null = Invoke-VmRun -Guest -Arguments @('createDirectoryInGuest', $vmxPath, $script:GuestSync)

# Deleted in the guest before the run, never merely overwritten after it. A run that writes nothing
# leaves the last one's files in place, the copy back succeeds, and the caller is handed a previous
# afternoon's trx as this run's result - which is worse than an error, because it looks like one.
#
# WW150: winwright.trx and discovered.txt used to be on this list and were never on this path - they
# live under the repository's TestResults, not the sync directory, so deleting them here deleted
# nothing and the guard everybody read as covering them covered neither. They need no guard now:
# this run's results directory is named for this run, so there is nothing of an earlier one in it.
#
# WW406: and the three the blame gather leaves, for exactly that reason one file further out. They
# live beside the sync and outlive the tree, so a run that ends cleanly would otherwise copy back the
# dump of the run before it - which is worse than no dump, because it is a dump with a date on it.
foreach ($stale in @('vm-exit.txt', 'vm-run.log', 'sync.log', 'blame.dmp', 'blame-sequence.xml', 'blame.txt')) {
    $null = Invoke-VmRun -Guest -Arguments @('deleteFileInGuest', $vmxPath, "$script:GuestSync\$stale")
}

foreach ($file in @('source.zip', 'sync.ps1', 'sync.cmd', 'run.cmd', 'blame.ps1')) {
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

    # WW404. The tree would not delete, and the guest was asked who has it before the answer got
    # this far. An empty list is an answer too, and a different one: it says the walk ran and found
    # nothing running from under the tree, which sends a reader somewhere other than Task Manager.
    if ($syncSaid -match 'GUEST-HELD') {
        $said = $syncSaid -split "`r?`n"
        $windows = (($said | Where-Object { $_ -match '^GUEST-HELD ' }) -replace '^GUEST-HELD ', '') -join ' '
        $holders = @(($said | Where-Object { $_ -match '^GUEST-HOLDER ' }) -replace '^GUEST-HOLDER ', '')
        $by = if ($holders.Count -gt 0) { "held open by $($holders -join '; ')" }
              else { 'held open by nothing the guest is running, so something outside it has a file there' }
        Refuse "the guest tree is $by" "Windows said: $windows. End them at the guest console, or revert the VM to a clean snapshot. A run that -Bound gave up on, or one killed by hand, leaves exactly this."
    }

    Refuse "the guest tree would not sync: $syncSaid $($synced.Output)"
}
Write-Host "  guest tree  $syncSaid"

# --- the run ------------------------------------------------------------------------------------

Write-Host ''
Write-Host "  running the suite in the guest ($Configuration). The host is yours." -ForegroundColor Cyan

# TestResults\vm and never TestResults itself. Measured the hard way: the guest's trx landed on the
# host's, and the only thing in it saying which desk had produced the run was a computerName buried
# in the XML. Two machines writing one path is a result that cannot say where it came from, which is
# the same defect as a green that cannot say what it covered.
#
# WW386 made this the line before the run rather than the line after it. What a bound produces is a
# reading, and the log it fetches belongs where the log of a run that finished goes: a person who has
# just been told the guest stopped answering should not also be told about a directory.
$results = Join-Path $script:Tree 'TestResults\vm'
if (-not (Test-Path -LiteralPath $results)) { $null = New-Item -ItemType Directory -Path $results -Force }

# Through the same door the probe used, so a desk that locked itself between the two is refused with
# the sentence the probe would have given it rather than with "the guest never finished the run".
# WW386: and with a bound, which is the one call here that has ever had time to need one.
$ran = Invoke-OnTheDesk -Vmx $vmxPath -Arguments @("$script:GuestSync\run.cmd") -Minutes $Bound -Stage $results
if (-not $ran.Ok) {
    Refuse "the guest never finished the run: $($ran.Output)"
}

# --- back to the host -----------------------------------------------------------------------------

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

$guestResults = "$script:GuestRepo\$script:ResultsIn"

$first = $script:Bring[0]
$got = Invoke-VmRun -Guest -Arguments @('copyFileFromGuestToHost', $vmxPath, "$guestResults\$first", (Join-Path $results $first))
if (-not $got.Ok) { Write-Host "  no $first came back from the guest" -ForegroundColor Yellow }

# Everything after the first is only chased on a green run, and that is a fact about this repository
# rather than about the guest. The roll call hangs off AfterTargets="VSTest", so a failed VSTest never
# reaches it and writes no discovered.txt - warning about its absence on a red run reports a missing
# check that was never due, which is noise on exactly the runs already carrying a real answer.
if ($code -eq 0) {
    foreach ($also in ($script:Bring | Select-Object -Skip 1)) {
        $roll = Invoke-VmRun -Guest -Arguments @('copyFileFromGuestToHost', $vmxPath, "$guestResults\$also", (Join-Path $results $also))
        if (-not $roll.Ok) { Write-Host "  the run passed and no $also came back" -ForegroundColor Yellow }
    }
}

# WW406. The mirror of the block above, and chased only on a red run for the same kind of reason: a
# run that passed left no dump, and asking after one every time would be two vmrun calls a run to be
# told so. A host that stopped answering is the one state where the evidence is not in the trx.
#
# It has been lost three times. The dump is written under the tree, the next sync deletes the tree
# before writing it again, and the run that would have explained the last one is the run that
# destroys it - so this is the only window there is, and nobody has ever been at the keyboard for it.
if ($code -ne 0) {
    $null = Invoke-VmRun -Guest -Arguments @(
        'runProgramInGuest', $vmxPath,
        'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe',
        '-NoProfile', '-ExecutionPolicy', 'Bypass',
        '-File', "$script:GuestSync\blame.ps1")

    $note = Join-Path $stage 'blame.txt'
    $null = Invoke-VmRun -Guest -Arguments @('copyFileFromGuestToHost', $vmxPath, "$script:GuestSync\blame.txt", $note)
    $named = if (Test-Path -LiteralPath $note) { (Read-ConsoleText $note).Trim() } else { '' }

    # Said out loud either way. "nothing" is a reading - the host exited on its own rather than
    # being waited out - and a line that only appears when there is a dump makes its absence look
    # like a step that did not run.
    if ($named.Length -eq 0) {
        Write-Host '  blame       the guest could not be asked what the collector left' -ForegroundColor Yellow
    }
    else {
        Write-Host "  blame       $named"

        foreach ($pair in @(@('blame.dmp', 'hangdump.dmp'), @('blame-sequence.xml', 'sequence.xml'))) {
            $kept = Join-Path $results $pair[1]
            $fetched = Invoke-VmRun -Guest -Arguments @(
                'copyFileFromGuestToHost', $vmxPath, "$script:GuestSync\$($pair[0])", $kept)

            if ($fetched.Ok) { Write-Host "              $kept" }
        }
    }
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
