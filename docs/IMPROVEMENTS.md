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

### §WW436 the read that waits for a thread this process owns

`TopLevelWindows.OfProcess` walks `EnumWindows`, keeps the windows owned by the process
it was asked about, and calls `Win32.TextOf` on each of them — which is
`GetWindowTextW`.

For a window belonging to another process that reads the cached title and returns. For a
window belonging to the *calling* process it sends `WM_GETTEXT` and does not come back
until that window's own thread pumps for it. There is no deadline on it and no argument
to give one.

This suite hosts its fixture windows inside the test host, so a case asking for the
windows of its own process asks about windows whose threads it also owns — and this
repository parks threads on purpose, because a window that stops answering is a thing it
exists to test. Both kept dumps say the same: `Win32.TextOf` under the `EnumWindows`
callback, under `CaseRun.Captured`, under the captures case. The bound then kills the
host and takes nine hundred cases with it, which has happened three times in about
fifteen guest runs.

The repair is `SendMessageTimeout` with `WM_GETTEXT`: `GetWindowText` with a deadline on
it, and the documented answer to this hazard. A window that does not answer inside it
has no title as far as this reading is concerned, which is already what `TextOf` hands
back for one that answers nothing.

What the design owes is that deadline. It would be the first number here deciding
whether a window is described or passed over, and every other wait in this engine argues
its own.

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

### §WW429 a floor measured on one machine

WW397 measured this guest producing four substitutions in 3600 rounds of a control that
does nothing. WW413 turned that into `Enough.Faults` — five on the leading side before
an attribution is allowed — and the number's whole justification is that one
measurement, on one machine, twice.

The tool is not this repository's private instrument. It ships in `tools`, it is driven
by a .cmd a person types, and the thing it exists to answer is what a desk does to a
send — which is the property most likely to differ between desks. A machine with a floor
ten times this one's would pass the guard on noise; one with none would be refused a
real reading of four.

`transfer` already knows the answer. It runs a control arm that does nothing and reads
that arm's rate, which is the desk's floor measured by the run that is about to be
judged against it — the reading WW397 took by hand, taken every time.

So the shape is there and two arms cannot use it: `sweep`'s three arms all type, and
`provoke`'s control answers a different question. What each would need is a round or two
of doing nothing before it starts, which is cheap next to what they already spend — and
then the floor in the verdict is this desk's rather than the one the tool was written
on.

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

### §WW434 the third family that did not move

A step has three families of fields that are one claim spelled several ways. WW275 and
WW292 gave `covers` its two relatives; WW308 gave `sameAs` its three; WW83 gave `label`
its two, `notLabel` and `beginsWithLabel`.

WW391 made the first two a list apiece. `Comparisons` and `Sweepings` each hold the
spellings in the order the fold takes them, beside what each one means, and everything
that needs either — the property, the mode, the claim set's naming, the refusal that
says which key to delete — walks that one list. A fourth spelling joins by being a row
in it.

The label family did not move, and it is the family with the most places to be told.
Each spelling is its own property, so three; `Claims` picks the field name out of a
ternary chain; and `RefusesTwoStringClaims` builds the list again to say which two were
written. A fourth would join in three of those and would compile without the fourth,
which is the shape WW323 and WW340 each closed once elsewhere.

What holds it back from being the same change is that these three are not folded.
`Sweeps` and `PointsAt` each hold one value with a mode beside it; the three label
fields are three values the run reads separately, and `CaseRun` resolves a different
string for each. So the list is what they share and the fold is not, which is what a
design has to answer: whether a family can be one list without being one field.

### §WW435 the two shapes the walk did not reach

WW58 made the format data and WW66 made the loader ask it: whether a field is text or a
flag is said in the schema row and read from there, so what a tool publishes and what a
run enforces cannot differ.

WW391 took that further for one of the three shapes. `OneStep` walks
`ScenarioSchema.Step` and reads each field by what its row says it holds, so a field
added to the schema is loaded without the loader being told — which is why a step's
fields now join in two places rather than five.

The other two did not move. `OneCase` names nine keys and the fixture reader names
eight, each on its own line, each spelling a key the row beside it already spelled.
Nothing holds the two lists together but a case, and what that costs is the failure this
format exists to refuse: a row added to the schema and not to the loader is a key an
author may write, a tool will publish, and the run will ignore.

What makes it a different task and not a repeat is the kinds. A step's fields are text
or a flag, and the walk is a two-armed switch. A case carries `steps`, `tags` and
`needs`; a file carries `cases` and `fixtures`. Those are arrays of shapes and arrays of
words, read by verbs answering different types — so the walk needs somewhere for a kind
that is not a value, and that is the design.

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

### §WW430 the third of the suite that runs twice

WW417's gate runs 738 of 2061 cases on the host in four seconds. The guest then runs all
2061, including those 738, and they cost it whatever they cost — a minute or two of a
run that takes ten to seventeen.

