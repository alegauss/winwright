<#
  WW472. Whether anything can take this desk, asked the way a fixture asks - before twenty minutes
  are spent finding out that nothing could.

  `desk-probe.ps1` reads which window is holding the foreground and names it. That reading was right
  every time and it is not this question. Measured twice while WW470 shipped, at 24 and 30 minutes a
  time: the guest's taskbar held the desk, the probe called it `shell`, the runner printed *the first
  case to take the foreground clears it* and carried the tree. It did not clear.
  `PumpedDialog.TakeTheDesktop` polls `SetForegroundWindow` and Windows refused it for both runs, so
  136 and then 137 checks were excused where a healthy run excuses nine, and five cases that cannot
  excuse a lost desk went red about nothing. Both exit codes said the tree was broken; the same suite
  passed 2187 of 2187 once the shell was restarted.

  So the word was never the discriminator. A taskbar holding the desk is usually a desk the next
  thing touched clears, and once it was not - and no reading of WHO holds it separates those two. The
  only thing that does is asking for it.

  **The nudge is what makes this a stand-in rather than a guess, and it was measured both ways.**
  WW472 was filed having tried this without one: a script carried in and run through
  `runProgramInGuest -interactive` was refused the foreground on a desk reading `clear`, where every
  fixture takes it, so it predicted nothing. Measured again on the same guest and the same idle
  desktop: refused for 4039ms with no input event, granted in 31ms with one. Windows hands the
  foreground to the process that received the last input event, and in a real run the suite has sent
  hundreds by the time a fixture asks. WW384 measured the other side of the same rule - a run of one
  case had sent none, and was refused the desktop for 4004ms on a desk the runner had just read as
  clear.

  A mouse move of zero pixels, so nothing moves and nothing is pressed. It is the smallest thing that
  makes this process one Windows will answer, and what it buys is a question about the DESK: can a
  process that has just sent input take it. That is what a run's fixtures are. Whether a process that
  has never sent one can take a desk is a question about the asker, and it is answered in the suite
  rather than here.

  What the answers mean:

    takeable  the window this put up got the foreground, and the desk is one a fixture can take.
    held      it asked for the whole deadline and never got it, and the window holding it is named.
              Nothing a run does will clear that desk, so the tree is not carried onto it.

  Measured on the guest, five desks. Four answer `takeable`: an idle desktop at 31ms; the desk left
  by another process pressing Win+D, at 36ms, so what that lock leaves behind does not survive an
  asker sending its own input; the taskbar selected, which is WW472's own desk, at 38ms; and a desk
  another process was holding a `LockSetForegroundWindow` on, at 47ms, which is why that API is not
  how a case arranges a refusal. One answers `held`: the Start menu up, which is `SearchHost`
  holding a `Windows.UI.Core.CoreWindow`, refused for the whole 4016ms.

  The window is put away afterwards and the desk goes back to whoever had it, which was measured too:
  the probe ran against a selected taskbar and `desk-probe.ps1` read `shell` again after it. So this
  answers a question and leaves the desk it asked about.

  -DefineOnly dot-sources it without asking any desk for anything, which is how the suite reaches
  both halves: the classification with results it made up, and the asking against a desk a case is
  holding.
#>
param([switch] $DefineOnly, [int] $WaitMs = 4000, [int] $PollMs = 100)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Read-DeskTake {
    <#
      What the asking came to, as the one line the runner switches on: state|process|pid|class|title.

      The same five fields `desk-probe.ps1` writes, and for the same reason: a runner that parses one
      shape parses both, and a state with a window to name puts it where the other one does.

      A pure function of what was measured, which is what lets anything run it. $Took is whether the
      foreground was granted, $ElapsedMs how long that took or how long it was refused for, and
      $Holder the window holding the desk when the asking stopped - $null where nothing was.
    #>
    param(
        [Parameter(Mandatory)] [bool] $Took,
        [Parameter(Mandatory)] [int] $ElapsedMs,
        [AllowNull()] [object] $Holder = $null)

    if ($Took) {
        return "takeable||||the foreground was granted after ${ElapsedMs}ms"
    }

    # Refused with nothing holding the desk. `desk-probe.ps1` has a word for that desk and this one
    # does not overturn it - what is said here is what was asked and what came back, so a reader
    # meeting both lines is not told two different things about one desk.
    if ($null -eq $Holder) {
        return "held||||nothing held the foreground and it was refused for ${ElapsedMs}ms anyway"
    }

    return "held|$($Holder.Process)|$($Holder.Pid)|$($Holder.Class)|$($Holder.Title)"
}

