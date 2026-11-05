# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW599 Name the guest log's host path

Met in wintimeline on 2026-11-04 (WT160, the clip copy and paste task). Its
run-tests-vm.cmd calls winwright's runner with -Run "run-tests.cmd Debug" and -ResultsIn
"TestResults\reports". The guest run went red in WinTimeline.Cases. The console the
session read was filtered (Select-String on "com falha|bem-sucedido|suites|desk"), so
the lines naming the broken case and its step were dropped, and the run looked as if it
had left nothing behind. The session searched the tree's bin and .cache folders, found
no log, and started the ten-minute guest run again only to read the failure.

The full guest log was on the host all along, at TestResults\vm\vm-run.log.
tools/run-tests-vm.ps1 copies it there (copyFileFromGuestToHost into $results, lines
1739-1740) and prints its text whole, but no Write-Host ever names the file or the
folder. A reader who has the console cut short, by a filter, a terminal's scrollback or
a tool that keeps the last lines, has no way to learn that a complete copy exists.

What winwright should offer: the runner's last lines name where the guest log was kept
(and the screenshot and every -Bring file, which already have a line of their own for
the picture), for a red run above all. Something like "  log
D:\...\TestResults\vm\vm-run.log (the guest's console, whole)" printed after the
verdict, so the path survives any tail of the output. Done when a red run's final lines
name the log's host path, with a test over the runner's closing lines.

### §WW600 The desk note claims only what the run can show

Met twice in wintimeline on 2026-11-04. Both guest runs started on a desk read as
'shell' and went red. tools/run-tests-vm.ps1 then printed its WW472 note (line 1941).
The note says the run "excuses its desk checks and reds the few that cannot excuse one,
which reads as a broken tree and is not."

Neither red was the desk. In WT52 the app's automation peer kept a stale selected flag,
and the app fix turned the next run green on the same kind of desk. In WT160 a case
selected a tab before its document had finished opening. vm-run.log says so: "broke
[step 2] ... nothing matched ... TabItem #TimelineTab0 (selected)". Every other case
passed both times, so the fixtures had the foreground.

The note prints on every non-zero exit after a 'shell' or 'stale' start. Its hedge, to
compare the excused count with earlier runs, leaves the reader a comparison whose
numbers the runner holds.

Blaming the desk only on a high excused count cannot be built for an adopter: that count
comes from winwright's own roll call (Roll.cs, Against), the runner never parses it, and
wintimeline writes no excused.txt. So the note stops claiming the tree is not broken. It
says the desk started as 'shell' or 'stale', quotes the roll call's excused line where
vm-run.log holds one, and otherwise says the runner cannot tell and the red is the
suite's to read first. Done when a log with that line and one without each have a test.

## Block C — Locate — the locator grammar and the tree an agent reads

### §WW601 Shut doors that say what would open them

Met in wintimeline on 2026-11-04 (WT160). A case clicked TabItem#TimelineTab1, read
"Showing reel2" in the status bar, and then clicked Pane#TimelineSurface >
ListItem#V1-2. The click missed with: "nothing in the window matches ListItem#V1-2: it
was renamed, removed, or it is behind something that is not showing - MenuItem #Item 1
(expanded), MenuItem #FileMenu (expanded), MenuItem #EditMenu (expanded), MenuItem
#ViewMenu (expanded), MenuItem #HelpMenu (expanded), TabItem #TimelineTab0 (selected)
are shut."

The words in brackets name the state each door would have to reach (LocatorMiss.cs
returns "expanded" for a collapsed menu and "selected" for an unselected tab). They read
as each door's current state. "TabItem #TimelineTab0 (selected)" was read as tab 0 being
selected, and two more ten-minute guest runs went into chasing a tab that switched back
on its own. The real fault was the app's surface keeping the last timeline's automation
children. The list also names every shut menu and tab in the window, though none of them
contains the pane the locator went through. What winwright should offer: say the act
that would open each door, not a bare state word ("TabItem #TimelineTab0, not selected;
MenuItem #FileMenu, collapsed"). Leave out doors that are not ancestors-to-be of the
locator's resolved prefix, so a miss under a pane that was found names only what could
hide inside that pane. Done when a miss under a resolved Pane does not list the window's
menus and tabs, and each door says its state unambiguously, with tests over both.

## Block D — Act — patterns before pointers

### §WW602 Double click from a case

Met in wintimeline on 2026-11-04 (WT56, opening a compound clip). Its design says a
double-click on a compound clip on the GPU-drawn timeline opens it in a sub-view, and
the line's criterion asks for a UI case that double-clicks one. The step that was
wanted:

{ "locator": "Pane#TimelineSurface > ListItem#V1-1", "act": "click", "with":
"PointerIsTheAct", "clicks": 2, "named": "the compound is double-clicked" }

The engine already has the gesture: Pointer.DoubleClick in
src/Winwright/Acting/Pointer.cs (PointerAct.Clicks, "which is how a double click is
said"). But nothing in the case grammar reaches it. Scenarios has no clicks key and no
"double click" act, so a case file cannot say it. The workaround in wintimeline is a
second route to the same command: Enter on a selected compound opens it. The case
presses Enter, and the double-click is left without a case, with this line's id beside
it in the case file.

What winwright should offer: a click step that takes "clicks": 2 (or an act "double
click"), sent as one input batch at the system's double-click interval and distance,
with the same holding, button and PointerIsTheAct rules as click, and the step's name in
the report saying it ("double click Pane#... > ListItem#V1-1"). Done when a case
double-clicks a list row that opens on a double click only, with a test over the refusal
of clicks other than 1 and 2.

### §WW603 Click at a point of an element

Met in wintimeline on 2026-11-04 (WT57, heat lanes on the timeline's overview strip).
The strip is drawn on a GPU surface, and a click within a few pixels of a hot spot moves
the playhead there. The case wanted to click the strip at the first marker, 8.9% across
and 6 pixels down:

{ "locator": "Pane#TimelineSurface", "act": "click", "with": "PointerIsTheAct", "at":
"8.9%,6", "named": "the strip is clicked at the first marker" }

A click lands only on the element's clickable point, its centre for a pane, and no key
moves it. WW572 gave drag a from and a to inside one element, in the same "x,y" grammar
(a percentage or device-independent pixels per half), but click has nothing like it. The
workaround in wintimeline is a drag from "8.9%,6" to "12%,6", which works only because
the app acts on the press and ignores what follows. The case says so in a comment with
this line's id. The claim is weaker: a drag is not a click, and an app that acts on
release or treats a move as a drag would fail it for the wrong reason.

What winwright should offer: click takes the same point as drag's from ("at": "x,y", a
percentage or device-independent pixels from the element's top-left), with holding and
button as they are, and the report naming the point ("click Pane#TimelineSurface at
8.9%,6"). Done when a case clicks a canvas at two points that answer differently, with a
test refusing a point outside the element.

### §WW604 Keys held across a drag

Met in wintimeline on 2026-11-04 (WT70, moving clips on the GPU-drawn timeline). A plain
drag of a clip overwrites where it lands. The same drag with Ctrl held at release
inserts, rippling what follows. Ctrl+Alt swaps it with the clip it lands on, and Alt
held at the press drops a copy and leaves the original. The line's done-when asks for a
case of each. The steps wanted:

{ "locator": "Pane#TimelineSurface", "act": "drag", "from": "52.9%,48", "to":
"88.2%,48", "holding": "ctrl", "named": "c3 is dragged and dropped as an insert" }

WW540 gave click a holding key (ctrl or shift, sent in one batch with the click), and
the README says holding beside any act but click is refused. So a case can only make a
plain drag. In wintimeline only the plain drag has a case. Insert, swap and duplicate
are proven by model tests of the same edits (TrackEditTests), and the gesture that picks
the edit by the key held has none.

What winwright should offer: drag takes holding as click does, and the key list widens
to alt and ctrl+alt (alt alone is the duplicate gesture). The keys go down before the
press, stay held across the move and come up after the release, all in one input batch
so nothing arrives between them, with the step's name in the report saying them ("ctrl
drag Pane#TimelineSurface from 52.9%,48 to 88.2%,48"). Done when a case drags with each
key on a canvas whose drop differs by the key held.

### §WW605 Digits pressed alone

Met in wintimeline on 2026-11-05 (WT81, multicam). The number keys cut a multicam clip
to an angle, and the case pressed one:

{ "locator": "Pane#ViewerSurface", "act": "press", "with": "2", "named": "2 is pressed"
}

The case loaded, since Chord.TryParse accepts a bare digit. But the step broke at run:
"InvalidOperationException: '2' reached the act and is not a traversal key, so the load
accepted what the act cannot run".

The cause is in src/Winwright/Scenarios/ActVerb.cs. Chorded starts with
Enum.TryParse<TraversalKey>(argument, ignoreCase: true, out _), and Enum.TryParse takes
a numeric string as an enum value, so "2" parses as the third traversal key and Chorded
returns null. Press then calls Traversing("2"), which matches by name and throws.
Letters are not numbers, which is why "A", "B", "M" and "S" press fine. Every digit 0 to
9 (and any number within the enum's range) takes this path.

The workaround in wintimeline is Edit > Cut to angle menu items, which the app has a use
for anyway. But the number keys themselves, the gesture an editor uses during playback,
have no case, and the case file says so with this line's id.

What winwright should offer: Chorded and Traversing agree on what a name is.
Enum.IsDefined on the parsed value, or a name-only match like Traversing's, keeps a
digit a chord. Done when a case presses "2" on a window that counts its digit keys, with
a test over "0" to "9" and a number outside the enum.

### §WW606 Starting the application again after an exit

Found by WinTimeline (WT164): a tab chosen and the window closed without saving should
reopen on that tab. The evidence the rule wants is the reopened window, but a case can
only follow a process that relaunches itself ('process: relaunches'). After 'process:
exits' the case can read files and nothing else. The project had to compare the
sidecar's bytes with a fixture instead, which ties the case to the JSON's layout and
line endings rather than to what the user sees.

Build a step that starts the application again the way the fixture launched it, with the
same staged files and arguments, and lets the steps after it read the new window: an act
such as 'launch again' on a process subject, never a spelling that starts with 'starts',
which WW586 already gives to a program the application starts. It is refused before an
exit has been claimed, so a case cannot kill a live window by accident. The new process
joins the list the run stops at its end (Following.Relaunched), and a single-instance
application whose mutex outlived the exit is a named refusal, not a timeout. The
fixture's 'before' and 'files' do not run again, because the point is to read what the
previous run left behind.

### §WW607 Every dump a hung run wrote comes back

Found by WinTimeline (WT168). The run hung at the test's hang timeout. The hangdump step
wrote two dumps in the guest's results folder: one of the application under test and one
of the test process that drives it. run-tests-vm.ps1 brings back only the largest
'*.dmp' as hangdump.dmp. Here that was the test process, stuck in winwright's
Resolve.Diagnose waiting on UiaFind. The question was whether the application was
deadlocked or just slow, and only the application's dump answers it, so it stayed in the
guest. The project then had to reason about the cause from the client side and rerun the
whole gate.

Bring back every dump the run wrote, together with the hang logs beside them. The test
host's dump keeps the name hangdump.dmp, which Show-Blame, Winwright.Blame,
runner-arms.ps1 and four test classes read; every other dump comes back beside it under
its own name (WinTimeline_4436_hang.dmp). The final report should name each one and say
which process it is: the application from the project's declared executable, or the test
host. A size cap per run can keep the copy bounded, with the report naming what it left
behind.

### §WW608 Waiting out what an invoke started

Met in claude-tray T435 (a Context rescan used to drop the "All projects" selection).
The case it wanted, against `--context --window --sample --observing --lang en`:

```json
{ "locator": "#ProjectList > ListItem[order=top]", "act": "select", "expect": "selected", "reads": "selected" },
{ "locator": "Button#RescanButton", "act": "invoke" },
{ "locator": "#ProjectList > ListItem[order=top]", "act": "read", "reads": "selected", "expect": "selected" }
```

The last read cannot tell a rescan that kept the row from one that has not happened yet.
WPF's `InvokePattern.Invoke` posts the click, so the read can run before the handler
starts, and the row is still selected then on the broken build too. The case passes on
the defect it exists for.

Nothing in the grammar waits for the act's consequence. The page shows `Scanning…` in
`Text#ScanInfo` while it works, but `absent` on that text passes before the handler has
set it, and `unlike` against the footer read before the click fails whenever the new
footer is word for word the old one (a warm fixture scan often is).

Workaround: a scratch UI Automation script in the session scratchpad, not in the repo.
It selects through `SelectionItemPattern`, invokes, sleeps 4 s and reads `IsSelected`.
It went red on the old logic and green on the fix, but no case in `cases\` holds it, so
the next rescan regression ships.

What would close it: the claim rides on the act step, armed before `Invoke`, since a
later read cannot see a value that already passed. Something like `"passes":
"context.scanning"` (a key, as `never` takes) on the invoke, read at a second locator
such as `Text#ScanInfo` through UI Automation events, since polls miss a short flash. A
value never seen is a failure naming it.

### §WW610 Enter and Space on a focused element

Met in claude-tray T438, which made the Sessions rows, the live chart's project strip
and the Context overview's rows reachable from the keyboard: Tab focuses one, and Enter
or Space opens it. The case it wanted, against `--main --observing --lang en` under the
tray's WinForms pump (the only host where a keyboard bug shows, claude-tray's T135):

```json
{ "locator": "TabItem#SessionsPane", "act": "select", "expect": "selected", "reads": "selected" },
{ "locator": "#SessionList > DataItem[order=top]", "act": "press", "with": "Enter" },
{ "locator": "#SessionList > DataItem[order=top]", "act": "read", "reads": "expanded", "expect": "expanded" }
```

`press` sends its key at the window, not at an element, so the case has no way to say
"with focus on this row". Enter, Space and Escape are already keys it can name: a chord
may be a bare key (Chord.cs, the Named table). Tab-walking the window to the row would
make the case depend on the focus order of every control before it.

The claim settled for: the code ships, and the keyboard path is unproven by any case.
Pointer clicks and the `invoke` pattern were already covered, so the one route T438
added is the route nothing checks.

What would close it: a `focus` act that takes focus on its element the way `type`
already does (Keyboard.TakeFocus) and reads `focused` back, followed by the `press` of
today. Making `press` take focus first would change every existing case that sends Tab
or a chord from wherever focus stands, so the press keeps its meaning.

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system

## Block N — The name — winwright becomes paneproof
