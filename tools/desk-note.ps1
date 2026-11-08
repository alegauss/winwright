#Requires -Version 5.1
<#
.SYNOPSIS
  The note a red guest run prints when it started on a desk that was not clear. WW600.

.DESCRIPTION
  WW472 added a note under a red run that started on a desk read as 'shell' or 'stale', and it said
  the red "reads as a broken tree and is not". Met twice in wintimeline on 2026-11-04, and neither
  red was the desk: one was the app's automation peer keeping a stale selected flag, the other a case
  selecting a tab before its document had opened. Every other case passed both times, so the
  fixtures had the foreground. The note sent the session to restart the guest shell instead of
  reading its own failure.

  The runner cannot tell a held desk from a broken tree. What can tell is the roll call's own count
  of excused checks, and only winwright's suite writes one: an adopter's run carries no excused.txt.
  So the note says what the desk read, quotes that count where the guest's log holds it, and
  otherwise says the red is the suite's to read first.

  A script of its own, the way carry.ps1 is, so the suite can drive the one the runner uses.
#>
[CmdletBinding()]
param(
    # Define the function and do nothing else, so the runner and the suite can drive it.
    [switch] $DefineOnly
)

<#
.SYNOPSIS
  The lines to print under a failed run, or nothing where the desk started clear.

  -Desk is what the desk read when the run started. -Log is the guest's console as text, empty
  where none came back.
#>
function Get-DeskNote {
    param(
        [string] $Desk,
        [AllowEmptyString()] [string] $Log
    )

    if ($Desk -notin @('shell', 'stale')) { return @() }

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add(
        "  desk        this run started on a desk that read '$Desk' rather than clear, so the " +
        'foreground may not have been its own.')

    # The roll call's clause, cut where its ASCII ends: what follows it is a dash and the list of
    # facts, which an older log may hold double-encoded and which the count does not need.
    $excused = if ($Log) {
        [regex]::Match($Log, '\d+ check\(s\) were excused[ -~]*?(?=\s*(?:[^ -~]|$))', 'Multiline')
    }

    if ($excused -and $excused.Success) {
        $lines.Add("              the roll call says: $($excused.Value.Trim())")
        $lines.Add(
            '              a count several times the runs before it is the desk; one like theirs ' +
            'is not, and the red is the suite''s to read.')
    }
    else {
        $lines.Add(
            '              the log holds no count of excused checks, so this runner cannot tell a ' +
            'held desk from a broken tree. Read the red first; restart the guest shell only if it ' +
            'names the foreground.')
    }

    return $lines.ToArray()
}
