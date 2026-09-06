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
    #>
    param([Parameter(Mandatory)] [string] $Suite)

    $found = New-Object System.Collections.ArrayList

    foreach ($file in Get-ChildItem -LiteralPath $Suite -Filter *.cs -Recurse -File) {
        if ($file.FullName -match '\\(bin|obj)\\') { continue }

        $text = Get-Content -LiteralPath $file.FullName -Raw
        if ($text -match '\[Collection\(WindowFixture\.Serial\)\]') { continue }

        foreach ($match in [regex]::Matches($text, '(?m)^public sealed class (?<named>\w+)')) {
            $null = $found.Add($match.Groups['named'].Value)
        }
    }

    return ($found | Sort-Object -Unique)
}

function Get-HostFilter {
    <#
      Those classes as one VSTest filter.

      Fully qualified and anchored on the namespace, so a class whose name is a substring of a
      serial one cannot pull that one in - `Sweep` and `SourceSweepTests` are exactly that pair.
    #>
    param([Parameter(Mandatory)] [string] $Suite)

    $named = Get-GatedClasses -Suite $Suite
    if ($named.Count -eq 0) { return '' }

    return (($named | ForEach-Object { "FullyQualifiedName~Winwright.Tests.$_." }) -join '|')
}

function Invoke-HostGate {
    <#
      Run them, and answer what happened rather than deciding what to do about it.

      A red here is a red in the guest too, and the sentence has to say so: this is not a second
      suite with a verdict of its own, it is the same suite's cheap half asked sooner.
    #>
    param(
        [Parameter(Mandatory)] [string] $Project,
        [Parameter(Mandatory)] [string] $Filter,
        [string] $Configuration = 'Debug')

    $said = & dotnet test $Project --nologo --configuration $Configuration --filter $Filter 2>&1
    $code = $LASTEXITCODE

    return [pscustomobject]@{
        Ok = $code -eq 0
        Code = $code
        Output = ($said | Out-String)
    }
}

if ($DefineOnly) { return }

if (-not $Suite) { throw 'host-gate.ps1 needs -Suite, the directory holding the cases.' }

Write-Output (Get-HostFilter -Suite $Suite)
