#Requires -Version 5.1
<#
.SYNOPSIS
  Read whether the guest has what a run declared it needs, before anything is carried. WW618.

.DESCRIPTION
  Found by Modelado (MOD73): a run registering an unsigned MSIX layout called
  `Add-AppxPackage -Register` and met HRESULT 0x80073CFF, because the guest's Developer Mode was
  off. The runner's session is not elevated, so the script could not turn it on either, and the
  refusal came after the carry, from inside the adopter's own script, with nothing saying why.

  The runner never changes the guest: it does not provision a machine, and it has no elevated
  channel to do it with. What it can do is read the setting unelevated and refuse up front, naming
  the setting a person has to change. That reading is this file.

  A script of its own, the way carry.ps1 and holders.ps1 are, so the suite drives the reading the
  runner sends rather than a copy of it. Run in the guest it writes one line per need to -Into:
  `met|<name>|<what it read>` or `unmet|<name>|<the setting to change>`.
#>
[CmdletBinding()]
param(
    # Define the functions and read nothing, so the runner and the suite can drive them.
    [switch] $DefineOnly,

    # The needs to read, as the runner was given them.
    [string[]] $Need,

    # Where the answer goes, one line per need.
    [string] $Into
)

# Every need this knows, with where it is read and what a person changes to meet it. A name not in
# this list is refused as a parameter, on the host, before the VM is touched. Not `$script:Needs`:
# the runner dot-sources this file, and that name would overwrite its own -Needs.
$script:NeedsKnown = @(
    [pscustomobject]@{
        Name    = 'developer-mode'
        Key     = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock'
        Value   = 'AllowDevelopmentWithoutDevLicense'
        Setting = 'turn on Developer Mode in the guest (Settings > System > For developers), which sets AllowDevelopmentWithoutDevLicense = 1 under HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock. Registering an unsigned MSIX layout needs it; a package signed with a certificate the guest trusts does not'
    }
)

<#
.SYNOPSIS
  The names a run may declare, split on commas the way -Bring and -Carry are (WW578), and refused
  by throwing where one is not a need this file knows.
#>
function Get-NeedNames {
    param([string[]] $Need)

    $names = @($Need | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ } | Select-Object -Unique)
    foreach ($one in $names) {
        if (-not ($script:NeedsKnown | Where-Object { $_.Name -eq $one })) {
            throw "-Needs '$one' is not a requirement this runner knows; it knows: $(($script:NeedsKnown | ForEach-Object { $_.Name }) -join ', ')"
        }
    }

    return $names
}

<#
.SYNOPSIS
  Read one need, unelevated, and answer `met|...` or `unmet|...`. -Key moves where it is read, which
  is how the suite points it at a key of its own under HKCU instead of the machine's.
#>
function Test-Need {
    param([Parameter(Mandatory)] [string] $Name, [string] $Key)

    $need = $script:NeedsKnown | Where-Object { $_.Name -eq $Name } | Select-Object -First 1
    if (-not $need) { throw "-Needs '$Name' is not a requirement this runner knows" }

    $at = if ($Key) { $Key } else { $need.Key }
    $read = $null
    try { $read = (Get-ItemProperty -LiteralPath $at -Name $need.Value -ErrorAction Stop).($need.Value) }
    catch { $read = $null }

    if ($null -ne $read -and "$read" -eq '1') {
        return "met|$Name|$($need.Value) is 1"
    }

    $found = if ($null -eq $read) { "$($need.Value) is not set" } else { "$($need.Value) is $read" }
    return "unmet|$Name|$found; $($need.Setting)"
}

if ($DefineOnly) { return }

$answer = foreach ($one in (Get-NeedNames -Need $Need)) { Test-Need -Name $one }
Set-Content -LiteralPath $Into -Encoding utf8 -Value $answer
