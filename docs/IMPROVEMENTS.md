# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

### §WW179 The desk fact that arrives before the act

Measured while shipping WW172. Holding the guest's desk the way the run that produced 32
failures held it took the same tree to 3, and the three that remained are all the same
shape: the desk fact arrived during setup, where there is no act to answer it.

Two of them were `TrayIconFixture.Placed`, which waits for the shell to put an icon
somewhere findable and throws `InvalidOperationException` when it does not. WW168
already made the search say which it was — the icon is absent, or the overflow could not
be looked in — and the fixture drops that distinction on the floor by throwing either
way. The third was `CaptureRouteTests`, whose helper throws "the shell put no menu
window on this desk".

A throw is not a hole. `RunVerdict` ranks a broken harness above a failure precisely
because nothing past the throw was observed and the reader is being sent to this
repository rather than to the one under test — which is the wrong repository when the
shell was merely covering the taskbar.

What is owed is that setup answers what an act answers. A fixture that could not be
built because the desk refused carries the reading that says so, and the case excuses
itself on it exactly as it now does for a foreground it was not granted. The three that
remain are the list; a fourth found later is the same repair.

### §WW182 The shape that was already there

WW181 is the measurement, and it is worth stating plainly: the defect was written,
reviewed, tested and shipped, and what it did was report a clean desk it had never read.
That is this project's founding non-goal, committed inside the reading built to stop it.

The cause is not carelessness about the third state. `TrayGhosts` was written with a
`Sentence` and a list, and a list has no way to say "I did not look" — so the third
state had nowhere to live and the sentence rounded it down to the second. The engine
solved this three tasks earlier: `Finding` carries `bool? Holds`, and its own comment
says why, in the words WW151 used — a run that took no fingerprint and a run that took
one and found it clean are not the same fact, and two states could only ever report them
the same way.

Nothing pointed the new reading at the old shape. `RecordedResultTests` reads the engine
assembly, so the suite's own readings — `Provocation`, `Cooperating`, `Rendered`,
`BusyDesk`, `TrayCensus` — are governed by nothing at all.

What is owed is the reuse, and then the rule: a suite reading that can fail to observe
answers the engine's `Finding` rather than a shape of its own, and a check says so. The
engine's rules stop at its assembly boundary, and the suite is where this project's own
defects have been shipping.

### §WW183 The desk fact the list forgot

`BusyDesk` decides whether a red becomes an excuse, and it decides it from five names
typed into an array. The engine declares twelve conditions and readings; which five are
desk facts is a judgement, and the judgement is kept nowhere but that array.

It has already missed one. `ForeignInput.PreconditionName` — "no input this run did not
synthesise" — is the reading WW157 added so a run can say the desk was not its alone,
and `Preamble` takes it beside the other twelve.
`ForeignInputTests.Input_this_run_synthesised_does_not_read_as_somebody_else` asserts
directly against it and went red twice while WW172 was being measured, once on the host
and once in the guest, both times saying somebody had used the machine and it was not
this run. Both times that was true: a person was driving the machine.

The case is right, the reading is right, and the red is the misattribution this project
exists to end — a run that could not observe the thing it was asked about did not
observe it. The case already guards on whether anything was sent; what it cannot do is
tell its own typing from a second person's, which is precisely why the answer is a hole.

What is owed is that the array stops being the record. A desk fact is what the engine
says is one, read off the assembly the way `Provocation` and `Rendered` read theirs, so
the sixth is not found by a red nobody can reproduce.

### §WW185 The sweep that never says which machine

WW177 joined the reading to the verdict for a single run. `SweepSummary` is the same
surface one level up and did not get it: `EnvironmentRun` is a record of an
environment's name and its `RunVerdict`, and there is nowhere in the type for what that
environment turned out to be.

A sweep is the place this costs most. A single run's reader can at least ask the machine
they are sitting at. A sweep exists because the answer differs between machines, so the
whole point of reading it is to find out which one behaved differently — and what it
reports is that an assertion was unchecked in two of five environments, deduped and
counted, with not one word about the five.

