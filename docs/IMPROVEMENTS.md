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

### §WW376 the fraction about the set, not the member

WW363 was measured on five runs that excused 8, 8, 8, 9 and 10. The run that shipped it
excused 9 against four runs of 8, and the case that made the difference was
`TrayPlacementTests.Two_icons_from_the_same_run_are_each_placed_before_their_own_add_returns`
— the chevron pressed and no flyout inside 2003ms, which is the tray shape the whole
task is about. Not one line of that report carried a rate.

Every clause was correct. The eight that recurred are in all four runs before it, so the
stronger claim fires and the rate stays quiet by design. The ninth was at one, and one
is where every excuse starts. So the reading arrives on the run after the one a reader
is holding, and the run that first shows a rise reads exactly as it did before WW363.

What is missing is the same argument one level up. WW363 answers how often *this case*
was excused; nobody asks how much of *this run's* excusing is old. Three of nine excuses
being cases the last twenty runs excused in over half of them is a sentence about the
set rather than about any member of it, and it can be said on the run that adds a new
case, because it does not depend on that case having a history.

The numbers are already read — `HowOften` carries the whole table. What is undecided is
which fraction a reader is owed and whether it belongs in the sentence or under it.

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

### §WW368 the arm that is not quite the act

WW355 added four readers to WW342's arm and every one read zero: the window's title
through USER, one cached ask, one property of a pre-resolved element, and that element's
`ValuePattern`. Eight hundred rounds each. The engine's settle then took the last of
those and read 1 of 1200 with the pause taken out, where the pause reads 0.

Thirty-one times better than the 2.58% WW329 measured, and not the zero the arm
predicted. So the arm is not the act — and the gap is small enough to stay invisible
until somebody looks, and large enough to have kept fifty milliseconds on every send
this engine makes.

What differs is listable rather than known. The arm takes the focus once before all its
rounds and the act takes it before every send. The arm disturbs for a fixed three
hundred milliseconds whatever happens, where the act stops polling the moment the text
matches — so the act reads fewer times and faults more, which is the part most wanting
an explanation. And the act reads through `Admitted.Do`, which the arm reaches past to
the element.

Each is testable in the arm that already exists, one at a time, which is what makes this
an entry rather than a shrug. The prize is the interval: a reader that provoked nothing
in the act as the act runs it would take the pause off every send, and what stands
between here and that is four more arms and an evening.

### §WW377 the same promise, one verb over

WW364 annotated `Locator.TryParse` and four bangs in `StepDeclaration` went with it.
`Chord.TryParse` has the same signature — a bool and two outs, neither annotated — and
was not touched, because the task was about the sites that had gone wrong rather than
about the shape.

It costs three bangs, all in `ChordTests`: `one!.Text` and `two!.Text` after an
`Assert.True`, and `wrong!` after an `Assert.False`. Fewer than the four WW364 removed,
and in a test rather than in the engine, which is why this is filed small rather than
done in passing.

What makes it worth doing anyway is that the engine's own callers are already written as
if the annotation were there. `ActVerb.Refuses` reads the reason on the false branch
without a bang because it interpolates it, and `ActVerb.Chorded` returns the chord on
the true branch as a `Chord?` and lets the caller carry the nullability onward. Both are
right, and neither is what the method promises. The next caller that has to name the
chord rather than pass it along is the one that will spell a bang, and by then the
argument for the annotation will be a bang somebody already wrote.

`[NotNullWhen(true)]` on the chord and `[NotNullWhen(false)]` on the reason is the whole
change, and `LocatorTests.The_signature_makes_the_promise_the_body_keeps` is the shape
the check takes. Worth reading the two `ActVerb` sites first: an annotation that makes
either of them warn is a reading this filed too quickly.

## Block E — Capture — the picture that proves what it photographed

### §WW374 the gap between a window and an answer

An application answers renders from the moment it hooks the message and not before, and
nothing tells a harness when that is. `Suite.Launch` waits for a window, which is
earlier: the fixture's own line runs at `ContentRendered`, so there is a window, it is
on screen, it is enumerable, and a capture asked for in that gap is refused.

Found by a case rather than reasoned about. WW361's toast case waited for two windows
and asked, passed on the machine it was written on, and failed in the guest, where the
harness got in between the toast appearing and the frame finishing. It was repaired by
waiting for the frame to answer first — a fix for that case and not for the class.

What makes it worse than an ordinary race is the sentence. WW362 taught the refusal to
name the fault, and every name it has is about how the application was built or started:
no half, told nowhere to write, a window it does not own. "Not yet" is none of those, so
a run asking too early is told something untrue about the product, and the reader goes
to check a line that was right all along.

