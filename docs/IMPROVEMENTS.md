# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system

## Block N — The name — winwright becomes paneproof

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

Set aside on 02/11/2026, open question first: an adopter's settings name
`winwright@winwright`, and this repository's `marketplace.json` is that marketplace.
What Claude Code does with an installed plugin when its marketplace's manifest changes
its name, and whether one marketplace can offer `paneproof` beside a `winwright` stub
whose SessionStart hook announces the move, has to be tried on a real install before the
tree changes. Pushing the rename without that answer breaks every adopter's plugin at
its next marketplace refresh.

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

### §WW598 The old ids' last release points at Paneproof

NuGet cannot rename a package, so after WW590 the tree packs `Paneproof` and
`Paneproof.InApp` under new ids, and `Winwright` and `Winwright.InApp` on nuget.org stop
receiving versions. An adopter restoring the old ids sees nothing that says they moved.

What it should do: one last release of each old id, cut from the commit before the
rename (the parent of WW590's commit) with only `<Description>` and the README's first
line changed to say the package is now `Paneproof` / `Paneproof.InApp` and where to read
the move. Its version is the next patch after the last one published, so `publish.yml`'s
"nuget.org has not got it" rule is not needed. It goes out after the first `Paneproof`
release, so the description points at something that exists.

Deprecating the old ids on nuget.org is not this line: it waits for the adopters
(WW594). Cutting a release is a person's decision and runs from the publish workflow, so
this line waits for that person.
