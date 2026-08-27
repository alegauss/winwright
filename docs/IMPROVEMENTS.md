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

### §WW257 A tray is a launch with nothing to attach to

`Suite.Opened` waits for the launch to draw a window and refuses where none arrives,
which is the right answer for every fixture written so far: nothing about the case was
observed, so nothing about the application was.

A tray is the counter-example, and `WW82` is blocked on it. claude-tray's
`--second-tray` puts an icon in the notification area and draws no window at all; the
window in that case is what a left-click on the icon is supposed to *produce*, which is
the claim rather than the precondition. Refusing the fixture makes the one thing being
asserted a reason not to run.

The launch itself is already right. `ProcessRegister` starts it, keeps what it was
launched with and stops it inside the project's budget, and none of that assumed a
window. What assumes one is the wait after it and the root every locator resolves
against.

So a fixture needs to be able to say that this launch draws nothing, and the run needs
to hold it as a process rather than as a window — with the refusal kept for every
fixture that did not say so, because a launch that was *supposed* to draw a window and
did not is still the failure this refusal was written for. A flag that turned that off
everywhere would buy one case and cost the answer on all the others.

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

`WpfInputTests` is the negative control `WW246` was missing, and it does not hold every
time. Measured with nothing changed between runs: host green, guest green, guest red,
guest green. The red was `Failed` rather than a hole, so the keys were sent and the
read-back did not match.

What the red said was `Expected: Passed / Actual: Failed` and nothing else, because the
assertion threw its own evidence away. Fixed then, and it paid: the next guest red named
what the box read.

Then eight consecutive host runs of that one case, and none of them red. So it is the
guest that reproduces it, which is a fact about timing rather than about logic.

The hypothesis was a focus arriving while the keys were already going, so the box reads
part of what was typed. That is refuted. `WW246` was typed and the box read `W6246`:
five characters for five, one **substituted** rather than lost, and the replacement is
the character two later in the same string. Not a truncated read — the order the keys
arrived in, which points at the send.

The typing path was then read for a defect that would produce it and has none: one input
pair per code unit, each a fresh struct, one `SendInput`. So what is owed is a
reproduction rather than another hypothesis. The case now drives five rounds with a
different string each and names the round it failed on, which is `TrayPlacementTests`'
answer to the same shape.

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

### §WW261 One declared string is an expectation too

`covers` derives the strings *under* a key and claims each reads somewhere the locator
matches. That is the answer for a set. For one control it is no answer at all: a leaf
key has no children, so the derivation comes back empty and the sweep is broken rather
than failed — which is right, and leaves the claim unwritable.

So a case asserting that the profile picker announces its label has to type the label.
The script never did: it called the strings file for every one of them, and `WW84` is
nine such reads. A typed label is the hardcoded set with one member — it goes stale the
day somebody edits the string, and it is wrong in the other four languages this
application ships from the moment it is written.

The shape is the derivation `covers` already does, stopping one level higher: a key that
names a string rather than a parent of them, resolved for the language the run resolved,
compared against the reading the step names. It is not `covers` with one member — that
claims a set was read *somewhere*, and this claims *this* element says it.

The refusals it needs are the mirror of the sweep's: a key that names an object rather
than a string, and a key nothing declares, both refused where the author wrote them.

### §WW262 The flat sweep is over elements, not over strings

`covers` is one claim over many elements and it is about *strings*: every value the key
declares reads somewhere the locator matched. The names case needs the other axis —
every element the locator matches answers something, where the case cannot know and
should not care what.

The predicate is not *non-empty* and the engine already knows it. `NameCheck` sorts a
name into spoken, missing, a font glyph, a template nobody filled in, and the automation
id handed back — and the last three all satisfy non-empty while being silent, or worse,
to a screen reader. It has been in `Winwright.Asserting` since block F and nothing in a
case file reaches it.

What it is for is the shape a settings page repeats: thirty-odd rows across six panels,
one naming rule, and the assertion written against three controls of one panel. A case
that names those three covers the rule where it was already known to work, and adding a
row to a panel nobody listed is covered by nothing — the hardcoded-list defect wearing
element clothes.

It is not `answers` over many. `answers` asks whether a reading said anything; this asks
whether what it said is a name, which is the whole of what `WW175` measured and what a
picture can never show.

### §WW264 A name that belongs to the row next door

A sweep proves a control announces something. It cannot prove the something is its own
row's header, because it has no idea which row the control is in — and the failure that
hides there is worse than the one it does catch. A rule that pairs the wrong two things
gives three controls in a row one name, and a screen reader then reads the same label
three times over three different controls.