The candidates are a readiness the launch door waits on — waiting for a window, one step
later — or a sixth answer meaning the half is here and this window is not hooked yet,
which costs nothing and turns the untrue sentence into a wait.

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

### §WW372 the popup ask no scenario can make

WW359 built the ask and left it reachable only from C#. `CaseRun.Rendered` calls
`OwnRender.Into` with the window it found, and no clause a declared step can carry would
make it call `PopupInto` instead. So the surface a scenario most wants a picture of — a
flyout nobody has clicked — is the one a scenario cannot ask for.

WW349 wired its own ask in the commit that built it, which is why this reads as an
omission. It is not the same work. That step already named a window and the route
decided the rest; this one has to name something inside the window, and the grammar has
nowhere to put it.

The obvious spelling is a capture step taking a popup's name, and what it opens is what
happens where the name is wrong. The channel answers four refusals — no such popup, more
than one, holding nothing, path refused — each a fact about the case rather than the
desk. So they are reds, which is the opposite of how `RenderAsked` counts: `AsAssertion`
reports every absence as unchecked, because WW349's only failure was an unadopted half.

That collapse is the finding. A run told the application did not render its own tree,
where the truth is a scenario naming a popup that is not there, has a green-adjacent
answer to a typo.

The candidate is a step clause naming a popup, and a rule separating the refusals a case
can fix from the absence only a machine can.

### §WW378 the list that outlived its rule

WW365 moved every refusal that runs after the step is built. One family did not move,
because it runs before: `absent` is judged where the parameters are still the only thing
in hand, and its first rule is a seventeen-term chain over them — `expected is not null
|| moves || answers || sweeping is not null || ...`, one term per claim the format had on
the day it was written.

That is the shape WW323 was filed for and WW340 and WW351 each closed once. The list
here is the last hand-written copy of the claim set, and it is already one behind:
`contains` joined the format in WW326 and is not in the chain, so a step claiming
`absent` beside it falls through to the generic multi-claim refusal and is told it makes
two claims rather than that a claim about nothing is a claim about something.

Nothing between this block and the construction throws, so it could run after the step
instead and read `Claims` — which is the whole of the fix, and the fix changes the
sentence a step with `absent` and `contains` is refused with. That is the decision this
holds: a better refusal for a case nobody has written, against a sentence this suite may
already assert.

There is a second reason it is worth doing. `Of` now reads at exactly 15 against an
allowed 15, and most of what remains is this block. The next claim added to that chain
puts the verb back over the line, which is the same forgetting in a new place.

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

### §WW369 the tree nothing reads back

WW356 changed the fixture's tray entries from buttons to menu items, and one case
noticed: the scenario reading `Menu > MenuItem[order=top]`. What it said while the
entries were still wrong is "nothing answered to it in 30 polls over 4080ms" — which is
what a menu that closed says, what a menu that never opened says, and what a locator
naming a control type the tree does not carry says. Three faults, one sentence.

That is the confusion WW343 spent four guest runs inside and WW356 spent two more. The
entries are right now and nothing here holds them right: a container swapped, or an
`Items.Add(string)` written the short way, and the first sign would be a scenario
failing in the words of a desk problem.

What the fixture lacks is a case that reads its own tree and says what is in it.
`Inspect.Under` and `Inspect.Render` already produce exactly that — the probe WW356
threw away was fifteen lines of them — and asserting the control types under the
drop-down, a Menu and two MenuItems, is a red that names the fault in its own sentence.

Worth doing for the Win32 kind at the same time. Nothing here has ever read that one's
tree either: it is asserted through the reading verbs and never as a shape, so what an
adopter's locator would find in it is unmeasured rather than known — which is the state
the drop-down was in until somebody spent six runs.

### §WW370 the look that is supposed to be nothing

WW357 made the polling runnable and ran it: two cases arrange a window, ask for two
looks, and read what the loop built. Every look those take is a window, which leaves one
branch untouched — the one answering `$null` because the class is on `$script:Desktop`.

It is the branch a quiet desk depends on. Progman and WorkerW hold the foreground of an
idle logged-in session, and skipping them is what makes `clear` mean "nothing but the
desktop". If it stopped skipping, a quiet desk would build twelve looks of one window
and read as a question — which is the refusal WW331 was filed about, produced by the
loop this time instead of by the classification.

A case cannot arrange a desktop-held desk. What it can do is pass the list: the loop
reads `$script:Desktop` off the script, and a parameter defaulting to that would let a
case name the class of the window it just put up and assert the look came back as
nothing. What is under test then is that a class on the list is skipped, which is the
branch — rather than that Progman is a desktop, which is a constant a case would only be
restating.

The list itself stays checked as it is now, by a case reading it out of the file beside
the shell surfaces. Those are two claims and not one, and the second is about what the
words are rather than about what the loop does with them.

