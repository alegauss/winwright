# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

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

### §WW389 the readings nothing composes

Five readings come off the same ledgers: WW298's series, WW248's *none of them is new*,
WW376's count of what the deeper window has not seen, WW281's split between the desk and
a budget, and WW363's rate on each excused line. Every one is right, and each knows when
to be quiet only because whoever added it read the ones already there.

The rules are written where they were added and nowhere together. WW363 is silent beside
the recurrence clause and below two; WW376 where WW248 spoke and where a row names no
case; WW281 where one kind is the whole set. A sixth reading has to find all of those to
decide its own, and the way to find them is to know they exist.

What that produces is a fragile sentence rather than a wrong one. Two clauses that both
fire say the same thing twice — the failure WW363 wrote its rule against — and the
reader who stops finishing it is the one the report is for. It is also how a reading
goes quiet for good: WW363's rate appeared on no run this session that *none of them is
new* had not covered.

So what is worth having is the composition in one place: which readings a run can make
of its excuses, over which window each speaks, and which is the stronger where two are
true. A list rather than a rule — the shape this project already has wherever two things
must agree.

### §WW396 the run that ended and would not say so

`run-tests-vm.cmd` was started with its output piped, on a host where the guest was
powered off, and printed nothing for an hour. At sixty-five minutes the host's process
table said the run was already over: the wrapper's shell had no descendants at all, so
the .cmd, the .ps1 and every vmrun they make had exited. The pipe was open and empty.

What that run does and a warm one does not is start the guest. `vmrun start` on a
powered-off VM launches VMware's own window, and that process outlives the script by
design — it is the console a person watches. It inherits the handles it was launched
with, so the write end of the caller's pipe stays open in a process nobody is waiting
for, and the end of file arrives when somebody closes the VM.

Re-run through `Start-Process -RedirectStandardOutput`, the same command reached the
guest in seconds and finished green, which is what makes the inherited handle the
suspect rather than the boot.

So it is a host-side hang on a run that worked, which is the worst shape available:
nothing is wrong in the guest, nothing is red, and a person watching an empty terminal
has no reason to think the suite has already passed. The wrapper is where the repair
goes — it is the thing a person types, and it can hand the guest's console a handle of
its own rather than the caller's.

## Block C — Locate — the locator grammar and the tree an agent reads

### §WW398 the second renderer, in a test file

`Inspect.Rendered` is deliberately the only walk that turns a tree into lines, and
`Render` is documented as its text — "so the step a reader is handed cannot drift from
the step the line opens with". WW382 wanted the same tree with the rectangle and the
window class taken out, because those are what two tray menu kinds are entitled to
differ in, and it got there by writing a second recursion in `NotificationAreaTests`.

It is eleven lines and it already disagrees with the first in a way nobody chose: the
elision marker is indented one way here and another way there, and neither is wrong
because nothing compares them. The next case wanting a shape copies whichever one it
finds.

There is no need for the recursion at all. `Rendered` returns a `RenderedLine` per
element carrying the element and its level, so a projection is a `Select` over what the
one renderer already walked — the same tree, the same order, the same elisions, and a
line built from whichever facts the caller wants.

What it would take is somewhere for that to live. A `Shape` beside `Render`, taking the
fields to keep, is one answer; a caller writing its own `Select` over `Rendered` is
another and needs nothing added at all. What decides is whether more than one case wants
it, and today exactly one does — which is the moment to move it, before the second one
copies the recursion instead of the line.

## Block D — Act — patterns before pointers

### §WW390 the promise no list holds

Two verbs in this engine answer a bool with an out for the value and an out for the
reason, and both got their annotation by somebody noticing the other. WW364 annotated
`Locator.TryParse` because four bangs in `StepDeclaration` had accumulated and WW351 had
just added one; WW377 annotated `Chord.TryParse` because WW364's own shipping made the
omission visible one verb over.

Nothing holds a third. A method written tomorrow with the same signature and no
attributes compiles, its callers spell bangs, and the argument for fixing it arrives the
way both of these did — as a bang somebody already wrote, months later, in a file about
something else. Each of those repairs cost a task.

The check is derivable rather than curated, which is what this project reaches for
whenever two lists could drift. Every exported method that answers a `bool` and carries
`out` parameters is findable by reflection; requiring `NotNullWhen` on the nullable ones
is one assertion, and a method that means something else says so in a list beside it —
the shape `MayAnswerYesOrNo` already has in `RecordedResultTests` for a neighbouring
rule.

