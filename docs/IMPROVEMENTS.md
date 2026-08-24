# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

### §WW190 The guard nothing requires

`BusyDesk` is how this suite answers a desk it did not get, and WW172 applied it across
every case that needed it at the time. What it did not do — because it could not — is
make the next case use it.

Now measured rather than counted. Holding the guest's desk after WW179 shipped produced
nine failures, and every one is this: `NotificationAreaTests` six times, over the
taskbar holding no icons, the chevron being absent, the overflow refusing to open twice,
a search that could not look everywhere, and a menu asked of an icon nothing could find;
`ForeignTreeTests` once, over the shell's own tree; `TraversalTests` once, over what
holds the focus. Not one was a broken harness — WW179 closed that door — and not one
excused the desk.

Three of the nine were written after WW172, by somebody who had just applied the rule
elsewhere. The rest predate it and were simply never reached. Each is a
three-and-a-half-minute run to find, and that is the cheap outcome: the expensive one is
the same case passing on a quiet desk for a month and then failing on somebody else's.

The shape is the one this repository keeps reaching for. `Deadlines` pairs every wait
with what its look answers as nothing, read out of the sources. The same reading finds
every case that asks a desk-dependent reading for a verdict and pairs it with how it
excuses the desk — or with a stated reason it needs none.

### §WW191 The rule that would not have caught its own defect

WW182 shipped a check: every suite reading that answers a verdict answers the engine's
`Finding` too, so the third state is a property of the type rather than something an
author remembers. It is worth stating what that check would have done about WW181, which
is nothing.

`TrayGhosts.Showing` answered `IReadOnlyList<string>` and a static `Sentence` taking
one. No verdict, no `AsAssertion`, nothing for the sweep to find — and a case asserted
on the list directly, which is how a reading that had never looked reported a clean
desk. The verdict arrived later, in the repair. So the rule keys on the thing that was
added *after* the defect.

That is not an argument against the rule. A reading that answers a verdict is one a run
counts, and governing those is worth doing. It is an argument that the boundary is drawn
at the wrong moment: the hazard begins when a case asserts on a reading, not when the
reading learns to produce a verdict.

What is owed is the earlier boundary, and a way to draw it that is not a guess. A
reading in this suite that swallows an exception and answers a value has a third state
whether or not it has anywhere to put one — and finding those is the same source-read
`Deadlines` already does for waits.

### §WW192 Three unchecked, and no word on whose fault

WW183 gave this engine something it did not have: a written judgement about which of its
conditions are the desk's, and why each one is. Only the suite reads it.

`VerdictSummary` is where it is worth reading. The headline says how many assertions
never ran and the detail names each with the precondition that was absent. What it does
not say is the thing that decides what the reader does next. Three holes because a
foreground was not granted and a window stood over a capture is a machine to clear.
Three holes because a binary was stale and a page was still computing is a repository to
open. The exit code is `2` either way and the lines read the same shape.

`SweepSummary` needs it in the same commit, and that is written here because the
alternative has already happened once: WW177 joined the reading to the verdict for a
single run, the sweep did not get it, and WW185 was the repair. A division shipped for
one summary and not the other is that split again — and a sweep is where whose-fault
matters most, because a hole in two of five environments is a question about those two
machines.

This block's first criterion is that a degraded run is legible without reading the log,
and legible means a reader can act. What is owed is the division, in both headlines,
with an honest third bucket for a hole whose condition this engine has not classified at
all.

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

### §WW193 The walk that is about to exist three times

`Deadlines` and `Sleeps` both read the sources: walk up to the solution file, enumerate
`*.cs` under `src` and `tests`, skip `bin` and `obj`, skip the file that names the thing
it is looking for, count occurrences, cache it once. The rules they apply are different
and should be. The walk is the same twice.

It is worth extracting because of how the first one went. `Deadlines` shipped recursing
into `bin` and `obj` — thousands of files instead of two hundred, inside a suite whose
other cases are waiting on five-second deadlines — and the guest went red twice with two
different timing failures before the cause was found. The run went from 2m50s to 3m53s
and back. That was one copy getting one exclusion wrong.

There is already a third asked for. WW190 says in as many words that the reading which
finds every case asserting a desk verdict without excusing the desk is "the same
source-read `Deadlines` already does". Writing it against a shared walk costs nothing;
writing it against a third hand-rolled one is a third chance at the same hour.

What is owed is the walk alone — the trees, the exclusions and the caching — with each
catalogue keeping its own question. A shared answer would be the opposite of what this
is for.

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

### §WW187 Two refusals behind an optional argument

WW38 and WW41 gave this block two readings a capture needs, and WW40 gave the receipt
the refusals that fire on them. Both arrive as optional arguments. `CaptureReceipt.Of`
has four now, and a caller who passes neither gets a receipt that refuses nothing and
records honestly that nobody asked.

Recording it honestly is the part that works. What does not is that nothing asks. This
project already argued the case, in `Preamble.Of`'s own comment about the readings a run
takes: a reading reached by its own call is one a runner is free to forget, and the
forgotten one stops being measured while every assertion that needed it starts passing.
Two of these are exactly that, and the third and fourth are coming — WW42's flat colour
and WW43's page still computing are two more questions a capture has to be asked.

So the composition belongs where WW170 put the run's: in a verb that takes both halves
at once. A capture taken through it reads the region and the glass, refuses on either,
and carries what it read into the receipt either way — and a caller who wants the pieces
separately still has them.

What is owed is that door, and the decision about what it does where a reading cannot be
taken: a compositor that would not answer is not a window with no backdrop, and the
receipt already knows the difference.

### §WW188 Five refusals wearing one name

`Provocation` pairs every refusal the framework names with a fixture flag or a stated
reason, checked against the assemblies in both directions. It reads exception *types*,
and that was right when a type was a refusal.

`WrongCaptureException` is now five. It fires on a window belonging to another process,
on a window nothing is drawing, on a region another window stood over, on a window whose
glass transmits, and on a picture of exactly one colour. The catalogue carries one
entry, naming one case, and its shape cannot carry more: an entry holds a flag or a
reason and never both, which WW40 already had to write into its prose because there was
nowhere else to put it.

So Block K's first criterion — every refusal has something that provokes it, checked
both ways — is now true of one arm in five here. The other four are provoked, and by
real fixture shapes: `--intrude` for the region, `--backdrop` for the glass. Nothing
reads that back, and a sixth arm added next week is a refusal nobody has to provoke.

What is owed is the finer unit. A refusal is what a reader meets, and a reader meets an
arm rather than a type — so the pairing is by the sentence a refusal is thrown with, and
the count stops being one per class of thing that can go wrong.

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