The block's first criterion is that a degraded run is legible without reading the log,
and a sweep is the degraded run a person is least able to reproduce. Its headline
already refuses to collapse a count of holes into a count of occurrences, because those
are two properties and reading one for the other is the failure this project is about.
The environments are a third, and they are collapsed to a name.

What is owed is the same join, per environment: the reading beside the verdict it
explains, and the sweep's own rule about the word *every* applying to it — a sweep that
could not read one machine says so rather than reporting five it read.

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

### §WW180 A count taken before the thing being counted exists

`InstanceCheckTests.A_resident_instance_showing_nothing_is_the_ordinary_case_and_never_stops_a_run`
copies `cmd.exe` twice, launches both windowless through the register, and immediately
asks `InstanceCheck.Of` how many are running. It expects two. Twice in eight guest runs
it saw one.

Measured rather than blamed. The failure appeared while a change to unrelated test files
was in the tree, so the change was stashed and the same guest ran HEAD green, then ran
the change green as well — two greens and two reds across the same code, which is a race
and not a regression. It is filed here rather than left as folklore because the next
person to see it will spend the same three runs.

What races is the reading against the launch. `InstanceCheck.Of` identifies an instance
by the binary the process is running, and a process that has been created but has not
yet mapped its image answers nothing to that question. The register hands back a pid as
soon as there is one, so the case can reach the count before the second process is
countable.

The fix belongs in the reading or in the register, not in the case: a count that is
right only when the machine is fast is the kind of green this project withdraws.
Whichever end takes it, what the reading owes is that a pid it was given and cannot yet
identify is said rather than skipped.

## Block C — Locate — the locator grammar and the tree an agent reads

### §WW184 The sleeps nobody wrote down

Found by reading Block C's criteria when WW175 emptied it. The second says no scenario
carries a sleep — every wait is a deadline on a condition, and how long it took is in
the trace for whoever wants to tune it. Nothing checks it, and `Thread.Sleep` appears in
eight files: `Attempt`, `Expectation`, `FrameRun` and the fixture's `Program` in the
engine tree, and `FixtureTests`, `FrameRunTests`, `TraversalTests` and `Waits` in the
suite's.

Several are certainly right, and their being right is the point. `Attempt` sleeps
between polls, which is the deadline machinery itself. `FrameRun` samples, and WW143
already argued that case in writing: the interval is the resolution of the measurement,
so turning it into a deadline would delete the observation. The fixture sleeps because a
page that is still computing is the shape `--loading` exists to be.

The others nobody has argued either way, which is the whole finding. This is the
criterion's own claim, resting on whoever last looked — the reading WW176 is about, met
a second time in a second block.

The shape is next door and was written yesterday. `Deadlines` pairs every wait in both
trees with what its look answers as nothing, checked against the sources both ways. A
sleep is the same question inverted: pair each with why it is not a wait, and a sleep
added later is red until somebody says.

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

### §WW41 A backdrop transmits what is behind it

Measured in freewilly: with nothing overlapping, the copy still carried a blurred image
of the desktop behind the window - another application's content legible through the
frame - because a Fluent window's backdrop composites what is behind it by design.
Z-order reasoning cannot answer for that: the intruder is not in front of the window, it
is showing through it. The refusal is positive evidence rather than a name, so the
compositor is asked which backdrop the window opted into, and a popup - the one thing
the screen copy exists for - is not refused by it. A printed warning was the first
response and was not enough, because a warning is not a refusal and the file gets
written either way.

### §WW42 One colour is not a window

Measured in freewilly while shipping a task: a copy of the notification area came back
as exactly one distinct colour, with the session present, the shell running and the
environment reporting an interactive desktop. The display was simply not rendering
anything a copy could read. Without this assertion the script would have written that
file and exited zero, and the reader would have had a picture of nothing that claimed to
be a picture of something.

### §WW43 The page is still saying it is loading

Measured in claude-tray: a report on a machine with 213 recent transcript files took
about 25 seconds to build, and at the default wait the copy came back as a heading, a
subtitle and the words computing your consumption pace. Two variants captured that way
are near-identical for the same reason, so comparing them proves nothing, and it was
caught only because somebody looked. A longer wait is the wrong answer twice - it slows
every capture and still passes the page that needed longer still - so the loading
strings are read from the project's own language files and asked of the tree instead. A
key none of those files carries refuses the run, because a check that silently matches
nothing is the shape of defect this whole path exists to stop.

### §WW46 Byte-identical is the cheapest visual assertion

freewilly's window skill states the rule: a change meant to be invisible must produce a
byte-identical file, and the render is deterministic, verified by re-capturing unchanged
code. Three findings about theme handling in that project came from this and from
nothing else, and its test suite saw none of them. It also avoids choosing a tolerance,
which is the argument every other image comparison eventually turns into.

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

### §WW57 A case is data

The interaction harness in claude-tray is 2,732 lines for eight cases, and most of what
is in it is the same loop written eight times. The steps, their locators, their acts and
their expectations are fields; the loop, the waits, the retries, the process register
and the verdicts belong to the engine. This is the whole reason for a framework here
rather than the library it would otherwise have been.

### §WW58 Refused at insertion, not linted afterwards

roadkeep's first law, transferred. A linter reports after the text exists, and by then
the work is done and the author is being asked to delete what they just wrote. A field
validated at the point of insertion refuses before the case is composed, which converts
an analytical act into a procedural one - and the saving is the analysis rather than the
characters.

### §WW59 One case runs alone

The value of a small case is partly that it costs ten seconds when a name is what
changed. Run takes a file, a case or a tag, and says what it did not run - because a
filtered run reporting success without qualification is the same silent pass the third
verdict exists to prevent, one level up.

### §WW60 A fixture reaches every launch a case makes

The states a menu exists to report are the ones where the environment disagrees with the
application, and on a developer's machine it never does - so without a sampled
environment those assertions are only ever unchecked. One declaration decides both what
the app is launched with and what the expectations are read from, so the two cannot be
given different modes and a sampled menu is never compared against a real environment.

### §WW61 A precondition is declared

pportal's interaction tests fail rather than skip when no controller is plugged in, and
say so, because xUnit gives them no third outcome to use. With one, the precondition
belongs in the case: this needs two profiles, this needs a pad, this needs a display
that renders. Its absence is then named and counted rather than argued about, and the
case stays honest on a machine that cannot run it.

### §WW62 One launch, lent to the cases that only read

Three cases in claude-tray drive the same window and each used to own its process, so a
full run paid the launch, the first layout pass and the wait for the first poll three
times over - seconds each, for a window none of them leaves in a state the next would
reject. Sharing is opted into per invocation rather than being a merge of the cases, so
a case run alone still owns its process, which is the property that keeps it worth
running alone.

### §WW63 A case names the defect it exists to catch

Every case in these harnesses carries a task id and a sentence about what went wrong
without it, and that is why they survive: a check nobody can justify is a check nobody
dares delete and nobody dares change. The field is part of the schema, so the
justification is written when the case is, rather than reconstructed a year later out of
a commit message.

## Block H — The Claude Code surface — plugin, tools, skill, hook

### §WW65 The plugin is the installation

Two commands in the adopting repository write both declarations into its settings, and
committing that file wires every clone - no per-machine step, no path entry, no
instruction that differs by operating system. What arrives with it is the hook, the
tools, the commands and the skill. This is roadkeep's adoption story, and it is the
reason that tool gets used rather than admired.

### §WW66 The schema arrives as the tool's input schema

Flag names typed from memory are guesses, and a guess costs a refusal and a retry at
best. A tool whose input schema is this project's scenario schema makes the fields
arrive already named, already typed and already constrained, which is the difference
between being told what is wrong and being unable to express it in the first place.

### §WW67 The hook is what makes the verb the easy path

A hand-written harness script is always available and always faster in the moment, and
that is exactly how 2,732 lines happen. The guard denies the write and names the verb
that replaces it, which is the same shape roadkeep puts in front of a governed file -
and the reason it works is that the refusal arrives before the work rather than after
it.

### §WW69 The skill loads when a window is in play

