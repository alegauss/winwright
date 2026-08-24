# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

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

### §WW204 The reading nobody looked at

Found on a guest run while WW200 was being measured. `TrayIconFixture` opens the
overflow to confirm its icon was placed and shuts it again — and threw away what
shutting it answered. A shell that would not shut the flyout left it standing and said
so to nobody, so
`TrayPlacementTests.The_fixture_leaves_the_overflow_the_way_it_found_it` went red about
this fixture over something the shell had decided.

Repaired where it was found: the close is read, and a flyout that would not shut is a
`DeskRefusedException` exactly as one that would not open already is. What is not
repaired is the class.

WW197 established the rule at the other end — a reading whose answer is thrown away is
not a case asking for a verdict, which is why a discarded call is not counted as an ask.
That is right about cases and it is the reverse of the hazard here: a *helper* that
discards a desk reading has not asked either, and the cost lands on whoever asserts
afterwards. Two ends of one shape, and only one has a check.

What is owed is the reading. Every desk-dependent call in this suite whose answer is
discarded, paired with why discarding it loses nothing — the same both-ways catalogue
`DeskAsks` already holds, over the calls it currently passes over. `DeskAsks.Calls` is
the list; what is missing is asking the other question of it.

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

### §WW202 The fifth sweep to find this out by going red

Four sweeps in this suite have now read what somebody wrote about a call as the call
itself, each found by a red and repaired on its own: WW191 in `DeskAsks`, WW197 in
`Flattening` and again on a doc comment, WW198 in `Sleeps` and in
`SerialCollectionTests`. `Checkout.Code` is the answer they share, and nothing requires
the next one to ask for it.

Counted after WW198: eight sweeps match C# source, four go through `Checkout.Code`, and
three do not. `ApartmentTests` matches `SetApartmentState` in raw text and there are
four of those in the tree. `FixtureNeedsTests` matches the calls a fixture must not
make. `Deadlines` counts `Attempt.Until(` occurrences in raw text — which is `Sleeps`
exactly, the sibling written in the same shape, repaired last task and left with the
same defect.

Nothing is miscounted today: no comment happens to name those calls. That is what makes
it worth a task rather than a red — the next catalogue entry explaining itself in prose
is the one that breaks a count, and the reader meets it as an arithmetic failure in a
file nobody edited.

What is owed is the rule holding rather than being remembered, which is WW190's shape
one floor down: a check that reads the sources for a sweep matching source text and does
not go through the one reading. Whether that is a catalogue with stated exceptions — a
sweep over markdown needs no such thing — is the judgement.

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

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

### §WW203 Six milliseconds over, twice

Measured on two guest runs a task apart.
`FixtureTests.The_dump_the_fixture_writes_is_one_the_layout_check_can_read` failed at
5009ms over 159 looks, and `The_fixture_dumps_the_geometry_it_laid_out` at 5006ms over
158, both against the 5000ms `Waits` declares for `wrote`. Each passed on the next run.

Nine milliseconds and six milliseconds past a five-second budget is not a slow machine.
It is a budget sitting exactly where the thing it waits for lands, and the honest
reading of a deadline met on the 158th look is that the 159th would have done.

Nothing here is wrong about the fixture: it draws a window, lays out a tree and writes
what it drew, and doing that in about five seconds under a running suite is what it has
always done. What is wrong is that a red says the fixture never wrote its dump, which is
a claim about the application under test, when what happened is that this suite asked
for one millisecond less than it takes. That is the misattribution Block A's criterion
is about, arriving through a number.

What is owed is the number measured rather than chosen. `Waits` seeds three deadlines
and this is one of them; a run already reports how long each wait actually took, so the
material is there. Whether the answer is a wider budget, a budget read off a run, or a
wait on something earlier than the whole dump is the judgement.
