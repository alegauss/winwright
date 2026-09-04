<#
  WW311, WW331, WW345. Which desk this is, before twenty minutes are spent on it.

  WW305 made a cold start ordinary, and a cold desk is one still putting its startup notifications
  up. The first run through it excused twenty-six checks where the four before it excused eight
  each; what that cost was measured later, when the same prompt - OneDrive's *Habilitar o Backup do
  Windows*, two buttons, the same process id hours apart - held the foreground while the adoption's
  keyboard case ran, and that case came back unchecked with three steps unwalked. Not noise in a
  count. A blocker.

  Neither remedy this was opened with survives. Waiting for the shell to go quiet cannot work
  against a question that stays until answered. Killing the owner is worse: the window is
  `ShellExperienceHost`'s, killing that cleared the prompt and cost the tray, and the next full run
  went red with *this desk was called placing and holds no icon anywhere*.

  So this reads rather than fixes, and the reading is the whole point: a toast goes and a question
  does not, and nothing but time tells them apart. The foreground is polled, and what separates the
  answers is whether one window held it for every look.

    clear    nothing but the desktop ever had it.
    busy     something had it and let go. The suite's own foreground handling is for this.
    asking   one window held it for every look, so it is waiting for an answer and no amount of
             waiting is the answer. Named with its process and its title, because a person has to
             go and click it.
    shell    the same, and the window is the taskbar or the overflow flyout. WW331: not a question,
             because the shell has none to ask - a desk somebody left selected, which the next
             thing touched clears. Said and not refused, and it names WW330, which is what stops a
             run leaving one behind.
    broken   nothing holds the foreground at all, which on a logged-in desk means no shell.

  The looks are the measurement and the deadline is the argument: a toast this guest could raise
  lives for seconds, and the prompt that cost a run had been up for hours.

  WW345 moved it out of the runner's here-string and into a file. It decides whether a guest run
  happens at all and it had been wrong twice - once calling a focused taskbar a question, once
  repairing that by making the reading say nothing - and nothing had ever run any of its answers.
  A here-string cannot be run by anything but the runner, so the classification is a function here
  and `DeskProbeTests` calls it with looks it made up. The runner sends this same file to the
  guest, so what the suite exercises and what refuses a run are one file rather than two copies.

  -DefineOnly dot-sources it without reading any desk, which is how the suite reaches the function.
#>
param([switch] $DefineOnly)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# The desktop is not a window holding the foreground: an idle logged-in desk has one of these, and
# reporting it would make every clear desk read as busy.
$script:Desktop = @('Progman', 'WorkerW')

# WW331. The shell's own surfaces, which are neither. A run whose tray cases failed inside the
# overflow left the chevron focused (WW330), the taskbar then held the foreground for all twelve
# looks, and this refused every later run with "the desk is waiting for an answer" - sending a reader
# to a guest console to answer a prompt that a screen capture showed was not there.
#
# They are not the desktop either, and folding them in there was the first repair and the wrong one:
# it made the refusal go away by making the reading say nothing, so a desk somebody had genuinely
# left the shell selected on read as "nothing but the desktop held the foreground". The reading was
# always right. What was wrong is the word for it.
#
# Narrow on purpose, and it has to be: the prompt this probe exists to catch belongs to
# ShellExperienceHost and is a different class from any of these, so naming the taskbar and the
# overflow flyout hides no question anybody has to answer.
$script:ShellSurfaces = @('Shell_TrayWnd', 'Shell_SecondaryTrayWnd', 'TopLevelWindowForOverflowXamlIsland')

function Read-DeskState {
    <#
      What the looks come to, as the one line the runner switches on: state|process|pid|class|title.

      A pure function of what was seen, which is what lets anything run it. $Looks carries one entry
      per look, $null where that look found the desktop or nothing at all; $StillNothing is whether
      the foreground was empty when the polling stopped, which is the only thing separating a desk
      with no shell from a desk nobody is using.
    #>
    param(
        [AllowEmptyCollection()] [Parameter(Mandatory)] [AllowNull()] [object[]] $Looks,
        [bool] $StillNothing = $false)

    $held = @($Looks | Where-Object { $null -ne $_ })

    # Nothing at all, at any look. A logged-in desk with no shell answers this, and it is the one
    # state the other two readings would describe as quiet.
    if ($held.Count -eq 0) {
        if ($StillNothing) {
            return "broken||||nothing held the foreground across $($Looks.Count) look(s), so this desk has no shell"
        }

        return 'clear||||nothing but the desktop held the foreground'
    }

    if ($held.Count -eq $Looks.Count -and
        @($held | Where-Object { $_.Handle -ne $held[0].Handle }).Count -eq 0) {
        # One window for every look is the answer that does not clear. WW331: and which of the two it
        # is turns on whose window it is. A question is some application's, with a caption, and no
        # amount of waiting answers it; the shell holding the desk is a desk somebody left selected,
        # which clears the moment anything else is touched and is what WW330 stops a run leaving.
        $one = $held[0]
        $state = if ($script:ShellSurfaces -contains $one.Class) { 'shell' } else { 'asking' }
        return "$state|$($one.Process)|$($one.Pid)|$($one.Class)|$($one.Title)"
    }

    # Anything else moved, whatever it was.
    $moved = ($held | ForEach-Object { "$($_.Process) '$($_.Title)'" } | Select-Object -Unique) -join '; '
    return "busy||||held for $($held.Count) of $($Looks.Count) look(s): $moved"
}

