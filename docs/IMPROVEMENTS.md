# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW584 Provoke a held desk to prove the shell restart

WW580 made the runner restart the guest's explorer once when the taskbar holds the desk
and nothing may take the foreground, then ask again before refusing. The decision is
held by RunnerWaitsTests and the run's own lines, but the restart has never run: the
gate run that shipped WW580 found the desk takeable after 8 ms.

Measured 2026-10-27, with the user's go-ahead: the guest's `ForegroundLockTimeout` was
raised to 200000 live and in the registry, with `Shell_TrayWnd` in the foreground, both
read back. The next run read `shell`, not `held`, and was granted the foreground in 14
ms; the value was then restored to 0 and read back. The lock cannot provoke this arm:
`desk-take.ps1` sends an input event before it asks (WW472), and Windows gives the
foreground to the process that sent the last input, which is what the timeout exempts.
So whatever refused the desk on 2026-10-26 was not the foreground lock.

Still to try: a guest helper holding `LockSetForegroundWindow(LSFW_LOCK)`, measured
first, since input may lift that lock too. If it holds, it proves only the refusal
("restarted once and still held"), because the lock is the helper's and a shell restart
cannot clear it. The success arm needs explorer itself refusing the desk, and nothing
here provokes that on demand. If no provocation is found, this line is retired with the
measurement above as its reason. Any probe holds the desk claim, because quickshell
shares the guest.

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
