# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW589 CommittedOnly carries the working tree's bytes

Met in revelei on 30/10/2026, gating the v0.4.0 release at HEAD in a checkout that
carried another session's uncommitted work:

    run-tests-vm.cmd -Tree D:\Git\alegauss\revelei -CommittedOnly -Run run-tests.cmd -ResultsIn TestResults

The runner printed `carrying 289 files, 1.7 MB (HEAD only)`, and the guest build failed
with CS0234: a modified tracked file referenced a type that only an untracked file
declares. HEAD itself does not reference it.

The cause is in `tools/run-tests-vm.ps1`. `-CommittedOnly` narrows the listing to `git
ls-files -c`, but the zip is built from each path's file on disk
(`CreateEntryFromFile`). The listing is wrong too: `ls-files -c` is the index, not HEAD,
so a file only staged is carried, and a tracked file deleted on disk is skipped by the
`Test-Path` check without a word.

What it should do: with `-CommittedOnly`, both the list and the bytes come from HEAD
(`git archive HEAD`, or `git ls-tree -r HEAD` with each blob read through `git show`),
so the label is true. Not a refusal on a dirty tree: carrying HEAD from a checkout
somebody is editing is the case the flag exists for. Files named by `-Carry` still come
from disk and stay labelled as carried.

Tests: a modified tracked file arrives as HEAD has it; a staged new file is absent; a
tracked file deleted on disk arrives; `-Carry` beside `-CommittedOnly` carries both.

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

### §WW595 The install command the docs print

WW554 renamed the marketplace to `winwright`, and the README, `llms.txt` and the site's
landing section moved to `claude plugin install winwright@winwright`. The area page
`site/docs/src/content/docs/installing.mdx` still prints `winwright@alegauss` under
"Wiring Claude Code", which is the marketplace WW554 left, so an adopter who installs
from the docs gets the per-project conflict WW554 was filed for.

The fix is the one word. What keeps it fixed is a site test that reads every `claude
plugin install` command the site and the README publish and requires each to name the
plugin and the marketplace `.claude-plugin/marketplace.json` declares, so the next
rename is caught by the gate rather than by an adopter.

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system

## Block N — The name — winwright becomes paneproof

### §WW590 Paneproof in the code and the package ids

Decided on 02/11/2026, in the visibility repository, before winwright goes to any
channel.

`civyk-official/civyk-winwright` was created on 28/02/2026, six months before this
repository (21/08/2026). It is a "Playwright-style MCP server for Windows desktop,
system, and browser automation", with 21 stars, already used in other people's
repositories. It is the same space, the same audience (agents driving Windows desktop
apps over MCP) and the same pun on Playwright, so a search, a reader or an AI assistant
cannot tell the two apart. Holding the `Winwright` id on NuGet keeps the package, not
the name. The rename will never be cheaper than now: 1 star, no external adopter, no
post published.

The new name is **paneproof**: *pane*, the glass of a window and a pane of an interface,
and *proof*, which is the promise that a green never covers a check that did not run. On
02/11/2026 there was no `Paneproof` package on NuGet, no repository with that name on
GitHub and no product by that name on the web. A trademark search was not done.

This line is the code: assemblies, namespaces, the solution, the executables
(`Winwright.Mcp.exe`, `Winwright.Blame.exe`, ...) and the package ids, which become
`Paneproof` and `Paneproof.InApp`. NuGet cannot rename a package, so the first Paneproof
release is a new id, and the old ids get one last release whose description points to
the new ones. Deprecating the old ids on nuget.org waits for the adopters (Block N, last
line). The roadkeep prefix `WW` stays: the ids are history.

### §WW591 The old file and variable names keep working

An existing suite is written against `winwright.json`, `winwright.read-back.json` and
the variables winwright owns. Renaming them in one release breaks every adopter at once,
and silently where a variable is only read when present.

They come in two directions, and each needs its own rule:

- **What the adopter writes**: the two files, `WINWRIGHT_CONFIGURATION`, and the runner's `WINWRIGHT_VM_ENV`, `WINWRIGHT_VMX`, `WINWRIGHT_VM_PASSWORD`, `WINWRIGHT_GUEST_USER` and `WINWRIGHT_GUEST_PASSWORD`. Read `paneproof.json` and `PANEPROOF_*` first; accept the old name when the new one is absent and say so in the run's output, naming both; when both are present, refuse and name both.
- **What the engine writes for the in-app half**: `WINWRIGHT_SURFACES`, `WINWRIGHT_RENDERS` and `WINWRIGHT_GEOMETRY`, set on the launched app and read by `Winwright.InApp` inside it. The engine and the in-app package are upgraded separately, so an engine that set only the new names would leave an app on the old in-app half without its own render, with nothing said. During the period the engine sets both names, and `Paneproof.InApp` reads the new one, then the old.

A variable the author names in a fixture (`WINWRIGHT_ROLE` in the tests) is the
author's, not the product's, and is left alone. The release notes state how long the old
names are accepted.

### §WW592 The plugin moves to paneproof

The marketplace, the plugin, the MCP server (`winwright` in `.claude-plugin/mcp.json`,
`tools/winwright-mcp.cmd`), the skill (`skills/winwright`) and the guard hook
(`hooks/winwright-guard.cmd`) all carry the old name, and so does the
`winwright-friction` skill that other projects use to file lines here.

An adopter who installed the plugin under the old name and installs the new one would
have two MCP servers registered, driving the same desktop. What it should do: the new
plugin is `paneproof`, and the README and the release notes give the two commands that
remove the old one and install the new; the last `winwright` plugin release says, at
session start, that it was renamed and how to move. The notes also say that the
adopter's permission rules naming `mcp__plugin_winwright_winwright__*` stop matching the
renamed tools without a word, and give the new prefix.

### §WW593 The repository, the site and the README say paneproof

The rename is only real for a reader once the places they land on say paneproof: the
GitHub repository (renamed by Alexandre to `alegauss/paneproof`; GitHub redirects the
old address), the site at alegauss.github.io/paneproof/, the README, the documentation
area, the social preview and the NuGet descriptions.

The old page, alegauss.github.io/winwright/, has to redirect to the new one instead of
returning 404 as /freewilly/ did after the FreeHarbor rename. The README and the site
carry one line saying the product was called winwright, so a search for the old name
still lands somewhere that explains it. The visibility repository renames the product in
`products.json`, keeping its series.

Two traps. nuget.org's trusted publishing policy names the repository, so the first
`publish.yml` run after the rename fails until the policy names `alegauss/paneproof`:
update it in the same step. And the redirect page must not come from a new repository
named `winwright`: creating one ends GitHub's redirect from `alegauss/winwright`, which
is the address every adopter's plugin marketplace points at. Serve it from elsewhere,
such as the user site.

### §WW594 The adopters move, then the old ids are deprecated

claude-tray, freeharbor, quickshell, modelado and pportal reference the Winwright
packages, the plugin, `winwright.json` or the `WINWRIGHT_*` variables (claude-tray 22
files, freeharbor 7, quickshell 22, modelado 15, pportal 33 on 02/11/2026). They are the
adoption proof, so each moves to Paneproof through a line in its own roadmap, filed from
here, and the line closes when each has shipped.

Only then are `Winwright` and `Winwright.InApp` deprecated on nuget.org, pointing to
`Paneproof` and `Paneproof.InApp` (done by Alexandre on the site), and the
`winwright-friction` skill is renamed.