# WW357. Above the -DefineOnly return, so the polling can be reached by something other than a real
# guest. It used to be below it, which meant the loop had exactly one caller and a look built wrong
# classified perfectly: a window whose class read empty is not the desktop and not a shell surface,
# so a quiet desk would read as a question and refuse the run.
#
# Guarded, because dot-sourcing twice in one session is what a suite does and Add-Type refuses a
# type it already has.
if (-not ('Fg' -as [type])) {
    Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public static class Fg {
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowTextW(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassNameW(IntPtr h, StringBuilder s, int n);

    public static string TextOf(IntPtr h) { var s = new StringBuilder(512); GetWindowTextW(h, s, s.Capacity); return s.ToString(); }
    public static string ClassOf(IntPtr h) { var s = new StringBuilder(256); GetClassNameW(h, s, s.Capacity); return s.ToString(); }
}
"@
}

function Get-DeskLooks {
    <#
      Poll the live foreground and build the looks Read-DeskState judges. WW357.

      One entry per look, $null where the look found the desktop or nothing at all - which is the
      shape that function documents and the shape its cases make up by hand. A look that is built
      here is a look nobody made up, which is the whole reason this is reachable: both defects this
      probe has had were in the classification, and the classification is a pure function of what
      this returns.

      The count and the pause are parameters because the guest's twelve looks over six seconds are a
      measurement and a case is not. A case arranges a desk it already owns and asks for two looks
      with no pause, which runs every line of this and takes no time.

      WW370 makes the desktop's own list the third, and for the same reason: a case cannot arrange a
      desk the desktop is holding, so the one branch here that answers $null was run by nothing. A
      case names the class of the window it has just put up and asserts the look came back as
      nothing, which is the branch under test - that a class on the list is skipped, rather than that
      Progman is a desktop, which is a constant a case would only be restating.

      Defaulted to the list itself, so the guest's own run is the run it always was and no caller has
      to know this parameter exists. What the words are stays checked where it was, by a case reading
      them out of this file beside the shell surfaces: those are two claims and not one.
    #>
    param([int] $Count = 12, [int] $PauseMs = 500, [string[]] $Desktop = $script:Desktop)

    $looks = @()
    for ($at = 0; $at -lt $Count; $at++) {
        if ($at -gt 0 -and $PauseMs -gt 0) { Start-Sleep -Milliseconds $PauseMs }

        $handle = [Fg]::GetForegroundWindow()
        if ($handle -eq [IntPtr]::Zero) { $looks += $null; continue }

        $owner = 0
        [void][Fg]::GetWindowThreadProcessId($handle, [ref] $owner)
        $class = [Fg]::ClassOf($handle)
        if ($Desktop -contains $class) { $looks += $null; continue }

        $named = (Get-Process -Id $owner -ErrorAction SilentlyContinue)
        $looks += [pscustomobject]@{
            Handle  = [int64] $handle
            Pid     = $owner
            Process = if ($named) { $named.ProcessName } else { "pid $owner" }
            Class   = $class
            Title   = [Fg]::TextOf($handle)
        }
    }

    # Comma, because a one-look array unrolls to the look itself on the way out and the caller counts
    # what it was given. A probe that asked for one look and was handed an object would classify it
    # as no looks at all.
    return ,$looks
}

if ($DefineOnly) { return }

$looks = Get-DeskLooks -Count 12 -PauseMs 500

$answer = Read-DeskState -Looks $looks -StillNothing ([Fg]::GetForegroundWindow() -eq [IntPtr]::Zero)

# Into a file beside this script and never to stdout: vmrun runs the program and does not carry
# what it printed, so an answer written to the console is an answer nobody reads.
Set-Content -LiteralPath (Join-Path $PSScriptRoot 'desk.txt') -Value $answer -Encoding utf8
