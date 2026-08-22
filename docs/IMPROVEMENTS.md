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

### §WW119 The tray fixture waits for the wrong thing

Measured across four consecutive full-suite runs while WW54 was being finished, on an
untouched machine: two were green at 480, and two were red with two failures each, every
one of them in `NotificationAreaTests`. Twice it was
`An_icon_added_now_hides_in_the_overflow_and_is_not_in_the_tree_until_it_is_opened`,
failing on `Assert.NotNull(found)` — the icon this run had just added was not in the
overflow at all. Once it was
`Opening_an_overflow_that_is_already_open_is_answered_rather_than_toggled`. WW54's own
twelve tests never failed in any of the four.

`TrayIconFixture.Add` waits for `Shell_NotifyIconW` to return true, and its summary says
it blocks "until the shell has it". Those are different claims. A true return means the
shell accepted the message; placing the icon in the overflow and building the automation
tree under it happens afterwards and on the shell's own schedule, so a test that looks
immediately is racing it.

The repair is the deadline the engine already has: wait until
`NotificationArea.Find(tip)` answers, with the project's declared timeout, and fail the
fixture naming the tip where it never turns up. That also removes the temptation to fix
this with a sleep, which would trade a flake for a slower suite that still flakes on a
busy machine.

Distinct from WW111 and WW114. Those are about the foreground being contested; this one
reproduces with the foreground uncontested, because nothing here needs the desktop —
only the shell's own timing.

### §WW127 A check reads a window this suite made

Two checks in the actionability file read `AutomationElement.RootElement` and the
desktop's first child, and assert about the control type and the pattern names that come
back. What they are really asserting is that this machine, at this instant, was showing
something whose patterns are all in the locator grammar's vocabulary.

Measured across this session: one of them went red on a run where nothing had changed
and green on the next two, and it is the last remaining source of a failure the code
under test cannot explain. Whatever is first under the desktop is whatever the person or
the run happened to open, and a custom control in somebody else's application can report
any pattern it likes.

The property they check is worth checking - short names and a vocabulary the grammar
accepts are both real claims. The subject is what is wrong. Pointed at a window this
suite creates, both become claims about this project, reproducible on every desk, and
the fixture already exists to be that window. The desktop root itself is still worth one
reading, but as a statement about what the engine does with an element it did not choose
rather than as an assertion about the element.

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

## Block C — Locate — the locator grammar and the tree an agent reads

### §WW124 An id is quoted like a name

Found by driving the fixture rather than by reading. Windows gives a window's own system
menu the automation id `Item 1`, and inspect renders that element as `MenuItem#Item
1[name="Sistema"]` - which the grammar refuses at the space, reporting that it expected
`>` or the end and found `1`.

The name field is already quoted for exactly this reason and the id is not, so an id is
assumed to be an identifier. Nothing this project controls produces one with a space in
it, which is why it survived: every fixture in the suite names its own controls, and the
first tree walked that somebody else built broke it on the first line under the title
bar.

It matters more than a chrome element. The whole claim of the verb is that a line it
printed can be copied into a scenario, and a line that cannot is worse than no line - it
is an answer that looks usable and fails at parse time, in a file somebody wrote from
it. The existing test asserts the property against a window this repository builds,
which is the shape of check that only ever proves what the author already assumed.

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

### §WW130 Collapsed is not the same as measuring nothing

Found by running the layout check against a real window rather than against a dump
somebody typed. The fixture's loading note is collapsed unless a run asks for it, so it
lays out to nothing - correctly, deliberately, and on every page that hides anything.
The check reports it as an element laid out to no size, which is the fault it exists to
catch, and on a real page it fires on every hidden thing at once.

Nothing distinguishes the two today because the dump does not carry visibility. A
rectangle of no area is all a reader gets, and a caption that wrapped at column zero and
a note the page is deliberately not showing produce exactly the same line.

So the dump carries it and the check reads it: an element the application collapsed is
left alone, and one that is visible and still measures nothing is the finding it always
was. The cost is one field in a format two packages have to agree about, which is the
same price every other honest reading in this project has paid.

### §WW131 The application's elements and the template's parts

Measured against the fixture's own window. Four of forty-five elements are laid out
wrongly by every rule the check has, and every one of the four is a part of the default
tab template: a selected header is drawn four pixels outside the panel holding it and
two past the border containing it, on purpose, because that is how a selected tab lifts
over the edge. The elements are real, the rectangles are real, and the faults are true
statements about what was drawn.

They are also not what anybody asked. A geometry check exists to catch a caption that
wrapped and a button nine pixels below its box - things somebody wrote - and it
currently answers with the framework's chrome instead, which no adopter can fix and
every adopter would have to read past.

Narrowing by name does not separate them: the application named the tab item, and the
template drew it out of place. What separates them is who declared the element, and the
dump is where that is known - the walk is standing inside the application when it
happens. A field saying so is cheaper than every reader learning to ignore four lines.

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

### §WW122 One package reference, literally

Read against this block's own criterion after its last line shipped, and the gap is
plain: nothing in either project declares a package id, a version beyond the tree-wide
one, or a pack step. The only way to adopt the in-app half today is a project reference
into this repository - which WW70 already reads as unpinnable by construction, and which
an application shipping to its users cannot take at all.

The criterion says the adoption is one package reference and that a project taking it
deletes its own capture, surface and thread helpers. The second half is proved: this
repository deleted eight copies of one runner in a single commit. The first half is not
built. So the criterion is met in intent and unmet in fact, which is exactly the state a
block's criteria exist to surface before somebody calls the block done.

What this owes: a package id, packing in continuous integration, and the agreement
reading pointed at the packed version rather than at a path. Not a public feed - a
package an adopter can reference from a local source is still a package, and publishing
is a decision nobody here has made.

### §WW123 The separation is asserted, not remembered

Both halves currently reference nothing, and both project files say why in a comment. A
comment is not a check. One line added in either project would merge them, the build
would stay green, and the consequence would only be found by whoever shipped a test
harness inside their product or a presentation stack inside a headless runner.

The separation is load-bearing in both directions. The engine is taken by the harness
driving an application; the in-app half is taken by that application. An application
referencing the engine ships a test harness to its users. A harness referencing the
in-app half inherits a drawing stack it never needed, and the two module initializers
that each ask for per-monitor awareness stop being independent - which is the reading
WW121 measured rather than argued.

The check is cheap and belongs to the suite rather than to a reviewer's memory: read
both project files and refuse a reference either way. This block's own criterion says a
project that cannot take the package still works, and nothing today would notice the day
that stopped being true.

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

### §WW132 Every refusal against the flag that provokes it

Counted while reading this block's own criterion after its last line shipped. The
framework names seventeen refusals; the fixture has a flag that provokes six of them -
another instance, a store that moved, an unknown flag, a backdrop, a page still loading,
a region covered. The other eleven are asserted against hand-built windows, against
arguments passed in a test, or not at all.

Some of the eleven need no flag and saying so is the point: a locator that does not
parse is a string, and no window has to exist for it. Others do and cannot be provoked
here today - a render that lays out to nothing, a capture with no background declared, a
picture nothing drew. The fixture always does the right thing, so it can never make any
of them happen, and each is a refusal that will quietly stop working with nothing to
notice.

The durable half is the pairing itself. The catalogue and the exception types are two
lists nobody compares, and a refusal added later starts unprovokable and stays that way.
A check that reads both, and a declaration on each refusal saying which flag reaches it
or why none can, turns this block's criterion from a sentence into something that fails
when it stops being true.
