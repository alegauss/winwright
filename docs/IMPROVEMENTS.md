# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW542 A token for the project's root

Found by quickshell QS292, the drop of a file from an SSH server's side onto an Explorer
folder. The server is the guest's own sshd, signing in as a second local account, so the
file it serves has to be one that account can read. The fixture's staged directory is
under the runner's own temp and is not; the project's checkout, under the drive's root,
is. So the case types the checkout's path into the client's remote path box, and has to
spell it whole: `"with": "/C:/src/quickshell/cases/fixtures"`, the guest's checkout as
quickshell's guest scripts place it. A desk with the checkout anywhere else reds the
case for a reason the case never states.

`{files}` is replaced in a fixture's arguments, variables, working directory and
`opens`, and `{reach0}` in a filled file, but nothing names the project's root, and a
step's `with` takes no token at all.

What to build: a `{root}` token, the directory winwright.json sits in, replaced in a
step's `with` beside `{files}`, and in the places `{files}` already is. A path with
forward slashes is the application's spelling to ask for, so a second form, `{root/}`,
gives the same path with `/` separators and a leading `/` before the drive, which is how
Windows OpenSSH names it.

Done when a case types `{root}/cases` into an edit, reads back the checkout's own path,
and an unknown token in `with` is refused naming the tokens that exist.

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

### §WW540 Adding to a selection

Found by quickshell QS314, whose sidebar now takes several rows at once (an extended
ListBox: Ctrl+click adds or removes a row, Shift+click takes a range, Ctrl+A everything
listed). The design's evidence is a case that chooses two sessions and a folder and
reads IsSelected on each row, and none can be written: the select act replaces the
selection through SelectionItemPattern.Select, and a click holds no modifier.

Two routes, and the pattern one is the better: SelectionItemPattern.AddToSelection and
RemoveFromSelection are what UI Automation offers a multiple-selection container, and
they need no foreground. So `"act": "add to selection"` and `"act": "remove from
selection"`, read back through IsSelected like select is, and refused naming the
container where it is not multiple-selection (CanSelectMultiple false). A click with a
modifier held, `"with": "Ctrl"` or `"Shift"` beside a click, is the pointer's half for
an application that answers the gesture and not the pattern, and can follow.

quickshell proves the selection in process instead (SessionSelectionTests, through
ListBoxItemAutomationPeer), the substitute this line retires.

Done when a fixture case adds three items of a multiple-selection list and reads each
selected, removes one and reads it unselected, and the same act on a single-selection
list is refused naming why.

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

### §WW539 Claiming an element sits within another

Found by quickshell QS312: a tab's close button was in the tree, 22 by 22, and painted
nothing, because the theme's padding left its glyph no room. Every case passed, since a
case can read that an element exists and what it is named, but not where it is drawn or
whether it fits inside the element holding it. The design asked for a case reading
CloseTab's bounding rectangle inside its tab's, and none can be written: `reads` takes
anything, value, range, toggle, selected, picked, expanded, text, name, description,
status, enabled and focused, and nothing about geometry.

A claim, not a reading, fits the evidence rule best: `"within": "<locator>"` on a step
asserts that the subject's UIA BoundingRectangle lies inside the named element's, and
fails naming both rectangles; an empty rectangle (zero width or height, or offscreen) is
a failure of its own, since that is the shape the defect took. Raw numbers as a reading
would invite cases that pin pixels a DPI change moves.

quickshell proves its fix in process instead (TabStripTests renders the button and
counts painted pixels), which is the substitute this line exists to retire.

Done when a fixture case asserts a button within its container and green, one clipped by
a narrow parent fails naming both rectangles, and one laid out zero wide fails as empty.

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing
