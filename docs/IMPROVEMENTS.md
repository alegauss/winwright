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

### §WW363 the count that cannot see a slope

WW289 gave every run its count against the run before it, WW298 made that four counts
rather than one, and WW300 said which of them recur. Between them a reader can tell a
busy desk from this suite's own structure, for a count that stays put and for one that
jumps.

What none of the three reads is a slope. Five runs over WW346 to WW350 excused 8, 8, 8,
9 and 10, and each rise was a different notification-area case: a chevron that opened no
flyout, an overflow that shut mid-search, input the run had not synthesised. Every
clause was true and none alarming — the count is compared, a new one is named, and a
case excused once and not again recurs with nothing.

The tray cases are the reason and they are correctly written. Each excuses the desk fact
it needs, so each is individually right; together they are a growing set of checks a
slow shell can take away one at a time, and the run that takes eight away reads like the
run that takes one.

So what is missing is a rate rather than a count. How often a given case is excused
across the ledgers already on disk is a number this could carry without measuring
anything new, and a case excused in half its runs is a different thing from one excused
in its first. What that number should do — reported, or a threshold — is worth deciding
rather than assuming.

## Block C — Locate — the locator grammar and the tree an agent reads

### §WW364 the promise the signature does not make

`Locator.TryParse` answers a bool and two outs, and neither out is annotated. So a
caller that throws on false still holds a `Locator?`, and the compiler has no way to
know the only route past that throw has one. `StepDeclaration` spells `parsed!` four
times for it.

The attribute is the one .NET puts on exactly this shape. `[NotNullWhen(true)]` on the
locator and `[NotNullWhen(false)]` on the reason say what the method already promises,
and every bang goes with them.

A bang is not a bug, and that is why this is filed rather than fixed in passing. Each
one is a place a reader has to rebuild the argument the compiler could have made, and
every rebuild is right today because the throw is directly above the use. What it costs
is that the next person to move code between the two has nothing telling them they did —
which is what happened in WW351: a construction moved above the one bang that had been
narrowing the rest of the method, and the compiler asked for a second rather than for
the annotation.

Small, and worth one check before it is done. An annotation makes reading a failed
parse's locator a warning rather than a habit, so what is worth knowing first is whether
anything does that deliberately — a caller collecting refusals rather than stopping is
the shape this verb exists for, and it is the shape most likely to look at both outs.

## Block D — Act — patterns before pointers

### §WW366 the two instants on one line

WW353 went looking for a click that fails a case and found it cannot.
`CaseRun.Attempting` puts both `expect` and `moves` through `Expect.That` with the act
budget, so the reading a verdict turns on is polled for after the act — and the act's
own single reading decides nothing about pass or fail.

What it does decide is the trace. `ActResult.AsTraceStep` sets `ReadBack` from `After`,
the reading `Synthesised.Landed` took the moment the act returned; the expectation then
polls and may settle on something else. So a run can print a step whose read-back is
from before the act landed, beside a verdict that passed on the value after it. Both are
true, they are about different instants, and nothing in the line says which.

It is the shape this project refuses everywhere else, moved one surface along. A trace
is what a reader opens when a verdict surprises them, and one that disagrees with the
verdict sends them to the control rather than to the timing — which is the afternoon
WW353's own entry described, relocated from the verdict to the page a person actually
reads.

Two candidates and both are small. The trace step could be composed after the
expectation, out of the reading it settled on; or it could carry both, which is honest
and is a second value per line. What decides it is whether a reader wants what the act
saw or what the run concluded, and those are different questions rather than one asked
twice.

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

## Block E — Capture — the picture that proves what it photographed

### §WW359 the popup the channel does not carry

WW347 opened the way through and WW349 built the channel — most of it. A harness now
sends a window handle and a path, and the application draws the tree behind that window
and says it did. What it cannot ask for is one popup's own tree, which is the surface
WW347 exists about: `Popups.Picture` is still reachable only from inside the process.

The gap is in what the message carries rather than in whether there is one. An open
popup is its own top-level window, so a handle reaches it — but what is wanted is the
tree it holds rather than the layered surface Windows composites. A closed popup has no
handle at all, and that is the case worth having: its child is in a tree the process
owns, so a preview of a flyout nobody has clicked is a picture this could take and no
copy of the screen ever could.

So the ask needs a second field, and what that field is, is the task. A popup's name is
the obvious one — `Popups.Under` already walks them and each carries the name its author
gave it — but a name is not unique across a tree, and the failure mode of the wrong one
is a picture of the wrong surface, which is what this block refuses everywhere else.
What is not in doubt is that both halves of the verb exist and only the sentence between
them is missing.

