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

### §WW332 The menu the fixture never had

`OpenMenu` is the hardest verb here and the only one whose success nothing observes. The
suite drives it once, at an icon that is not on the desk, and asserts the sentence a
search refuses with. Every case around it stops short of the menu: the overflow opens
and shuts, an icon is found hidden.

The reason is the fixture rather than an oversight. `TrayIconFixture` places a real icon
with `Shell_NotifyIconW` over a popup window whose whole procedure is `GetMessageW`, and
it answers no tray callback — so there is no menu for the verb to open, and a case that
called it would be asserting that nothing happened.

What that costs is being paid now. Three adopted cases fail on `showed no menu`, and the
question they raise — does this route open a menu on this shell — cannot be asked here.
It is asked through a publish and an adopting repository: a round trip per attempt,
against a defect nobody has reproduced.

A fixture icon that shows a real popup menu answers it in one run. If the verb opens
that and not the adopter's, the difference is the application; if it opens neither, the
route is wrong on this shell and WW322 is about the engine.

The menu has to be the shell's own kind — a popup tracked from the icon's window on the
tray callback — because a menu drawn any other way would prove the verb against a thing
no application does.

### §WW333 One kind of menu is not the verb

WW332 gave the fixture icon a menu so the verb's success path would be observed here
instead of by adopters. It works, and it proved the wrong half.

The fixture puts up a `TrackPopupMenu` — a Win32 popup, tracked from the icon's own
window on the tray callback. That kind announces itself as the focused element, which is
what `OnTheDesk` asked for, so the case went green on the first run and stayed green
while three adopted cases failed on the same verb.

The other kind is a WinForms `ToolStripDropDown`, which is what a `NotifyIcon` with a
`ContextMenuStrip` puts up and what a great many tray applications therefore show. It
does not answer the focus, and WW322 is the whole cost of nobody having noticed: the
engine reported no menu while the application's own log had one standing for six
seconds.

So the case covers one of two kinds and reads as though it covers the verb. The fix in
WW322 is a fallback the Win32 arm never exercises — it is reached only when the focus
answers nothing, and against this fixture the focus always answers.

What is owed is a second shape. The fixture can host a WinForms menu as readily as a
Win32 one, and the case that drives it is the case that would have failed before WW322
and passes after — which is the only kind of case worth adding to a defect already
fixed.

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

## Block E — Capture — the picture that proves what it photographed

### §WW334 the shadow behind a popup is not a picture of it

Measured beside freewilly's menu. The SysShadow window Windows draws behind a
drop-shadowed popup is WS_POPUP with no caption, owned by nothing, and two pixels larger
on every side - so it is a popup by every test the route has, and it is also
WS_EX_LAYERED. A copy of its rectangle is a copy of whatever the menu is standing in
front of.

Glass asks the compositor about the system backdrop and answers None for it, truthfully:
it never asked for one. The transmission is the layering instead, and nothing reads
that. So the adopter photographing a popup has to know a class name to keep away from,
which is exactly the kind of knowledge this framework exists to take off them.

### §WW335 a frame with no chrome reads as a popup

WW87 taught the route to read the style bits, because a drop-down that nothing owns had
to be recognised by something, and its class name carries a per-thread number. A WPF
window with WindowStyle None sets the same two bits - WS_POPUP, no caption - and it is
an ordinary frame with a visual tree the application can render.

The two-argument overload is unaffected: a window that is the main one short-circuits
before any of this. What is exposed is CaptureRoute.For(window) alone, which WW320 added
for an application showing only a menu, and which would send a borderless main window to
a screen copy.

Nothing measured has met one yet, which is why this is filed rather than guessed at. The
discriminator would have to be something the window says about itself rather than a
third style bit: WS_EX_TOOLWINDOW was tried and is clear on the menu this exists for and
set on the shadow behind it.

## Block F — Assert — the expectation is derived, never typed

### §WW323 A key from one well priced against the other well's value

One claim per step is this format's rule, and it is enforced by a list of fields per
claim rather than by a set. `expectReported` is checked against `expect` and against
nothing else, so a step carrying `label` and `expectReported` together passes every
guard.

