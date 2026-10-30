# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW585 Configuration in before commands

Met in modelado, MOD67 (2026-10-30), the first adopter of WW582. The case reads in the
window's About a cloud job that modelado-cli sent before the launch, so its fixture runs
the CLI the same build produced:

```json
{ "name": "a window after a Meshy job sent from modelado-cli",
  "variables": { "LOCALAPPDATA": "{files}" },
  "before": [
    { "run": "tools/Modelado.Cli/bin/{configuration}/net10.0/modelado-cli.exe",
      "arguments": ["cloud", "image-to-3d", "{root}/cases/fixtures/photo.png", "-o", "{files}/meshy.glb"] } ] }
```

The project's `executable` fills `{configuration}` (WW521: the caller's configuration,
then WINWRIGHT_CONFIGURATION, then winwright.json's `configuration`), but a fixture's
`before.run` is resolved with the fixture tokens ({files}, {root}, {root/}) and the
environment only (FixtureDeclaration.Preparing), so `{configuration}` stays a literal
brace and the command cannot start. modelado pins `bin/Debug/`, with WW585's id beside
it in cases/window.cases.json: a Release run of the cases would launch the Release
window and the Debug CLI, or a CLI left over from another build, which is the mismatch
WW521 removed for the executable.

The seam is FixtureDeclaration.Rooted, the one door WW542 made for every token: it fills
a fixture's `run`, arguments, variables, `application`, `workingDirectory` and `opens`,
and a step's `with` and file paths. `{configuration}` goes there and into `Tokens`,
filled from ProjectDeclaration.Configuration (WW521's three sources), so every field
takes it at once. Tests: a `with` naming it loads, where today it is refused as an
unknown token; one left unfilled refuses at the run, naming the field and
WINWRIGHT_CONFIGURATION; it matches case-insensitively, as the executable's does.

### §WW586 Processes the application starts

Met in modelado, MOD46 (2026-10-30). The Export card is to gain "Open in Blender", which
starts Blender importing the model shown. Its case wanted to be:

```json
{ "locator": "Button#OpenInBlender", "act": "invoke", "named": "Open in Blender is pressed" },
{ "locator": "Window[nameStarts=\"Blender\"]", "act": "read", "reads": "name", "matches": "cube", "named": "Blender opens on the model" }
```

and then the program the application started has to go when the case ends. winwright
stops only what it launched itself (ProcessRegister.Ending): it asks the window to close
and kills the tree only when that fails. An application that closes cleanly leaves its
children running, and even the kill misses a grandchild whose parent already exited. The
Blender outlives the case, holds the desk for every case after it, and is never named
among the survivors. Nor can a case read what was started.

The fix sits in ProcessRegister.Launch, so it covers every launch, a fixture's before
commands (WW582) included: a Job object per launch, stopped with it, a member that will
not stop named as a survivor. Not descendants by parent id, which lose orphans and risk
a reused pid. Tests: a child started before the assignment; an adopted process never
joins a job; a lent fixture's children stop at the run's end; an instance older than the
case is never stopped. A case reads a child's executable and arguments through
NtQueryInformationProcess, never WMI, a package the engine may not take. That read says
what started; keeping a GUI from coming up is the fixture's, pointing the application at
a stub.

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
