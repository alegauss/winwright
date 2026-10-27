# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

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

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system
