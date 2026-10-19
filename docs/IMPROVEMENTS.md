# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW549 A launched window comes forward without help

Found by quickshell (QS368), soaking one case on the guest desk: `Suite.Launch` over
"clicking a tab on the strip puts that tab's terminal on screen" alone, whose first step
types. It was Degraded three of three, twice: "the foreground belongs to explorer
'Program Manager', which no case in this run started ... still true 5002ms later". The
same case passes in the full run of 31, after earlier cases sent synthesised input.

So the foreground a launched window gets depends on what ran before it. On an unattended
desk Windows' foreground lock refuses a process started by one that is not the
foreground, and nothing the engine does at launch lifts it; a case first in its run, or
alone in it, cannot type or press.

The workaround is quickshell's, in `tests/Quickshell.Cases/CasesRun.cs` (`Nudge`): a
`SendInput` mouse move of no distance before each launch. With it the same soak went
three of three green.

What winwright should offer: at launch, before waiting for the window under test to come
forward, lift the lock the way the adopter had to (a zero-distance synthesised input, or
`AllowSetForegroundWindow` for the launched pid), and say in the trace that it did. Then
a case's verdict no longer depends on its position in the run, and an adopter deletes
`Nudge`.

Falsified when a single typing case, launched first on a fresh guest login with no input
since, comes forward without any help.

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
