# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

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

### §WW611 A backdrop window rendered in-app

Met in claude-tray T449, in dark mode: the app's windows set `ThemeMode="System"`, which
leaves their background to the system backdrop (Mica). Every in-app render of them is
washed out: `preview.cmd shell`, `panels` and `context` all write a white page with
near-white labels. Only controls that paint their own fill (an accent button, a selected
row) read. The cases pass.

Seen with the harness at 0.1.0-alpha.18 and again at 1.1.7, which carries WW583's wait
for a fade, so it is not a picture taken mid-animation. The window's own background is
transparent under the backdrop, and DWM draws the backdrop outside the WPF tree the
render reads. Dark-theme foreground brushes are then composed onto white, because white
is what an empty pixel becomes.

What would close it, in the order a reader of the picture would want:
- When the render's root is a window whose background is transparent or a backdrop, paint the
  theme's window background (the resolved `ApplicationBackgroundBrush`, or the system window colour
  for the active theme) under the tree before rendering it. That is what a person sees there.
- Failing that, refuse the picture as one the engine cannot vouch for, which is the rule `capture`
  already states, instead of writing it and passing.

No workaround in claude-tray: giving the windows an opaque background would change what
users see (no Mica) only to make pictures right, which is the wrong way round.

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system

## Block N — The name — winwright becomes paneproof
