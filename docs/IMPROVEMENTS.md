# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW536 A second window a fixture opens

Found by quickshell (QS292, 2026-10-14) adopting 1.1.0. A drop with toDesktop needs a
window to land in, and for a file that is an Explorer folder opened on a directory the
case can check afterwards. A fixture launches the application under test and nothing
else, so the folder window cannot be put on the desk by the case, and ExplorerDropRun
opens it from C# instead.

What to build: a fixture may declare a second window the engine opens before the first
step and closes after the last, by a shell verb on a path token ({files}/landing, opened
with explorer.exe), registered like any launch so a window left open is named as a
survivor. Its locator root is the desktop, as toDesktop's is.

Falsified when a drop case needs C# to put its target window on the desk.

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

### §WW538 Clicking with another button

Found by quickshell QS310, where a middle click on a tab closes it, as it does in every
browser and terminal on Windows. The case cannot be written: the `click` verb takes only
a reason as its argument and always presses the left button, although `Pointer.Click`
already takes a `MouseButton` and `Win32` already sends the middle button's down and up.
quickshell proves the gesture with a unit test that raises WPF's routed events on the
tab item instead, which is the substitute the evidence rule says not to need.

The engine half exists, so this is the grammar's: a step names the button, say
`"button": "middle"` (or `right`) beside `"act": "click"`, defaulting to left so no case
changes, and the step's rendered line and its refusal name the button. A right click
opens a context menu, which QS311 in quickshell will want next for a tab's Close others.

Done when a case middle-clicks an element of the fixture app and reads what the click
did, and a case naming an unknown button is refused with the buttons it takes.

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

### §WW535 A file a case can read

Found by quickshell (QS292, 2026-10-14) adopting 1.1.0. WW530 lets a case drop a file
onto an Explorer folder with toDesktop, and the README says whether the file arrived is
the next step's to check, which for Explorer is a file on disk and not an element. No
act or reading reaches a file, so the claim quickshell's ExplorerDropRun makes, that the
dropped file is there byte for byte, still cannot be written as a case.

What to build: a reading of a file the fixture named, by a path token the engine
resolves (such as {files}/landing/report.bin), which says whether it exists and whether
its bytes equal another named file's, waited for as any read-back is. Its absence after
the wait is a red naming the path; a different length or content says where the first
byte differs.

Falsified when a case cannot say that a file dropped onto Explorer landed with the bytes
it had.

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing
