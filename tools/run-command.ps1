#Requires -Version 5.1
<#
.SYNOPSIS
  Run a guest run's -Run the way a prompt would, and exit with what it exited with. WW576.

.DESCRIPTION
  The guest script used to say `call <Run> > vm-run.log 2>&1`. A -Run chaining commands with & or
  && then had the redirection bound to its last command only, so the build before it logged nothing
  and one run came back with no exit code; and a `for %B` in it was read as a batch line, where %B
  must be %%B, and refused before anything ran. Found by Modelado, whose workaround was to wrap its
  -Run in `cmd /c "..."` by hand.

  The command is read from a file and handed to `cmd /d /s /c` verbatim, through the process's own
  argument string, so nothing between the host and the guest rewrites its quoting and it is parsed
  as a line typed at a prompt. The child writes to this script's own streams, which the guest's run
  script sends to the log, so every command in the chain is in it.
#>
[CmdletBinding()]
param(
    # The file holding the command, exactly as -Run gave it.
    [Parameter(Mandatory)] [string] $CommandFile,

    # The directory to run it in, which is the tree's root in the guest.
    [Parameter(Mandatory)] [string] $In
)

$ErrorActionPreference = 'Stop'

$command = (Get-Content -Raw -LiteralPath $CommandFile).Trim()
$start = New-Object System.Diagnostics.ProcessStartInfo 'cmd.exe', ('/d /s /c "' + $command + '"')
$start.UseShellExecute = $false
$start.WorkingDirectory = $In

$ran = [System.Diagnostics.Process]::Start($start)
$ran.WaitForExit()
exit $ran.ExitCode
