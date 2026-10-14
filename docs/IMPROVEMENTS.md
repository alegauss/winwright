# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW534 One runner per guest desk

Found by quickshell (QS302, 2026-10-14). quickshell's guest suite and winwright's own
(C:\winwright-sync\run.cmd) ran in the same VM at once, started by two sessions. Three
quickshell UI cases went Degraded, each waiting for a foreground held by
"Winwright.Fixture", and winwright's run had quickshell's clients on its desk too.

The verdict read it honestly as a foreground it could not get, but nothing said another
runner was on the desk, so the reader goes looking for a defect in the cases.

What to build, shared by every adopter that drives a guest: a claim on the desk for the
length of a run, which a second runner waits on and names (a host-side named mutex keyed
on the .vmx path is enough, since one host drives the VM). And where a foreground is
lost to a window of a process the run did not start, the Unchecked line says that
process belongs to no case here.

Falsified when two runners' suites interleave on one guest desk without either saying
so.

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
