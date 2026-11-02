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

<#
.SYNOPSIS
  Write the archive a guest run carries and answer how many files are in it. WW589.

.DESCRIPTION
  The tree as git lists it, plus the files -Carry named, read from disk. With -CommittedOnly the
  listing and the bytes are both HEAD's, through `git archive`: the runner used to narrow the list
  to `git ls-files -c`, which is the index, and read each file off the disk, so a checkout another
  session was editing reached the guest as a tree that existed nowhere, labelled HEAD (revelei's
  v0.4.0 gate, 30/10/2026). A carried file still comes from disk, and replaces HEAD's copy of the
  same path where there is one.
#>
function New-CarriedArchive {
    param(
        [Parameter(Mandatory)] [string] $Tree,
        [Parameter(Mandatory)] [string] $Zip,
        [switch] $CommittedOnly,
        [string[]] $Carried = @()
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem | Out-Null

    if ($CommittedOnly) {
        & git -C $Tree archive --format=zip -o $Zip HEAD
        if ($LASTEXITCODE -ne 0) { throw "git would not archive HEAD of $Tree" }
        $archive = [IO.Compression.ZipFile]::Open($Zip, 'Update')
    }
    else {
        # -c and -o together, minus what .gitignore covers: tracked files plus the untracked ones that
        # are really part of the tree. A sync built from `git diff` alone carries neither an untracked
        # test file nor a new fixture, and a suite that never saw them is green about nothing.
        $listed = @(& git -C $Tree ls-files -c -o --exclude-standard)
        if ($LASTEXITCODE -ne 0) { throw "git would not list $Tree" }
        $archive = [IO.Compression.ZipFile]::Open($Zip, 'Create')
    }

    try {
        $paths = @(if ($CommittedOnly) { $Carried } else { @($listed) + @($Carried | Where-Object { $listed -notcontains $_ }) })
        foreach ($relative in $paths) {
            $full = Join-Path $Tree $relative
            if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { continue }

            $name = $relative -replace '\\', '/'

            # Only HEAD's archive can already hold the path; one opened to create can neither be
            # asked nor needs to be, its list having been made without duplicates above.
            if ($CommittedOnly) {
                $existing = $archive.GetEntry($name)
                if ($existing) { $existing.Delete() }
            }
            $null = [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $full, $name)
        }
    }
    finally { $archive.Dispose() }

    # Read back once written, because an archive opened to create cannot list itself. Files only:
    # `git archive` writes an entry for each directory as well.
    $written = [IO.Compression.ZipFile]::OpenRead($Zip)
    try { return @($written.Entries | Where-Object { -not $_.FullName.EndsWith('/') }).Count }
    finally { $written.Dispose() }
}

if ($DefineOnly) { return }
