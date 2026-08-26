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

### §WW249 The proof that WPF takes input is itself intermittent

`WpfInputTests.Typing_reaches_a_wpf_text_box` is the negative control `WW246` was
missing, and it does not hold every time. Measured with nothing changed between runs:
host green, guest green, guest red, guest green. The red was `Failed` rather than a
hole, so the keys were sent and the read-back did not match.

What the red said was `Expected: Passed / Actual: Failed` and nothing else, because the
assertion threw its own evidence away — the fourth time in one session that a red here
was written without its diagnosis. Fixed: it now carries the result, so the next
occurrence names what the box read.

Then eight consecutive host runs of that one case, and none of them red. So it is the
guest that reproduces it, which is a fact about timing rather than about logic — a
slower machine, fewer cores, and a WPF window that has to be focused before a key means
anything.

The hypothesis that fits, and it is written down as a hypothesis: the focus arrives
while the keys are already going, so the box reads a part of what was typed. If that is
it then it is not a flake in the test — it is `Keyboard.Type` sending before the window
is ready, which is the engine's own contract being sometimes false.

Deliberately not settled by reasoning. Two runs were spent today refuting hypotheses
that looked certain, and the sentence that settles this arrives on the next guest red.

### §WW250 Between naming a value and naming nothing

`expect` names the value exactly. `answers` says there is one. There is no third, and a
real claim sits between them.

Measured while migrating `WW80`. claude-tray's list-price note interpolates the date its
rate card was read, so no case can name what it says — and the script it replaces
asserted exactly the right thing: that the note carries `\d{4}-\d{2}-\d{2}`. A figure
whose provenance has gone is the defect, and a note that lost its date still *answers*.

So the migration has to drop that assertion or weaken it to `answers`, and weakening it
is worse than dropping: it reads as covered.

The shape that fits is a `matches` field beside `expect`, taking a regular expression,
mutually exclusive with it as the other three claims already are. What it must not
become is a way to write a loose `expect`: `.*` is a claim that cannot fail, which is
the same unearned green `WW237` and `WW238` closed twice, and a pattern that matches
everything has to be refused the way an always-answering reading is.

There is a second reason to be careful. `T361` is written into that script: an assertion
matched the English words *list prices*, found nothing in the other four languages, and
reported a readable note as unreadable. A pattern is exactly where that mistake is
easiest, so whatever this becomes should be as hard to write against a translated string
as `expect` already is.

### §WW251 A disclosure is not one reading moving

`moves` claims one reading of one element ended up different. `covers` claims a derived
set was read across many. Neither says *there is more here than there was*, and that is
what a disclosure is.

Measured while migrating `WW80`. Clicking a conversation row unfolds the call tree that
produced it, and the script asserted two things about that: the row went from N readable
fields to more than N, and at least one of the new ones is a task line. Both are claims
about a subtree before and after an act, and a step can hold neither.

The claim is worth having beyond this case. A tree view, an expander, a details pane, a
search that fills a list — every one of them is *an act put something in the tree that
was not there*, and each is currently written as an expectation about one element
somebody picked out of the result, which is the hardcoded-list defect `WW236` closed one
level down.

What it must not be is a count somebody types. `at least 4 fields` is the same stale
literal as a listed set: the row grows a field and the case goes on asserting four. The
honest shape compares the subtree against itself a moment earlier, which is what `moves`
already does for one reading — the same idea over a locator's descendants rather than
over a single value.

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

Half of it is written and runs. The tab selects through the pattern in 52 polls, which
is the engine's retry replacing the script's hand-written three-attempt
Select-then-confirm loop; the info dot resolves and reads `Off`. The click that opens
the note is a hole on this desk — `explorer` holds the foreground — so the question this
case exists for is **not yet measured**: whether a popup in its own top-level window
resolves from the main window's root.

Three of the script's assertions cannot be written at all. A count of readable fields on
a row and a subtree that grew after a click are `WW251`; a value carrying a date rather
than equalling a string is `WW250`. The third is the one that matters most here: a note
that lost the rate-card date still *answers*, so weakening that assertion would read as
covered while checking nothing.

None of the three is dropped quietly. The file says in its own comment which are missing
and why.

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
