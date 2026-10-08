# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

### §WW516 A press that checks the foreground whatever it located

Found in quickshell's guest run on 2026-10-08, against the published 1.0.0 this tree is.
Its palette case presses `Ctrl+Shift+P` on `TabItem[order=right]`, traced `synthesised
keyboard, selected, Ok`, opened no palette, and broke step 2 with "nothing matched".

What this line first claimed is ruled out, and that is most of what is known. Every
keyboard path checks the foreground before sending or reports it after: the chord half
waits and returns Unchecked unsatisfied, the traversal half does, `Type` reads it back,
`Pick`'s keyboard route checks it where its pattern route never needed to. The chord
path consults nothing about what the locator resolved to, so checking for a Document and
not a TabItem is not something it can do.

The readback was misread, and that was the load-bearing half: the step declares no
`reads`, and `selected` is a TabItem's own reading taken after the act. Nothing selected
the tab, so no pattern act stood in for the keyboard half.

What is left is narrower and worse — the check said the foreground was ours and the
chord reached nothing. Either the desk changed between `Waited` and `Keys.Send`, which
no re-reading afterwards closes, since a chord opening a dialog moves the foreground and
that is WW317's own reasoning; or `Between` called it ours wrongly, as it says ours
where the roots match and what `top` resolves to for a TabItem is worth knowing.

Needs the trace, or that run again. The two cases used two fixtures, so the difference
may be timing.

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing
