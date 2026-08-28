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

Both became `5`, the last character sent; seven for seven, over 35 packets for five
rounds. So it is not one character but one **or more**, each taking the value of the
last element written.

`Keyboard.Send` builds one input pair per UTF-16 code unit, each a fresh struct, into a
single `SendInput`, the union written so its size is right on x64. That path has no
defect producing this, and **the array is ruled out with it.** `SendInput` inserts the
events serially and returns once they are queued, so nothing the array does afterwards
can reach a message — the standing suspect was never one. What is left is between
insertion and `WM_CHAR`, which is the queue.

`VK_PACKET` before `TranslateMessage` is measured out. Every lParam is `00000001`, the
scan code having eight bits where a code unit needs sixteen. The count is all it claims.

### §WW260 Some expected sets are the application's data, not its strings

`covers` is the answer to the hardcoded list and it derives from one place: the language
files the project declares. That is right for the set it was built for — every tab
header the strings declare has to read somewhere — and it is the wrong well for the menu
case.

What that case counts is profiles. The script asked the application, running it with a
flag that prints them, and compared the submenu against what came back: one entry per
profile plus the two toggles the submenu carries. Neither half of that is in a strings
file. The profiles are this machine's data, and the number is whatever this machine has.

Typing it is the defect `covers` exists to refuse, one well over. A case asserting two
profile entries goes on asserting two after a third is added, and it says nothing when
it stops covering what it was written for — which is the shape a run reported as *all
three tab headers read* against a four-tab window.

`needs` is the near miss and does not answer it. A requirement is measured as present or
absent by the adopting project's runner, and this is a count the expectation is about
rather than a condition the case runs under. What is missing is a set the application
itself reports, declared once where the project declares its strings, and derived per
run like the other one.

### §WW269 A minute of tolerance, and only a minute

The reset caption names when a quota window turns over, and it counts down while the
window is open. Read twice across a round trip it is legitimately different: a run that
crosses a minute boundary changes it by one, and nothing about the application is wrong.

`sameAs` compares exactly, so the caption cannot go through it. Dropping the claim is
worse than tolerating the minute: an hour of drift is another profile's window, which is
precisely the defect `WW81` was filed against.

**The script's answer cannot be copied, and that is the finding.** It parsed the caption
to minutes by matching `(\d+)\s*d`, `h` and `m` — English letters. In the four other
languages this application ships those letters differ, so a parser keyed on them reads
nothing and the tolerance silently becomes an exact comparison. That is `T361`: an
assertion matching English is loud when it fails and silent one step over, where it
matches nothing and passes.

So the shape is open rather than decided, and two candidates are worth measuring before
either is built. Compare the digits and ignore the letters, which is
language-independent and assumes the caption counts down with its smallest unit last. Or
have the application report the instant through the in-app half and compare instants,
which assumes nothing about the text and costs a reading the adopter has to provide.

What it must not become is a general tolerance on `sameAs`. A percentage is the same
number or it is not.

### §WW275 covers is two-way and reads as one-way

`covers` holds where nothing is missing **and** nothing is unexpected. That is
deliberate and it is the right default: the tab set it was built for is the whole of
what a `TabItem` locator matches, and a window carrying one more tab than the
expectation had heard of is exactly the defect it exists to catch.

The README documents half of it — *every string declared under it reads somewhere this
step's locator matches* — and a reader writing a case from that sentence gets a red they
cannot explain from it.

Measured migrating `WW84`. The claim wanted was *the sidebar offers every settings panel
the strings declare*, and the sidebar items are the only elements addressable by their
words, so the locator is `Text`. All six matched. It failed on nine strangers — a
section heading, a value caption, `Save`, `Cancel` — because the panel beside the
sidebar is full of Texts and no locator separates the two. There is no one-way form to
fall back on, so the claim went unwritten.

Its sentence has a plural fault as well: *'a', 'b', 'c' were read and **is** declared
nowhere*.

Two things are owed. The documentation should say both directions, in the paragraph that
teaches the field. And a case that means the one-way claim should have a way to say so,
rather than reaching for a locator that cannot exist.

## Block G — The scenario — a case is a data file

### §WW258 The notification area is a surface no locator addresses

Every locator in a case resolves against the window the fixture launched. The
notification area is in the shell's tree and not in that window's, so there is no
locator that reaches it — and the icon has no clickable point either, which is why
`NotificationArea` addresses it by rectangle and finds it by the name the shell gives it
rather than through the grammar at all.

The engine does the hard parts already. It opens the overflow flyout, because an icon in
it is not in the tree until it is open; it puts the flyout back; it names the icon by
tooltip, which is the only name the shell exposes; and `OpenMenu` answers whether a menu
came up with the desk fact that stopped it where one did — the collapse `WW174` closed,
so that a shell which would not open the flyout is not read as an application that would
not show a menu.

None of it is nameable from a data file, and `WW82` is the only case in any of these
repositories that drives it. The icon is named rather than located, so what a step
writes is a name in `with` and not a locator — which means the shape here is not another
verb over the existing grammar but a second kind of subject, and that is the question
worth settling before anything is added.

### §WW259 A submenu that only opens to a key needs the verb that presses one

`expand` asks ExpandCollapsePattern, and every other synthesised verb has a pattern act
beside it for exactly this reason: which of the two a case names is the whole of what an
interaction loop is for. The menu walk has no such pair. `Menu.Enter`, `Menu.To` and
`Menu.Expand` go in the way a keyboard user goes in, and nothing a step may write
reaches them.

The defect that shape exists to catch is already on record. A WinForms submenu that is
empty when the menu opens exposes no ExpandCollapse at all, draws no arrow, and the
shell handles Right as *activate a plain command* — which dismisses the whole menu. A
mouse hover always worked, which is precisely why it went unnoticed until something
drove it from the keyboard. A case naming `expand` against that menu would ask the
pattern, get nothing to ask, and report a control rather than the gesture.

So this is the fifth pair: `expand` through the pattern and a menu walk that presses
Right, with the same rule as the other four — the walk needs the foreground, so a desk
that refused it is a hole naming the absence and never a red about the application.

It is separable from `WW258`. That one is about reaching a menu at all; this is about a
menu that is already open, and it is the same question for a menu bar as for a tray
icon's.

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
