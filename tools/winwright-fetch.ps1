# WW587. Where the plugin's binaries come from when nobody built them.
#
# The two install commands clone the plugin and stop. WW221 made the missing build readable, but it
# was still a build: the .NET SDK, the plugin's cache directory found by hand, and the same step again
# after every update. The release now carries the MCP server and the guard, published, in one zip with
# its SHA-256 beside it - and this is what puts them on the machine.
#
# Called by tools\winwright-mcp.cmd only. The guard never downloads: it runs on every write under a
# ten-second timeout, and a download there would be the guard standing in front of the write.
#
# Everything it says goes to stderr, because the launcher's stdout is the protocol.
param(
    [Parameter(Mandatory)] [string] $Version,
    [Parameter(Mandatory)] [string] $Into
)

$ErrorActionPreference = 'Stop'

function Say([string] $line) { [Console]::Error.WriteLine("winwright: $line") }

# WINWRIGHT_RELEASES points somewhere else: a mirror, for a network that cannot reach github.com,
# and a folder for the suite. It names the folder that holds the release's assets, so the tag is
# appended to the default and never to the override.
$releases = $env:WINWRIGHT_RELEASES
if (-not $releases) { $releases = "https://github.com/alegauss/winwright/releases/download/v$Version" }

$asset = "winwright-plugin-$Version-win-x64.zip"
$zipUrl = "$($releases.TrimEnd('/'))/$asset"
$sumUrl = "$zipUrl.sha256"

# PowerShell 5.1 offers TLS 1.0 by default and GitHub refuses it.
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

$parent = Split-Path -Parent $Into
New-Item -ItemType Directory -Force -Path $parent | Out-Null

# Unpacked beside the target and renamed into place, so a session that finds the folder finds all of
# it. Two sessions starting together each unpack their own copy, and the second rename loses.
$staging = Join-Path $parent ("$Version." + [Guid]::NewGuid().ToString('N') + '.partial')
$zip = "$staging.zip"

# The answer is kept and given after the cleanup, rather than exited with from inside the try.
$code = 1

try {
    Say "fetching the MCP server and the guard for $Version from $zipUrl"

    # WebClient rather than Invoke-WebRequest: it follows GitHub's redirect to its storage, takes the
    # system proxy, and reads a file: URL as readily as an https: one.
    $client = New-Object Net.WebClient
    $client.DownloadFile($zipUrl, $zip)
    $published = ($client.DownloadString($sumUrl).Trim() -split '\s+')[0]

    $actual = (Get-FileHash -Algorithm SHA256 -Path $zip).Hash
    if ($actual -ne $published) {
        throw "$asset hashes to $actual and the release publishes $published, so it was not unpacked"
    }

    Expand-Archive -Path $zip -DestinationPath $staging

    try {
        Rename-Item -Path $staging -NewName (Split-Path -Leaf $Into)
    }
    catch {
        # Somebody else's rename landed first. Theirs is the same release, so it is the answer too.
        if (-not (Test-Path (Join-Path $Into 'mcp\Winwright.Mcp.dll'))) { throw }
    }

    # An update replaces: the versions beside this one are no plugin's any more. One still open in a
    # session that has not restarted is left for the next fetch to take.
    Get-ChildItem -Path $parent -Directory |
        Where-Object { $_.Name -ne (Split-Path -Leaf $Into) -and $_.Name -notlike '*.partial' } |
        ForEach-Object { Remove-Item -Recurse -Force -Path $_.FullName -ErrorAction SilentlyContinue }

    $code = 0
}
catch {
    Say "could not fetch $zipUrl - $($_.Exception.Message)"
}
finally {
    Remove-Item -Force -Path $zip -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force -Path $staging -ErrorAction SilentlyContinue
}

exit $code