The obvious saving is not available. WW117's roll call refuses a run where the cases
discovered and the cases recorded disagree, and that refusal is the reason a green here
means what it says: it caught a test host that died with 900 cases never attempted. A
guest told to skip 738 would have to be told, and a mechanism for telling it is a
mechanism for telling it the wrong number.

There is also a reason not to want the saving. The gate and the guest run the same cases
on two machines, and twice this session that has been the finding rather than the waste:
a case that passes on the host and fails in the guest is what this repository exists to
notice, and the desk-free half is only desk-free by declaration.

So what is worth having is the measurement rather than the skip. Nobody knows what those
738 cost in the guest, because the suite reports one duration. If it is twenty seconds
the question is closed; if it is two minutes, the roll call could carry the gate's own
reading — the same cases, answered on the host — which is a different claim from
skipping them and one WW117 could still refuse.

### §WW431 the case that could have answered here

WW417's gate runs the classes outside the serial collection, and that is the only
division this project has: a class needs the desk or it does not. It is the right unit
for the collection, which exists to stop two classes fighting over one foreground.

It is the wrong unit for the gate. `NotificationAreaTests` is serial because most of it
drives a real tray — and one case in it reads `SKILL.md` and asserts a sentence. WW414
changed that sentence, the gate passed, and the guest reported it eleven minutes later:
the exact cost WW417 was filed to remove, met inside the task that removed it.

The same shape is elsewhere. `DeskProbeTests` is serial and holds the cases that read
the runner's source; `FixtureTests` is serial and holds the ones that read its
catalogue. Each is a case that could answer on any machine sitting in a class that could
not.

Splitting by case rather than by class is what the gate wants and xUnit does not offer
against a collection. A trait would: a case that needs no desk says so, the gate filters
on it, and the collection goes on meaning what it means. What has to be decided is which
way round the trait goes — marking the desk-free cases is the smaller edit and the one
that fails safe, because a case nobody marked stays in the guest.

### §WW432 the file the second caller assumes is there

`holders.ps1` reaches the guest with `source.zip` and the generated scripts, which is
the right place for it: the sync is the step that needs it, and by the time the run
starts it is there.

The bound is the second caller and it does not have that guarantee stated anywhere. It
fires during a run, and a run has been synced — true today, and true because of an
ordering nothing holds. `Invoke-OnTheDesk` takes a bound too, and the desk probe that
runs before the carry uses the same function; give that one a `-Minutes` and the refusal
asks a guest for a file no sync has put there.

What it answers then is honest: `Get-WhatHoldsGuest` returns "the guest could not be
asked" with vmrun's own words, which is a sentence and not a crash. So this is not a
defect waiting to bite — it is a claim about ordering that only the code knows.

Two ways to close it. The walk could be copied when the sync folder is first made rather
than beside the tree, which is one line earlier and removes the ordering entirely. Or
the runner could say what it depends on: WW420 wants a case that drives the failure
arms, and a bound fired before a sync is exactly the kind of arm that case would run —
where today it would prove the sentence rather than the walk.

### §WW433 the classes the gate cannot see

`host-gate.ps1` says what it takes and why: a class that needs the desk carries
`[Collection(WindowFixture.Serial)]`, a class that does not, does not. It derives the
list rather than writing one, because "a class left off the gate is a class the gate
silently stops covering". Its regex is `^public sealed class (?<named>\w+)`.

Twenty-four of this suite's public test classes are `public class`. They sit outside the
gate for a keyword that has nothing to do with the desk, and nothing reports it: the
gate prints a pass over what it ran, and a class it never saw looks exactly like one
that passed.

Measured while shipping WW391, which changed the scenario format and wanted the cheap
half first. The gate answered 744 cases; the twenty-four unsealed classes answered 278
more in 606ms, and `StepDeclarationTests`, `ClaimsTests` and `ScenarioFileTests` are
among them — the three most likely to be red after a change to the format. The cases
missing from the saving are the ones that catch the edit.

Two ways to close it and they are not the same. Sealing the twenty-four makes the gate
right by changing the suite, under a rule nothing states. Matching `public (sealed
)?class` makes the gate read what its own prose says, which is what the script argued
for — and a case holding the gate's list against the suite's is what keeps either from
drifting again.

### §WW439 the numbers WW416 did not catalogue

WW416 catalogued the words that cross between the machines and held them both ways. The
numbers beside them did not move.

`sync.ps1` exits 91 where the guest has no SDK and 92 where the tree would not delete.
Both sit on the same line as the marker saying the same thing, and neither number
appears anywhere in the host's half: it takes whether the code was zero, then decides
which failure it was by matching the log. The marker is the protocol; the number is a
second copy nothing asks for.

Worse than unused, it is unestablished. `runProgramInGuest` is what would carry a guest
program's code across, and no case and no run has established that 91 and 92 arrive here
at all — a number nobody reads is also one nobody has watched survive the crossing.

Two ways out and they differ. The numbers could be read: the host would switch on the
code, and the markers would carry only what a number cannot, which is Windows' own
sentence about the file that would not delete. Or they could go, leaving the log as the
one protocol and a single non-zero code, which is honest about what is used.

