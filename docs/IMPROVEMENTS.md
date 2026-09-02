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

### §WW337 the delay may be affordable after all

WW304 swept the spacing and found 0 in 1100 at 128ms and above against 2% below it, and
128ms a code unit is a price no keystroke can pay. WW310 then read a band across 48 to
64ms, and between them there was no delay to choose: too dear where it worked, and no
monotone direction anywhere else.

WW312 swept it again over three send shapes, the engine's own among them. 2700 rounds,
two substitutions, both at no spacing at all - and every one of the fifteen spaced
cells, from 32ms up, read zero. So the region WW304 called 2% is empty here, and the
band that made the curve unusable is not there either.

What that would buy is 32ms a code unit, which is 288ms on a nine-character send: a
quarter of what was refused, and paid only where a case types.

It is not a measurement yet and that is the task. The baseline this is against is 0.7%,
so 750 clean rounds are about five events short of where they were expected -
suggestive, and not a rate. What it needs is the spacing swept against enough rounds at
no spacing to say the two differ, on a desk whose rate has been read the same evening.
The repair the engine ships stays whatever this answers: a resend costs nothing on the
99% that arrive.

### §WW338 two menus with the same nothing for a name

WW322 gave the tray verb a second reading: a top-level menu standing on the desktop,
compared against whatever was standing before the key. The comparison is `menu !=
standingBefore`, and both sides are what `Standing` calls a menu - its automation name,
or the phrase `a menu with no name` where it has none.

So two menus that are called the same thing are one menu to this reading. An application
that had a menu up when the verb started and put a second one up in answer reports
nothing came; two unnamed menus do the same, and unnamed is what a `ToolStripDropDown`
with no accessible name is.

Neither has been seen. The verb takes the focus first, which dismisses most menus that
were standing, and the case that provokes the drop-down puts up one menu on a desk with
none. What makes it worth filing rather than leaving is that the failure is silent and
reads as the application declining to show a menu - the exact confusion WW322 spent
itself on.

What would settle it is the element rather than the name: a runtime id is what UI
Automation promises for the life of an element, and the tray search already matches an
icon that way for the same reason - a tooltip an application rewrites. Two menus are
then two elements whatever either is called.

### §WW339 the field that stopped meaning what it is called

`TrayMenu.Highlighted` was the focused element's name, which for a Win32 popup is the
entry the menu has highlighted - and that is what the field is called and what a trace's
read-back carries.

WW322 asked a second question first: is a menu standing on the desktop, which is the
only reading a drop-down answers. Where it answers, the value is the menu's own name and
not an entry's. So a trace of the same act against the same application now records the
menu where it recorded the entry, and the field's name is right about one of the two
paths.

Nothing is wrong with what it reports. What is wrong is that one word covers two
readings, and this project's rule about the third verdict is the same rule: two facts
under one name is a reader unable to tell which they have.

Two ways out, and the choice is the task. Either the field says which reading answered,
so a trace carries `the menu 'Context'` against `the entry 'Open'` and a case can ask
for either - or the standing route goes on to read what its menu is highlighting, which
is one more cross-process call on a path that has just proved a menu exists. The first
is honest and cheap; the second keeps every existing reading meaning what it did, which
matters to nobody yet and would matter to an adopter asserting on it.

### §WW341 the same look, four more times

WW329 measured one act. `SendInput` returns once the events are queued rather than
processed, and `Settled` polled from the instant `Send` returned - which put a
cross-process read into the window's thread while its packets were still being
translated. 31 substitutions in 1200 rounds with no pause, none with one.

Nothing about that is peculiar to typing. `Act.Through` reads back through the subject
the moment the act returns, and four verbs reach it after synthesising input: click,
press, nudge and the two picker walks. Each queues events and each is read while the
queue drains.

What differs is the observable. A typed string arrives wrong in a way a case can see,
character for character, which is why this fault was found at all and why it took ten
sightings to find. A click that lands late is a step that reads the value from before
it, which the engine reports as a read-back that did not arrive and a retry then covers
- so the same provocation would show up as a rate of retries rather than as a wrong
answer, and nothing has ever counted those against a machine that waited.

The measurement is the task and not the pause. The typing arm exists and takes rounds, a
pause and a rate; what a click needs is an observable that separates late from wrong,
and that is what has to be built first.

### §WW342 what the fifty milliseconds are paying for

WW329 measured the repair and not the mechanism. A cross-process read against the window
under test is two things at once: a call into another process, and a message pump run on
that process's own thread to answer it. Either could be what disturbs the queue while
its packets are being translated, and delaying the read removes both.

Two things rest on the difference. The interval is fifty milliseconds because that is
where the fault stopped and 150 was no better - it is a floor found by sweeping, not a
duration anything derived, so nothing says whether five would do. And any other reader
of that window inherits the same question: a case that watches a caption while a send is
in flight is doing whatever the first look was doing, and there is no rule to tell it
apart from one that is not.

What would separate them is a reader that does not pump. UI Automation's cached reads
and a raw property fetch take different routes through the target, and an arm that pumps
the thread without reading anything - a posted message answered and dropped - would
provoke it with no read at all. Both are cheap in the arm WW329 already built: the round
is the same, and what changes is what happens during the drain.

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

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

### §WW336 the one thing an adopter still writes in C#

Every verb the vocabulary has acts on a control or on a tray icon. A capture acts on a
window and needs three things a step cannot say: which window, which route, and where
the file goes. So freewilly's menu capture, claude-tray's and pportal's are each a
hand-written test beside the data files - which is the shape block G exists to remove.

Most of it is already somewhere a step could reach. CaptureReceipt.Taking composes every
reading without a caller remembering one, and CaptureRoute answers which way the picture
is got off the window itself. What is left for the author to say is the subject and the
destination.

The destination is what makes this worth its own task rather than a fourth line in
ActVerb: it would be the first verb in this vocabulary that writes a file, and a case
that names a path is a case that means something different on the next machine. The
other fields a case carries are derived for exactly that reason.

### §WW340 the claim that is declared four times

WW323 replaced eleven per-claim lists with one set, and the set lives inside
`StepDeclaration.Of` as a local that is used for the refusal and then dropped. So the
same enumeration is still written three more times.

`Checkable` is the nearest: a chain of nineteen ORs over the same fields, answering the
same question the set answers by being non-empty. A claim missing from it is a step that
reads as unfalsifiable, and `CaseDeclaration` then refuses the case that carries it - a
refusal about the wrong thing, which is the shape WW323 was filed for one layer up.

`ScenarioSchema.Step` is the second, and it has to stay a list of fields because it
publishes types and prose. What it does not have to be is a list nothing relates to the
claims: nothing says that `label` and `expectReported` are two claims and `reads` is
not, so the schema a tool carries cannot tell an author what it is about to be refused
for.

And the third is the arity. `Of` takes 28 parameters and the constructor 23, one per
field, so a claim arrives by being threaded through both - which is why every claim so
far has also arrived in a list it forgot.

What would close it is a claim being a thing rather than a spelling: named once, with
its field, what it says, and whether it is checkable, and every one of the four reading
that.

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
