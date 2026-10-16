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

### §WW543 A staged file written now

Found by quickshell QS335. Its client opens an agent's feed only when the feed was
written in the last ten minutes, which is the right rule for a person's desk. A case
stages a feed with `files`, and the staged copy keeps the modification time the file had
in the tree, because staging is `File.Copy`, and `fill` writes the file again only when
a token changed its text. So the case passed on the day the fixture was made and failed
the next day, with nothing changed, saying only that no tab appeared.

The workaround is a `{files}` token in the fixture's text, which forces the fill to
rewrite it. That puts a path in the fixture that means nothing to it, only to touch the
file.

What to build: a staged file is written now, as though the application had just been
given it, unless the fixture says otherwise. Either stage with a fresh modification time
always (touch after the copy), which matches what "a store this case put there" means,
or add a per-file `"modified"` field naming an age, `"now"` by default or `"-2h"` to
stage a stale one on purpose, so a case can test both sides of a rule like the client's.

Done when a fixture file staged a day after its source was last written is seen by the
application as just written, and one marked `"modified": "-1d"` is seen as a day old.

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

### §WW544 A modifier pressed alone

Found by quickshell (QS354) on 2026-10-16. Its menu bar is hidden by default and shown
the Windows way: Alt pressed and released alone, with no other key between. A case
cannot do that. A chord must end in a key (`Chord.TryParse` takes the last part as the
key and checks it against letters, digits, F1 to F24 and the named keys), so `"with":
"Alt"` is refused with "'Alt' is no key this can press", and `Alt+Escape` is a different
gesture, since the release of Alt after another key is not a tap.

The same gap stands in front of every Windows app with a classic menu: Notepad,
Explorer's ribbon access keys, and any WPF or WinForms app whose MainMenu answers Alt.
Each is reached by a modifier alone.

What to build: a press of a modifier alone, spelled `"with": "Alt"` (and Ctrl, Shift,
Win for symmetry), sending that modifier's down and up with nothing between. It is
reported as "Alt, alone" in the step's name, so a trace does not read as a chord that
lost its key. Refusing a modifier held with a modifier alone (`Ctrl+Alt`) stays as it
is.

quickshell's case shows the bar through its palette instead until this exists. That
proves the bar and not the gesture a person uses most.

Proven by a fixture case: Alt alone toggles a menu bar in DragsPane's window, and the
menu is read from the accessibility tree.

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

### §WW545 The substitution, on a terminal

Found by quickshell (QS361) on 2026-10-16. Its case "clicking a tab on the strip puts
that tab's terminal on screen" types `title first-tab-marker` into a terminal, a
`Document[class="Terminal"]` that publishes the Text pattern. It failed in two guest
runs out of about twenty, reading `first-tabrmarker` and then `first-tab-markrr`. Both
are WW310's signature exactly: the last code unit of the string, `r`, put where an
earlier one belongs, length intact. Each time, the next run passed.

WW249's backstop cannot reach this. `TookTheLastSent` compares a value read back with
the string expected, then erases and sends again. A terminal's text is its whole screen,
not the string typed, so the comparison never matches, and what was sent cannot be taken
back: a terminal runs it. So the roughly 2% fault reaches the case unrepaired,
indistinguishable from a defect in the application.

What to look at: the send itself, since a repair after it cannot work here. One idea is
to send each code unit as its own SendInput call with its own key-up, rather than as one
array, then measure the fault rate on a terminal across a few hundred rounds, as WW310
measured on a text box. Alternatively, have `type` refuse to report green on an element
whose reading cannot hold the expected string, naming the fault as possible.

Proven by quickshell's tab case, or a fixture terminal, passing three hundred guest runs
in a row with no substitution.

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing
