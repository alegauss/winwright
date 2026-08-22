# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

### §WW108 The verdict and the trace do not reference each other

Block A shipped both halves and joined neither. A summary line says `failed the report
renders - the file was never written`; the trace says step 7 was an assert on `#status`
that waited 240 ms and polled three times. Reading one against the other is a person
matching prose to prose, which is the re-run this block exists to make unnecessary - the
criterion "A failure is diagnosed from the record and not from a re-run" is met by what
the trace contains and not by how a reader reaches it.

What is missing is one field in each direction: a result carrying the ordinal of the
step that settled it, and a step carrying the assertion's name where the step was an
assert. The ordinal is already assigned by the writer and returned from the write, so a
runner holds it at the moment the result is built and nothing has to be looked up
afterwards.

It is filed under this block rather than under the runner's, because it is a property of
what a verdict is. A runner left to invent the join would invent a different one, and
the next runner another.

### §WW109 A trace that does not parse says which line

The trace reader hands the JSON parser's exception straight out. What a reader gets is a
complaint about an invalid start of a value and a byte offset into a string that is no
longer on screen: no path, no line number, and nothing saying the file was a trace at
all. The test written for it can only assert that something was thrown, which is the
tell - nothing about the refusal was worth naming.

A trace is read after a run that already went wrong, and often after one that was
truncated, so this is the second bad moment in a row for whoever is reading it. The
refusal should carry the file, the ordinal of the line and that line's own text cut to
something a terminal can show, which is the shape the scenario refusal already has for a
file that will not load.

The blank line is deliberately skipped and stays skipped: a trace ended by a crash
finishes on one, and that is the reader working rather than failing. What this is about
is the line that has content and is not a step.

### §WW117 A crashed host is not a pass

Measured here while building WW39. A test declared a sixteen-byte `RECT` as an
eight-byte `long`, so the call corrupted the stack and the host died partway through an
unrelated class. `dotnet test` printed `Aprovado!` with `Com falha: 0` and a total of
352 where the run before it had 374 — twenty-two tests gone, and the only sign was a
number nobody had a reason to read.

This is the defect this project was started over, in the suite that is supposed to prove
the project does not have it. A green that covers tests which never ran is exactly the
hole WW6 exists to close, and it is worth nothing if the harness proving WW6 has the
same hole.

The count is the check: discovery already reports how many tests there are, and the run
reports how many executed. When they disagree the run is broken rather than passed, and
the message says how many are missing and where the host stopped. Nothing here tries to
diagnose the crash — a fatal error has no managed stack to read — only to refuse to call
the result a pass.

It belongs on the run rather than in a reviewer's habits. The number moved by six
percent and two consecutive readers, both of them me, took the word `Aprovado` at face
value.

## Block B — Attach, launch, and leave nothing behind

### §WW110 The run has no preamble

Block B shipped five measurements and joined none of them. Staleness, the running
binary, the foreground, the launch arguments and the resolved language each answer with
a precondition and a sentence, and each is reached by its own call on its own type.
Nothing lists them.

Two things follow, and the second is the one that matters. The block's criterion "a run
says which binary it drove" is currently met three times over by three sentences, which
is to say it is not met once: a reader gets whichever of them the caller remembered to
print. And a runner assembling the precondition set by hand will one day be edited by
somebody who does not know all five are there - at which point the forgotten one stops
being measured and every assertion that needed it silently starts passing. That is WW6's
defect with a different subject, and this block is where it can still be closed cheaply.

What is wanted is one reading, taken once at the start of a run: the target, the binary
with both its keys, the language and where it was read, the staleness comparison, the
foreground at the moment input was first synthesised, and what other instances were
open. It renders as the preamble a summary opens with, and it hands over a set the
assertions are then resolved against, so that adding a sixth measurement is one file and
not an audit of every runner.

### §WW111 The suite leaves the foreground somewhere else

Found while writing WW13. Creating a top-level window with WS_VISIBLE activates it, so
every fixture in this suite that needs a visible window takes the foreground for as long
as it lives. On a developer machine that is a flash over whatever was being typed into,
several times a run.

