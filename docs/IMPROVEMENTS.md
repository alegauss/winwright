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

### §WW288 The flyout that shuts, and whether a search should open it again

WW223 built the distinction and then waited for the occurrence to say which of its two
arms it was. The occurrence arrived on a guest run of 1735:
`TrayPlacementTests.Adding_one_and_finding_it_holds_every_time_rather_than_most_times`
was excused — *the overflow shut while this search was looking in it, so the flyout was
not read to the end*. So it is the flyout, not an absent icon, and the two earlier runs
of the same tree passed the same case. Do not go looking for a placement bug.

What that leaves is coverage. The case now reports a hole instead of a red, which is
right and is not the same as answering: on the runs the shell shuts the flyout, nothing
checks that an added icon can be found, and nothing outside the excuse ledger says how
often that is.

The shape is open. `Find` has deadline left when it gives up — it opened the overflow,
polled `Hidden()`, and read the desk once after the poll — so reopening and carrying on
inside the same budget is available and would turn most of these holes into answers.

Against it: whatever shut the flyout is unexplained, and if an application can close it
then reopening is the search papering over a fact about the application. That is the
same mistake WW168 was filed against, pointed the other way.

Measure first, then decide. What shuts it is the question, and the excuse ledger across
runs is where the rate is.

### §WW317 The chord press cannot spell

Found adopting this in quickshell, whose window is deliberately almost empty: a title
bar, a terminal, and nothing else. It has no menu and no toolbar on purpose, so every
command is on a chord — `Ctrl+Shift+F1` writes a diagnostic bundle, `Ctrl+Shift+I`
imports the incumbent's sessions. Both open a dialog with real text, which is what a
case wants to read.

`press` cannot reach either. `TraversalKey` is Tab, Shift+Tab and the arrows, which is
the right vocabulary for moving focus and the wrong one for invoking a command. There is
no `with` that spells a modifier plus a key.

What makes this more than one adopter's inconvenience: an application with no menu is
the shape this engine is best placed to test, because there is nothing to click and a
screenshot shows an empty window. The commands are the application. Reaching them
through the keyboard is the only route, and `click` needs a target that does not exist.

The shape, if it fits: `"act": "press", "with": "Ctrl+Shift+I"` — modifiers named, the
key last, parsed once rather than per case. The engine already presses with a modifier
held; `WithShift` in `Keys.cs` does it for Shift+Tab, so what is missing is the spelling
and not the mechanism.

Worth knowing before anyone starts: `Acting/Keyboard.cs` and
`Scenarios/StepDeclaration.cs` were both uncommitted in this checkout when this was
filed, so somebody may already be here.

Falsified when a command that only a chord reaches cannot be driven by a case.

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

### §WW248 A dialog beside a fixture takes the desk from it

`PumpedDialog` shows a window on this thread, and a window this process shows takes the
foreground. So a launched fixture in the same class is left without it, and every
synthesised act against that fixture is a hole — correctly reported, and for a reason
nobody wrote down.

Measured in one guest run. `NudgeTests` — a dialog and a launched fixture together —
excused a nudge on the launched slider. `WpfInputTests` — a launched WPF fixture and no
dialog — typed and clicked in the same run, neither excused. Two classes, one
difference.

The roll now carries the engine's own absence beside each excuse, so the difference is
readable rather than inferred. This run's five all say `another window of the same
process owns it: testhost 'winwright decoy'` — the decoy those cases open on purpose —
which a reader can see at a glance instead of trusting.

What is left is making a structural excuse a red, and asking the question turned up why
it is hard. Both obvious checks misfire on honest cases here. At run time, *the holder
is this process* marks `RefusedForegroundTests`, which takes the desk deliberately. Over
the sources, *a dialog and a launch and a synthesised act* marks `NudgeTests`, whose act
is against the dialog.

What separates them is not visible in one run: an excuse that arrives every time is
structural, and one run cannot say *every time*. That needs a history the suite does not
keep.

### §WW312 Why that band

WW310 measured the curve and stopped where measuring stops. Between 48 and 64
milliseconds the substitution runs at five times the engine's own rate, and by 80 it is
back under it. The fault itself never changes: one code unit out of place, the last one
sent standing in it, 130 times out of 130.

The tick was the obvious guess and the shape refuses it. Four ticks of the platform's
15.625ms timer is 62.5ms, which would put a spike on one value; what is there is a
plateau across forty milliseconds with a cliff on one side of it. Whatever this is, it
is not a beat against that clock.

What has not been looked at is the other end. Everything measured so far is on the
sending side — how many calls, how far apart — and the fault is a character arriving
where a different one was sent. The window has a message queue, the control has an input
scope, and neither has been observed while this happens. The fixture already carries a
recorder that showed the characters arriving substituted, which is how WW302 ruled WPF
out; what it has never been asked is what the queue looked like at that moment.

A guess worth pricing before adopting it: the band may be where a send is slow enough to
overlap the read-back the engine does after it, and fast enough that both are in flight.

### §WW318 Absence as a reading rather than a timeout

Found adopting this in quickshell, whose window makes its argument by what is not in it:
no toolbar, no status bar, no sidebar — and not hidden ones waiting to be switched on,
but no elements at all. That claim is the design, and the repository has an in-process
test walking the visual tree to assert it.

Through a case it cannot be said. A step reads a subject, and a locator matching nothing
has no subject to read, so `"expect": "absent"` fails as "nothing answered to it in 109
polls" — which is the same sentence a genuinely broken read produces. The two are
indistinguishable in a report, and one of them is the pass.

Why it is worth having rather than left to in-process tests. Absence from the
accessibility tree is the strongest form of the claim — what a screen reader would find
— and it catches what a tree walk cannot: chrome a theme, a style or a host puts on
screen without the window's own tree containing it.

What it needs from a reader: a locator resolving nothing must be a *result* rather than
a timeout, and only where absence is what was asked. Everywhere else it must stay the
failure it is now — an expectation of absence that quietly passed because the window had
not opened yet would be the worst of both.

Falsified when a case cannot say that something is not there.

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

### §WW83 The switch case rewrites a real setting

Until that case existed, the path that rewrites the setting, re-keys the stores and
takes the other account's token ran under no check at all. It is refused against a
resident process, because a pick there would repoint the real icon for real. Migrating
it inside the store comparison asserts the promise that a run touches nothing at the one
place most likely to break it.

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

### §WW311 The prompt that waiting does not clear

WW305 made a cold start work, and the first run through it excused twenty-six checks
where the four before it excused eight each. WW298 caught that: the count is read as a
series, so a run three times its predecessors could not pass as ordinary.

What it cost was measured later. The same prompt — OneDrive's *Habilitar o Backup do
Windows*, two buttons, the same process id hours apart — held the foreground while the
adoption's keyboard case ran, and that case came back unchecked with three steps
unwalked. Not noise in a count. A blocker.

That kills the remedy this task was opened with. Waiting for the shell to go quiet
cannot work against a question that stays until answered.

Nor is killing something the lever. OneDrive was not the owner — the window is
`ShellExperienceHost`'s. Killing that did clear it and cost the tray: the next full run
went red with *this desk was called placing and holds no icon anywhere*. A reboot fixed
the tray and brought the prompt back.

So this wants to see the desk before spending twenty minutes on it, and to say which it
is: busy, asking, or broken.

One thing to know first: the bench is gone. The guest now carries `ToastEnabled = 0`,
set to unblock WW78 after that prompt held the desk across three runs. Nothing there
raises a toast, so a remedy cannot be tried until that key goes back.

### §WW314 The check that runs too late

The runner refuses a locked guest, says so plainly, and points at the remedy. That part
is right and WW42 is why: a suite synthesising input into a lock screen is not a suite
that ran.

What is wrong is where it finds out. The check is `runProgramInGuest` failing with
*logged in interactively*, which happens after the tree is zipped, carried, extracted
and the SDK probed — so a guest nobody logged into costs a full sync before it says the
one thing it knew all along.

That was tolerable while the guest was almost always up and unlocked, because a stopped
guest meant a run that hung. WW305 fixed the hang, cold starts became cheap, and a
freshly booted guest is precisely the one most likely to be sitting at a lock screen.
The first cold start of the day reached a desktop; the second did not, and paid the
carry to learn it.

The probe is cheap and already written: the same `runProgramInGuest` call with something
harmless, before the sync rather than after it. What it must not become is a second
spelling of the rule — the refusal, its sentence and its remedy stay where they are, and
this only asks the question earlier.

Worth pricing against the other thing it could be: making the guest log itself in. That
is a change to somebody's machine to suit this runner, and it is theirs to make, not
this repository's to assume.

### §WW315 A guest that is not the machine under test

The first adoption run reached the guest, restored the published engine, built and ran
eleven cases. Five of them answered. Six had nothing to answer with, and said so
precisely: `--profiles reports 0, and the profile card is Collapsed below two`, and `no
*.jsonl under C:\Users\oobe\.claude\projects, so no report can render`.

That is the engine behaving. Not one of the six went green on absent data, and each
names the file it wanted rather than reporting a control that failed. WW42's rule
holding in a place nobody had put it yet.

What it blocks is the rest of the migration. WW83 moves the case that rewrites a real
setting — a profile switch — and WW85 the sweep that walks a submenu per sampled mode.
Both need two profiles to exist before there is a switch to make or a submenu to walk.
Written against a guest with none, they would migrate as cases that are correct,
refused, and never once observed to work.

So the guest needs to become the machine these cases are about: profiles it can switch
between, and a transcript to report on. What that costs is the question — fabricated
data is a fixture and has to be as disposable as the rest of the tree, and profiles that
a run repoints are the one thing here that writes outside it.

Until then WW83 and WW85 are waiting rather than ready, which is what this line says.

## Block K — The proving ground — a fixture app built to be hard to test

### §WW316 The instrument that moves what it measures

`Arrivals` is the recorder WW249 was narrowed with: it takes every `WM_CHAR` the window
under test receives and shows it, which is how the substitution was proved to arrive
already wrong rather than to be made by WPF.

It appends to a `StringBuilder` that is never cleared, and per keystroke it writes the
whole of it to a `TextBlock`'s `Text` and again to its `AutomationProperties.Name`. A
run of 400 rounds across five arms sends some eighteen thousand code units, so the last
keystroke rewrites an eighteen-kilobyte string and raises a name change over it, and the
one before it rewrote nearly as much.

Measured, and this is the number that matters: across one run the average round went
4600ms, 6968ms, 9135ms, 11325ms by quarter — two and a half times slower at the end than
the start — and the failures in those quarters went 6, 6, 11, 22. WW313 shipped on
exactly this reading.

So the instrument moves what it measures, and it moves it in the direction the
measurement is most sensitive to. WW310 found the fault five times likelier in a band of
spacings forty to seventy milliseconds wide; a fixture that slows every send as the run
goes on is walking the experiment through that band without saying so.

Arm against arm survives it — all of them meet the same window in the same round. Every
absolute rate in this repository's typing measurements does not.
