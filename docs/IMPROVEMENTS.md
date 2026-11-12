# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW619 Launching through a file association

From Modelado's MOD75 and MOD78 (11/11/2026). The Store package declares a
`uap:FileTypeAssociation` for .glb, .obj, .ply and .stl, and the claim is that
activating a file through the association opens it in the window.

Landed (12/11/2026): a fixture's `activates` names a file and `through` a ProgID, and
the launch goes through `ShellExecuteEx` with `SEE_MASK_CLASSNAME`
(`src/Winwright/Processes/Activation.cs`). An extension with no association, an
unregistered ProgID and one not claiming the type are refused before anything starts;
the process the shell started is taken on with `ProcessRegister.Adopt`, outside the
WW586 job, and the fixture's report line says what it starts is untracked; a process
gone before it is held, or one that exits without a window, is refused and never
attached to. `ActivatesTests` proves it in the guest against the fixture app under a
per-user ProgID.

What is left: a packaged application's user model ID in `through`, which is refused
today ("not supported yet"). Build it as
`IApplicationActivationManager.ActivateForFile(aumid, items, "open", out pid)`, the
items from `SHCreateItemFromParsingName` and `SHCreateShellItemArrayFromShellItem`, and
the pid handed to the same `Adopt`. Refuse an AUMID whose package does not declare the
file type. The proof needs an MSIX registered in the guest, and an unsigned layout
registers only with Developer Mode on (WW618's `-Needs developer-mode`), which the guest
does not have today, so this waits for a person to turn it on.

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

### §WW622 The README fence the -Carry example never closes

Found while adding `-Needs` to the README's "Running an adopting project's cases off the
desk" section (WW618). The `-Carry` example closes its code block with a fence that has
prose after it on the same line: the closing three backticks are followed by "**That the
runner can carry two trees is why it prints which one it took** ...". A fence with text
after it does not close a block in CommonMark, so on GitHub everything after the example
renders as code until the next fence, which is the `-Needs` example below it.

What to build: split the line, so the fence stands alone and the sentence about which
tree the runner took is its own paragraph (it reads best after the `-Name` paragraph it
explains). Then a desk-free case over README.md that every fenced block's closing line
is the fence alone, so the next example added in this section cannot do it again. Check
whether an existing README sweep (the WW503 and WW499 gates read the README) is the
place for that rule rather than a new class.

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system

## Block N — The name — winwright becomes paneproof
