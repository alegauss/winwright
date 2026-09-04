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

### §WW381 the pause on the wrong side of the send

WW329 put fifty milliseconds after the send and the fault went away, and every reading
since has been about what happens during the drain. WW342 acquitted the pumping over
4800 dispatched messages. WW355 acquitted four cheap readers over 3200 rounds. WW368
walked the arm to the act and the rate appeared on the rung that adds `SetFocus` before
every round — a provider round-trip issued on the line *above* the send, where the pause
this engine pays is spent below it.

So the pause is guarding the reader, and the reader has now been acquitted twice. What
provokes is on the other side of the keys.

The candidate is a pause after the focus rather than after the send, measured rather
than assumed. `Keys.FirstLookMs` is one constant two verbs sleep on, so trying the other
placement means the tool holding both: a rung that focuses, waits and sends, beside one
that focuses and sends. If the fault follows the focus the interval moves and costs the
same; if it does not, both calls need one — a worse answer and a true one.

Two things make this worth doing rather than filing and forgetting. The measurement is
already built: `transfer` runs the ladder and one more rung is a line. And the prize is
the same one WW355 chased — an act with no interval in it at all — reached from the end
nobody has looked at.

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

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

### §WW380 the wiring the list cannot read

`TypingArmTests` holds the arms to `run-typing.cmd` in both directions and says so at
the top: *nothing here runs an arm — the tool takes the desk for minutes, and a guest
run should not pay for a question asked once.* That is right, and it is why WW367's own
check is a reflection assertion about a constructor parameter rather than about
behaviour.

So what is now guaranteed is that an arm carries *a* delegate. Which delegate is not
checked by anything. `sweep` pointing at `FirstRead.Run` would compile, pass every case
in that file, and print the wrong experiment's numbers under the word a person typed —
which is WW354's failure again, arrived by a different route: not an arm with no branch,
but an arm wired to the wrong one.

The lambdas make it invisible to reflection. Each is a compiler-generated method on a
display class, so the target says nothing about `Sweep` or `Landing`, and comparing them
to each other only proves four lambdas are four lambdas.

Two ways out. Each runner could carry its own name, checked against the arm's — one more
thing to keep in step. Or the arms could be method groups rather than lambdas, adapting
the four signatures at the runner rather than in the list: then `Run.Method` names a
real method on a real type and a case can assert which. The second removes a spelling
rather than adding one, which is the direction this project has taken every other time.

### §WW385 the branch between the field and the ask

WW372 added `popup` to the step and every case that proves it stops one layer above a
run. `OwnRenderTests` drives `PopupInto` against a real answering window and reads the
verdict off `RenderAsked`; `CaseRunTests` and `ScenarioFileTests` prove the field
parses, refuses under the wrong verb, and arrives on the step. Nothing declares a
capture naming a popup and runs it.

So the wiring in `CaseRun.Captured` is asserted by nothing, and each of its three lines
is a way to be wrong: a popup step taking the copy route photographs the window and
passes, `PopupInto` handed the wrong handle answers about another window, and the ask
fetched with its reading dropped answers nothing. All three end in a green with a file
beside it.

What it needs is a case that runs, and the pieces exist: `AnsweringWindow` draws a popup
and answers for it, and `CaseRunTests` runs declared cases against real windows under a
project declaring `captures`. What is missing is the join — declare a capture naming
`AnsweringWindow.PopupNamed`, run it, read the file's pixel count. The popup's child is
90x40 against a 240x160 window, so the count says which tree was photographed and a run
that took the window cannot pass.

The red belongs in the same case: the same declaration with a name no popup has,
asserting the verdict is a failure rather than a hole — which is the half of WW372 a run
has never produced.

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

### §WW382 the difference that stops at the desk

WW369 read both tray menus' trees and the reading is the same on each: a `Menu` holding
two `MenuItem`s, named as the adopters name them. The Win32 kind had never been read as
a shape at all, so what an adopter's locator would find in a `TrackPopupMenu` was
unmeasured until this ran. It is what they find in the drop-down.

That is worth more than the check it landed as. WW322 built the pair because the kinds
differ at the *desk*: a Win32 popup answers the focus reading and a drop-down does not,
and three adopted cases failed on it for weeks. What WW369 measured is that the
difference stops there — inside the tree they are one shape, so a locator proven against
either is proven against both.

Nothing says so. The two cases sit apart, each asserting its own kind, and a reader
comparing them has to notice that the assertions are word for word identical. The claim
that they *must* be identical is the useful one, and it is one line: the two trees,
rendered, are equal.

What that would catch is a framework changing one kind and not the other — which is
exactly the failure WW356 was, arriving from the other side. It also gives WW322's pair
a sentence it does not have: what these two fixtures differ in is the desk, and a case
that asserts the trees agree is where that stops being folklore.

### §WW383 the other list nothing reaches

WW370 gave `Get-DeskLooks` its desktop list as a parameter and a case now runs the
branch that answers `$null`. The shell surfaces are the other list in that file and they
got nothing: `Read-DeskState` decides `shell` against `$script:ShellSurfaces`, read
straight off the script with no way in.

The two are not the same shape. The desktop list is read by the loop, which WW370
opened; the shell list is read by the classification, which cases already reach —
`Classified` hands it looks somebody typed, so a case names `Shell_TrayWnd` in a made-up
one. What is missing is the join: nothing puts a real window up, calls its class a shell
surface, and reads `shell` out the far end.

That join is what WW357 was for on the other branch, and it caught a real defect there —
a look whose class read empty classified perfectly, so a quiet desk would have refused
every run. The same defect on this path would send a reader to a guest console to answer
the taskbar, which is WW331 arriving from the loop instead of from the words.

The change is the one WW370 already made, one list over: a `-Shell` parameter on the
polling would let a case say its own window is the taskbar and assert the runner is told
`shell` rather than `asking`. Cheap, and it turns the one classification a person acts
on by *not* going to the console into something a case has actually produced end to end.

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