What happens then is worse than either claim alone. `CaseRun` resolves the declared
string out of the project's strings, and the branch below it overwrites that with the
value the application reported — so the comparison is against the reported value while
the sentence a failure carries names the strings key. A reader of that red goes to a
strings file to correct a label that was never what the run compared.

WW83 met it while adding a third member to the family and left it alone deliberately:
closing it means deciding whether `expectReported` joins the label group — `label`,
`notLabel`, `beginsWithLabel` and it are all one reading against one derived value — or
whether the whole rule stops being a list of fields and becomes one set the refusal
names out of.

The second is the answer the pairs keep pointing at. Each new claim has added itself to
five or six lists, and the one it forgets is a hole exactly this shape. What it costs is
a way to say which claims a step is making without every rule enumerating them.

### §WW329 Waiting out the drain instead of repairing it

WW312 swept the same send quiet and watched: identical rounds, identical wall time,
differing only in whether anything read the box while the queue drained. Six hundred
quiet rounds faulted nowhere. Watched, three of a hundred and fifty — the rate the
engine has measured on itself all along.

So the engine is provoking the fault it repairs. `SendInput` returns once the events are
queued rather than processed, and `Settled` begins polling the instant `Send` returns,
which puts a cross-process read into the window's thread while its packets are still
being translated.

That makes a repair available that the resend is not. Three resends cost a failing send
three more of itself and leave the fault at its rate; waiting out a nine-character drain
before the first read costs every send a fixed interval and may leave no fault to
repair. Neither number is known: the drain was measured at 2 to 5ms a character after a
long pause, and what a first read owes is that pause plus the drain, which nothing has
measured.

The sweep is already shaped for that measurement. What is not known is whether the
provocation is the read or the pumping it forces — anything else pumping that thread
would do as well. Delaying the read fixes it either way; changing how the read is taken
needs to know which.

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

### §WW330 The flyout nobody closed

Measured, and it stopped a session. The adopting repository's tray cases ran in the
guest and failed inside the overflow flyout. The next run there — a different
repository's suite, minutes later — was refused before it started, with the desk probe
reporting that the taskbar had held the foreground for every look.

A picture of the guest says what no exit code did: no dialog, no prompt, an ordinary
desktop. The overflow chevron carries the keyboard focus and its tooltip is drawn beside
it. That is what the shell looks like after somebody focused the chevron and never took
the focus back.

Opening the flyout is the engine's own act. `OpenMenu` opens the overflow, focuses the
icon and presses the application key, and puts nothing back. Where a menu appears,
dismissing it is the case's business; where none does, there is nothing to dismiss and
the focus stays where the act left it.

So the cost lands on the run after, which is the shape this block's criterion names: a
run leaves the machine as it found it. A failing act left the shell selected, and every
later run inherits it.

Where the restore belongs is the question rather than whether. Putting the focus back
unconditionally would close a menu a case meant to read. Restoring only where nothing
opened leaves the successful path leaking — and it is the failing path that was
measured, and the one with no menu to lose.

### §WW331 A shell surface is not a prompt

The probe refused a run: `the guest's desk is waiting for an answer: explorer (pid 1008,
Shell_TrayWnd) '' held the foreground for every look`. A capture taken seconds later
shows an ordinary desktop — no dialog, nothing to answer — with the overflow chevron
holding the focus and its tooltip drawn. WW330's leak, one layer down.

The reading is right and the sentence is not. Something did hold the foreground for
every look. That it is a question somebody must answer is an inference, and it does not
follow: the shell holds the foreground whenever the last thing to touch the desk was the
shell.

What separates them is already in the reading and thrown away. `Shell_TrayWnd` is the
taskbar's own class and an empty title is what it has. A prompt is a window of some
application, with a caption, that a person could read. The probe names the class in its
own refusal and then does not use it.

The remedy is what makes this expensive rather than untidy. It tells a reader to answer
a prompt that does not exist, and warns against killing the owner because doing so once
cost the tray — so the honest response to this message is to go looking at a console for
something that was never there.

A refusal that named the shell and said the desk was left focused rather than asked
would point at WW330, which is the actual repair, and cost nobody the trip.

## Block K — The proving ground — a fixture app built to be hard to test
