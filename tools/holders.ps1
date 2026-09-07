<#
  Who in this guest has the tree open. WW404, lifted out of the sync for WW415.

  Windows refuses a delete with the name of the directory and never with the name of whoever has it
  open, and the one machine that can answer that is the guest itself. Two readings, because a suite
  there is two kinds of process: the test host runs from under the tree, and the runtime hosting it
  does not but has the tree's assemblies mapped in.

  A file of its own rather than a here-string, which is the shape `desk-probe.ps1` and
  `desk-clear.ps1` already have and the reason they have it. Two callers want this now - the sync,
  which cannot delete the tree, and the bound, which is about to leave whatever it gave up on
  exactly where it is - and the second is where the answer is worth most: at the bound the process
  is still doing whatever wedged it, and its name is the question. By the next sync it is only in
  the way.

  Sent as it sits on disk, so the walk a case here exercises and the walk the guest runs are one
  file.
#>
[CmdletBinding()]
param(
    # Define the function and walk nothing, so the suite can drive it.
    [switch] $DefineOnly,

    # The tree to ask about.
    [string] $Tree,

    # Where to write what was found, one per line. A file rather than stdout, because this is
    # fetched by exact path out of a guest and vmrun cannot read a pipe.
    [string] $Into)

function Get-WhatHolds {
    <#
      Every process running from under that tree, or with a file from under it mapped in.
    #>
    param([Parameter(Mandatory)] [string] $Tree)

    $held = @()
    foreach ($one in Get-Process) {
        $where = $null
        try { $where = $one.Path } catch { }
        if ($where -and $where.StartsWith($Tree, 'OrdinalIgnoreCase')) {
            $held += "$($one.ProcessName) ($($one.Id)) runs from $where"
            continue
        }

        $mapped = @()
        try { $mapped = @($one.Modules | Where-Object { $_.FileName -and $_.FileName.StartsWith($Tree, 'OrdinalIgnoreCase') }) } catch { }
        if ($mapped.Count -gt 0) { $held += "$($one.ProcessName) ($($one.Id)) has $($mapped[0].FileName) loaded" }
    }

    return $held
}

if ($DefineOnly) { return }

if (-not $Tree) { throw 'holders.ps1 needs -Tree, the directory to ask about.' }

$found = @(Get-WhatHolds -Tree $Tree)

# An empty answer is an answer, and a different one: it says the walk ran and found nothing running
# from under the tree, which sends a reader somewhere other than Task Manager.
$said = if ($found.Count -eq 0) { @('nothing') } else { $found }

if ($Into) { $said | Set-Content -LiteralPath $Into -Encoding ascii }
else { $said | ForEach-Object { Write-Output $_ } }