The whole content of an instruction file is loaded on every turn against a budget, which
is why claude-tray keeps its flag catalogue in a skill and only the rules in the file
the harness reads. The skill says which loop answers which question - a picture proves
layout, an interaction proves input, a render proves determinism - and what it costs a
session is measured rather than assumed to be small.

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

### §WW78 The keyboard case, first

It is the shortest path through the whole framework - launch under a named host,
navigate by clicking a control with no automation peer, resolve by id, type, read back
through a pattern, traverse, and drive a range - and it is the case whose absence let a
window ship with no keyboard input at all. Migrating it first means the engine is
exercised end to end before anything else about it is claimed.

### §WW79 The panes case runs on every machine

This assertion used to live inside the profiles case, which opens by counting profiles
and skipping below two. That is right for a round trip and wrong for the property that
made the round trip readable: a tab body being in the tree has nothing to do with
profiles, and behind that skip it did not run on a single-profile machine - which is
most machines and every hosted runner. Migrating it separately is what keeps the two
apart.

### §WW80 The sessions case is the argument for the whole loop

A popup is its own top-level window, so no render over a page's content can photograph
it and no published screenshot ever will. Whether that note is readable at all is a
question only the accessibility tree can answer. The case also waits out an asynchronous
scan, expands a row into a tree and puts the surface back afterwards, which makes it the
widest single test of locate, act, wait and restore in one place.

### §WW81 The profiles case is the only thing that drives the picker

Every capture renders one profile, which is structurally incapable of seeing three
defects that all need a second switch and two of which need it to come back. It also
carries a timing claim - a line that must never be observed on the way back - and that
claim is void unless the walk reports how many selection changes it took. Migrating it
proves the hop count and the watch-while-waiting shape both survive the move.

### §WW82 The menu case reads the notification area

Nothing else in any of these repositories opens a tray menu, and everything hard about
it is Windows-specific: an icon with no clickable point, an overflow flyout that has to
be opened before the icon is in the tree at all, a right-click the current shell does
not deliver, and a submenu that expands only by keyboard. It is also where the
expectations are derived from the application's own read-out instead of typed by hand.

### §WW83 The switch case rewrites a real setting

Until that case existed, the path that rewrites the setting, re-keys the stores and
takes the other account's token ran under no check at all. It is refused against a
resident process, because a pick there would repoint the real icon for real. Migrating
it inside the store comparison asserts the promise that a run touches nothing at the one
place most likely to break it.

### §WW84 The names case observes what no screenshot can

A picture cannot see an accessible name, and an unnamed control is invisible to every
other check. The case sweeps every panel the page declares - derived from the navigation
labels, so a panel added later is covered with no edit here - and reads every control
the naming rule is responsible for, covering both the branch that must fire and the
branch that must not. Getting the second wrong gives three controls in one row the same
name, which is worse for a screen reader than one unnamed.

### §WW85 The environment sweep, last

It walks one submenu per sampled mode, and it is the case that proves a fixture reaches
every launch a case makes rather than only the first. It is also the case that produced
the deduplication rule, by counting one absent assertion three times and reading as
three holes. Migrating it last means the fixture machinery is already in place and this
is a use of it rather than the reason to build it.

### §WW86 claude-tray loses both scripts

451 lines of capture and 2,732 lines of interaction, and the argument for this framework
is that neither should exist inside a product repository. They go once every assertion
in them is a case, and the line count removed is reported rather than described - a
saving nobody measured is a saving nobody can check.

### §WW87 freewilly loses its copy and its probe

Its capture script is 382 lines sharing most of their reasoning with claude-tray's 451,
and differing in two real ways - the backdrop refusal and the flat-colour refusal - both
of which belong in the engine and neither of which the other project has. The page probe
is the geometry dump this framework already owns, pointed at an installer surface
instead of at a window.

### §WW88 pportal loses the harness and the runner

The interaction file becomes scenarios and the twenty-seven copies of the
single-threaded runner become one package reference, which is the largest single
deletion the whole adoption produces. It is also the hardest, because a thousand other
tests sit around it and the migration must not disturb the parallelism setting the
runner config exists to hold in place.

## Block K — The proving ground — a fixture app built to be hard to test