Scope is what to decide first. The engine's two are the whole population, so the check
passes the day it is written and its only value is the third method — which is exactly
the value `Deadlines` and `Sleeps` have. Whether the fixture and the tools are in it is
the other half: a catalogue covering less than a reader assumes is worse than none.

### §WW394 the rung the ladder would misname

`Transfer.Verdict` says which rung a rate entered on, and says what that rung adds with
a switch over two cases and a default: `focus` names the focus taken every round,
`split` names End sent in a call of its own, and everything else names the read stopped
the moment the box says what was sent. That is right today for one reason — the search
runs over `Walk`, and `settle` is the only rung the default can reach.

WW381 added the fifth rung and kept it out of `Walk` on purpose, so nothing here
changed; reading the switch to check that is how this was found. A rung that did join
the walk — the next difference somebody finds between the arm and the act — would arrive
through the default and be reported under its own name carrying `settle`'s sentence. The
row above would say one thing and the verdict under it another, and the verdict is the
line a person reads.

It is WW354's failure in the one place that entry did not reach: a name in one list and
not in the other, answered rather than refused. The repair is WW354's own shape — the
sentence beside the rung it describes, so a rung declared without one does not compile
rather than borrowing its neighbour's.

### §WW395 the interval the ladder moves, spelled twice

WW381's `guard` rung is `settle` with the engine's own pause spent above the send
instead of below it, and the whole reading rests on it being the *same* interval: what
the pair answers is where the milliseconds go, and a rung sleeping a different number
would be answering how many.