The claude-tray rule has two branches and the case has to cover both. The one that must
fire: a picker, a slider, a text box or a switch with no content of its own takes its
row's header. The one that must **not**: a field with a labelled button beside it keeps
the header on the field, and each button keeps its own text. Only the second branch can
produce the duplicate, which is why the case that only checked the first was checking
the easy half.

What makes it askable is that the row has an automation peer of its own, so its header
is readable and the controls under it are reachable from it. The claim is then
structural rather than textual: of the controls in this row, each announces this row's
header or its own text, and none announces a header belonging to another row on the page
— with the set of headers derived from the page rather than listed.

### §WW268 The other half of a comparison with a step

`sameAs` claims a reading is back to what a named step read. The profiles case needs the
opposite as well, and for the same reason it needed the first: two accounts can read the
same percentage, so the switch is judged on the pair rather than on either alone.

The script wrote it as *the report follows the picker* — the used percentage at the
second stop differing from the first's, or the reset caption landing in a different
minute. An identical reading at both means the panes were never repainted, or were
repainted with the profile being left behind, which is the defect the whole case exists
for.

`moves` is the near miss and cannot answer it. It compares a reading across one act in
one step, and this compares two steps with a walk and a wait between them. The reading
being compared is also not the one the act was about: the picker moved, and what has to
have moved with it is a number somewhere else on the page.

It is the same machinery `sameAs` already has — the recall of a named step's reading —
with the comparison the other way round, so what is left to decide is the word rather
than the mechanism. Whatever it is called, it takes no value: the point is that a case
cannot know either number, only that they must not match.

### §WW269 A minute of tolerance, and only a minute

The reset caption names when a quota window turns over, and it counts down while the
window is open. Read twice across a round trip it is legitimately different: a run that
crosses a minute boundary changes it by one, and nothing about the application is wrong.

`sameAs` compares exactly, so the caption cannot go through it. Dropping the claim is
worse than tolerating the minute: an hour of drift is another profile's window, which is
precisely the defect `WW81` was filed against, and the caption is one of the two
readings that would show it.

The script drew the line where the reasoning does. It parsed the caption to minutes and
allowed a difference of one, treating anything larger as a red — and where the caption
could not be parsed at all it fell back to comparing the strings, so a format it did not
recognise was still checked rather than waved through.

What this is not is a general tolerance on `sameAs`. A percentage is either the same
number or it is not, and a comparison that quietly allowed some drift on every reading
would turn the round trip into a claim nobody can state. The tolerance belongs to
readings that are about a clock, declared as that, so a reader of the case can see which
claim was weakened and by how much.

### §WW270 The label a reading must not be

The live strip's headline is a reading either way. What tells a working one from a
broken one is which string it is: the application declares a label for *throughput
unavailable*, and a headline still reading it once a stop has settled means the tail was
disposed on the way out of a profile and never restarted for the new config directory.

A percentage cannot see that — the numbers are all present and all plausible. It is the
one defect in the profiles case that no reading of a value would catch, and the claim is
the shape `expect` has turned around: not this string, where the string is a key the
project declares rather than words typed into a case.

`never` is the near miss and answers a different question. It watches a whole window
across a wait and stops looking when the locator arrives; this is one element at rest,
after everything has settled, and a headline that goes back to *unavailable* a moment
later is exactly what `never` would miss.

`matches` cannot do it either. A negative lookahead would have to name the English
words, which is what a translation rewrites — and a pattern matching the empty string is
already refused, which is what the naive spelling of *not this* becomes.

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

### §WW263 A case cannot repeat itself over a set it did not list

Steps are an array and the array is written out. That is right for every case so far,
and it is what stops `WW84`: the panels it visits come from the `settings.nav.*` keys,
so the number of steps is data the file must not carry. Listing them is the defect the
derivation exists to refuse — a panel added later is swept by nothing, and the run
reports a clean pass over the panels somebody remembered.

The script solved it with a loop and paid for it in the ways a loop costs. It counted
panels opened apart from panels asserted, because a panel with no row control had still
been visited and rolling the two together would report it as one that got away. It
guarded on the derived list being empty before walking, because zero panels makes every
assertion inside run zero times and report nothing at all — a clean run over nothing,
which is this project's founding defect reached through a language flag.

Both of those are the runner's job here, not the case's. What the format needs is a
step, or a group of them, that says *once for each member of this derived set*, with the
member reaching the locator — and the two guards owned by the engine: an empty
derivation is broken and never passed, and what was visited is counted apart from what
was asserted.

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
