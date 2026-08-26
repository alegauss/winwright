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

The first run after WW231 said it: `all 1571 discovered cases ran, and 11 check(s) were
excused - 11 for the foreground belongs to the window under test.` Eleven, on a green
run, on the machine built so these cases could run at all — and all eleven for the same
fact, which is what makes it one cause rather than a busy desk.

That is worth more than the number. A single condition means it is not contention:
nobody is typing on the guest. It is the window under test not owning the foreground at
the moment those cases ask, which points at the launch rather than at the desk. `--show`
exists in the fixture precisely because a suite raising a window thirty times a run must
not take the desk, and these are the cases that need the opposite.

Also measured, and it is why this was invisible: WW229's positive case was almost
certainly among the eleven. It came back green, its duration looked like a real launch,
and nothing in the results said whether it proved the claim or excused itself. The
reasoning was sound and the evidence was absent.

Nothing here should become a red. What should happen is that eleven becomes zero, by
giving those cases a window that owns the foreground when they ask — and the number is
now the thing that says whether it worked.

### §WW233 The count without the names

WW231 records one line per excuse, and the line is the desk fact. That is enough for the
sentence — eleven, all for one condition, which is a cause rather than a busy desk — and
it is not enough to act on. WW232 needs the eleven cases, and the ledger cannot name
them.

Half of it is already in hand. `BusyDesk.Excused(AssertionResult)` holds a verdict that
carries the assertion's name, so those sites could write it beside the condition at no
cost. The other overload takes a bare precondition and knows nothing, which is honest
and is most of the call sites: the reading it excuses is not an assertion yet.

What that asymmetry means is worth naming rather than papering over. A ledger where some
lines carry a name and some do not is a ledger a reader has to hold two rules for, and a
count that says "eleven, four of them named" is worse than the count alone. What closes
it is the excuse knowing the case regardless — which xunit will hand over through a
test's own context, at the cost of every one of the eighty-one sites taking an argument.

That is the trade. It is not obviously worth paying, and this exists so the next person
deciding has the measurement rather than the impression: eleven holes, one condition, no
names.
