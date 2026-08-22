# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

### §WW137 The roll counts names and not answers

Found in the check the moment after it shipped. The roll reads every result the file
carries and counts it as a case that answered. A results file records an outcome for
each one, and NotExecuted is among them - a deliberate skip, or a case the runner listed
and then abandoned. Both are recorded and neither ran, so the roll would call a run
whole that executed none of them.

That is the founding defect again, one level in. The check exists because a total of 352
where the run before had 374 read as a pass; a run where all 374 are recorded and 22 say
NotExecuted reads as a pass to this check for the same reason - a number that agrees,
and nobody asking what it is a number of.

The fix is small and the shape is here already. The results file says which outcome each
case had, so the roll can count answers rather than names: what ran, what was recorded
and did not, and the second on its own line rather than folded into the first. A
deliberate skip is then visible, which this project's own non-goal asks for anyway - a
green never covers an assertion that did not run, and a suite is a pile of assertions.

What must not happen is the two being merged back into one total. A recorded skip and an
executed pass are different facts, and a check that adds them is the check being
replaced.

### §WW138 A check beside the run is a habit again

The roll call landed as its own process with its own tests, and it is reached two ways:
a step in the CI workflow, which is unconditional and therefore real, and a script at
the root, which is typed by whoever remembers to type it. Every developer running the
suite runs the middle command of the three, because that is the command every .NET
project has, and gets the same pass the check exists to withdraw.

The task's own note said it: it belongs on the run rather than in a reviewer's habits.
Half of it went into a reviewer's habits.

What is wanted is that the ordinary command carries the check. The test project can hang
a target off the run that lists the tests and compares, so a bare invocation is the
checked one and there is nothing shorter to type. Two things have to be true of it. It
must not need a network or a second build - a check that doubles the wait is a check
somebody disables - and it must be skippable by one obvious switch when a person is
filtering down to a single case, since a filtered run is short of discovery on purpose
and reporting that as a lost host is the false red that turns the whole thing off.

The general form is worth stating: a check that has to be invoked separately is not a
check the project has, it is a check the project offers.

## Block B — Attach, launch, and leave nothing behind

### §WW133 A refused foreground is a fact about the desk

WW114 asked for a desktop of the fixtures' own and the measurement killed it. A desktop
made with CreateDesktop takes windows, and UI Automation reads them from either side of
the boundary. Three readings decide it. An STA thread cannot enter one at all: the
apartment already owns a window, so the call comes back ERROR_BUSY, and the pumped
fixture is STA by design. SetForegroundWindow on a desktop that is not the input desktop
returns false and GetForegroundWindow answers zero, so the fixtures would go from
sometimes refused the foreground to always refused it. And SendInput from a thread on
one fails with ERROR_ACCESS_DENIED, so the input tests the idea existed to steady could
not send a key. Only SwitchDesktop makes it the input desktop, and that blacks the
screen of the person at the keyboard for the length of the run - the one thing the same
idea promised not to do.

What the complaint underneath was right about stands: whether these tests pass is partly
a question about what else is on the screen. The answer is this block's own criterion,
that nothing about the desk is reported as a defect in the code. A case that needs the
foreground and is refused it should record a hole naming the desk, the way every other
unmeetable condition here does, rather than go red about the application. The fixture
already declines to insist; what is missing is the sentence on the other side of the
refusal.

### §WW140 A blip is not a missing assembly

Caught in a full-suite run while WW119 was being verified. The desk reading touches
AutomationElement.RootElement to find out whether UI Automation is usable at all, and
catches COMException among the ways it can be unusable. Once in a loaded run it came
back with "Unexpected HRESULT has been returned from a call to a COM component", the
reading said this desk cannot observe, and the case asserting the conditions a running
suite proves went red. The class passes six times of six on its own.

Both halves of the catch are right and they are not the same fact. A machine with no
automation assemblies fails that call every time; a machine under load fails it once and
answers the next moment. Reporting them identically is this block's own criterion
pointed the wrong way - nothing about the desk should be reported as a defect in the
code, and a blip reported as a missing subsystem is a defect in the desk reported as a
fact about it.

The remedy is the one the rest of the engine uses. Every other reading that can lose a
race here is a deadline on a condition rather than a single look, and the retry type is
capped and counted, so a reading that needed a second attempt says so. What must stay
true is that a machine which genuinely cannot reach UI Automation is still reported as
such promptly, and does not spend a cap discovering what the first attempt established.

## Block C — Locate — the locator grammar and the tree an agent reads

### §WW143 The suite does not take its own advice

Counted while closing this block against its own criterion, which says no scenario
carries a sleep and every wait is a deadline on a condition with the time it took
recorded. The engine keeps that. The suite does not: eighteen hand-rolled loops across
five files, each a for loop over a fixed count with a Thread.Sleep in it, and one bare
sleep of 120 ms in the traversal cases waiting for a focus change with no condition.

They are not wrong so much as unowned. Each is the engine's Attempt written out longhand
with a different cap and poll, so the two things the criterion asks for are absent from
all: none reports how long it took, and none fails saying what it waited for. When one
is slightly too short on a busy machine the result is a red about the application, which
is the misattribution WW119 measured.

