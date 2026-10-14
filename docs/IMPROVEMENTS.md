# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

### §WW532 A run compares its cases against nothing

Raised in a comment on the dev.to article about the 374-to-352 run. The commenter
reproduced it in pytest: four tests, `4 passed`, exit 0; then one test became a skip and
one was renamed, and the run still exited 0 with the line people read unchanged.
Comparing totals catches 374 becoming 352. It cannot catch a suite that keeps its total
and changes its contents, because the check that stopped being made has no test left to
fail.

Half of that is already closed here. `SuiteVerdict.Skipped` names every case a selection
left alone, in the sentence, and a case whose `needs` the desk lacks comes back
unchecked with the run degraded rather than passed. So winwright has no silent skip.
What it has no answer for is the other half: every run reads only itself, so a case
renamed or replaced between two runs at the same total is invisible.

What is missing is a memory. A run would record the names of the cases that actually
ran, and the next run over the same selection would read the difference in both
directions, naming each case that appeared and each that vanished.

Open before this is designed: where the record lives (a project-declared path, as
`captures` is, and never the checkout); which verdict a difference earns, since it is
neither the application failing nor the desk refusing; and how a deliberate rename is
acknowledged once, so the difference does not become a line everybody learns to skip.

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

Whether this replaces the name diff, sits beside it, or is a reading WW532 takes is the
decision to make once WW532 has a design.

## Block B — Attach, launch, and leave nothing behind

### §WW531 A survivor names its case

Found by quickshell (QS298, 2026-10-13). A guest run with all sixteen cases passed went
red on CasesRun's last check, `Assert.Empty(register.StopAll())`, with one line: `pid N
quickshell.exe - outlived its case and would not stop`. The same cases alone passed
straight afterwards, and the pid was gone by the time anybody read it, so nothing said
which case had started the client.

Suite.Launch knows: Opened is handed the case's name and the register's Launch is called
inside it, and the per-case Stop in the finally is where WouldNotStop is found. The
register is not told, so a Survivor carries a pid, an executable and a fate, and a
reader with sixteen cases has sixteen guesses.

What to build: the register takes the name of the case a launch is for (Launch with an
owner, or a LaunchedProcess.For the suite sets), and Survivor and Departure carry it, so
`ToString` reads `pid N quickshell.exe, launched for 'connected tab reads its bars' -
outlived its case and would not stop`. A lent fixture names the first case it was
launched for and says it was lent.

The adopter's workaround until then is to pair register.Launched with verdict.Ran by
order, which holds only while no case is skipped as unreachable after the launch list
was built.

Falsified when a survivor's line does not name the case that launched it.

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

### §WW530 A drop onto another window

Found by quickshell (QS188, 2026-10-13). Its file browser offers a file dragged out of
its server pane as a virtual file, and the claim to prove is that the file lands in an
Explorer folder byte for byte. The gesture starts in the window under test and ends in
another process's window, and `drag`'s `to` is looked for in the same window as the
`locator` (`Drag.Onto`), so the case cannot be written: the drop target is not there to
name.

What it needs is a `to` that can name an element of another top-level window —
Explorer's folder view, the desktop's list — resolved the way a resident fixture's later
step finds a window it opened, from the desktop. The press, the threshold and the
release are unchanged; only where the release lands is looked up elsewhere. The
foreground rule is the same as now: the gesture needs the window under test in front at
the press, and a desk that will not give it is a hole.

What it then claims is still only the gesture. Whether the file arrived is the next
step's to read, which for a drop onto Explorer is a file on disk rather than an element,
and that read may be the case's own fixture check rather than winwright's.

Falsified when a case cannot drag an element of the window under test onto an Explorer
folder view and have the drop land there.

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing
