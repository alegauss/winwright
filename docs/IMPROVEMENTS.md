# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW584 Provoke a held desk to prove the shell restart

WW580 made the runner restart the guest's explorer once when the taskbar holds the desk
and nothing may take the foreground, then ask again before refusing. The decision is
held by RunnerWaitsTests and the run's own lines, but the restart has never run: the
gate run that shipped WW580 found the desk takeable after 8 ms.

Two provocations were measured on 2026-10-27, with the user's go-ahead, and neither
holds the desk against the runner's asking:

- `ForegroundLockTimeout` raised to 200000, live and in the registry, with `Shell_TrayWnd` in
  the foreground. The next run read `shell` and was granted the foreground in 14 ms. The value
  was restored to 0 and read back.
- A guest helper calling `LockSetForegroundWindow(LSFW_LOCK)` with the taskbar in the
  foreground, and the call succeeded. The next run was granted the foreground in 8 ms, and
  `LSFW_UNLOCK` then answered access denied, with no lock left to lift.

Both fall to the same rule: `desk-take.ps1` sends an input event before it asks (WW472),
and input lifts both the timeout and the lock. A fixture does the same, so neither state
is one a run can meet. Whatever refused the desk on 2026-10-26 was something else, and
nothing known here provokes it on demand.

Unless a held desk is met again, with the process and window that held it, this line has
nothing to measure and is retired with these two readings as its reason.

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