The work is mechanical and the judgement is in two places. A loop waiting on a file to
appear and one waiting on a window to be drawn want different deadlines, and the
declared timeouts already name several - taking one wholesale would make a fixture wait
a launch timeout to notice a file. And the bare sleep has no condition to convert, so it
needs one written: what it waits for is a focus that moved.

The suite is this project's own demonstration, and a demonstration that breaks its own
rule eighteen times is an argument against the rule.

### §WW144 The line is not a structure

Noticed twice in one block, which is the signal. Inspect renders each element as a
string that begins with the locator step and continues with the rectangle and the
patterns, and anything wanting the locator back has to find where it ends. Two test
files now carry the same helper for that, and the second was rewritten mid-task when a
name turned out to contain a run of spaces: the separator is two spaces, and two spaces
occur inside a name somebody else wrote.

The helper that works scans for a double space outside the quotation marks, honouring
backslash escapes. That is a small parser, written twice, to recover something the
renderer had in its hand a moment earlier. It is the shape of code that is correct until
the day the format gains a field.

What is wanted is that the rendered form carries its parts. A rendered element with the
step, the indent and the whole line on it costs nothing to produce, keeps the line
exactly as it prints today, and means nothing downstream has to know where the fields
meet - the diagnosis view, the copied-line checks and anything an adopter writes.

There is a second reader worth converting at the same time: the check that every printed
line parses is the check most likely to be defeated by the format changing under it, and
it is the one that should be reading a field rather than a substring.

## Block D — Act — patterns before pointers

### §WW134 The guard speaks one language

Found while shipping the guard itself. A declared entry is matched against the
automation id first and the displayed name second, and the second is the one every
project will reach for, because the name is what an author reading a menu can see. It is
also the one field a translation rewrites.

So a project declaring "Quit" is guarded on an English desk and unguarded the moment the
same application comes up in pt-BR showing "Sair" - and unguarded silently, because
nothing about a name that matched nothing looks different from a name that was never
dangerous. The failure mode is the worst available: the run presses the entry that ends
the run, on the machine where somebody was least expecting it.

This tool already knows how to resolve a string across languages. It reads the project's
language files, resolves which one the application is showing, and has a precondition
about the answer. The destructive list is the one place that knowledge is not being
used, and the fix is small: declare the entry by whatever a translation cannot move -
the automation id, or a key the language files resolve to the displayed text - and
refuse a declaration that can only be matched by a name where the project ships more
than one language.

The general rule underneath is worth keeping: a safety check compared against text a
person sees is a safety check with an expiry date, and the expiry is whenever somebody
translates the application.

### §WW135 A guard the caller may decline by accident

The refusal landed on the door every act passes through, which is right, and it reads
the list off the subject, which is where the trouble is. Three constructors make a
subject and only one carries a declaration; the other two carry a bare deadline or a
bare Timeouts, and a subject made either way has an empty list and refuses nothing.

That would be fine if the declaration-carrying one were the obvious one. It is not. A
scenario author with a project in hand writes the timeouts out of it - that is what the
type is for - and reaches for the constructor that takes them, and the guard is gone
with no line anywhere saying so. This project has closed exactly this shape twice: a
process cannot be launched outside the register, and an act cannot reach an element
without an admission. Both work because the weaker route does not exist.

What is wanted is the same: the subject a scenario makes against a declared project is
the one shape that is reachable, and a subject built from a bare deadline is what a test
of the locating machinery uses and says so. Whether the answer is folding the timeouts
constructor into the declaration one, or carrying the list separately so every route
takes it, is the task's to decide. What must be true afterwards is that no scenario
silently loses the guard by writing the constructor that was easier to reach.

### §WW136 A check that shipped and was never joined

Filed against the thing that had just shipped. A pointer act now states why the pattern
route was unavailable, and there is a check that reads each stated reason back against
the live tree and answers with what agreed, what the tree disputes, and what claimed
nothing it could answer. Nothing calls it.

That is this project's founding defect wearing a new subject, and the block before this
one closed the same shape by making one list: five measurements had shipped and none of
them was joined, so a run's claim about the machine was met three times over by three
sentences and therefore not once. Here it is met zero times. A reason recorded in the
file, never read back, is exactly the sort of comment that is true on the day it is
written and quietly false a year later - and the report will keep printing it as though
somebody had checked.

The join is small and the place is decided: the reading the run already takes before it
starts is where the machine is described, and what the file claims about the
application's controls belongs beside what the machine claims about itself. A disputed
reason is not a failure of the run - the act still works, that is the point of a pointer
- so it is a finding in the preamble rather than a red. What it must not be is absent,
because absent and checked read the same to whoever skims.

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

### §WW139 The notes in the file are not strings

Read off a failing assertion while WW118 was being shipped. JSON has no comments, so a
strings file that wants one writes a key nobody reads - the convention is a key named
"//", and the fixture's own file uses it twice. The derivation takes every string under
the key, so both notes join the expectation, and the set demands that a window somewhere
displays the sentence "The pathological key. An exact-name read can never match this".

It is the founding defect pointing the other way. A set with two members nothing can
ever read is red on every run for a reason that has nothing to do with the application,
and a red nobody can fix is a red people learn to ignore - which is how the green that
covers a missing tab header gets shipped next.

The fix is small and the judgement is where the work is. Skipping a key named "//" is
the convention and covers the file here; "//2", "_comment" and "$comment" are the same
convention spelled differently. What must not happen is silent removal: WW118
established that what a derivation leaves out is named in the source sentence every
verdict prints, and a skipped note belongs in that list rather than in a rule nobody
sees the effect of.

Worth checking whether the label reader has the same hole, since it walks the same files
by key and would answer a comment as a label.

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

### §WW141 The uncooperative application is nobody's case

Read against this block's second criterion once its last line shipped. It says every
verb needing no cooperation runs against an application that references nothing, which
is what keeps this usable on a product nobody here owns. In fact hundreds of cases do
exactly that - they build a bare Win32 window and drive it - so the criterion holds
today by accident of how the fixtures were written, and no case anywhere states it.

That is the shape this project keeps closing. A rule met by whoever remembers is met by
nobody: the day a verb quietly starts reading something only the in-app half provides,
every one of those hundreds still passes against fixtures that happen to have it, and
the first person to find out owns a product this cannot drive.

What is wanted is a list and a run over it. The verbs that claim to need no cooperation
are nameable - resolve, inspect, the pattern acts, the readings - and an application
referencing nothing is a fixture this suite already knows how to build. Driving the list
against it in one case makes the claim checkable, and adding a verb to the engine
without adding it to the list should be the thing that fails.

Its opposite is worth naming in the same breath: the verbs that do need the in-app half
are a set too, and one that is written down is one an adopter can read before deciding
what the package buys them.

### §WW142 A gate with nothing standing in it

The agreement reading has an ExitCode property whose own comment says the difference
between a gate and advice is the number it leaves behind. Nothing leaves it behind.
Outside its own tests the whole reading is called by nobody, so the copies of the engine
in play are compared exactly as often as somebody opens the file.

It was not buildable before now, and that is worth recording rather than treating as an
oversight: until the halves packed there was a source tree and a path, one copy of two,
and the reading refuses fewer than two copies on purpose. WW122 supplied the second -
what the build actually produced, read out of the nuspec - so a real comparison exists
for the first time.

What it owes is one step that reads the copies this repository can name and stops on
anything but agreement: the version the tree declares, the version that was packed, and
the version the sample adopter pins. Those three moving apart is the exact failure the
type describes - a stale copy does not fail, it agrees with a rule that has moved - and
today the sample's pinned version is a literal somebody has to remember to bump.

The same step is what an adopting project would run, which makes it worth building here
in the form they would take rather than as something only this repository's layout
understands.

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

### §WW145 The pairing is checked one way only

Filed against WW132 the moment it shipped, which is where this kind of gap is cheapest
to see. The pairing now compares two lists that nobody compared before - the refusals
the assemblies name against the entries, and the entries against the flags the built
fixture prints - and both directions are real. The claim in the middle is not checked at
all.

An entry says --language reaches the unusable-label refusal. What holds that up is that
somebody wrote it down. Renaming a flag is caught; changing what the flag draws is not,
and neither is an entry that was wrong when it was written. Three of the four entries
naming a flag are provoked in the suite by building the situation directly rather than
by running the fixture with that flag, so the sentence and the evidence are about
different things.

What closes it is a case per entry: launch the fixture with the flag, drive the thing
the refusal is about, and assert the refusal. Four today, and the number is small
because the count of reachable refusals is small - which is the other half of why this
is worth doing now rather than at forty.

The shape to avoid is a case that catches any exception and calls the pairing proved.
The refusal type is named in the entry, so the case can insist on that type and nothing
else, and a refusal arriving for a different reason is a red rather than a pass.

### §WW146 The four the fixture cannot be

Counted by WW132 and left standing by it. Four refusals need a shape the fixture cannot
take: a receipt about a window other than the one captured, a picture nothing drew, a
capture of an element with no background above it, and a render of a tree that lays out
to nothing. The fixture always does the right thing, so it can never make any of them
happen, and each will quietly stop working with nothing to notice.

This block's criterion is explicit that a refusal with no flag is a finding the fixture
closes rather than a gap nobody sees, so the count being visible was the previous task's
work and closing it is this one's.

Three of the four are one shape apiece and the shapes are the point rather than the
refusal: a pane that lays out to nothing, a pane with no background above it, and a window
drawing nothing into the rectangle a capture would take. Each is a real product defect - a
page that renders empty, an element captured as transparent, a surface that came out blank
- and the fixture exists to be those on demand.

The fourth may not be a shape at all. A receipt about the wrong window is a harness
handing over the wrong handle, not an application misbehaving, and the honest answer may
be to move it to the reasons no flag can reach rather than invent a fixture that lies
about its own window.
