<#
  The half of the suite that can answer before a VM is started. WW417.

  A large part of this suite never touches a desk: the catalogues held both ways, the rules that
  read sources, the arithmetic. They cost milliseconds and they are the cases most likely to be red
  after an edit, because they are the ones nobody was thinking about.

  What that cost was measured at: WW404 added a case that copied an executable into a temp tree,
  started it and deleted the tree - the shape WW201 wrote a rule against - and the rule said so
  seventeen minutes into a guest run, after carrying the tree, building nine projects and running
  two thousand other cases. The fix took a minute. Then another guest run, because the first is not
  evidence of the second. It happened three times in one session.

  The split is already written down and is not invented here. A class that needs the desk carries
  `[Collection(WindowFixture.Serial)]`; a class that does not, does not. Everything outside that
  collection runs on the host, in any order, while somebody is using the machine - which is the
  whole reason the collection exists.

  Derived and never listed. A hand-written list of the classes to run is the drift WW424 is about,
  and it would be wrong in the direction that hides things: a class left off the gate is a class
  the gate silently stops covering.

  Read as a file rather than as a class, and that is a deliberate coarsening. A file holding one
  serial class and one that is not is treated as serial, so the gate runs less than it could and
  never more - a case run here that wanted a desk is a red on the host about the host, which is the
  one failure this must not produce.
#>
[CmdletBinding()]
param(
    # Define the functions and run nothing, so the suite can drive them. The same door
    # `desk-probe.ps1` and `desk-clear.ps1` have, for the reason WW345 gives: a here-string has no
    # caller but its own script, and a decision nothing can run is one nobody can check.
    [switch] $DefineOnly,

    [string] $Suite,

    [string] $Configuration = 'Debug')

function Get-GatedClasses {
    <#
      Every test class whose file does not put it in the serial collection.

      Sealed or not. WW433: the pattern read `^public sealed class` and twenty-four of this suite's
      classes are `public class`, so they sat outside the gate for a keyword that has nothing to do
      with the desk - and nothing reported it, because a class the gate never saw looks exactly like
      one that passed. Measured while shipping WW391: the gate answered 744 cases and those classes
      held 278 more, among them the three most likely to be red after a change to the scenario
      format. The cases missing from the saving were the ones that catch the edit.

      Which of the two repairs this is matters. Sealing the twenty-four would make the gate right by
      changing the suite under a rule nothing states; this makes the gate read what its own prose
      above already says, and `DeskProbeTests` holds the list it produces against the suite's own
      declarations, both ways, so neither can drift again.
    #>
    param([Parameter(Mandatory)] [string] $Suite)

    $found = New-Object System.Collections.ArrayList

    foreach ($file in Get-ChildItem -LiteralPath $Suite -Filter *.cs -Recurse -File) {
        if ($file.FullName -match '\\(bin|obj)\\') { continue }

        $text = Get-Content -LiteralPath $file.FullName -Raw
        if ($text -match '\[Collection\(WindowFixture\.Serial\)\]') { continue }

        foreach ($match in [regex]::Matches($text, '(?m)^public (?:sealed )?class (?<named>\w+)')) {
            $null = $found.Add($match.Groups['named'].Value)
        }
    }

    return ($found | Sort-Object -Unique)
}

function Get-HostFilter {
    <#
      Those classes as one VSTest filter, and the cases inside a serial class that say they need no
      desk either.

      Fully qualified and anchored on the namespace, so a class whose name is a substring of a
      serial one cannot pull that one in - `Sweep` and `SourceSweepTests` are exactly that pair.

      WW431. The collection is the wrong unit for this gate and the right one for itself: it exists
      to stop two classes fighting over one foreground, and a case that reads a file is in it because
      of the cases beside it. `DeskProbeTests` holds the ones that read this runner's own source, and
      twice the guest has reported eleven minutes later what they would have said here. So a case
      marks itself with the `desk=free` trait and the filter takes it too - marked rather than
      derived, and on the cases that need nothing, because a case nobody marked stays in the guest.
    #>
    param([Parameter(Mandatory)] [string] $Suite)

    $named = Get-GatedClasses -Suite $Suite
    if ($named.Count -eq 0) { return '' }

    return ((($named | ForEach-Object { "FullyQualifiedName~Winwright.Tests.$_." }) -join '|') + '|desk=free')
}

function Get-GateSummary {
    <#
      The line dotnet prints when cases have run, whatever they said. WW441: its presence is what
      tells a suite that answered from a host that never got as far as running one.

      Matched on the words rather than on a count, because the runner already matches this line to
      print it - and both localisations are here for the reason the runner has them: this machine
      answers in Portuguese and CI does not, and a reading that knew only one would call a whole
      half of them a host with no toolchain.
    #>
    param([Parameter(Mandatory)] [AllowEmptyString()] [string[]] $Said)

    return ($Said | Where-Object { $_ -match 'Aprovado|Passed!|Failed!|Com falha' } | Select-Object -Last 1)
}

function Invoke-HostGate {
    <#
      Run them, and answer what happened rather than deciding what to do about it.

      A red here is a red in the guest too, and the sentence has to say so: this is not a second
      suite with a verdict of its own, it is the same suite's cheap half asked sooner.

      Three endings and it used to answer two. WW441: `Ok` came from the exit code alone, and the
      runner turned every non-zero into "the desk-free half is red on this host" - which is a
      sentence about the cases, said on a host where none of them ran. Measured on 2026-09-14: an
      update took the SDK `global.json` pins, dotnet exited before building anything, and the only
      way to a guest run that would have passed was to switch the gate off on the run where it
      could have answered. So `Ran` is asked first, and it is the summary line and not the code:
      a build error and an SDK that will not resolve both end here, and neither is about the suite.

      Rendered line by line rather than through Out-String, which is not a nicety: `2>&1` on a
      native command wraps each stderr line in an ErrorRecord, and Out-String prints those with
      their position, category and fully qualified error id. What dotnet actually said was four
      lines inside twenty of PowerShell describing how it said it.
    #>
    param(
        [Parameter(Mandatory)] [string] $Project,
        [Parameter(Mandatory)] [string] $Filter,
        [string] $Configuration = 'Debug')

    # Run inside a scope that does not stop on it, which is the line that makes the third ending
    # reachable at all. `2>&1` on a native command wraps each stderr line in an ErrorRecord, and the
    # runner dot-sources this with `$ErrorActionPreference = 'Stop'` — so on the one host this task
    # is about, the first line dotnet wrote to stderr ended the run inside this function, before
    # anything could ask what happened. The refusal WW419 met came from further out.
    $said = & {
        $ErrorActionPreference = 'Continue'
        & dotnet test $Project --nologo --configuration $Configuration --filter $Filter 2>&1
    }

    $code = $LASTEXITCODE

    $lines = @($said | ForEach-Object { "$_" } | Where-Object { $_ -ne 'System.Management.Automation.RemoteException' })
    $counted = Get-GateSummary -Said $lines

    return [pscustomobject]@{
        Ok = $code -eq 0
        Code = $code
        Output = ($lines -join [Environment]::NewLine)
        Ran = [bool]$counted
        Counted = if ($counted) { $counted.Trim() } else { '' }
    }
}

if ($DefineOnly) { return }

if (-not $Suite) { throw 'host-gate.ps1 needs -Suite, the directory holding the cases.' }

Write-Output (Get-HostFilter -Suite $Suite)
