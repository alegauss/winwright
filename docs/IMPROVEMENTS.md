# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

### §WW235 A collision timed against a thread pool

`FinishedTests.A_replacement_lands_through_a_reader_that_briefly_holds_the_destination_open`
failed twice in one session, both times `UnauthorizedAccessException` out of `File.Move`
— the last go, the one outside the retry loop, which throws what the collision actually
was rather than swallowing it. Earlier runs in the same session passed.

The arithmetic is the whole finding. `Finished` retries 8 times with 25ms between them,
so it has about 175ms to get through a collision. The case holds the destination open
and releases it with `Task.Delay(BetweenMs * 2).ContinueWith(...)` — fifty milliseconds,
scheduled on the thread pool. A suite of 1,574 cases saturating that pool is what
decides whether fifty becomes two hundred, and the runs that failed took 4m45s and 5m18s
where the ones that passed took 3m20s.

So the case is timed against the machine's load rather than against the code, and it
gets less true as the suite grows. That is the shape this project refuses by name: a
control is a timing claim, and one measured on a machine that changed measures the
change.

Raising `Attempts` is the wrong repair — it edits the product to suit its test. What the
case needs is a release that does not queue behind the suite: its own thread, and a hold
measured against the budget rather than expressed as a multiple of one constant that
happens to be the same one the retry sleeps for.

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

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

### §WW78 The keyboard case, first

It is the shortest path through the whole framework - launch under a named host,
navigate by clicking a control with no automation peer, resolve by id, type, read back
through a pattern, traverse, and drive a range - and it is the case whose absence let a
window ship with no keyboard input at all. Migrating it first means the engine is
exercised end to end before anything else about it is claimed.

### §WW79 The panes case runs on every machine

This assertion used to live inside the profiles case, which opens by counting profiles
and skipping below two. That is right for a round trip and wrong for the property that
made the round trip readable: a tab body being in the tree has nothing to do with
profiles, and behind that skip it did not run on a single-profile machine - which is
most machines and every hosted runner. Migrating it separately is what keeps the two
apart.

### §WW80 The sessions case is the argument for the whole loop

A popup is its own top-level window, so no render over a page's content can photograph
it and no published screenshot ever will. Whether that note is readable at all is a
question only the accessibility tree can answer. The case also waits out an asynchronous
scan, expands a row into a tree and puts the surface back afterwards, which makes it the
widest single test of locate, act, wait and restore in one place.

### §WW81 The profiles case is the only thing that drives the picker

Every capture renders one profile, which is structurally incapable of seeing three
defects that all need a second switch and two of which need it to come back. It also
carries a timing claim - a line that must never be observed on the way back - and that
claim is void unless the walk reports how many selection changes it took. Migrating it
proves the hop count and the watch-while-waiting shape both survive the move.

### §WW82 The menu case reads the notification area

Nothing else in any of these repositories opens a tray menu, and everything hard about
it is Windows-specific: an icon with no clickable point, an overflow flyout that has to
be opened before the icon is in the tree at all, a right-click the current shell does
not deliver, and a submenu that expands only by keyboard. It is also where the
expectations are derived from the application's own read-out instead of typed by hand.

### §WW83 The switch case rewrites a real setting

Until that case existed, the path that rewrites the setting, re-keys the stores and
takes the other account's token ran under no check at all. It is refused against a
resident process, because a pick there would repoint the real icon for real. Migrating
it inside the store comparison asserts the promise that a run touches nothing at the one
place most likely to break it.

### §WW84 The names case observes what no screenshot can

A picture cannot see an accessible name, and an unnamed control is invisible to every
other check. The case sweeps every panel the page declares - derived from the navigation
labels, so a panel added later is covered with no edit here - and reads every control
the naming rule is responsible for, covering both the branch that must fire and the
branch that must not. Getting the second wrong gives three controls in one row the same
name, which is worse for a screen reader than one unnamed.

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

### §WW228 One package reference, and one line nobody mentions

claude-tray's csproj sits at the repository root, so every default glob the SDK applies
reaches everything beneath it. Adding `tests/ClaudeTray.Cases` there made the
application compile the driving project's sources — and its `obj` — into itself. The
build failed with eight `CS0579` duplicate-attribute errors, every one of them naming a
`_wpftmp` project, which reads like a WPF build problem and says nothing about tests. It
took moving the folder out of the tree and building again to know what it was.

The fix is one line — `DefaultItemExcludes` rather than a `Compile Remove`, because it
is every glob and not only that one. The cost is that nothing told anybody, and the
failure names the wrong thing.

This is the adoption story's own gap. The README says an application under test takes
the in-app half by one package reference and by nothing else, and `samples/Adopter`
proves it. Neither says where the project that *drives* the application goes, and the
obvious placement is the one that breaks. An adopter whose app is not at the root never
sees this; one whose app is at the root loses an afternoon to a WPF error message.

What closes it is the adoption section naming the requirement, and `samples/Adopter`
growing the shape that proves it — an app at a root with a driving project under it,
building.

