# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW158 A display that renders is not a display that is attached

Display() reads three things and none of them is rendering: GetSystemMetrics for the
monitor count, for the virtual screen width and height, and SM_REMOTESESSION as a suffix
on a failure it has already decided. All three are proxies. The condition is named a
display that renders and answers a different question - is a display attached, and does
it measure something.

WW42 is the measurement that settles it. A copy of the notification area came back as
exactly one distinct colour, with the session present, the shell running and the
environment reporting an interactive desktop. This reading would have called that desk
met. It was caught by the capture, per capture and after the fact, and only because
somebody looked.

Read from the desk instead, the same fact answers once and answers first, which is what
the whole reading is for: the flat rectangle is refused before 999 cases run on the desk
that produces it, rather than once per picture afterwards.

The evidence is composition state and what the desk actually draws, never a named pixel
- the non-goal about individual pixels binds this as it binds Block E, and a colour
count is not a claim about a coordinate. WW42 stays where it is: the capture keeps its
own refusal, because a desk that renders can still be photographed while nothing is on
it.

### §WW472 A taskbar that held the desk for two whole runs

Measured twice while shipping WW470, at 24 and 30 minutes a time. The guest's taskbar
held the foreground before either run started; `desk-probe.ps1` called it `shell`, which
`desk-clear.ps1` deliberately never touches — WW330's rule that a run may not put the
shell away — and the runner printed *the first case to take the foreground clears it*
and went on.

It did not clear. `PumpedDialog.TakeTheDesktop` polls `SetForegroundWindow` and Windows
refused it for both runs, so 136 and then 137 checks were excused where a healthy run
excuses nine, and five cases that cannot excuse a lost desk went red about nothing. Both
exit codes said the tree was broken; the same suite passed 2187 of 2187 once the shell
was restarted.

The sentence beside the failure landed. What is left is refusing before the carry, and
two mechanisms were measured and refuted:

**The focus is not the discriminator.** This section proposed reading it beside the
foreground. `GetGUIThreadInfo` on the guest's idle desktop answers a focus of its own —
`Progman` with a `SysListView32` focused — so refusing on a foreground thread that holds
one would refuse every healthy run.

**A window the runner puts up is not a fixture.** A script carried in and run through
`runProgramInGuest -interactive` was refused the foreground on a desk reading `clear`,
where every fixture takes it. So it predicts nothing about the test host and cannot
stand in for the premise.

What is wanted is a stand-in that behaves as a fixture does.

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

### §WW483 A tray step that clicks the icon

Found migrating claude-tray (WW86). T158 made a left-click on the icon that
application's main entry point, opening its one window. The script clicked the icon's
centre, three bounded attempts because the shell drops synthesised input, then asked the
window for its nav strip's three destinations. No case can say it. A tray step takes
`read` and `open tray menu`, and the second is WW31's route: focus and the application
key.

So the step is `click` on a `tray`, and the pieces mostly exist. WW31 addresses an icon
by its rectangle, because every taskbar button refuses a clickable point.
`PointerReason` already has `NotificationArea`, so the reason a case writes in `with` is
already a word. And `Find` opens the overflow and puts it back.

Two things are open, and both are measurements. Which route reaches a WinForms
`NotifyIcon`'s click on this shell: a pointer click at the rectangle, or the icon
button's own Invoke, which needs no desk if the shell turns it into the message the
application listens for. And where the next step resolves: a resident fixture launches
with no window, so a locator after the click has to be looked for among the windows that
process opened since. Without that, the claim cannot be written even once the click
lands.

The claim itself stays in the next step, as it does for `invoke`: the click is an act,
and what it opened is read.

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

### §WW86 claude-tray loses its interaction harness

2,732 lines when this was filed and 3,004 now. It goes once every assertion in it is a
case, and the count removed is reported rather than described: a saving nobody measured
is a saving nobody can check.

Surveyed on 2026-09-20, about nineteen claims short. Eleven have landed: the linking
plan's five, the profile round trip's three, a sessions row read as text, and the names
of the two Statistics controls T165 found unnamed. Eight are left, of three kinds.

Settings. Both switches announcing their position, the scope sentence on the icon's
entry, and Open Claude Code expanding or staying a command (T146) all depend on
`FollowActiveProfile` or `SyncEnvironmentProfile`, and the guest holds their defaults
where a developer's desk need not. The script read them off `--profiles`. A case wants
the fixture to fix them, the way `--sample-env` fixes the variable: an observing flag in
the application that samples the two settings, so each position gets a fixture rather
than whichever one the desk is in.

Machine. A pin needs follow on and a hand pick, which the switch case already makes, so
a sampled fixture can drive the marker and its undo. A junction between two config dirs,
and a live reading for the tooltip, cannot be fabricated honestly. Those become `needs`
under T161's rule, and the note's other direction, no note where nothing is shared, is
claimed on the bench.

Engine. The left-click that opens the window is WW483.

The capture script left with WW480.

### §WW88 pportal loses the harness and the runner

The interaction file becomes scenarios and the twenty-seven copies of the
single-threaded runner become one package reference, which is the largest single
deletion the whole adoption produces. It is also the hardest, because a thousand other
tests sit around it and the migration must not disturb the parallelism setting the
runner config exists to hold in place.

### §WW384 the repair nothing has watched

WW371 put a second guest-side script beside the probe, and it arrived in the state the
probe was in before WW345: the decision is a function cases can call, and the acting
half is run by nothing but a real guest. `Test-Clearable` is driven with styles somebody
typed; what happens when the script meets a window — the minimise, the foreground handed
on, the sentence it writes — is exercised only by the runner refusing a run.

That is the shape both of this probe's defects had. WW345 made the classification
runnable and a look built wrong still classified perfectly; WW357 closed that. The
clearer has the same room: a `ShowWindow` on the wrong handle, a foreground handed
nowhere, or a sentence that says it worked would each leave the desk as it was — and the
run after would refuse under a line saying the clearing had happened.

Half of it is writable today. `PumpedDialog` is `WS_POPUP` with no minimise button, so a
case can put it up, run the clearer on the desk it is holding, and assert the script
left it alone and said so — which is the arm that must never move a window.

The other half needs a window this suite does not build: one with `WS_MINIMIZEBOX`, put
up, cleared, and read back as gone from the foreground. That is a fixture window and a
line of style bits, and it is the arm that decides whether an unattended run can start
at all.

### §WW480 The preview loop leaves the repository

`Capture-Window.ps1` came out of WW86 because it is not a harness. It asserts nothing
about a verdict: it launches a window, copies the pixels inside its rectangle and writes
a PNG for a person or an agent to look at. `AGENTS.md` and three skills — `preview-ui`,
`dev-flags`, `file-map` — point at it by name and tell the reader to read the picture
and judge it.

Its four guards are assertions, and the engine already has all four. The window belongs
to the process the run launched, which is the capture receipt. No second instance is
open, which is the register. No foreign window overlaps the rectangle, which an
off-screen render makes vacuous rather than answers. And the page is not still showing
its own loading text — `winwright.json` already declares `"loading":
["stats.computing"]` for it.

So the work is not migrating claims. It is giving the engine the loop: a `capture` step
that writes the file the preview flow reads, `captures` declared in the project, and the
four documents rewritten to name that instead of a script. The count removed is reported
the way WW86 reports its own.

The promo pipeline is not in scope. `Capture-Frames.ps1` and `Encode-Clip.ps1` build a
clip for a README, which is nothing this framework claims to replace, and folding them
in would be this line growing a second argument.

## Block K — The proving ground — a fixture app built to be hard to test
