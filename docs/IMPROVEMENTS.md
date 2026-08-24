# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

### §WW197 One BusyDesk covers the whole case

WW190 reads the suite for cases that ask a desk-dependent reading and pairs the ones
that do not excuse the desk. A case counts as excusing where its body mentions
`BusyDesk` anywhere, which is one line of scanner and is the wrong unit by exactly the
margin WW190 was about.

Found rather than argued, on WW191's first guest run.
`TrayPlacementTests.The_fixture_leaves_the_overflow_the_way_it_found_it` builds its icon
through `BusyDesk.Built` and returns where the desk refused — properly, and that is what
makes it invisible here. Its actual assertion is
`Assert.Null(NotificationArea.Overflow())`, which says the shell is not showing the
flyout, and nothing excuses that at all. It went red on a desk whose flyout somebody
else had left standing, then passed on a re-run. The case's own comment already names
this as one of the two flakes it was written for.

So the guard has the shape of WW182's: it keys on a mention rather than on the
assertion, and a case that mentions `BusyDesk` for one reading is credited for a second
the desk can refuse just as easily. Both the cases that fixture-guard and then assert on
the shell are exactly this pattern, and there is no reason to think they are the only
two.

What is owed is the finer unit. Whether that is an assertion counted against a preceding
excuse, or something a case declares once and the reading checks, is the judgement — and
the reason this is a task rather than a line of scanner.

### §WW201 Deleting a binary that is still running

Measured on WW196's first guest run. `CaptureReceiptTests` failed with
`UnauthorizedAccessException: Access to the path 'other.exe' is denied`, thrown out of
`Directory.Delete(root, recursive: true)` in its own `Dispose`. The re-run passed, which
is what makes it worth filing rather than fixing in passing.

What it does: `Elsewhere()` copies `cmd.exe` into a temp directory as `other.exe` and
starts it, so a receipt can be composed against a target in another process. Nothing
waits for that process to end, and Windows will not delete a running image. The case's
assertion had already passed.

So the red is about the teardown and says nothing about the code under test, which is
this block's own criterion pointed at the suite. Worse than a plain failure: a throw out
of `Dispose` reads as a broken harness, which `RunVerdict` ranks above a failure
precisely because nothing past it was observed — and it sends the reader to this
repository over a file handle.

`Attachable` already has the shape: WW126 learned that waiting for windows is the wrong
moment and waits for the process to leave the machine instead. What is owed is that door
here, and a look at whether other cases that copy a binary and run it have the same
teardown. The count is unknown, which is why it is a task rather than a line.

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

## Block C — Locate — the locator grammar and the tree an agent reads

### §WW198 The spelling it warned about

`Sleeps` pairs every file that calls `Thread.Sleep(` with why its sleeping is not a
scenario waiting, both directions, and it argues for being a catalogue rather than a ban
in these words: a rule that admitted no exceptions "would be answered by somebody
spelling the sleep differently, and then nothing would know about it at all".

It matches one spelling. `FrameRun.cs` parks a thread twice — `Thread.Sleep` for the
bulk of the interval and `Thread.SpinWait(200)` for the last sixteen milliseconds — and
the catalogue records that file as sleeping once. Its own entry mentions the spin in
prose; the count does not, and
`SleepTests.The_count_in_the_catalogue_is_the_count_in_the_file` reads one and agrees.

Nothing here is wrong about `FrameRun`: WW143 argued the pacing and the spin in writing,
and both are right. What is wrong is that a second way was reached for and the reading
did not notice — the failure the catalogue was written against, arriving from inside
rather than from a future author.

What is owed is the other spellings, counted the same way. `Thread.SpinWait` is the one
measured; `Task.Delay`, `SpinWait.SpinUntil` and a blocking `WaitOne` are what a reader
reaches for next and none is in the tree today, which makes now the cheap time. The
judgement about which parking is machinery stays as it is — only how many the reading
can see changes.

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

### §WW199 A fixture, or something

Block E's third criterion reads "Every capture refusal has a fixture that provokes it",
and it is paired with
`ProvocationTests.A_flag_named_here_is_one_the_fixture_actually_has` — which asserts
that flags named in the catalogue exist in the built article. That is a real check and
it is not this claim: it says the flags that are named are good, never that every
refusal names one.

Counted after WW195. `WrongCaptureException` has six arms; two are provoked by a fixture
shape (`--intrude`, `--backdrop`) and four by a case that composes the reading. Each of
the four carries a written reason, which is the `Without.NoShape` mechanism
`Provocation` has had since WW160 — so the catalogue was always honest and the criterion
above it was always wider than the catalogue could be.

Block K words the same idea differently: "every refusal has something that provokes it".
That is the claim this repository actually keeps, and WW188 quoted it while building the
arm pairing. Two blocks carrying two wordings for one property is how a reader ends up
believing the stronger one.

The judgement is which way to close it, and that is the task. Amending the criterion to
what is checked is one line and may be right — a picture of one flat colour is a display
rendering nothing, which no fixture can be. Building shapes for the arms that could have
one is the other, and `--intrude` is the precedent for doing that rather than arguing.

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

### §WW200 The fifth type, on the other side of a boundary

WW196 named five refusals thrown from many places and armed four of them: the locator
grammar, the label reader, the declaration and the actionability check.
`UnknownFlagException` is the fifth and it is untouched, which is worth saying plainly
rather than leaving to a reader counting entries.

It is untouched because it lives in the fixture, and `Provocation` says why the suite
cannot reach it: the fixture is referenced without its assembly on purpose, because an
application under test is launched from its own output rather than read from beside the
harness. So the arm shape WW196 built — an enum a type exposes, swept by reflection —
has nothing to reflect over.

Counted: eleven throw sites across `Flags` and `Intruder`. Not a flag at all, a flag
this fixture does not have, a flag given no value, one given a value it takes none for,
one given a value outside what it accepts, a number that is not a number, a flag that
needs a companion, two renders asked for at once, and two more the intruder adds. A
person meets each of those with a different thing to fix.

The judgement is how a boundary that exists for a good reason gets crossed for a
reading. The fixture already prints its catalogue, and WW146's three shapes were
provoked by running it and matching what it said — so the arms may be text this suite
matches rather than an enum it reflects over. That choice is the task.