### §WW371 a refusal an unattended run cannot answer

`desk-probe.ps1` reads and never repairs, and WW311 argues that well: a toast goes and a
question does not, and killing the owner cost the tray once already. What it leaves is a
refusal with one remedy — go and click it at the guest console — and a session working
the backlog has nobody there.

Measured on WW358. The first run cold-started the guest and passed 1961 cases. The
second refused: an Edge window held the foreground for all twelve looks. Edge restores
its session at login, so the runner manufactured the desk that then refused it.

Both non-destructive repairs failed. `Shell.Application.MinimizeAll` left it in front,
and `ShowWindow(SW_MINIMIZE)` aimed at the window itself was called twelve times, each
one reporting the same handle back in the foreground 600ms later. So the window is not
merely selected — something restores it — and the probe's reading was right both times.

That leaves three things unseparated that a run would treat differently: a question
nobody can answer but a person, an ordinary window that minimises, and a window that
refuses to. Only the first deserves the refusal it gets. `WS_MINIMIZEBOX`, the owner
handle, and whether a minimise holds are each readable before the twenty minutes are
spent.

The candidate is a repair the probe may attempt and must then re-read, refusing only
where the desk did not clear — and a cold start that does not restore a browser session
at all.

### §WW373 the run nobody can tell from a slow one

A case that deadlocks does not fail. `run-tests.cmd` passes no `--blame-hang`, so the
run sits, the guest holds whatever windows the case left on the desk, and the only thing
that ends it is a person noticing and killing `testhost` inside the guest.

Measured on WW361. A hook disposed across a dispatcher that was not pumping wedged the
run on its own thread; the host command went on waiting, the guest console showed two
windows and nothing else, and what stopped it was the operator seeing them. The run had
been going long enough to have finished twice.

The cost is not the minutes. It is that a wedge and a slow suite read identically from
outside, so the honest response to a run taking too long is to wait longer, and what
separates them is a desk nobody is watching. `run-tests-vm` already argues this shape
about `vmrun start` and refuses to block on it: ten silent minutes and a wedge look
alike, which is why it polls. The suite it then launches has no such bound.

`dotnet test` takes `--blame-hang --blame-hang-timeout`, which ends a stuck case, names
it, and writes a sequence file saying what was running. That turns a wedge into a red
with an address, which is what every other failure in this repository already is.

The candidate is a timeout on the runner, generous enough that the slowest honest case
never meets it, and a guest kill that no longer needs a person.

### §WW375 the question nobody can read

WW331 taught the probe that the shell holding the desk is not a question, and WW357 made
the looks themselves runnable so a look built wrong could be caught. Neither reading
covers the desk that refused a run today: a Microsoft Edge window, left focused in the
guest, held the foreground for all twelve looks and was classified `asking`. The remedy
printed with it sends a reader to the guest console to answer a prompt, and names the
ShellExperienceHost dialog that cost a run — a window that was not there.

What the desk held was measured before anything was touched. `IsIconic` on that handle
was true: the window was already minimized, and Windows had kept it as the foreground
because nothing else claimed it. Minimizing it again changed nothing, and so did the
shell's own `MinimizeAll`. A `Win+D` at the guest handed the foreground to `Progman` and
the next run read `clear`.

So the classification has a third shape it does not know. `Get-DeskLooks` drops the
desktop by class and the shell surfaces by class, and a minimized window is neither and
cannot be a question either: nobody can read it, so nobody can answer it. The reading is
available where the look is built — `IsIconic` beside the class check. What is worth
deciding is whether an iconic foreground is a null look, making the desk `clear`, or its
own word, saying a desk was left with a stale foreground and letting the run go on.

## Block K — The proving ground — a fixture app built to be hard to test

### §WW379 the control that is late on purpose

WW366 wanted a case where a control settles after the act returns, and this fixture has
none. `CaseRunTests` builds four Win32 controls by hand and every one answers the
instant it is asked. So the check that landed pins the mechanism through a reading that
differs by *projection* — a checkbox reads `On` through its patterns and `Wrap lines`
through its name — and the timing half is asserted by nothing.

The gap is older than this task. `SlowMachineTests` excuses three checks a run because
the desk was slow, which is slowness by accident; a control late on purpose is a
different thing and nothing here can ask for one. WW353 found the defect by reading
`CaseRun` rather than by running it, and WW366 could only close it the same way.

The fixture is the place for it, and Block K is what that block is: an application built
to be hard to test. A control whose reading arrives a declared number of milliseconds
after the act returns would give this suite the one case it cannot currently write — and
it is exactly the shape a real application has, where a click starts work and the label
catches up.

What to decide is where the delay is declared. A flag on the fixture is one answer and a
control that is always late is another, and the second is a control every unrelated case
would have to wait for.
