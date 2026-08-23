# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

### §WW163 The record nothing writes

Read against this block's criteria the moment its last open line shipped, which is the
reading that stops a line count reaching zero from meaning finished. Two of the three
hold. The third does not: it says the trace of a failed run carries the locator, what it
resolved to, what was read back and the verdict for every step before the one that
broke.

The machinery is all here. TraceStep names those fields, TraceWriter numbers the steps
and writes them, TraceFormat renders a line, and the format is asserted both ways. What
is missing is anybody filling it: the whole engine constructs exactly one TraceStep, and
WW147 added it a day ago for a restore. No click, no read, no assert, no launch produces
one, so a failed run's record is empty and the reader's only tool is the re-run this
block exists to make unnecessary.

Nothing is wrong with the design; the join was never made. The scenario runner that
would make it is Block G and is not built, which is why this went unnoticed - but a verb
that writes its own step needs no runner, and every verb already answers something
carrying what the step wants.

So what is owed is the join, verb by verb, and a check that a verb answering without
recording is a red. Block A empties either way. What it must not do is empty while the
criterion it declares is met by nobody.

### §WW167 The output nobody reads back

Two of these were found in two days, both by accident. WW149 moved the roll call off the
console it happened to find, and the finding underneath was that nothing had ever
checked the words it printed - a tool whose entire output is a sentence somebody acts
on, with no case asserting any of it. WW153 fixed an agreement report that printed a
version no file in the tree holds, in a column too narrow to hold it, and that had
shipped because nothing asserted the report either.

The engine names forty of these across thirty-six types: a Render that answers lines, a
Sentence that answers one. Some are thoroughly checked. How many are not is unknown, and
the obvious way of asking is worthless - every one of those thirty-six types is
referenced by at least one test file, and Agreement was referenced by two while its
report was asserted by none.

This block's criterion is that a degraded run is legible without reading the log.
Legible is a property of the text, so a rendering nothing reads back is a criterion held
up by whoever last looked.

The remedy is the shape this project already uses twice: WW132 paired every refusal with
what provokes it, and WW145 made the pairing drive a case each. Pair the renderings the
same way, so the count is arithmetic rather than a promise, and a rendering added later
starts unasserted and says so.

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

## Block D — Act — patterns before pointers

### §WW165 The one verb in this block that answers yes or no

Seen on a loaded guest while shipping WW150, and the flake is not the finding. The case
opening the overflow twice went red saying "Expected: True, Actual: False", and that
sentence is the whole of what the run knew: which of the two calls failed, what the
shell was doing, whether the flyout was there and would not open or was never found -
none of it survived the return type.

Its sibling in the same file does it properly. Asking a nameless icon for its menu
answers a reading that carries whether it opened, a Because naming what could not be
found, and a trace step with a verdict on it, and the case asserts all three. The
overflow verb answers a bool.

So this is the same repair one verb over, and it is cheap: the reading type exists, the
reason is already known at the point of failure, and the trace step is a method away.
What it buys is the difference between a red that names the shell and a red that names
nothing - which is the reading Block A's own criterion is about, met here by one verb
and not by its neighbour.

Worth checking the rest of the block for the same shape while this is open rather than
filing a fourth line later: a verb answering yes or no is a verb whose failure has
nowhere to put the reason.

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

### §WW166 The third capability waiting for a caller

Read against this block's criteria the moment its last open line shipped. Two hold. The
second says a red step carries its diagnosis - the control view it failed to read
attached to the failure, so no throwaway script is written to find out what the window
had. The type that builds one exists, it is bounded and budgeted, its tests are
thorough, and outside them nothing in the engine calls it.

That is the third time this reading has come out the same way in two days. WW163 found
the trace: every field defined, the writer built, and one step constructed in the whole
engine. WW151 shipped the store fingerprint into the reading every run takes and could
only take the before half - the after half is a call nothing makes. Now the diagnosis.

One cause under all three: nothing composes a run. Each capability is finished and a
call away from a caller that does not exist, so every block reads as done because the
type is there. That is the failure the criteria were introduced to stop, arriving
through the door they left open - presence is not enforcement, and a list of finished
parts is not a working whole.

So this ships behind WW163 rather than beside it, and whoever takes it should read the
store join in the same sitting. What is owed here is the attachment: a failing step
carries the view, and a red without one is itself a red.

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

### §WW159 The animation check is a race, and a loaded desk loses it

Measured while closing WW146: the case ran three times alone on the host and passed, ran
once on a loaded host and saw four of five states, and ran in the VMware guest and saw
three. Nothing in it had changed, and neither had the fixture.

