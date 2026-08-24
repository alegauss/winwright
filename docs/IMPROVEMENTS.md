# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

### §WW172 The suite breaks the promise the engine keeps

Measured, not supposed. A guest run came back with 32 failures across nine classes —
menus, the notification area, traversal, pick, focus, the capture route. A screenshot of
the guest's desk showed the Start menu wide open, holding the foreground and covering
the notification area. Dismissing it took the same tree from 32 failures to 4, and a
shell restart took it to zero. Nothing in the run said any of that. Every one of the 32
read as the application being wrong.

This is the defect this project was started over, in the suite that proves the project.
The engine already knows the answer: a foreground Windows would not grant is a hole and
not a failure, the run's own reading takes the foreground before any assertion, and
`Precondition.Absent` is the shape a reading that could not be taken comes back as. The
cases here reach past all of it and assert directly.

So what is owed is not a new capability. It is the existing one, applied to the suite's
own desk-dependent cases: a case whose act needed the foreground and did not get it
resolves unchecked, names what held the desk instead, and exits 2 rather than 1.

The block's first criterion says a degraded run is legible without reading the log.
Thirty-two reds and a screenshot is the opposite of that, and it was this repository's
own suite that produced them.

### §WW176 The claim that decides a block, held up by memory

WW169 is the measurement. It was filed with a full design section arguing that the
fixture's justification field was written by the compiler, printed by `--flags`, and
read back by nobody — "nothing asserts that a single Because is there, or that it says
anything". WW106 had shipped both read-backs, and its own ledger line says so. Running
the two cases before building anything was what settled it. The design was written from
memory, and memory was wrong.

That is not a one-off. This project decides a block is finished by reading its criteria,
roadkeep asserts the list exists and not that anything satisfies it, and the reading is
a judgement with nothing underneath it. Thirty-two criteria across eleven blocks
currently rest on whoever last read them.

The repository already answers this shape three times. `Provocation` pairs every refusal
with what provokes it and a case; `Cooperating` pairs every verb with what it needs;
`Rendered` pairs every rendering with the case that asserts its text, and caught an
unpaired one on the very next task after it shipped. Each is checked against the
assembly in both directions, so the count is arithmetic.

What is owed is the fourth: a criterion, its address, and the case or catalogue that
demonstrates it — with an honest bucket for the ones nothing demonstrates yet, counted
rather than left off. Then "the block is finished" is a reading and not a recollection.

### §WW177 Two pages that never meet

`VerdictSummary.Render` takes a `RunVerdict` and nothing else. `Preamble.Render` answers
its own page. Neither file mentions the other, and no third thing joins them.

So a run now measures thirteen things about the machine before it starts and, since
WW170, closes the store fingerprint when it ends — and none of it appears in the page a
person or a CI job is handed. The reader gets the outcome, the exit code, the tally and
a line per assertion that failed or never ran. What they do not get is the desk it ran
on, which binary it drove, whether that binary was stale, the resolved language, whether
anything else was showing the application, whether the desk was this run's alone, or
what the run left changed.

This block's first criterion is that a degraded run is legible without reading the log.
A `2` names the assertions that never ran, which is the half WW6 was filed over, and the
reason each one could not run is a precondition that was measured on the other page. A
reader holding an exit code and a list of holes still has to go and find the reading,
and nothing in the output tells them there is one.

What is owed is the join, and the order it goes in: the reading first, because it is
what makes the verdict underneath it legible, and the summary refusing to print a
preamble belonging to a different run.

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

### §WW173 What a dead run leaves in the shell

Three guest runs of the same tree, in order. The first died with the Start menu holding
the foreground: 32 failures. The menu was dismissed and nothing else touched; the second
run came back with 4, every one of them in `TrayPlacementTests`, every one of them
`TrayIconFixture.Placed` waiting 5000ms and reporting that the shell took the icon and
never put it anywhere a reading could find. Explorer was restarted in the guest and
nothing else changed; the third run passed 1087 of 1087.

No process outlived any of the runs — the guest's process list was checked between the
second and the third and held nothing of this suite's. So what survived was state inside
the shell, and the run that created it is the run that died.

This block's criterion is that a run leaves the machine as it found it, and the
notification area is machine. What is owed is the reading: a run that added a tray icon
says whether the shell still holds one when it finishes, the same way it says what
processes it had to stop. That is a finding and not an assertion — nothing failed, the
shell simply kept something — but a finding is what the next run's four reds needed and
did not have.