The engine's copy is `Keys.FirstLookMs`, which is internal, so the rung has `GuardMs =
50` of its own and `run-typing.cmd` says "fifty milliseconds" twice in prose. Three
spellings of one number, and the two that matter are in different assemblies with
nothing between them. Move the engine's and the ladder goes on running, printing rows,
and reporting a placement at a price the engine stopped paying — a wrong answer with no
red anywhere, which is the shape this project keeps finding in its own catalogues.

`Spaced`'s interop is the precedent for the duplication and it is not the same case:
that is a send the engine does not make, deliberately reimplemented so the arm can vary
it. This is the engine's own number, borrowed to be moved.

The cheap repair is the one `PollMs` and `SettleMs` did not need: a case that reads the
constant and fails where the ladder disagrees with it. Reading it needs the field
visible to the tests, which is a smaller ask than making it public — and it is the
assertion, not the constant, that has to exist.

### §WW397 the baseline that stopped being a baseline

`transfer` refuses to attribute anything on a run where `arm` faults, and it is right
to: that rung is WW355's own reading, the one measured at 0 of 3200, and a ladder whose
control faults has no clean floor for the rungs above it to have departed from.

It faulted on both of WW381's runs. 1 of 1200 each time, and the rest of the rows moved
with it — 1, 0, 2, 0, 3 up the ladder on the first and 1, 2, 0, 4, 1 on the second, with
the control, the act and the candidate repair all inside the same handful. Nothing
separates at that spread, and two runs of it cost about seventy minutes of guest desk to
be told so twice.

So the instrument is currently unusable on this guest, and that is the finding rather
than an obstacle to one. WW368 read the same ladder clean on 2026-09-04, so something
about this desk changed inside a day: an update, a service, a snapshot that came back
different. Nothing here says which.

What it needs is a fact about the guest and not about the engine — what the fault's
floor is on this machine now, taken with the ladder or with any of the four arms that
used to read zero. Until that exists, every reading `transfer`, `provoke` and `sweep`
are asked for is a reading somebody has to throw away after the run.

## Block E — Capture — the picture that proves what it photographed

### §WW402 the flat surface a receipt cannot tell from a blank one

`CaptureReceipt` refuses a picture of one colour, and the sentence says why: it is what
a display that was rendering nothing copies as. That rule was written for the screen
route, where a flat rectangle means the copy reached a surface nobody was drawing, and
it is right there.

A render is not a copy. WW385 asked the application to draw a closed popup, the
application drew it, the file was a correct 90x40 picture of a Firebrick rectangle, and
the receipt failed the case. The picture was of exactly what the popup held.

The fixture was changed, because the fixture was standing in for a surface and a real
flyout holds more than one colour. But a real one can hold exactly one — a colour
swatch, a progress fill, a blank canvas an application draws on demand — and an adopter
photographing one gets a red whose sentence is about a display, in a route where no
display was involved.

What separates the two is the route, which the receipt already carries. On the render
route there is no screen to have been blank: the tree was walked, the size came back,
and a single colour is a fact about the surface rather than a symptom. The reading is
worth keeping either way — a flat render is still worth saying out loud — and what it
should not be is a failure about a display in a picture no display took.

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

### §WW391 the field that joins in five places

WW351 made a *claim* join the format by being a field, and WW378 closed the last place
that had not caught up. A *field* still joins in five: a property, a parameter on `Of`,
a line in the construction, a schema row, and a read plus an argument in the loader.
Adding `popup` paid all five, and nothing but a case holds the schema and the loader
together.

`Of` is at twenty-nine parameters because of it, which the analyser reports on every
build and nobody has filed. WW352 took the constructor from twenty-three to an
initialiser and left the verb, so what remains is the half that grows — and `Trayed`
threads twenty-five of the same values through a second signature beside it.

The shape a step is built from already exists: the loader reads named fields out of a
document and the schema declares what those names are. What is missing is the step
taking that shape rather than a positional argument list, after which a field is a
property and a schema row — two things already held to each other.

What makes it worth deciding rather than doing is the refusals. `Of` is where every rule
about a step runs, and several read the raw text a case wrote rather than what the step
holds: `reads` is the one every family asks for, and WW378 spent a task moving one
family off the parameters. A different door has to carry those, and which ones is the
design.

### §WW393 the arm nothing has ever run

Three tasks have now been spent on the arms and not one of them ran an arm. WW354 held
the names to `run-typing.cmd` in both directions, WW367 made each carry the code it
runs, and WW380 made that code nameable so a case can say which runner an arm reaches.
Every one is a claim about shapes: a list, a constructor parameter, a delegate's
declaring type.

`TypingArmTests` says why, and the reason is right: the tool takes the desk for minutes
and a guest run should not pay for a question asked once. What that sentence has come to
cover is something else — no line inside any runner has been executed by anything but a
person. A runner that throws on its first statement compiles, satisfies all three
checks, and waits for whoever next measures.

The reason does not hold at one round. Every runner takes its count as an argument: at
one round `sweep` is eighteen, `provoke` eight, `transfer` four, `delay` and `acts`
three — seconds of desk, and every line of every runner reached. What costs minutes is
the measurement, not the code that takes it.

So what is worth deciding is whether one round is a case or a command. A case in the
suite would find a broken runner on the run that broke it; a `--smoke` a person types
finds it when somebody remembers. WW368 spent thirty minutes learning that `transfer`
worked at all, which is the price of the second answer.

### §WW403 the label a criterion is filed under

`Criteria.Known` is a list of blocks and leads, grouped by comment into A through K, and
it has held every criterion this roadmap declares since WW176. WW384's partial ship
raised the first one that is not a block's.

roadkeep files a criterion raised by an open line under a heading naming the task, so
the roadmap declares it as `WW384` and the catalogue was given `J`. Both gates fired —
one saying the roadmap declares something nothing here mentions, one saying something
here is not in the roadmap — and between them they name the fault exactly. That is the
catalogue working.

What is missing is anywhere that says the label can be an id. The type's own field is
documented as "the block it binds, as the roadmap labels it", which is true and reads as
a block letter; the list is grouped under block comments; and the next partial ship will
put its criterion under whichever letter looks right and cost another guest run to find
out.

It is a comment and a sentence, or it is a smaller field: `Block` could be `Under`,
which is the thing it has always held. Either is cheap, and what decides it is whether a
criterion bound to a task should sort with its block's or stand apart — which is a
question about how the list is read rather than about what it holds.

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

### §WW387 the answer with nobody to give it

WW374 was filed with two candidates and only one exists. The second was a sixth answer
meaning *the half is here and this window is not hooked yet*, and there is nobody to
give it: the harness sends `WM_COPYDATA` to one window, and where nothing is hooked
there no code of the in-app half runs at all. `Renders.Everywhere` does not change it —
it hooks per window on `Loaded`, the very event the gap waits for.

The half's own comment says so about the why ask: *telling the two apart needs the
process-wide hook first*. There is none. What `Everywhere` gives is a class handler
hooking each window as it loads — a per-window hook arriving later, which is the thing
WW374 waits for rather than one that could answer for it.

So the wait is what there is, and it costs what a wait costs: an application with no
in-app half now spends two seconds per capture step being told the truth about itself.
That is the right trade at one capture and the wrong one at forty, and forty is what an
adopting suite has.

What removes it is a reading per process rather than per window: one hook answering
*this application has the half* whatever window is asked about, put up by `Everywhere`
on a message-only window of its own. The gap becomes a question with an answer instead
of a duration, and no run waits to learn what the application could have said at once.

## Block J — Adoption — the proof is the deletion

### §WW83 The switch case rewrites a real setting

Until that case exists, the path that rewrites the setting, re-keys the stores and takes
the other account's token runs under no check at all. It is refused against a resident
process, because a pick there would repoint the real icon for real. Running it inside
the store comparison asserts the promise that a run touches nothing at the one place
most likely to break it.

The engine half has landed. Three things had to exist first, and each was measured
missing on this menu: a locator matching the front of a name, because an entry reads
`Pessoal — used 41%  · active now` and equality addresses it on no machine; a reading of
the sentence an element says beside its name, because the accessible object carrying
that sentence at all costs the entry its toggle pattern; and a claim about the front of
that sentence, because the state is announced as a word in front of free text that may
contain the word again. `open submenu` learned which entry it is about at the same time
— it pressed Right at whatever the menu opened on, which is never the fourth entry.

What is left is the adopter's, and waits on a publish rather than on a decision. The
case, the `other-profile` read-out that names an end no case may type, and the store
bracket around the suite are written; the cases project restores the engine from
nuget.org, and the three fields are not in the published version.

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

### §WW384 the repair nothing has watched

WW371 put a second guest-side script beside the probe, and it arrived in the state the
probe was in before WW345: the decision is a function cases can call, and the acting
half is run by nothing but a real guest. `Test-Clearable` is driven with styles somebody
typed; what happens when the script meets a window — the minimise, the foreground handed
on, the sentence it writes — is exercised only by the runner refusing a run.

That is the shape both of this probe's defects had. WW345 made the classification
runnable and a look built wrong still classified perfectly; WW357 closed that. The
clearer has the same room: a `ShowWindow` on the wrong handle, a foreground handed
nowhere, or a sentence that says it worked would each leave the desk as it was — and the
run after would refuse under a line saying the clearing had happened.

Half of it is writable today. `PumpedDialog` is `WS_POPUP` with no minimise button, so a
case can put it up, run the clearer on the desk it is holding, and assert the script
left it alone and said so — which is the arm that must never move a window.

The other half needs a window this suite does not build: one with `WS_MINIMIZEBOX`, put
up, cleared, and read back as gone from the foreground. That is a fixture window and a
line of style bits, and it is the arm that decides whether an unattended run can start
at all.

### §WW386 the wait the runner does not bound

WW373 bounded a case and left the run around it unbounded. `run-tests-vm.ps1` starts the
suite in the guest and waits for it to write an exit code; nothing there says how long
that may take. What ends a wedge now is the suite's own timeout, and that only works
while the wedge is inside a case.

Everything outside one is the old shape. A guest that stops answering vmrun, a build
that hangs on a restore, a testhost that dies without writing the exit file — each
leaves the host command waiting with no bound, which is what `Start-Guest` refuses to do
about `vmrun start`: ten silent minutes and a wedge look alike, so it polls. The run it
launches inherits none of that.

The numbers are in hand. A guest run of this suite is seven to fifteen minutes and the
carry adds one, so a whole run has never taken twenty; the bound wants to be an hour or
so — several times the longest, the same margin WW373 gave a case, because a bound that
decides a red is worse than none.

Beside a number it needs a reading. A run that hit the bound has to say what the guest
was doing, or it is WW371's refusal in another form — an operator sent to a console. The
desk probe is already carried and already answers, so the shape is: stop waiting, read
the desk, bring the log back, refuse with what both said.

### §WW388 the desk two readings both answer

WW371 and WW375 landed an hour apart and answer the same desk two different ways. A
minimised window holding the foreground is `stale` — not a question, so the run goes on
— and it is also exactly what `desk-clear.ps1` puts away: it has a minimise button, it
is already minimised, and handing the foreground on is the half that repair exists for.

Nothing decides between them, and the order settled it by accident. `stale` is
classified before the `asking` arm, so the desk WW371 was filed about never gets there:
the Edge window that refused every run is reported and stepped over, and the clearer
runs only for a window that is not iconic. WW384 already says nothing drives that path.

The run going on is right and is not the whole answer. WW331 accepted the same for a
focused taskbar — the first case to take the foreground clears it — but the first case
is not always one that takes it. A case reading the foreground as a precondition sees a
window that is neither the desktop nor the one under test, and excuses a check over a
desk this run could have cleared in a second.

So what is worth deciding is whether `stale` should be cleared as well as said. The
runner has both readings and both tools in hand; what it does not have is a sentence
about which desks it is willing to tidy before a run, and which it only reports.

### §WW399 the container an adopter must not name

WW382 read both tray menu kinds and found the entries identical and the containers not:
a `TrackPopupMenu` reports its `Menu` with a name and a WinForms drop-down reports one
with none. Everything below is the same — two `MenuItem`s named as the adopters name
them — so a locator that starts at an entry is proven against both kinds by either, and
a locator that starts at the container is proven against neither.

That is a trap in the shape adopters keep walking into. WW322 exists because three
adopted cases failed for weeks on the desk half of this difference, and the tree half is
easier to hit: the container is the first thing the inspector prints, its line is
written to be copied, and copying it from a Win32 tray produces a locator that matches
nothing on a drop-down and says "nothing answered to it" — the sentence WW356 spent six
guest runs inside.

The case that measured it is where the fact lives, and an adopter does not read this
suite. What they read is the inspector's output and whatever this project tells them
about locators. So the fix belongs on one of those two: a line in the guidance saying a
tray menu's container is named by the shell in one kind and not the other, or the
inspector itself marking the line as one not to start from.

Which of the two is a question about how much the inspector should know about menus.

### §WW400 the arms a real look still cannot reach

The probe writes six words and the runner switches on all six. Three are now produced
end to end from a window this suite put up: `asking` by WW357, nothing by WW370's
skipped look, and `shell` by WW383. The other three are still only ever made of looks
somebody typed, which is the state each of the joined ones was in when a defect was
found in it.

`stale` is the one worth doing and the cheapest. WW375 put `Iconic` on the look because
the classification is a pure function of what the loop returns, and nothing has ever
read that field off a real window — a loop that answered it wrong would send a reader to
a guest console to answer a window nobody can see, which is the failure WW375 exists
for, arrived at from the loop instead of the words. A case owns its dialog and can
minimise it.

`clear` is one line from where WW370 stopped. That case asserts the looks come back as
nothing and never hands them to the classification, so "nothing but the desktop held the
foreground" is a sentence no case has produced from a real poll.

`broken` cannot be arranged and should not be pretended: it means a logged-in desk with
no shell, and a suite that could make one would have nowhere to run. `busy` needs the
foreground taken between two looks, which is a race rather than an arrangement.

### §WW401 the red that names the wrong thing

This project's rule about a green covering an assertion that did not run has a mirror
nobody has written down. WW384 put a case in that showed the desktop, and Windows'
foreground lock then refused every later request for it — measured twice, and both runs
cost the same five cases in `ContainsTests` and `ChordTests`. Every one of them failed,
and none said anything about a desk. The one printed was `Assert.Contains() Failure:
Sub-string not found`.

A reader handed that goes looking at `Contains`. What actually happened is that the keys
went to explorer, the read-out never changed, and the assertion compared two strings
that were both honest about a window nobody had typed into.

The suite already knows how to say this. `BusyDesk.Excused` turns a lost foreground into
a hole naming the desk, and the cases that lose it *before* acting take that door. What
has no door is losing it *during* — the precondition was met, the act ran, and the
reading came back off a desk that had moved underneath it.

Which is the excused check's own argument one step later: a reading taken after the desk
moved is not a reading about the subject. The repair is a second look, and where the two
disagree the verdict is a hole rather than a failure. The cost is one foreground read an
act, against a class of red that sends a reader to the wrong file.

## Block K — The proving ground — a fixture app built to be hard to test

### §WW392 the five lists a pane joins

Adding one pane failed the suite on a catalogue rather than on behaviour.
`SurfaceCatalogueTests` named a type the fixture carries with no row saying which flag
reaches it; a second red was avoided only because the same edit had already touched
`FixtureTests.Value`, the switch supplying a numeric flag's value. Both lists did their
job.

What is worth noticing is how many there are and that nothing names them together. A
pane joins in five places — the file, a `Flags.Known` row, a line in `MainWindow`, a
`Surfaces` row, and a value in that switch — and the only way to learn the set is to add
one and read the reds. WW391 filed the same shape for a step's fields; this is the
fixture's.

The reds are cheap and arrive one run apart, which is the cost: a guest run is ten
minutes, so learning a five-place list by failing it is most of an hour. Two of the five
need no run at all — `Surfaces` and `Flags` are both read by reflection over one
assembly, and a pane with a flag but no row is knowable at build time.

What to decide is whether that is one check or a note. A single case asserting the five
lists agree would replace three, and it would be the place a person adding a pane is
sent — which is the thing missing now, since the current answer is a run that fails,
then another.