### §WW230 The feed is a folder in the tool's own tree

`Winwright.0.1.0.nupkg` exists in `packages/`, which is gitignored, and nowhere else. So
claude-tray's driving project carries a `nuget.config` naming
`..\..\..\winwright\packages` — a path that assumes the two repositories are cloned side
by side, and a path into the tool from the project adopting it.

That is precisely what block I's criterion forbids for the in-app half, and
`samples/Adopter` exists to prove it: one package reference, no path into this
repository, no second package. The driving side has no such proof and now has a
counter-example.

It is written down as a bootstrap in the file itself rather than left to be found, and
the comment says the day the engine is published that file is deleted and nothing else
about the adoption changes. That deletion is the measurement — the same shape as this
block's first criterion, where the proof is what goes away.

Two things it also costs today. The package has to be repacked by hand after any engine
change, and WW78's own migration would have loaded a case naming verbs the packaged
engine did not have if that had been forgotten. And an adopter cannot restore at all on
a machine that has one clone. Measured in the guest the moment WW227 could carry
claude-tray there: `NU1301: the local source 'C:\src\winwright\packages' does not exist`
— the tree was there, from this project's own runs, and the folder never travels because
it is gitignored.

## Block K — The proving ground — a fixture app built to be hard to test

### §WW223 The repetition caught what a single green hides

`TrayPlacementTests.Adding_one_and_finding_it_holds_every_time_rather_than_most_times`
repeats five rounds precisely because a single green is what the old fixture produced
about half the time. On the guest run of WW67 it failed on round 1 — "it is on neither
the taskbar nor the overflow" — and passed on an immediate re-run of the same tree. So
the claim in the case's own name is false, and the evidence is the case doing exactly
what it was written to do.

**Ruled out.** This was first filed suspecting `TrayIconFixture.Add` of returning before
the shell had placed the icon, round 0 having just shut the flyout. WW220 had already
closed that: `Find` polls `Hidden()` for the name rather than reading once, and its
comment describes this exact sequence. Do not go back there.

What landed instead is the distinction the sentence was missing. "On neither" covered
two different things — an icon that is absent, and a flyout that shut while the poll was
running, after which `Hidden()` answers empty for the rest of the deadline and the
absence is assembled out of a desk that stopped being lookable. The second is now a hole
under the search's own condition, which is the shape WW168, WW174 and WW179 each caught
once. A genuine absence now carries how many icons the bar and the flyout held.

What is left waits on the next occurrence rather than on any work: it will say which of
the two it was.

### §WW232 Eleven holes the guest has every time

The first run after WW231 said it: `all 1574 discovered cases ran, and 11 check(s) were
excused - 11 for the foreground belongs to the window under test.` WW233 then named
them, and the eleven are two different things.

Five are correct.
`RefusedForegroundTests.A_click_that_could_not_be_sent_is_a_hole_naming_the_desk`,
`KeyboardTests.Typing_with_the_desktop_elsewhere_sends_nothing_and_names_the_intruder`
and three like them have the absent foreground as their subject. They should excuse, and
they should stay.

Six are losses, all added in one session, all the positive cases for `type`, `click`,
`press`, `nudge` and `moves`. None of them has ever run. They were reported as proven on
the strength of a green.

**Measured, and the first repair was wrong.** Launching the fixture with `--show`
changed nothing: the same eleven, the same list. The reason is in `Act`'s own header —
Windows refuses the foreground to a process that does not already own it — so a fixture
started by a test host that has no foreground cannot take one, whatever flag it carries.
`KeyboardTests` proves the same verbs today because `PumpedDialog` is an in-process
window, and its comment says exactly that: only a thread that owns one gets the
foreground.

So the six belong on a pumped dialog, not on a launched fixture — and WW226's slider
pane, added to prove `nudge`, is in the wrong process to do it. The count is what says
whether the move worked.

### §WW234 The pane that was not needed

WW226 shipped saying `Traversal.Nudge` had nothing driving it, so the branch that
reverses direction at the end of a range had never run against a real control.
`TraversalTests` drives it four times — including the refusal — against an
`msctls_trackbar32` child of a `PumpedDialog`, and its own class comment says the slider
starts at its minimum precisely so the direction has to be chosen.

The premise came from grepping the fixture application for `Slider` and finding none.
That is true and it is the wrong question: a verb needing the foreground is proven
against an in-process window, because Windows refuses the foreground to a process that
does not already own it. The pane was added in the one process that cannot serve it,
which WW232 then measured — six positive cases excusing themselves on every guest run,
`nudge` among them.

This is WW169's shape, and `Criteria`'s note on that says what would have caught it: run
the cases before building anything. A grep is not that.

What is left is a decision rather than a deletion. Three ranges in the fixture are still
the right shape for a `.cases.json` driving a real application — which is what block J
is for — and they are reachable by every verb that needs no foreground. What has to go
is the claim that they exist because nothing drove the verb, and `nudge`'s own proof
belongs on the trackbar that already existed.
