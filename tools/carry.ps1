#Requires -Version 5.1
<#
.SYNOPSIS
  Name the files a guest run carries beside its tree's git listing. WW574.

.DESCRIPTION
  Found by Modelado (MOD28): its evidence is its own installer, written under artifacts\, which
  .gitignore keeps out of the tree as build outputs should be. The runner carries the tree's git
  listing, so the installer never reached the guest, and the project staged a throwaway folder with
  `git init` in it to get it there.

  A script of its own, the way holders.ps1 is, so the suite can drive the one the runner uses rather
  than a copy of it.
#>
[CmdletBinding()]
param(
    # Define the function and resolve nothing, so the runner and the suite can drive it.
    [switch] $DefineOnly
)

<#
.SYNOPSIS
  The files each pattern names, as paths relative to the tree. A pattern is a path or a wildcard
  relative to the tree, ignored by git or not. Several may arrive as one string joined by commas,
  which is how `powershell -File` binds a list (WW578), so each is split here.

  Refused, by throwing, where a pattern matches no file or names one outside the tree: a carry that
  quietly carried nothing is a guest run that fails on a missing file nobody named.
#>
function Get-Carried {
    param(
        [Parameter(Mandatory)] [string] $Tree,
        [string[]] $Pattern
    )

    $root = (Resolve-Path -LiteralPath $Tree).Path.TrimEnd('\')
    $carried = New-Object System.Collections.Generic.List[string]

    foreach ($one in @($Pattern | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })) {
        $matched = @(Get-ChildItem -Path (Join-Path $root $one) -File -ErrorAction SilentlyContinue)
        if ($matched.Count -eq 0) {
            throw "-Carry '$one' matched no file under $root"
        }

        foreach ($file in $matched) {
            if (-not $file.FullName.StartsWith("$root\", [StringComparison]::OrdinalIgnoreCase)) {
                throw "-Carry '$one' names $($file.FullName), which is outside $root; a carried file lands at its path under the tree"
            }

            $relative = $file.FullName.Substring($root.Length + 1)
            if (-not $carried.Contains($relative)) { $carried.Add($relative) }
        }
    }

    return , $carried.ToArray()
}

if ($DefineOnly) { return }