Where the icon cannot be withdrawn, saying so is the whole of the repair.

## Block C — Locate — the locator grammar and the tree an agent reads

### §WW175 A deadline that quietly stopped being one

`Attempt.Until<T>(Func<T?> look, int deadlineMs, int pollMs)` polls until the look
answers something other than null. Where `T` is a reference type the caller has made
non-nullable, the first look always answers something, and the deadline collapses to one
look. Nothing throws, nothing warns, and the `Sighting` that comes back says it was
found — because it was.

Measured while shipping WW168, and nearly shipped. `NotificationArea.Find` changed from
`TrayIcon?` to a reading, and `TrayIconFixture.Placed` waits on it for five seconds to
prove the shell placed the icon. That wait became one look, and the fixture would have
gone on passing: the icon is usually there by then. What it would have stopped doing is
what it exists for, which is failing when the shell is slow — the exact race WW159 was
filed over.

It was caught by reading the call site rather than by any check. That is the part worth
fixing: a deadline nobody can see is not being waited is a deadline that decays without
a red.

What is owed is the refusal. A look whose static type cannot be null is a caller asking
a poll to do nothing, and the right answer is to say so at the call rather than to
return a `Sighting` that is true and useless. Where the wait is genuinely wanted, the
condition belongs in `UntilTrue`.

## Block D — Act — patterns before pointers

### §WW174 The same collapse, one call up

WW168 gave the search for a tray icon a reading that says how far the looking got, so an
absent icon fails and a flyout that would not open is a hole. `OpenMenu` is the caller
directly above it, and it takes only half of that.

Two things it does with the other half. It passes the search's sentence into
`TrayMenu.Because`, which is the improvement WW168 bought and is asserted. Then
`AsTraceStep` writes `Verdict = Opened ? Ok : Failed`, so a menu that never came up
because the shell would not open the flyout is recorded as a step the application
failed. That is the collapse WW168 was filed over, still live one call up, in the verb
an adopter reaches for more often than the search underneath it.

The second half is quieter. `TrayMenu` answers no `AsAssertion` at all, so it never
enters the pairing `RecordedResultTests` enforces — that check fires on types that
answer a verdict, and a type answering none is invisible to it. A scenario asserting
that an icon's menu opens has nothing to count, and whoever writes one first will invent
a verdict at the call site.

Both are the same repair. The menu carries the search that produced it, answers `Pass`,
`Fail` or `Unchecked` from it, and the step agrees with the verdict beside it rather
than restating `Opened`.

## Block E — Capture — the picture that proves what it photographed

### §WW38 A region, not a handful of points

Nine sampled points cannot cover a window: the capture taken to verify one task passed
all of them while carrying two windows of another process across its lower-right corner.
More points only move the threshold - the number that finally covers a window is the
number of pixels in it - so the question is asked about the region instead. The z order
above the window is enumerated and each frame intersected with the copy rectangle, which
answers for the whole area in one pass and names the intruder rather than merely
refusing.

### §WW40 Fail, never crop

An overlap on the edge is now inside real content, because the copied rectangle is the
painted frame and there is no invisible border left for a foreign window to hide in. A
file quietly trimmed to dodge an intruder is a picture of something nobody asked for.
The refusal names the intruder, its process and the rectangle it covers, because
something else was in the way is not actionable and a title with a pid is.

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

### §WW171 A guard that fires on a healthy fixture

WW162 shipped a guard, and the guard is right: a sampler that looks less often than a
state lasts cannot have observed the sequence, and dividing by a count that lost members
is how a missed sample becomes a confident number about the application.

It fired on a working fixture. A full run on the VMware guest that `run-tests-vm.cmd`
exists for read the window every 251ms against a declared 200ms state, and
`A_full_cycle_takes_about_as_long_as_the_length_the_run_declared` went red saying
exactly that. Nothing was wrong with the animation. Reading the tree of another process
through UI Automation on that desk simply costs more than 200ms, and the case had asked
for something no reader here can deliver.

The neighbouring case already learned this and wrote it down:
`The_states_arrive_in_the_order_they_were_declared_in` launches with `--animate=500`,
carrying a comment that measured why 150 was not enough. So two cases in one file
disagree about what this harness can read, and only one of them measured it.

The repair is not to loosen the guard, which is the load-bearing part. It is to declare
a length this reader keeps up with on the desk the suite is meant to run on, and to say
in the case that the number is a property of the reading rather than a preference about
the fixture. A case that goes red on a healthy machine teaches a reader to ignore the
guard that fired.