### §WW361 the window nobody remembered to answer for

`Renders.Answer` hooks one window, and the harness sends to the window it wants a
picture of. Both halves of that are right and together they leave the second window an
application draws answering nothing: a dialog, a wizard page, a tool window a run opened
on purpose is exactly the surface somebody reaches for a capture of, and it is the one
the application forgot.

Per window rather than per application was the correct call and is not the thing to
revisit. An application answering only for its main window would hand back a picture of
the wrong surface with nothing in the file saying so, which is the failure this block
refuses hardest. What is wrong is that remembering is left to the adopter, with no
reading anywhere saying which windows are covered.

The fixture is the evidence. It calls `Protocol.Renders(this)` in `OnContentRendered`,
which is the one window it has, so nothing here has ever needed a second — and the
README calls it one line, which is true of an application with one window and of no
other.

Two candidates. The application could hook every window as it is shown, which is
`Application.Current.Windows` and an event, and turns one line into one line for real.
Or the answer could carry what it is answering for, so a harness asking about a window
nobody hooked is told that rather than told the application does not take the message —
which is the same sentence today and two different faults.

### §WW362 the door that cannot say where pictures go

WW349 puts `WINWRIGHT_RENDERS` on the launch, derived from the project's `captures`, so
an adopter declares one thing and the default capture route works. That is
`Suite.Launch`. The other door hands every case a window a caller already has, and it
has no launch to put anything on — the application was started by somebody else, so
whether it may write at all depends on the environment that started it.

Both doors exist because each answers a question the other cannot, and this is the first
thing one of them can do that the other cannot. A run attached to a product already up
is what `AppTarget.AttachTo` is for, and a capture step in one answers a hole about a
variable, on a machine where whoever is running it never heard of the variable.

Nothing here is wrong so much as unsaid. The hole names what to set, which is the right
sentence for the launch door and an incomplete one here: what an attached run needs is
for the application to have been started with it, and there is no moment left at which
this run could have arranged that.

So the candidates are about honesty rather than mechanism. The refusal could say which
door it came through, since the remedy differs; or the attach door could read the
variable out of the target process and say whether it agrees with the project, which
turns a guess into a reading — and is the shape this project reaches for elsewhere.

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

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

### §WW365 the refusals that could ask the step

WW352 took the constructor apart and left `Of`'s body alone, which is where the
analyser's number actually comes from: S3776 reads 119 against an allowed 15, and it is
six hundred lines of refusals rather than the parameter list above them.

They cannot move out as they stand. Each reads the locals `Of` parsed — `back`, `apart`,
`declared`, `sweeping` and about twenty more — so a refusal extracted into a helper
would take half of them as arguments and buy nothing: the arity would move rather than
go.

What changed is that the thing they are about now exists before they run. WW351 moved
the construction above the refusals so the claim set could be read off it, and every
local those refusals read is a field on that step. A refusal that took the step and the
subject is two parameters, and a group of them is a private static named for the family
it turns away.

The catch is the one that made WW352 careful. A refusal names the field the case
actually wrote, and the step carries some families folded: `PointsAt` with `Pointing` is
four spellings in two fields. WW351 made the fold carry that precedence for the claim
set, and the same question has to be asked of each refusal moved rather than assumed —
which is why this is a task and not a tidy-up. The order is the other half: this suite
asserts which refusal wins where a step is wrong twice over.

### §WW367 the half the list does not dispatch

WW354 made the arms' names one list and stopped a mistyped word being answered by the
default. What it did not reach is which code each name runs. `Measured` still asks
`arm?.Name == "sweep"` and then `"delay"`, `"acts"`, `"provoke"`, so an arm added to
`Arms.All` and to no branch there is recognised, launched, and answered by the bare
typing run — the failure that entry was about, one level down and with a refusal now
standing in front of it.

Smaller than it was and the same shape. The word is checked, so a typo is refused; what
is not checked is that a recognised word has somewhere to go.

The reason it was left is that the four runners take four different argument sets.
`Sweep` and `FirstRead` want the box and two captions, `Landing` wants the root, and
`Disturbance` wants the window handle too. A delegate on `TypingArm` needs one shape all
four fit, so it needs a context carrying the root, the box, the captions and the handle
— a thing to design rather than extract, because what it holds is what a future arm may
reach for.

The cheap half is worth having whichever way that goes. `Measured` could refuse an arm
it has no branch for rather than falling past all four into the typing run, which turns
the remaining hole from a wrong answer into a red — and a red is what every other list
in this project gets for the same mistake.

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
