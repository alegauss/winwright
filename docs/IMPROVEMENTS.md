# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

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
