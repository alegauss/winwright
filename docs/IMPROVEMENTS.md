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

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

### §WW350 the reading that answers is whichever one got there first

WW322 asked the standing menu first and kept the focus behind it, on the argument that a
TrackPopupMenu answers both and the day one stops answering the other still does. WW339
split the two readings apart, measured which answers on both kinds this suite can put
up, saw the standing one answer both times, and asserted it.

That assertion held for one run. On WW340's first run the same fixture, unchanged, had
the Win32 popup answer through the highlight instead - and both are true of a
TrackPopupMenu, which is a top-level menu on the desktop and also takes the focus. Which
question reaches it first is a race between the shell highlighting the entry and the
menu window becoming enumerable, and neither engine nor suite gets to pick.

Two things follow. WW322's argument is vindicated rather than dead: the focus arm is
reached, so it is not the unprovoked branch this entry was filed about. And the suite
now asserts the invariant - exactly one reading answers, and it is the one the trace
carries - rather than the winner a single measurement had made it assert.

What is left is whether a race decides what a trace says. A step that opened the same
menu twice can report the entry once and the menu once, and a reader comparing two runs
is comparing which question won. Preferring one needs a reason, which is what this entry
now holds.

### §WW353 the verb with nothing behind its reading

`Pointer.Run` sends the click and then returns `subject.Read().Values` on the next line.
No poll, no deadline, no second look. Every other verb that synthesises input settles
first: typing polls and now waits before its first look, press polls until the focus
moves, nudge polls until the range moves, and both picker walks poll per hop.

WW341 built the observable that tells a late reading from a wrong one and ran it: 1800
rounds of click, press and nudge on the guest, none late and none lost. So this is not a
fault that was measured - it is a shape that disagrees with every neighbour, bounded
under about 1% on one desk rather than shown to be absent.

What makes it worth an entry is what a stale click costs. The others pay a poll and
answer right; this one has no poll to pay, so a reading taken before the click lands is
returned as the click's answer. A case that clicks and expects the state to have changed
then fails on a busy desk, and the failure names the control rather than the timing -
which is the failure a person spends an afternoon on.

The fix is small and the cost is what has to be decided: a poll until the reading moves
adds a deadline to a verb that has none, and a click that legitimately changes nothing
would spend all of it. WW341's arm is what would price that.

### §WW355 the reader that would need no pause

WW329 put a fifty-millisecond pause in front of the engine's first look and the fault
went away. WW342 then took the read apart and found which half does it: making the
window's thread dispatch 4800 messages provoked nothing at all, and the UI Automation
read provoked 8 of 400. So the pause is not waiting for the queue to drain. It is
waiting out whatever the automation provider does on that thread when it is asked.

That was never the question WW329 could ask, and it changes what the interval is. Fifty
milliseconds is a floor found by sweeping - 150 was no better and nothing says five
would not do - and it is paid by every send this engine makes, forever, on every desk. A
read that does not provoke would be a repair with no interval in it.

Three readers are worth measuring in the arm WW342 already built, because the round is
the same and only the disturbance changes. A cached request, which asks the provider
once and answers from the copy. A single property rather than the whole `PatternValues`
pass, which is several requests where one would do. And the text through `WM_GETTEXT`,
which the window's thread answers without WPF's automation peer being built at all.

If any of them reads the box without provoking, the engine reads that way and the pause
goes. If all three provoke, the pause is the repair and this is what says so.

## Block E — Capture — the picture that proves what it photographed

### §WW359 the picture a harness cannot ask for

WW347 opened the way through and left it needing a hand at each end. `Popups.Picture`
draws a popup's own tree with nothing composited behind it, and only the application can
call it — the engine references no part of the in-app half and never will, which is what
`SeparationTests` holds in place. So the refusal names a route the harness cannot take,
and the first adopter to meet it builds the channel.

Two channels exist and neither is this shape. `WINWRIGHT_GEOMETRY` and
`WINWRIGHT_SURFACES` are announcements: the application writes a file while it lays
itself out, and the harness reads it afterwards. A popup picture is an ask at a moment —
this popup, now, to that path — because a run photographs a flyout once it has one open,
and a file written at startup is a picture of a popup nobody had opened.

The fixture is the evidence that this is missing rather than merely unwritten.
`Protocol` is the surface protocol implemented once for adopters to copy, and it has
`Report`, `Hold` and `Background` and nothing that renders a popup. So no case here
drives the route the refusal names end to end: the ones that exist call `Popups.Picture`
in process, which is the verb and not the channel.

What the channel is, is the task. A watched directory, a request file beside the report,
a named pipe: each is a different promise about what an application under test may be
made to do while a run is watching.

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

### §WW351 the last place a claim is spelled by hand

WW340 closed the two lists that could disagree: Checkable reads the set and the schema
marks the fields it holds. What it did not close is where the set comes from. `Of`
builds it with one `Claiming(...)` call per claim, over its own parameters, before the
step exists - so adding a claim is still adding a field, a schema row and a line in that
block, and forgetting the line still makes the step read as unfalsifiable.

The block reads parameters for one reason: a refusal has to name the spelling the file
used, and three claim families fold several spellings into fewer fields. That reason no
longer holds. `covers`, `coversAtLeast` and `coversWithin` are recoverable from `Sweeps`
with `Matching`; `sameAs`, `unlike`, `sameCountdownAs` and `contains` are four fields of
their own; so are `label`, `notLabel` and `beginsWithLabel`. Every spelling the block
resolves is readable off the finished record.

So `Claims` could be computed over the step rather than passed into it, and the block in
`Of` would go. A claim would then be a field plus a schema row, and the row is already
checked against the set in both directions by `ClaimsTests`.

The cost is ordering. `Of` refuses before it constructs, and this suite asserts which
refusal wins where a step is wrong twice over. Computing the set needs the record, so
the one-claim refusal moves after construction and every precedence a case asserts has
to be re-established rather than assumed.

### §WW352 the widest signature in the engine

Every field a step can carry is a parameter of `Of` and a parameter of the constructor
under it, and a field added reaches both by hand. That is 28 and 23 today, growing by
one per feature, and the analyser has been reporting it for as long: S107 on both, and
S3776 on `Of` at cognitive complexity 131 against an allowed 15.

WW340 was filed partly about this and closed the other half instead - the two lists that
could silently disagree - because the arity is a different kind of defect. Nothing here
is wrong; the refusals are right and the suite proves them. What is wrong is that the
signature is now the widest thing in the engine and the compiler stopped helping: three
nullable strings in a row means a transposed pair of positional arguments builds, and
only a case that happens to write both catches it. The suite writes them by name, which
is what has kept this harmless so far.

The tests are also the cost. Around two hundred call sites spell `Of` with named
arguments, and that ergonomics is worth keeping - a step written as a locator, a verb
and one named claim is why a case is readable in a test file. So a builder that replaces
it has to read as well as that does, or it buys a smaller signature with a worse suite.

Worth doing after WW351, which removes one of the two hands a field passes through.

### §WW354 the arms nobody checks against each other

`run-typing.cmd` is where a person reads what the measurement tool can do: four arms
now, each with a paragraph saying what it drives and what it reports. `Program.Main` is
where the second word is parsed, one `string.Equals` per arm, falling through to the
default.

Neither knows about the other. An arm added to the switch and not to the .cmd is a
measurement nobody can find. An arm named in the .cmd and not in the switch is worse:
the word is not recognised, nothing refuses it, and the tool runs its default experiment
and prints that experiment's numbers under the run a person started for something else.
A typo does the same thing.

This project refuses that shape everywhere it can see it. The verbs have a catalogue
checked against the engine in both directions, so do the flags the fixture takes, the
desk facts, the capture arms and the renderings. The measurement tool is outside the
suite by choice - it takes the desk for minutes and no guest run should pay for it - and
the two lists inside it were never brought under the same rule.

What would close it is what closed the others: the arms as data, named once with what
each drives, with the .cmd's prose and the parse both reading it, and a case asserting
an unrecognised second word is refused rather than silently answered by the default.

### §WW360 the second telling that costs a run

WW348 moved the refusal from the run that reached the step to the door of the run, which
is earlier and is still not where the author is. `winwright_check` reads a scenario and
answers whether it loads; it is handed that scenario as its own arguments and never a
project, so a capture step in a checkout declaring no `captures` comes back as "the
loader accepts them" and is then refused by `Suite`.

That is the shape WW58 exists to replace, one field further out. Everything else wrong
with a step — an unparseable locator, a verb that does not exist, an argument beside a
verb that takes none — is answered by the check tool at the moment the author writes it.
This one is answered by the thing that was about to run it.

What stops it is a decision rather than an omission. `Checking` is given a scenario as
JSON-RPC arguments and nothing else, while `winwright_run` takes a required `project`
beside its cases, so the shape is there to copy. Required of a check, it refuses every
check of a file whose project is not written yet, which is the state a scenario is
usually first checked in. Optional, the tool answers two different questions depending
on what it was given, and the answer has to say which one it answered.

The cost of leaving it is small and steady rather than sharp: an author is told twice,
and the second telling costs a run.

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

### §WW356 the menu that is not made of menu items

`TrayIconFixture.Built` makes a `ToolStripDropDown` and calls `Items.Add(string)` twice.
That builds a `ToolStripButton`, because `CreateDefaultItem` is `ToolStrip`'s -
`ToolStripDropDownMenu` is the subclass that builds menu items - and an automation
client sees `ControlType.Button 'winwright open'` under a Menu with no name.

Every case in this suite that reads the drop-down therefore names Button. The adopters
name MenuItem: the design of WW343 quotes claude-tray's own case as `open tray menu` and
then two reads of `Menu > MenuItem`, and freewilly's is the same. So the fixture
reproduces the container and not the entries, and a locator proven here is a locator
that would find nothing there.

It cost four guest runs to learn, which is the argument. A locator naming a control type
the tree does not carry fails as "nothing answered in 19 polls" - indistinguishable from
the menu having gone away, which is what WW343 was about and what three runs were spent
blaming.

`ToolStripDropDownMenu` is a one-word change and it is not obviously free: WW322's and
WW338's cases read this menu as "a menu with no name", WW339's split turns on which
reading answers, and a different window class may answer differently. So the change is
small and what has to be measured after it is the four cases that already read this
fixture.

### §WW357 the looks nothing arranges

WW345 split the probe in two. `Read-DeskState` decides what a desk is called and six
cases now call it with looks they made up. The loop above it - twelve looks half a
second apart, skipping the desktop's own classes, naming the owning process - is
untouched and still run by nothing but a real guest.

Both defects so far were in the classification, which is the argument for having stopped
there. But the loop is where a look is built, and a look built wrong classifies
perfectly: a window whose class is read as empty is not the desktop and not a shell
surface, so a quiet desk reads as a question and refuses the run. That is the exact
failure this probe has already caused once, arrived at from the other end.

What would run it is a desk this suite arranges. `PumpedDialog` puts up a window and
takes the foreground, which is the `asking` arm; letting it go is `busy`; the taskbar is
addressable by class, which is `shell`. The probe would have to take its deadline as a
parameter, because twelve looks over six seconds is a case nobody wants in a suite that
runs in eight minutes.

The awkward half is honest: the suite runs in the guest, so a case arranging the
foreground is competing with every other case that needs it. It belongs in the serial
collection, and it is one more class in there for a reading taken once a run.

## Block K — The proving ground — a fixture app built to be hard to test

### §WW358 a process whose only windows are a menu and its shadow

`TopLevelWindows.OfProcess` skips the classes `ShellDrawn` names, and two cases stand
behind that. Neither runs the arm the defect lives on.

The first opens a tray menu and asserts the largest window is not the shell's shadow,
but it runs inside testhost, which owns the suite's own windows — a decoy, a statistics
window, whatever the run left standing. The sort has real windows to put in front of the
shadow, so the case passes with the skip deleted. The second calls `DrawnByTheShell`
directly, which proves the rule and not the walk that consults it.

What the fault needs is a process whose only windows are a menu and the shadow behind
it, which is what a tray application is and what freewilly was when the measurement came
from there. The fixture cannot be one: it is a WPF application with a main window, and
every tray icon this suite raises is added to the suite's own process rather than to a
child.

So `ShellDrawn` names one class from one measurement on one machine, and nothing here
would notice a Windows build drawing a differently-classed shadow, or the skip being
removed. Block E asks that every arm of a capture refusal has something that provokes
it, and this arm has none.

The candidate is a fixture mode with a tray icon and no frame, so `Largest` can be asked
of a process with nothing else to answer with. Whether that is a flag or a second
executable is the task.