It is filed under this block rather than dismissed as test hygiene for two reasons. The
theme here is leaving nothing behind, and a foreground handed to a window that has since
been destroyed is something left behind. And the tool measures the foreground: a suite
that moves it is a suite whose own readings of it are taken on a desk the suite
disturbed, which is the shape of a test that agrees with itself.

The fix is small. A fixture window can be created at coordinates outside every monitor's
bounds, which keeps it visible to the enumeration under test and invisible to the person
at the keyboard. Where a test genuinely needs a window on screen, it can say so and
place it deliberately. What should not survive is the current arrangement, where forty
by forty is the default because it was the first pair of numbers typed.

### §WW114 The fixtures need a desk of their own

Measured while WW29 was being written, and it cost most of that task. Every fixture that
synthesizes input needs this process to own the foreground. Windows grants that to a
thread holding a window it has just created - usually. Once the process has been refused
once, it stops being granted, and from then on a fixture that opens a window is simply
not activated.

The evidence was unambiguous. Making the fixture insist on the foreground before
returning turned one busy desktop into forty-seven failures in a single run, each
costing four seconds of waiting first. Softening it back to a request made the suite
green again and left the fragility exactly where it was: whether these tests pass is
partly a question about what else is on the screen.

A second finding of the same afternoon has the same answer. An open menu holds its
thread inside a modal loop, so the quit posted to it is never read and its window
outlives the test - and the class that then counts what this process is showing fails
about a window it never touched. Repaired, but the same shape: one process, one desktop,
every fixture sharing both.

A desktop created for the run is where this ends. The fixtures are the only windows on
it, the foreground is theirs because there is nothing to lose it to, and nothing they do
reaches the person at the keyboard - which is what WW111 asks for from the other side.

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

## Block C — Locate — the locator grammar and the tree an agent reads

### §WW112 Actionability needs a door, not a convention

The check landed and the chokepoint did not. A verb about to click something can read
the facts, judge them and require the answer - or it can resolve, take the element and
press it, and nothing in the types notices. The block's criterion "an act never runs
against an element that cannot take it" is met by whoever remembers, which is the shape
of rule this project has closed twice already and should not leave open a third time.

Both closures are worth copying. A process cannot be launched outside the register
because the type a caller needs has no public constructor; an attached target cannot be
asked what arguments it was started with because the property is not on it. Neither
relies on anybody reading a note.

What is wanted here is the same move: the thing a verb needs in order to act is handed
out only by something that has already judged actionability, and carries the judgement
with it. A verb holding one of those knows the four properties held at the moment it was
made; a verb that wants to skip the check has nothing to call. Where the judgement then
belongs - refusal, hole or failure - stays the acting block's decision, and this only
makes it impossible to never have made it.

It is filed here rather than under the acting block because the door is part of what
locating hands over, and a door invented per verb is four doors that differ.

### §WW113 The root's line is the one that does not work

Measured while writing the subject tests. A step searches the descendants of the root it
is given and never the root itself, which is right and is what every other locator
engine does. The trouble is that inspect prints the root as its first line, in exactly
the same shape as every other line: a locator step, then the rectangle, then the
patterns.

So the flow this block exists for - read the tree, copy the line, write the locator -
has one line in it that quietly does not work. An agent copying the first line gets a
miss, and the miss is diagnosed as absent, because from the root's own point of view
nothing under it matches a step describing the root. That is the least helpful answer
this tool gives about the most obvious mistake.

There are two ways out and the task is not fixing which. The first step could match the
root or its descendants, which is what a reader copying the line means; or inspect could
mark the root as the root rather than printing it in the shape of something to copy.
Either closes it. What must be true afterwards is that a locator assembled from what
inspect printed resolves, whichever line the reader started from.

## Block D — Act — patterns before pointers

### §WW115 A declared cost with no reason beside it

Read back against this block's own criterion, which asks that the acts unable to go
through a pattern be "declared as pointer acts and carry the reason for it in the file".
Half of that shipped. A pointer act is its own type, nothing falls back to it, and the
set of them can be summarised before a run. What none of them carries is why it is one.

The summary as it stands lists three locators and three buttons. A reader deciding
whether a scenario can run unattended gets the count, which is the cheap half of the
question; what they wanted was that this one is a bare border with no automation peer,
that one a notification-area icon, and the third a segment of a custom template. Those
are three different futures - the first may get a peer, the second never will - and the
list flattens them into one number.

