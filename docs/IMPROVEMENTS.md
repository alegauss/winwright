# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

### §WW587 The plugin brings its binaries

The README promises the plugin in two commands, then says the MCP server and the guard
need `dotnet build -c Release` in the plugin's clone: "the one step the two commands do
not cover". WW221 made the failure readable, with the launcher naming the build command
on stderr, but the step is still there: the adopter needs the .NET 10 SDK, has to find
the plugin's cache directory and build it, and does it again after each plugin update.
The portfolio review of 30/10/2026 (visibility repo, `produtos/winwright.md`) found this
breaks the promise on the first screen of the README.

The design:

- **The binaries come with the release.** The release workflow publishes the MCP server and the guard as framework-dependent `win-x64` builds, attached as a zip to the GitHub Release that already goes with each NuGet version.
- **The launcher fetches what it does not find.** On the first start, or when its version differs from the plugin's, it downloads that release's zip, checks it against the published SHA-256, unpacks it beside itself and runs it. No SDK is needed; only the .NET 10 runtime.
- **A local build still wins.** A clone with `bin/Release` present runs that, so development does not change.
- **The README** drops the build step from the install, and keeps it under "working on winwright".

Done means a machine with only the .NET runtime runs the two commands and gets every
tool in the first session.

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

### §WW588 Whether to target .NET 8 LTS too

Every project targets `net10.0-windows`, and the SDK is pinned at 10.0.401. A team whose
desktop application is on .NET 8, the current long-term-support release and where most
line-of-business WPF and WinForms code sits, cannot reference `Winwright.InApp` at all,
and has to install a second runtime to run the harness beside its app. The portfolio
review of 30/10/2026 (visibility repo, `produtos/winwright.md`) lists ".NET 10 only,
which excludes whoever is on .NET 8 LTS" among the weak points, and left the question
open for the author: is .NET 10 a firm choice?

This line is an evaluation, not a commitment. It answers, with evidence from the code:

1. **What breaks under `net8.0-windows`.** Build the solution with `<TargetFrameworks>net8.0-windows;net10.0-windows</TargetFrameworks>` and list every API, language feature or package that does not compile or behaves differently, per project.
2. **Which projects matter.** `Winwright.InApp` is loaded inside the adopter's process, so its target is the one that decides adoption; the runner, MCP server and guard run in their own process and can stay on .NET 10 if the multi-target there costs too much.
3. **What it costs to keep.** A second target doubles the CI matrix and keeps every future change honest to the older runtime until .NET 8 support ends (November 2026, with .NET 10 as the next LTS).

The outcome is a short record for the author to decide: multi-target InApp only,
everything, or a non-goal that says why not.

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system
