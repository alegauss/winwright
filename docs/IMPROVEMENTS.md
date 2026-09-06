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

### §WW407 the reading its own neighbour covers

WW363 reads how often one case was excused across the deep window, and it is silent
where the recurrence mark spoke — the right rule, because two fractions about one case
on one line is a line nobody finishes. WW376 is silent where *none of them is new*
spoke, for the same reason.

What WW389 makes visible by putting them in one list is that this suite's ordinary run
silences both. Its steady state is that every excuse it makes recurs in every earlier
run, which is the exact condition the two loudest clauses fire on — so the rate appeared
on no run of a whole session, and the count against the deep window appeared on none
either.

That is not a defect and it is not nothing. Each is correct and each is dead on the runs
a person actually reads; both come alive only on a run that already looks unusual, which
is the run whose reader least needs a second opinion.

Two ways to weigh it, and the list is what makes either arguable. Either the precedence
is right and both are worth keeping for the rare runs, in which case what to add is a
case that goes red when one has been quiet for a whole window. Or the composition should
change: the rate is the reading that sees a slope, and a slope on a recurring case is
what the mark above it cannot show.

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

### §WW408 what a sweep means by shipped

Every reflection sweep here begins by saying which code it is about, and each says it
differently. WW390's reads the engine and the in-app half, because those are what
somebody else writes against. `RecordedResultTests` reads one assembly and narrows to a
namespace. `SourceSweepTests` walks files. None is wrong and no two agree.

What that costs is paid at the moment a sweep is written, which is the moment its author
has least to go on: WW390 spent a paragraph arguing that the fixture and the tools are
programs whose bangs their own author can see, and the next sweep will argue it again,
possibly the other way. A catalogue covering less than a reader assumes is worse than
none, and nothing here says what a reader should assume.

The fact is small and stable: two assemblies are consumed by code this project does not
own, and two are programs with one caller each. It belongs somewhere a sweep can read it
rather than in the sweeps that have already chosen — `Checkout` is where the equivalent
fact about paths lives, so there is already a place for it.

What it would settle is not this rule but the next one's first paragraph. A sweep would
name its coverage by pointing rather than by arguing, and one that meant something
narrower would say so against a list rather than in place of one.

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

### §WW404 the file nobody says who is holding

The sync deletes the guest tree and writes it again, and where something has a file open
in it Windows refuses with a sentence naming the directory. That sentence is the whole
of what comes back: `Remove-Item : cannot remove the item C:\src\winwright: the process
cannot access the file`.

Which process is the only question a reader has, and the guest already knows. WW386 made
this reachable twice over — the bound leaves whatever was running exactly where it is,
and the refusal now says the next sync will meet it — but a person who runs again anyway
gets the path and no name, and a person whose guest is holding it for some other reason
gets nothing at all.

This is the same shape as every other refusal here. WW371's clearer names the window it
left alone and why; the probe names the process holding the desk, its pid and its class,
because WW331 proved a state without a name sends a reader to a console. The sync is the
one refusal in this file that answers with a path.

What it would take is a handle reading in the guest — `openfiles`, or a walk of the
processes whose working directory is under the tree — run on the failure rather than
always, and folded into the same sentence. Nothing about it needs a new door: `sync.ps1`
is generated here and already reports back through `sync.log`.

### §WW405 the process the suite cannot have two of

A run drives an application in a process of its own, and every reading the engine takes
about "the application" means that process. This suite drives its fixtures in the
process running the cases, which is why WW247 moved three of them in — and it means one
process holds the harness, an application with no in-app half, one armed, and one about
to arm, all at once.

WW387 found the edge. A memory keyed by process id is the right key for a run and the
wrong one here: a fixture with no half taught the harness a sentence, and the harness
then said it about a different fixture's window that was seconds from answering. The
case that caught it is WW374's, which had nothing to do with the change.

The repair was to key by window, which is sound and buys less. What is not repaired is
the shape: the next reading the engine wants to hold per application meets the same
wall, and nothing says so before the guest run does.

Two doors, and they are not the same size. A fixture in a process of its own is what
`Winwright.Fixture` already is, so a case wanting one has one — at the cost of a launch.
Or the suite says out loud which readings it cannot prove, the way `Criteria` says which
claims nothing shows, so the wall is met at the design rather than at the red.

## Block K — The proving ground — a fixture app built to be hard to test

### §WW406 the exception that took the run with it

A guest run died 48 seconds in: *Falha no processo do host de teste :
UnrenderableException: Border 'sizelessPane' laid out to 0x0*. The suite reported 1121
of 1121 passing and the roll call refused it — 904 of 2025 were never recorded at all —
which is WW117 working exactly as it was built to. Then Blame waited out its ten idle
minutes and dumped, so the run cost twenty.

`SizelessPane` exists to provoke that refusal and the fixture raises it on purpose, in
its own process, exiting 3. Nothing here says how one reached the test host. No case
builds that pane in this process, and the last case to answer — `SuiteRunTests` — has
nothing to do with rendering, so what the trace names is the thread that died rather
than the case that armed it.

It did not reproduce: the next run passed 2025 of 2025 with nothing new excused, which
makes this a rare fault rather than a broken build.

The dump was the evidence and it is gone: it was written under the guest's own tree,
which the next run's sync deletes before writing it again. So what this needs first is
not a diagnosis but somewhere to keep one — a hang dump belongs where the trx goes, on
the host. Then the question: which thread raises this where nothing catches it, and
whether the rule `Renders` already states — never raise out of a window procedure —
belongs somewhere else too.

### §WW409 the shapes the driver steps over

`Every_shape_that_draws_opens_a_window_somebody_can_look_at` drives every flag the
catalogue lists and skips the ones marked `[draws nothing]`, which is right: a shape
that opens no window cannot be asserted to have opened one.

What nothing says is what those shapes do instead. `--render` writes a file and exits;
`--sizeless` and `--blank` do the same through it and exit 3 and 0; `--flags` prints and
stops. Each is exercised somewhere — `ProvokedByFlagTests` drives two of them and reads
the exit code — but the pairing is the reverse of the drawing one: there, a shape added
tomorrow is driven because the case reads the catalogue, and here it is skipped because
the case reads the catalogue, and nothing notices that nobody else picked it up.

WW392 met the clause from the other side. Its value rule had to learn that a non-drawing
flag needs no value, which is the same fact read for the opposite purpose — and the way
it learned was a red naming `--render`, which is exactly the cost that entry was about.

The shape of the answer is the one this project keeps reaching for: the catalogue
already divides the flags, so the skipped half is a list, and a list nothing claims
about is the thing to fix. Either each names the case that drives it, the way a shape
names the flag that justifies it, or the count of them is asserted so the fifth arrives
as a red rather than as a silence.
