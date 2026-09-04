<#
  WW371. The repair the probe deliberately does not make, kept beside it and run only on a refusal.

  `desk-probe.ps1` reads and never fixes, and WW311 argues that well: a toast goes and a question does
  not, and killing a prompt's owner cost the tray once already. What it leaves is a refusal with one
  remedy - go and click it at the guest console - and a session working a backlog has nobody there.

  So this separates three things the refusal treats alike: a question only a person can answer, an
  ordinary window somebody left in front, and a window that will not be put away. Only the first
  deserves the refusal, and the difference is readable before twenty minutes are spent on the run.

  What decides it is WS_MINIMIZEBOX. A window a person can minimise is a window a run may minimise;
  a modal prompt has no such button, which is exactly what makes it a question. Nothing is closed,
  nothing is killed, and a window put away here is one click on the taskbar from coming back.

  WW371 was filed believing the minimise itself failed - a browser window measured surviving twelve
  SW_MINIMIZE calls, each reporting the same handle in the foreground afterwards. It was read again
  and the premise was wrong: `IsIconic` on that handle answered true the whole time. The window was
  minimised and Windows had simply kept it as the foreground, because a minimised window stays the
  foreground until something else claims it. So a repair that only minimises reads as a repair that
  did nothing, and the second half - handing the foreground on - is the half that was missing.

  The clearing is never trusted. This says what it did; the runner reads the desk again afterwards
  and refuses on what that says, so a window that comes back is a window that refuses the run, and
  no green here can stand in for a desk that did not clear.

  -DefineOnly dot-sources it without touching a desk, which is how the suite reaches the decision.
#>
param([switch] $DefineOnly)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# The shell's own surfaces, spelled as the probe spells them. A taskbar holding the desk is `shell`
# and never `asking`, so it never reaches this file - but a run that put the taskbar away would be a
# run that took the thing WW330 exists to give back.
$script:Untouchable = @('Shell_TrayWnd', 'Shell_SecondaryTrayWnd', 'TopLevelWindowForOverflowXamlIsland')

# WS_MINIMIZEBOX. The one bit that separates a window a person can put away from a question they
# have to answer.
$script:MinimizeBox = 0x00020000

function Test-Clearable {
    <#
      Whether a run may put this window away, as a pure function of what was read off it.

      Both halves are refusals rather than permissions, which is the safer way round: a window with
      no minimise button is a question, and a shell surface is not this run's to move. Anything else
      is somebody's ordinary window left in front of a desk this run needs.
    #>
    param(
        [Parameter(Mandatory)] [AllowEmptyString()] [string] $Class,
        [Parameter(Mandatory)] [int64] $Style)

    if ($script:Untouchable -contains $Class) { return $false }

    return ($Style -band $script:MinimizeBox) -ne 0
}

if (-not ('Clr' -as [type])) {
    Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public static class Clr {
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr GetWindowLongPtrW(IntPtr h, int index);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowTextW(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassNameW(IntPtr h, StringBuilder s, int n);

    public static string TextOf(IntPtr h) { var s = new StringBuilder(512); GetWindowTextW(h, s, s.Capacity); return s.ToString(); }
    public static string ClassOf(IntPtr h) { var s = new StringBuilder(256); GetClassNameW(h, s, s.Capacity); return s.ToString(); }

    // GWL_STYLE. Read through the pointer-width call, because the narrow one is not there on 64-bit
    // and answers through a shim that truncates what it was asked for.
    public static long StyleOf(IntPtr h) { return (long)GetWindowLongPtrW(h, -16); }

    // What a person presses to get their desktop back. The foreground is handed on rather than only
    // taken from the window: a minimised window keeps the foreground until something claims it, and
    // that is the half a repair reading only IsIconic would call done.
    public static void ShowTheDesktop() {
        const byte win = 0x5B, d = 0x44;
        const uint up = 0x0002;
        keybd_event(win, 0, 0, UIntPtr.Zero);
        keybd_event(d, 0, 0, UIntPtr.Zero);
        keybd_event(d, 0, up, UIntPtr.Zero);
        keybd_event(win, 0, up, UIntPtr.Zero);
    }
}
"@
}

if ($DefineOnly) { return }

# SW_MINIMIZE, which puts the window down without activating whatever is behind it.
$minimize = 6

$handle = [Clr]::GetForegroundWindow()
if ($handle -eq [IntPtr]::Zero) {
    $said = 'nothing held the foreground, so there was nothing to put away'
}
else {
    $class = [Clr]::ClassOf($handle)
    $title = [Clr]::TextOf($handle)

    if (-not (Test-Clearable -Class $class -Style ([Clr]::StyleOf($handle)))) {
        $said = "left '$title' ($class) alone: it has no minimise button, which is what a question looks like"
    }
    else {
        [void][Clr]::ShowWindow($handle, $minimize)
        [Clr]::ShowTheDesktop()
        Start-Sleep -Milliseconds 800

        $now = [Clr]::GetForegroundWindow()
        $still = if ($now -eq $handle) { ', and it took the foreground back' } else { '' }
        $said = "put '$title' ($class) away, iconic=$([Clr]::IsIconic($handle))$still"
    }
}

# Into a file beside this script, for the reason the probe writes one: vmrun runs the program and
# does not carry what it printed, so an answer written to the console is an answer nobody reads.
Set-Content -LiteralPath (Join-Path $PSScriptRoot 'cleared.txt') -Value $said -Encoding utf8
