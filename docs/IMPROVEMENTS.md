# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW626 Known folders a fixture redirects

Found in Modelado (D:\Git\alegauss\modelado), MOD95: generated photos are kept in
Pictures\Modelado, which the app finds through Environment.GetFolderPath(MyPictures),
the Pictures known folder. A case must check that four candidates land on disk before
one is pressed, in a folder of its own, not the guest's real Pictures where earlier
runs' files make names collide and reads ambiguous. The fixture schema says it plainly:
"variables" reach only an application that reads the variable, and one asking Windows
for the known folder still reads the real profile. No fixture field redirects Pictures
(or Documents, Desktop, Downloads).

Workaround in Modelado: a variable only cases set, MODELADO_GENERATED, read before the
known folder (MainWindow.GeneratedPhotos.cs), and the fixture "an empty window with
Ideogram stood in by the fixture server" sets it to {files}/generated. The app now
carries a flag for its tests.

What winwright could offer: a fixture field mapping known folders to staged directories,
e.g. "knownFolders": { "Pictures": "{files}/pictures" }, applied for the launched
process only (SHSetKnownFolderPath on a throwaway profile, or a per-process redirection
the engine sets up and tears down), refused with its reason where the desk cannot do it.

## Block C — Locate — the locator grammar and the tree an agent reads

### §WW625 The taskbar Jump List as a locator root

Found in Modelado (D:\Git\alegauss\modelado), MOD94: recent files feed the taskbar Jump
List, and the task owes a case that opens two files and finds both in the app's Jump
List, on the installed build and on the MSIX package. A locator resolves inside the
application's own windows, so the Jump List, a shell window Explorer draws when the
taskbar button is right-clicked (or a pinned Start tile's list), cannot be reached; nor
can a case right-click the app's taskbar button. The step wanted:

{ "locator": "Taskbar > Button[name^=\"Modelado\"]", "act": "open jump list" }, {
"locator": "JumpList > ListItem[order=top]", "act": "read", "reads": "name", "expect":
"cube.obj" }

Settled for in Modelado: nothing; MOD94's Jump List half is deferred, since shipping it
unread would be a claim no case holds.

What winwright could offer: a verb that opens the application's Jump List from its
taskbar button (through UIA on Shell_TrayWnd, or ICustomDestinationList read back
through the shell's automatic destinations) and a locator root for that shell window,
tied to the fixture's process so another app's button is never pressed; on a desk
without a taskbar the refusal says so.

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

### §WW624 Text that fits its control

Found in Modelado (D:\Git\alegauss\modelado), MOD87: the window gets a Brazilian
Portuguese translation, and the task asks for a case that launches in pt-BR and confirms
the main buttons "render without clipping". German (MOD88) asks the same with longer
words.

What the case can say today: `within` (WW539) claims a control's rectangle lies inside
its panel's, so a button pushed out of the side panel fails. What it cannot say: that
the words on a control fit it. A WPF Button with HorizontalAlignment="Stretch" in a 340
px panel keeps its rectangle inside the panel whatever its caption, and a longer caption
is cut at the button's edge (ContentPresenter clips; a TextBlock with TextTrimming shows
an ellipsis). The UIA name still reads the whole caption, so `reads: name` passes too.
The step wanted:

  { "locator": "Button#ObjectButton", "act": "read", "reads": "fits", "expect": "fits",
    "named": "the translated caption is not cut off" }

Settled for in Modelado: `within` on the main buttons and the window title read by name,
in cases\window.cases.json beside a comment naming this line; a cut-off caption still
passes.

What winwright could offer: a reading that compares the text's drawn extent with the
element's rectangle. UIA gives no text extent for a label, so the likely route is the
in-app half (Block I): the app's helper reports, per element, whether its text was
trimmed or exceeded its arranged size (WPF: TextBlock's DesiredSize against RenderSize,
or TextTrimming having trimmed); failing that, a capture-based check that the caption's
pixels reach the control's edge. The refusal where neither is available should say
which.

### §WW627 A popup read without the foreground is unchecked, not failed

Found in modelado (cases\window.cases.json, the MOD94 case "a start lists the recent
files newest first on the drop zone..."), run on a host desk on 2026-11-15 instead of
the VMware guest. Explorer kept the foreground for the whole run: no case had started
it, and the window under test never got the foreground at launch.

On that desk winwright marked every drop step Unchecked, naming the absent precondition
"the foreground belongs to the window under test", which is right. But these two steps:

    { "locator": "Button#ToolbarRecent", "act": "invoke", ... },
    { "locator": "MenuItem#RecentMenu-1", "act": "read", "reads": "name", "expect": "cube.obj", ... }

came back as a plain failure: "nothing in the window matches MenuItem#RecentMenu-1: it
was renamed, removed, or it is behind something that is not showing". WPF opens the
toolbar's ContextMenu and closes it at once, because its owner is not the foreground
window. The same step failed the same way on the commit before any of that session's
changes, so the product was not at fault, but the verdict read as a product failure and
it took a worktree run of the old commit to tell.

What winwright should do: when a step reads under a popup (a MenuItem, or what an invoke
just opened) and the window under test does not own the foreground, mark the read
Unchecked with the same foreground precondition the drop steps name, not Failed. At
least the failure's diagnosis should say the foreground belonged to another process.

### §WW628 File steps that read text

Modelado (MOD110, Open in slicer): the Export card remembers the slicer picked in
`settings.json` under the fixture's `LOCALAPPDATA`, so the next session offers it first.
The case `the Export card opens the model shown in the slicer picked, as a 3MF` in
`cases\window.cases.json` wanted to end on:

    { "file": "{files}/Modelado/settings.json", "act": "read",
      "matches": "\"slicer\": \"OrcaSlicer\"", "named": "the slicer picked is kept" }

A `file` step takes only `sameBytesAs` (WW535) or `image` (WW573). `settings.json` holds
other keys whose bytes the case does not own, so `sameBytesAs` cannot say it, and
nothing else reads a text file's content. The case stops at the button's name and the
process started. The "remembered" half rests on code review alone, since the window has
no unit-test project.

What winwright should offer: a `file` step that reads a text file (UTF-8, BOM tolerated)
and claims `matches` (a regex over the whole text) and `expect`/`contains` the way a
reading does, waited for over the resolve timeout like the other file claims, with a
failure that quotes the first lines it read. A JSON-aware claim (`json: "$.slicer ==
OrcaSlicer"`) would be nicer but `matches` covers it.

### §WW629 Reading the launched process's memory

Where it happened: Claude Code Tray, T452. Its idle ceiling shipped as a `--selftest`
section that launches the exe over a fixture, reads `Process.PrivateMemorySize64` once
startup settles, and fails above 220 MB. It cannot drive windows, and a window never
released only shows after it has been opened and closed, so that half of T452 waits on
the requirement `winwright-memory`.

The case it wanted: launch the tray fixture, open the main window from the icon, close
it, three times, then compare the launched process's memory with a number the case
states:

    { "expect": "memory", "of": "fixture", "privateMb": { "atMost": 220 } }

winwright reads only the UI tree of what it launched, so the case stops at "the window
opened and closed" and the memory claim lives in the project's selftest, where no window
ever opens.

What to offer: an expectation over the fixture's own process. Private bytes through
`Process.PrivateMemorySize64` (in-box); the managed heap for a .NET target if it can be
read without injecting anything. A settle option, since a reading during startup work is
another number: read once the value moved under N MB over S seconds, and a timeout is a
hole, not a pass. The verdict names the value and the bound; a reading that could not be
taken is a hole.

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system

## Block N — The name — winwright becomes paneproof