The field is small and the discipline is the same one the declaration already has: the
act says what it needs and why, at the point where somebody chose it. Preflight is the
other half worth wiring, since it already resolves each act's element and could say
plainly that the control offers no pattern at all - which is the reason, checked rather
than asserted, at the one moment the tree is there to check it against.

### §WW116 The menu is safe and the verb beside it is not

This block's criterion says destructive entries are "named in the scenario and reached
only by traversal". The second half shipped and the first did not. Walking a menu cannot
invoke anything - there is no such method on that surface, and a test asserts it - so
the route this rule was written about is closed. The route beside it is wide open: the
general invoke will press a menu item called Quit exactly as willingly as one called
Open, and nothing anywhere in the project knows the difference.

That gap is the same shape as the one the pointer acts closed. What makes a click safe
there is not that it is hard to reach, it is that reaching it is a thing the file says
out loud. Here nothing says anything, so the safety rests on the author of every
scenario remembering which entry ends the run - which on claude-tray is one entry, and
on the next adopting project is a different one nobody has met yet.

Naming them per project is where the list belongs, beside the executable and the
timeouts, since which entry quits is a fact about the application. Then invoke refuses
one of them unless the act says it means that one, and the refusal names the entry. A
scenario that genuinely tests the quit path says so, once, where a reviewer sees it.

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

### §WW118 A set refuses a placeholder too

WW50 refuses a label whose value carries a placeholder, because a tree holding
`Bem-vindo, Alexandre` can never equal `Bem-vindo, {0}`, and skipping it in silence is
an assertion that did not run reported as one that passed. WW49 derives a whole set out
of the same files and has exactly the same hazard with none of the guard: a key under
`tabs` whose value is `{0} items` joins the expected set, is never read from any window,
and lands in `Missing` on every run as an unfixable red — or worse, is quietly matched
by a control that happens to render the literal braces.

It was left out of WW49 on purpose rather than missed. Widening a shipped task to cover
a case found afterwards is how the ledger stops describing what was actually built, and
`Labels.CarriesAPlaceholder` was made public at WW50's altitude precisely so this one
has something to call rather than a second regular expression to keep in step.

What is open is where the refusal goes. Refusing the whole derivation is the loud
reading and matches the label, but a project with one templated string under an
otherwise good key would then be unable to derive that set at all. The alternative is to
exclude the placeholder members and say so in `Source`, which keeps the set usable and
keeps the exclusion visible — a count that is not silent is not the defect. Decide it
against a real strings file before writing either.

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

### §WW89 A window that belongs to this repository

Every loop in this framework is currently developed against somebody's shipping product,
which means a real account, a real transcript directory, a real controller and a machine
somebody set up by hand. The fixture removes all of it. It is not a demo and not a
sample: it is the surface this framework's own tests drive, and its design goal is to be
hard to test in the specific ways Windows is hard.

### §WW90 Every refusal has a flag that provokes it

This framework's value is concentrated in its refusals, and a refusal nobody can provoke
is a refusal that will quietly stop working. Each one gets a fixture flag: cover this
region, opt into a backdrop, render nothing, stay loading this long, draw a control with
no name. The framework's own suite then asserts the red, which is the only thing that
keeps a refusal real rather than remembered.

### §WW91 The same window under two pumps

The difference between hosting a window under one message pump and another is invisible
in every picture and decides whether keyboard input arrives at all. claude-tray
discovered it by shipping windows that took no keystrokes while every screenshot of them
looked perfect. The fixture ships both hosts behind two flags, which is what makes the
check for it developable without inheriting a real product's hosting decisions.

### §WW92 One page holding the whole naming rule

A control with no name, one announcing a glyph codepoint, one whose label is a
neighbouring element, and beside them a button that must keep its own text - both
branches of the rule on one surface. That is the case set the naming check needs, and
assembling it out of a real product means waiting for that product to happen to have all
four at once.

### §WW93 Three kinds of absence

A collapsed pane, a closed popup and an unopened submenu are all missing from the tree
and all mean different things, and each one cost a real defect somewhere to learn.
Having the three behind flags is what lets the classification of a miss be developed and
asserted rather than reasoned about from memory.

