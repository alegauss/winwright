# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW582 Commands before the launch

Met in modelado, MOD65 (2026-10-26). modelado-cli gained `cloud image-to-3d`, which
sends a photo to Meshy AI and records the spend in
%LOCALAPPDATA%\Modelado\cloud-ledger.jsonl, the ledger the window's About reads. The
task's evidence is that a job sent from the CLI shows in About, so the case wanted to
be:

```json
{ "name": "a window whose LOCALAPPDATA had a Meshy job sent by modelado-cli",
  "variables": { "LOCALAPPDATA": "{files}" },
  "before": [{ "run": "tools/Modelado.Cli/bin/Debug/net10.0/modelado-cli.exe",
               "arguments": ["cloud", "image-to-3d", "cases/fixtures/photo.png", "-o", "{files}/m.glb",
                             "--cloud-api", "meshy=http://127.0.0.1:47631/meshy/"],
               "input": "msy_case_key", "expectExit": 0 }],
  "arguments": ["--cloud-api", "meshy=http://127.0.0.1:47631/meshy/"] }
```

then a step reading `Text#CloudUsage-meshy` for "1 job(s), estimated 30, charged 30". A
fixture can stage files, variables and arguments, but cannot run a program in its staged
folder, with the fixture's variables, before the application launches. Staging a
hand-written ledger instead would prove only that About reads a file, not that the CLI
writes what About reads.

What modelado settled for: a CLI test (tests/Modelado.Cli.Tests/CliJsonTests.cs) and the
Blender add-on test assert the ledger the CLI wrote, through CloudLedger.Drift, the call
About's line is made from. The window itself never opens on it.

winwright should offer: a fixture's `before` list of commands, each run with the
fixture's variables and working directory, its standard input given, its exit code
checked, and its output in the report, so a failing setup reads as a setup failure
rather than a red step.

### §WW584 Provoke a held desk to prove the shell restart

WW580 made the runner restart the guest's explorer once when the taskbar holds the desk
and nothing may take the foreground, and ask the desk again before refusing. The
decision is held by RunnerWaitsTests (the `shell` wait) and the run's own lines, but the
restart itself has never run: the full guest run that gated WW580 found the taskbar
selected and the desk takeable after 8 ms, so the arm did not fire. `Restart-GuestShell`
kills explorer through `vmrun runProgramInGuest -interactive`, starts it again with
`-noWait` (it does not come back on its own in this guest), and waits on
`listProcessesInGuest` for `explorer.exe`.

What is unproven: that the second `Read-GuestTake` after the restart reads `takeable` on
a desk that really was held, and that the three seconds left for the taskbar to draw are
enough.

What to build: a way to provoke a held desk on the guest on purpose, so the arm runs
under a probe. Two candidates, to measure first: a guest helper calling
`LockSetForegroundWindow` (LSFW_LOCK) while the taskbar is focused, or raising
`ForegroundLockTimeout` back to its default and restarting the shell without input,
which is how the desk stuck in October. The probe must hold the desk claim, because
quickshell shares the guest.

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

### §WW583 A render taken mid fade-in

Seen in claude-tray on 2026-10-27, verifying T390's Settings row. `preview.cmd panels`
runs `cases\preview.cases.json` over the in-app render (WW480) and passed green. Every
PNG it wrote is washed out: the page area is drawn at roughly a tenth of its opacity,
and only the sidebar, a slider thumb and the Save button are legible. `general.png` and
`claude-code.png` both show it.

The same page drawn by the app's `--capture-settings <out.png> ClaudeCode` is crisp, so
the tree is fine. The likely cause is the page's fade-in: the render lands while the
content host's `Opacity` animation is near its start, a state WW43 and WW465 already
refuse in other forms.

Workaround: the visual check read the `--capture-settings` PNG, the route WW480 meant to
retire.

What winwright should offer, not exclusive:

1. **Wait out animations before rendering**: until no clock on the subject's tree runs, bounded by
   the case's timeout, and a hole naming the animation if it never settles.
2. **Refuse a faint picture**: when the framed element's effective opacity (the product up the
   tree) is under a floor, answer a hole with that reading, not a pass.

The first makes the case pass correctly; the second keeps any remaining failure honest.

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system
