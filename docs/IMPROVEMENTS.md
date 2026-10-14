# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

### §WW533 What a substitution loses is a claim, not a name

An idea that grew out of WW532, filed apart because it may not survive its design. A
diff of case names, before against after, treats a rename as a change, and most renames
change nothing that is checked. A diff that is mostly noise is one people learn to wave
through, and then it misses the one difference that mattered.

What a substitution actually loses is a claim. A case may carry `catches`, the defect it
exists to catch, and that sentence survives a rename untouched. Diffing the set of
`catches` across runs would stay quiet when a case is renamed and its claim kept, and
speak when a claim no longer has any case making it, which is the subtraction the
commenter's pytest run hid behind an unchanged total.

Two things stand in the way. `catches` is optional on purpose, so a suite whose cases
leave it out has nothing to compare, and this must say so rather than read an empty set
as nothing lost; the run already counts the cases that say nothing, which is where that
sentence would come from. And two cases may name the same defect, so the set is of
claims, and a claim one case dropped while another still makes it is not a loss.

WW532 shipped the name diff as `RanBefore`, a per-selection record at the project's
`lastRun`; the claims fit beside the names in that record.

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