if (-not ('Ask' -as [type])) {
    Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public static class Ask {
    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT { public int dx; public int dy; public uint data; public uint flags; public uint time; public IntPtr extra; }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT { public uint type; public MOUSEINPUT mi; }

    [DllImport("user32.dll", SetLastError = true)] public static extern uint SendInput(uint n, INPUT[] inputs, int size);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowTextW(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassNameW(IntPtr h, StringBuilder s, int n);

    public static string TextOf(IntPtr h) { var s = new StringBuilder(512); GetWindowTextW(h, s, s.Capacity); return s.ToString(); }
    public static string ClassOf(IntPtr h) { var s = new StringBuilder(256); GetClassNameW(h, s, s.Capacity); return s.ToString(); }

    // MOUSEEVENTF_MOVE with no pixels in it. The input queue gets an event and the pointer stays
    // exactly where the person watching left it.
    public static uint Nudge() {
        var one = new INPUT[1];
        one[0].type = 0;
        one[0].mi = new MOUSEINPUT { dx = 0, dy = 0, flags = 0x0001 };
        return SendInput(1, one, Marshal.SizeOf(typeof(INPUT)));
    }
}
"@
}

function Get-DeskHolder {
    <#
      The window holding the foreground, as the fields `Read-DeskTake` names it by - or $null where
      nothing is. The desktop is not folded away here the way the reading probe folds it: a desk
      Progman is holding is one this asking takes, so it never reaches the refusing arm, and a
      Progman that DID refuse is the one thing a reader of that refusal would need to see.
    #>
    $handle = [Ask]::GetForegroundWindow()
    if ($handle -eq [IntPtr]::Zero) { return $null }

    $owner = 0
    [void][Ask]::GetWindowThreadProcessId($handle, [ref] $owner)
    $named = Get-Process -Id $owner -ErrorAction SilentlyContinue

    return [pscustomobject]@{
        Handle  = [int64] $handle
        Pid     = $owner
        Process = if ($named) { $named.ProcessName } else { "pid $owner" }
        Class   = [Ask]::ClassOf($handle)
        Title   = [Ask]::TextOf($handle)
    }
}

function Get-DeskTake {
    <#
      Put a window up and ask this desk for the foreground, the way a fixture does. WW472, and above
      the -DefineOnly return for the reason `Get-DeskLooks` and `Clear-TheDesk` are: a half run by
      nothing but a real guest is a half where a mistake reads as an answer.

      The deadline and the pause are parameters because the guest's four seconds are a measurement
      and a case is not. Four is the same order as the foreground lock's own timeout, and every
      granted desk measured answered inside 103ms - so a desk that has not answered by then is not a
      desk that is about to.

      A form and not a bare CreateWindowEx, and the difference does not matter to the question: what
      Windows weighs is a window with a pump belonging to a process that has just sent input, which
      is what a fixture is. The pump is `DoEvents`, called on every look, because a window that never
      answers a message is one the shell will not hand the desk to.
    #>
    param([int] $WaitMs = 4000, [int] $PollMs = 100)

    $sent = [Ask]::Nudge()

    Add-Type -AssemblyName System.Windows.Forms
    $form = New-Object System.Windows.Forms.Form
    $form.Text = 'winwright desk take'
    $form.StartPosition = 'Manual'
    $form.Location = New-Object System.Drawing.Point(80, 80)
    $form.Size = New-Object System.Drawing.Size(320, 120)

    # Off the taskbar, because this window is a question and not an application: a run watched at the
    # guest console should not be told something started.
    $form.ShowInTaskbar = $false

    try {
        $form.Show()
        [System.Windows.Forms.Application]::DoEvents()

        $watch = [System.Diagnostics.Stopwatch]::StartNew()
        $took = $false

        while ($watch.ElapsedMilliseconds -lt $WaitMs) {
            [void][Ask]::SetForegroundWindow($form.Handle)
            [System.Windows.Forms.Application]::DoEvents()

            if ([Ask]::GetForegroundWindow() -eq $form.Handle) { $took = $true; break }
            if ($PollMs -gt 0) { Start-Sleep -Milliseconds $PollMs }
        }

        $elapsed = [int] $watch.ElapsedMilliseconds
    }
    finally {
        # Before the holder is read, so what is read is the desk and never this window.
        $form.Close()
        $form.Dispose()
    }

    return [pscustomobject]@{
        Took      = $took
        ElapsedMs = $elapsed
        Nudged    = $sent -eq 1
        Holder    = Get-DeskHolder
    }
}

if ($DefineOnly) { return }

$asked = Get-DeskTake -WaitMs $WaitMs -PollMs $PollMs
$answer = Read-DeskTake -Took $asked.Took -ElapsedMs $asked.ElapsedMs -Holder $asked.Holder

# Into a file beside this script, for the reason the other two write one: vmrun runs the program and
# does not carry what it printed, so an answer written to the console is an answer nobody reads.
Set-Content -LiteralPath (Join-Path $PSScriptRoot 'take.txt') -Value $answer -Encoding utf8