The first is better if anything but this script ever runs `sync.cmd`; the second is
better if nothing ever does. Which is true is the design, and it is a question about who
else may drive the guest.

### §WW441 the gate that cannot tell a host with no toolchain from red cases

`Invoke-HostGate` in `tools/host-gate.ps1` answers `Ok` from the exit code of `dotnet
test` and nothing else, and `run-tests-vm.ps1` turns any non-zero code into one refusal:
the desk-free half of the suite is red on this host, and the guest would say the same.

That is true only where cases ran. Measured on 2026-09-14, during WW419: an update had
removed the host's SDK 10.0.303, `global.json` pins it with `latestPatch`, and `dotnet`
exited 155 with "A compatible .NET SDK was not found" before building anything. The
guest carries its own SDK and ran the same tree. The only way past was `-NoGate`, which
switches the gate off on the run where it could have answered.

`Show-Blame` in the same runner has the same shape, found during WW420: any non-zero
exit from `dotnet run` on the reader prints "the dump came back and could not be read",
so a reader that never started reads as a dump that was bad.

What to build: both call sites tell the endings apart. The command ran and answered,
green or red, which stays as it is. Nothing ran: a build error, an SDK that did not
resolve. That one is about the host, so it gets its own sentence naming the first line
dotnet printed, and the gate goes on to the guest. The summary line the gate already
matches says which ending it was.

Left open: whether the gate's third ending carries on by default or refuses unless a
flag says. `DeskProbeTests` already reads both scripts.

## Block K — The proving ground — a fixture app built to be hard to test

### §WW437 the third tree the sleep catalogue does not walk

WW184's catalogue says what it is for in its own words: to see every way of parking a
thread and then say which is which, because "an unseen one cannot be called right".
WW198 widened the spellings for exactly that reason, after `FrameRun` parked twice and
the count said one.

It walks `Checkout.Everything`, which is `src` and `tests`. The repository has three
trees.

Found by writing an entry and being told the file sleeps nowhere. WW406 put a parked
thread in `tools/Winwright.Blame/Parked.cs` — deliberately, because the reader of a hang
dump needs a hang to read — and the catalogue could not see it either way: the sweep
never offered it, and the entry describing it was refused as one that had stopped
matching.

What makes this a task rather than a widened constant is that the trees are not the same
question. A sleep in `src` is the engine waiting, which is what Block C's criterion is
about. One in `tests` is a case arranging a condition. One in `tools` is neither: those
are programs a person runs, several measure time on purpose, and `Winwright.Typing` is
built to take the desk for minutes. A catalogue sweeping them under the same four kinds
would file most of them under one and say nothing.

So the design is whether `tools` joins the sweep with a kind of its own or gets a
reading beside it — and the cheap half is that nothing today says it is outside.

### §WW438 the run that was green before the ship

WW176 holds `Criteria.Known` to the roadmap in both directions: a criterion the roadmap
declares and the catalogue does not is red, and so is one the catalogue keeps after the
roadmap drops it. That is the right rule and it caught both halves it was written for.

What nothing says is when the catalogue may be read. Shipping a task with a checked
criterion takes that criterion off the roadmap, so the entry describing it goes stale in
the same instant — and the suite that proved the work was run before the ship, because
running it after means the work is finished and the ship is the last thing anybody does.

Both tasks in this session hit it. WW391's guest run passed at 2068, the ship removed
its criterion, and the next run went red on an entry the code change had nothing to do
with. WW406 was the same, one task later, by somebody who had watched it happen.

The narrow reading is that a ship edits a file the suite reads, so the order is ship,
delete the entry, run. The wider one is that a criterion whose existence is a roadmap
line is something this catalogue could derive rather than hold — WW424 is that shape
over three other lists, and its argument is that a hand-written copy of a set is where
the set drifts.

What is owed either way is that the order stops being something a reader has to have
been bitten by.

### §WW440 the second walk that answers the same question

`OwnRender.Armed` decides whether a silence this run recorded still stands: it walks the
message-only windows under `HWND_MESSAGE` looking for the presence window, and matches
the owning process. `OwnRenderTests.Present` does the same walk again, with its own
P/Invokes and its own spelling of `HWND_MESSAGE`, and its doc says so — "read the way
the engine reads it".

The name the two look for is held together already: `Renders.PresenceWindow` and
`OwnRender.PresenceWindow` are two constants because the two packages may not reference
each other, and `RendersTests` asserts they are the same string. That is the half
somebody thought about.

The walk is the half nobody did. Where the window hangs, that only a message-only parent
finds one, and which process counts as the owner are all decisions the engine makes and
the suite repeats. An engine that started putting the window somewhere else, or matching
an owner differently, would leave the suite answering the old way — and answering
confidently, because the name still matches.

WW418 is why it is worth saying now. That reading used to answer one case about its own
subject; it is now the stated precondition of three, so a copy that drifts does not fail
— it quietly says the precondition holds when it does not, which is the one direction a
precondition must not be wrong in.

What it needs is for the suite to ask the engine rather than repeat it, and the engine
has no public door for the question.