### §WW94 Both arms of the backdrop refusal

A refusal with only one arm tested is half a check: it can be right about the window it
refuses and wrong about everything it lets through. The fixture ships one window that
opted into a system backdrop and one that never did, so the refusal and the pass beside
it are both driven - which is what proves the check reads the compositor rather than the
window's name.

### §WW95 A borderless window with no handle

A toast, a balloon or a menu is a top-level window the process object never reports, and
that shape exists today in exactly one product here. The fixture raises one on request,
which makes the enumerating launcher and the frame sequence both developable without
waiting for somebody's notification to fire on its own schedule.

### §WW96 A page that is loading for as long as the check needs

The loading refusal was discovered on a machine that happened to be slow, and
reproducing it means finding another one. The fixture takes the duration as a flag, so
the refusal is asserted at a moment the run chose - and the other arm is covered too, a
page that finishes inside the wait and must not be refused for it.

### §WW97 An animation with a known length

A frame sequence is currently checked by opening the frames, which is the thing this
framework exists to avoid. The fixture plays an animation of a declared duration with a
declared number of visible states, so the sequence is checked against numbers: how many
frames, at what interval, and that the states arrive in the order they were declared in.

### §WW98 Something to be identical to

The byte-identical assertion needs a surface fixed by construction: no clock, no machine
name, no real data, and no theme that follows the desktop unless the case asked for it.
Producing one is a design constraint rather than an accident, and it doubles as the
reference for what an adopting project has to do to make its own surfaces comparable at
all.

### §WW99 A second instance on request

The other-instance refusal and its override are both tested today by remembering to
leave a window open. The fixture opens a second window on request, so both arms are
driven - and the distinction that matters is covered too: a resident process showing no
window must not trip the refusal, because that is the ordinary state of every developer
machine here.

### §WW100 A store the run is allowed to break

The fingerprint check protects the store of whoever is running it, which makes it the
one assertion that cannot be developed against a real product without putting somebody's
settings at risk. The fixture writes a store of its own and offers to mutate it on
request, so both the clean run and the caught mutation are observable without anything
real being touched.

### §WW101 The reference implementation of the surface protocol

The protocol exists in one product and would be copied into the next, which is exactly
how two implementations of one line format come to disagree. The fixture implements it
and this framework's own suite drives that implementation, so the protocol has an owner
- and an adopting project has something to copy that is known to be current.

### §WW102 A localized window, including the key that must be refused

The label rule needs several languages to be developed at all, and it needs one specific
pathological case: a key whose value carries a placeholder, which an exact-name read can
never match and which has to be refused rather than skipped. Real products have the
languages and rarely have the pathological key on purpose.

### §WW103 An intruder over a named region

The region check is the most intricate piece of the capture stack, and today it is
exercised by moving a window by hand and hoping. The fixture puts a topmost window over
a rectangle the caller names, so the intersection, the naming of the intruder and the
raise-then-refuse loop are all driven - including the case that must pass, an intruder
that overlaps nothing.

### §WW104 A surface drawn without automation peers

The geometry dump exists because some surfaces have no tree, and the only example
available today is an installer page in another repository, behind a compiler that has
to be installed first. The fixture draws one, so the dump and the layout invariants over
it are developable here rather than borrowed.

### §WW105 The fixture says what it can do

A catalogue that lives only in source is a catalogue nobody consults, and a flag nobody
knows about is a shape nobody tests against. The application lists every flag it has and
the list is asserted against the flags that exist - the same rule claude-tray applies to
its own preview catalogue, where an unknown name prints the whole table and exits
non-zero.

### §WW106 A shape exists because a defect existed

A fixture that grows shapes nobody can justify becomes a second product to maintain, and
then it drifts from the things it stands in for and starts producing false confidence.
Each surface names the real defect it reproduces. One that can name none is removed, and
the removal is itself a reading about what this framework no longer has to defend
against.

### §WW107 The fixture is also for a person

When a case fails, the fastest way to understand it is to look at the thing it is
talking about, and that must not require writing anything first. Every flag opens the
surface it names in a window somebody can see, which is the property claude-tray's
preview flags have and the reason its harness is debuggable at all.
