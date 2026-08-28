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

### §WW298 One predecessor is not a baseline

WW289 shipped the comparison and it works: a run says "8 excused against 8 the run
before", and a number that was meaningless alone now has something beside it. The
weakness is in the shape, and is worth writing down before somebody trusts it further
than it goes.

One predecessor is a difference, not a baseline. The measurement that started WW289 was
49 against a steady 8 — but a notification toast is not a thing that appears for one run
and leaves. Where the desk stays busy for two runs, the second reads "43 against 43" and
says, in the tool's own words, that nothing changed. The anomaly becomes its own
baseline exactly when it is worst.

What is wanted is not an average, which hides the same thing more slowly. It is the
several most recent counts, said as they are: 49, where the last four runs excused 8, 8,
43 and 8. That is read in one glance and needs no rule for what counts as a jump — no
threshold to tune, and no run quietly promoted to normal by repetition.

The storage is already right for it. Each run files its own ledger under the history
root, `Readers.ExcusedBefore` already orders them by write time, and reading four rather
than one is a `Take` and not a new mechanism. What changes is `Roll`, which holds one
nullable count where it wants a short list.

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

**Which side it is on, answered.** `Arrivals` records every `WM_CHAR` the fixture's
window is delivered, read off the message pump below WPF. On the sixth red it said:

        typed WW246-1     the control read WW146-1
        Windows delivered WW146-1

So the characters **arrived at the window already substituted**. Not a text box that
lost one under load — the window was handed the wrong text — which rules out WPF, the
control and everything above the queue. It is the send.

**The eighth sharpens the rule and breaks half of it.** Seven reds fitted *one character
becomes the last one sent, length for length*. The eighth substituted two:

        typed WW246-5     delivered W5245-5

Both became `5`, the last character sent, seven for seven. So it is one **or more**,
each taking the last element's value — and the ninth, `2W246-2`, put it at index 0, so
no slot is fixed either.

`Keyboard.Send` builds one input pair per UTF-16 code unit, each a fresh struct, into a
single `SendInput`, the union written so its size is right on x64. That path has no
defect producing this, and **the array is ruled out with it.** `SendInput` inserts the
events serially and returns once they are queued, so nothing the array does afterwards
can reach a message — the standing suspect was never one. What is left is between
insertion and `WM_CHAR`, which is the queue.

`VK_PACKET` before `TranslateMessage` is measured out. Every lParam is `00000001`, the
scan code having eight bits where a code unit needs sixteen. The count is all it claims.

## Block G — The scenario — a case is a data file

### §WW296 Six fields for two ideas

`covers`, `coversAtLeast` and `coversWithin` are one set compared three ways. `sameAs`,
`unlike` and `sameCountdownAs` are one earlier step compared three ways. Inside, each is
already a target and a mode — `Sweeps` with `Matching`, and one pointer with a flag
beside it — and the format publishes the modes as six keys.

All six landed in one session, each from a real migration and each justified where it
sits. That is how a grammar grows sideways: no single field is wrong, and the sixth is
not obviously worse than the fifth.

The argument for keeping them is this project's own. WW267 refused a second meaning for
`with` because a step whose argument means different things depending on what the
application contains is one nobody can read, and a mode key brings that back — `covers`
plus `covering: within` is two things to hold to know what one step claims, and a file
omitting the mode reads as the exact claim whether or not the author meant it.

The argument against is the count. Six keys where a reader looks for one idea is a
format that teaches itself badly, and the families are already spelled as families: the
prefix says which idea and the suffix which mode, in the name rather than in a value.

Not a refactor to reach for. What it wants first is a reader who did not write them
saying which costs more, because the author of the sixth is the worst judge of that.

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

### §WW78 The keyboard case, first

It is the shortest path through the whole framework - launch under a named host,
navigate by clicking a control with no automation peer, resolve by id, type, read back
through a pattern, traverse, and drive a range - and it is the case whose absence let a
window ship with no keyboard input at all. Migrating it first means the engine is
exercised end to end before anything else about it is claimed.

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

## Block K — The proving ground — a fixture app built to be hard to test