The check samples the window for three seconds and asserts that the number of distinct
states it saw equals the number the window declares. At two hundred milliseconds a state
that is fifteen states inside the window and five distinct ones, which is generous right
up until the reading is slower than the state. Reading another process's automation tree
costs more than a state stands for at that speed - the sibling case says exactly that in
its own comment, and uses five hundred milliseconds for the reason.

So the failure is about the reader and not about the animation, and a red naming the
animation sends somebody to the wrong file. A longer window is not the fix: it only
moves which machine loses. The check waits until it has seen every declared state or a
deadline passes, and says which states it never saw where the deadline wins. That turns
a race into a deadline on a named condition, which is what WW143 did to every other wait
in this suite.

### §WW160 The half of the pairing nobody drives

WW132 paired every refusal the framework names with the flag that provokes it or the
reason none can, and WW145 wrote a case per reachable entry because a pairing nobody
drives is a claim held up by having been written down. WW146 closed the last gap on the
flag side: three shapes were built and the fourth was moved up rather than faked.

What was never checked is the other half of the list. Sixteen entries now say some
version of a case builds this - a locator that does not parse, a trace that is not a
trace, a receipt over a window and a target a case hands it - and nothing anywhere
asserts that such a case exists. The suite does contain most of them, which is exactly
what makes the gap quiet: the entries were true when they were written, and an entry
whose case somebody deleted reads identically to one whose case still runs.

This is the same defect one level up from the one WW145 fixed, and the same remedy fits.
Each no-shape entry names the case that provokes it, and a check reads the suite for
that name. Naming one is cheap - the pairing already carries a sentence per entry. What
it buys is that deleting a case fails the pairing rather than quietly shrinking what the
refusals are worth.

### §WW161 The two exit codes the catalogue does not print

WW146 gave the fixture an exit code of its own: three, for a shape that provoked the
refusal it exists to provoke. It is deliberately not two, which means the fixture was
driven wrong - a run that cannot tell those apart reads the fixture working as the
fixture being misspelt.

Nothing a person reads says so. The catalogue prints every shape, what it takes, what it
provokes, what it needs and whether it draws anything, and says nothing about what any
of it exits with. So both codes are learnt by reading Program.cs, and the suite's own
cases carry three as a private constant copied out of the fixture - which is the second
transcription of the same fact, and the exact shape the flag catalogue was built to
stop.

The catalogue is the place for it. It is already what the built fixture says about
itself rather than what a compile-time reference would have said, and the suite already
reads the flag names out of it for that reason. A shape that ends in a refusal says
which code it ends with, the codes are listed once where a person driving by hand meets
them, and a case asserting three reads it off the article instead of holding a copy that
nothing compares.

### §WW162 The second case in the same race

Found in the guest run that shipped WW148, and it is WW159's defect wearing different
arithmetic. Where that case counts distinct states and finds one short, this one divides
the elapsed time by the number of changes it saw: nine changes over 5225ms is 653ms
each, against the two hundred the run declared. Both readings are correct about what the
sampler saw and wrong about the fixture, which was cycling exactly as asked.

The cause is the one WW159 names. Reading another process's automation tree costs more
than a state stands for at two hundred milliseconds, so a loaded machine skips states -
and a skipped state does not read as a gap, it reads as a state that lasted twice as
long. Dividing by a count that lost members is how a missed sample turns into a
confident number about the application.

So it ships behind WW159 and not beside it: the deadline-on-a-condition that fixes the
first is what gives this one a sample set worth dividing. What is owed here is the
second half - the check measures against what the reader actually kept up with, and
reports that rate, so a run on a slow desk says the reading was too slow rather than
saying the animation was.

### §WW164 A wait for a name where the content was meant

Measured on a loaded guest while shipping WW150: the layout case read the fixture's
geometry dump and got "there was no geometry to check", against a fixture that had drawn
its window and was writing the file. The run before it and the run after it both passed.

The helper every fixture case goes through waits for the two dumps by asking whether
they exist. Existence is the first thing a write produces and the last thing that means
anything - the file appears empty, the wait comes back, and the reader gets a dump with
nothing in it. What comes out is a fault about the application, on a run where the
application was fine.

This repository has met the same defect once already and wrote the answer down: WW145's
store helper reads through the write rather than around it, because a file that exists
and is empty is the half of the write that happened to finish first. That case waits for
the content to be something other than what it already held. The fixture helper waits
for a name.

So the repair is the pattern already here: wait for what is about to be read, which for
a dump is a root and at least one element rather than a byte count. It is the same shape
as WW159 and WW162 without being the same cause - those lose a sample to a slow reader,
this one reads a file the writer had not finished.
