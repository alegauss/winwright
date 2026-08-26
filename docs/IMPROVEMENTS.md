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

### §WW230 The feed was a folder in the tool's own tree

`Winwright.0.1.0.nupkg` existed in gitignored `packages/` and nowhere else, so
claude-tray's driving project carried a `nuget.config` naming
`..\..\..\winwright\packages` — a path that assumes two clones side by side, and a path
into the tool from the project adopting it. Measured in the guest the moment WW227 could
carry claude-tray there: `NU1301: the local source 'C:\src\winwright\packages' does not
exist`. The tree was there; the folder never travels.

Answered by publishing to nuget.org from `.github/workflows/publish.yml`, keylessly.
Trusted publishing means no API key exists in this repository or its settings: the run
asks GitHub for an OIDC token, nuget.org exchanges it for a key good for an hour, and
the file's own name is half of what nuget.org trusts. Renaming it breaks the publish,
deliberately.

A release is one manual dispatch. It raises the last number of the declared version —
one rule that reads as the prerelease counter or as the patch — writes it into every
copy, packs, and only then publishes, tags and cuts the release. Publishing first is the
decision: a tag says a version exists, so nothing can point at nothing.

The copy list was four files long and the suite went red on the fifth, the README. That
is the net working, and is why the concordance check runs after the rewrite rather than
the list being believed.

What is left is the deletion the criterion measures.

### §WW239 Where the version lives is spelled twice

`publish.yml` raises the version by rewriting five named paths, and
`Winwright.Concordance` checks four of them because the CI step names those four on its
command line. Neither owns the list. The knowledge that a version lives in
`Directory.Build.props`, `.claude-plugin/plugin.json`, two sample projects and the
README is spelled in a YAML array, in two workflow invocations, and in `ReadmeTests` —
and the first of those was wrong on its first run, which is how the fifth copy was
found.

The net held, and that is the only reason this is an improvement rather than a defect:
the concordance check and `ReadmeTests` both run after the rewrite, so a forgotten file
is a red in the same run. But a net is not an owner. A sixth copy added tomorrow reaches
neither the array nor the check, and the failure it produces is a package that disagrees
with the tree that built it.

The shape that would own it is a verb on the tool that already reads them: the same
flags that say which copies to compare would say which copies to raise, so a copy the
rewrite forgot is a copy the check was never told about either — one list, and adding to
it does both.

What it must not become is a sweep over every file mentioning the old version.
`docs/CHANGELOG.md` names versions that have shipped, and a release that rewrote its own
history is worse than a stale pin.

### §WW240 The language belongs to the fixture, not to the project

`DerivedSet.From(declaration, under)` refuses a project declaring more than one
`languageFiles` entry, and the refusal is right: picking the first would derive an
expectation in a language nobody is looking at. But the consequence is that an
application shipping five languages has to declare one of them and pretend the other
four are not there.

Measured while migrating claude-tray, which ships `en`, `es`, `fr`, `pt-BR` and `pt-PT`.
Declaring all five made `covers` refuse; declaring only `en.json` works, and works
*because* every fixture in that repository launches with `--lang en`. So the answer the
engine needs is already written down — one line above, in the fixture — and it is being
supplied instead by a project-wide declaration that happens to agree with it.

The shape that would own it: a fixture says which language its window is in, and a set
derives from the file for that language. A project then declares every file it ships,
which is what it actually has, and two cases in one file may read two languages without
either lying.

This is not academic for this block. claude-tray's Names case is *about* the languages —
it reads accessible names across them — and it cannot be migrated while a run can only
resolve one. It is also what makes `destructive` entries resolvable by key in a window
that is not in English.

Until then the single declaration is the honest thing, and the comment beside it says
why it is not the general answer.

### §WW243 The first migrated case was written and never run

`WW78` wrote claude-tray's keyboard case, its runner and its project declaration, and
the whole of it was checked by loading rather than by running. Running it says:

> `[step 2] typing reaches the WPF text box — NotActionableException: Edit#DirectoryBox cannot take this act: nothing matched, or what matched has gone since.`

So the case stops two steps short and reports `Broken over 0 of 3`. `DirectoryBox` is
real — `src/Ui/SettingsPage.xaml` declares it — which leaves the step before it: a click
on `Text[name="Claude Code"][order=left]`, whose whole job is to put that page on
screen. Either the click did not land, or it landed somewhere that is not the sidebar
item, and the case cannot tell those apart because a navigation is not a check.

That is the shape `WW79` ran into from the other side and is worth saying once: a step
with no expectation is a navigation, and the step after it is what proves it worked.
When the proof is a *resolve* failure, the report names the box and not the click — so
the reader is sent to the wrong half of the case.

What this needs is a run against the window, reading what the sidebar actually offers,
in the same way the panes case was settled. It is not a guess about the locator; it is
the measurement nobody took.

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
