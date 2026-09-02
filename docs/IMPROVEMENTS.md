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

### §WW344 the half of the tidying nobody hears about

`PutBack` does two things and answers for one. It shuts the flyout and returns what
shutting it did - a reading with a reason and a precondition on it, the way every other
tray verb answers. Then it calls `SetForegroundWindow`, whose whole documented behaviour
is that Windows refuses it to a process that does not own the foreground, and returns
nothing about that at all.

So a run where the shell kept the desktop looks exactly like one where it did not. That
is the shape this project withdraws everywhere else: a reading nobody took and a reading
that came back clear are different facts, and a caller cannot tell them apart from one
value.

What it costs to close is small and the shape already exists. `Foreground.Now` reads who
holds it, so the call can be followed by a look and the two compared - which is also the
only honest way to report it, since the boolean the API answers is documented to be true
where nothing moved.

What it is worth is a run that says why the next one was refused. WW330 was found by a
picture of a guest, taken by hand, after a suite in another repository would not start;
a line in the first run's own output saying the desktop had not gone back would have
been the whole investigation.

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

### §WW346 the biggest window is the one to keep away from

`TopLevelWindows.Largest` answers the largest window a process owns, which is the frame
where there is a frame and is documented as such. A tray application showing a menu has
no frame, and what it does have is measured: freewilly's menu is 188x108 and the
SysShadow window Windows draws behind it is 190x111. The sort is by area, so `Largest`
answers the shadow.

WW334 means a capture of it is now refused rather than written, which is the important
half and is not the whole of it. A caller that asked for the largest window got the one
surface beside a menu that must never be photographed, and what it gets back today is a
refusal it did not expect about a window it did not know it had asked for.

The verb is right about what it promises and the promise is the wrong one here. What a
caller wants is the window the application drew, and a shadow is the shell's - it
belongs to the process only because the popup does.

Two candidates and neither is obviously better. Either `Largest` skips what `SeeThrough`
calls composited, which makes a listing verb read a layer attribute per window; or the
sort stops being the answer and callers name what they are after, which is more honest
and more typing. Measuring which the adopters actually need is the task.

### §WW347 the popup that can now be photographed by nothing

Measured while WW335 was being settled. A WPF `ContextMenu` puts up a top-level window
reading style 0x96000000 and ex 0x08080088: WS_POPUP with no caption, and layered with
an alpha per pixel, which is the drop shadow it draws for itself.

So the route calls it a popup, correctly - it is in no tree the application can hand
over - and sends it to the screen copy, which is the only capture that reaches one.
WW334 then refuses that copy, correctly as well: the shadow at its edges is the desktop,
and a picture of it is partly a picture of whatever it is standing in front of.

Both readings are right and together they leave a real surface with no way to be
photographed at all. That is a narrowing this project should make on purpose rather than
discover: nothing in the suite photographs a WPF popup, so nothing went red, and the
first adopter to try will meet a refusal with no door beside it.

Two doors and they are not the same size. The copy could be trimmed to what the window
actually painted rather than to its rectangle, which is `PaintedFrame`'s idea one layer
in and needs the layer's own alpha to say where the edge is. Or the in-app half could
render a popup's own tree, which is what it already does for a window and is the answer
that leaves nothing composited at all.

### §WW349 the default nobody outside the process can take

WW336 gave the engine `ScreenCopy`, which is the route that exists for a surface no tree
holds. The other route is the default and is the safer picture, and this engine cannot
take it: a render draws an application's own visual tree, and nothing outside that
process has one.

So a capture step against an ordinary window answers a hole naming `Winwright.InApp`,
which is honest and is not a picture. The verb serves the surface it was asked for - a
tray menu, a balloon, a popup a framework drew - and answers nothing for the window an
adopter is most likely to point it at first.

The half that can take it already exists and already talks to a harness.
`Winwright.InApp.Render` renders a tree and `Geometry` dumps one into a file the engine
reads back, through a variable the harness names - so there is a shape for this that
neither side has to invent: the run asks, the application renders into the file it was
given, and the receipt is composed over what came back.

What it needs deciding is who asks. A capture step cannot call into another process, so
either the in-app half watches a directory the way it watches WINWRIGHT_GEOMETRY, or the
application exposes a verb the run starts it with - which is what every adopter's own
capture flag already is.

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

### §WW348 the declaration a load could have asked for

WW336 made a capture step answer a hole where the project declares no `captures`, naming
the file to add it to. That is the right answer at run time and it is one step too late.

This format's founding rule is that a case which could not run anywhere is refused
before it runs here: an unparseable locator, a verb that does not exist, an argument
beside a verb that takes none. A capture step in a project with nowhere to put pictures
is the same kind of fact - it is about the file and the declaration beside it, not about
the desk, and it is knowable the moment both are read.

What stops it being refused there is only where the reading happens.
`StepDeclaration.Of` judges a step and is handed no project; `ScenarioFile.LoadAll`
reads files and is handed no project either. `Suite.Launch` has both and is the first
place that does, which makes it the candidate - and it is also where a refusal would
still arrive before a window is launched, which is the whole of what the rule buys.

The cost of leaving it is a run that launches an application, drives it to the step and
then says what the file could have said. The cost of moving it is that `Suite` starts
knowing about one verb, which is the thing the vocabulary exists to keep it from.

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

### §WW343 the desk a scenario cannot hand back yet

WW330 closed the arm it was filed for: where no menu opened, the act shuts the flyout it
opened and gives the desktop back, and a caller that did get a menu calls `PutBack` once
it has read one. The suite's two menu cases do exactly that.

A scenario cannot. `CaseRun.Trayed` already shuts the flyout in a finally - WW258 put it
there, and it is right - but the focus is the other half and it has no such place to go.
Calling `PutBack` in that finally would set the foreground back while the menu is
standing, and a drop-down closes the moment anything else takes the focus. So the step
that opens a menu would dismiss it before the step that reads it ran, which is the case
failing rather than the desk being tidied.

The reading has to live longer than the step. What opens a tray menu is one step and
what reads it is the next, so the restore belongs where the case ends - beside the
fixture teardown, which is already the place a case gives back what it took.

This is the adopters' own path. claude-tray's menu case is `open tray menu` and then two
reads of `Menu > MenuItem`, and it is a run of exactly that shape whose leftover chevron
refused the next run in the guest.

### §WW345 the probe nothing can run

`Read-GuestDesk` writes a here-string into the guest, runs it there, and reads one line
back. What it answers decides whether twenty minutes are spent: `asking` refuses the run
outright, and the state it names is the one this project has been wrong about twice -
once by calling a focused taskbar a question, and once by repairing that in a way that
made the reading say nothing.

Nothing runs it. WW331 gave it source-level checks - the states the probe writes are the
arms the runner switches on, the shell's classes are not the desktop's, the question
refuses and the selected shell does not - and every one of those is a claim about the
file. A classification that answered `busy` where it means `shell` would pass all three.

What it needs is the reading taken against a desk somebody arranged. The engine has the
pieces: a window put up and given the foreground is what `PumpedDialog` already does,
and the shell surfaces are addressable by class. The awkward half is that the probe
polls the live foreground for six seconds in another machine's session, so what would be
under test is either the classification lifted out of the here-string, or the probe run
against a desk this suite arranged on the host.

The first is smaller and answers less; the second is what the four states are actually
about.

## Block K — The proving ground — a fixture app built to be hard to test
