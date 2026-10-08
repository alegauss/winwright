# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

### §WW516 A press that checks the foreground whatever it located

Found in quickshell's guest run on 2026-10-08 (cases\palette.cases.json and
cases\tabs.cases.json, run by Quickshell.Cases through Suite.Launch). The guest's
foreground belonged to explorer's 'Program Manager', not to the client under test.

The two cases met it differently. In tabs, step 1 is `press Ctrl+Shift+T` on
`Document[class="Terminal"]`; winwright checked that the foreground was the window under
test, found it was not for five seconds, and reported the step Unchecked with that
reason. Good. In palette, step 1 is `press Ctrl+Shift+P` on `TabItem[order=right]`; the
trace reads `Pattern = synthesised keyboard, ReadBack = selected, Verdict = Ok`, so the
press was called successful, though the chord went nowhere the client could hear and no
palette opened. Step 2, `set value` on `Edit`, then broke with NotActionableException
"nothing matched, or what matched has gone since", which names the wrong thing: nothing
was ever there to match.

What winwright should do: a press delivered as synthesised keyboard input checks the
same foreground precondition whatever its locator resolved to, a TabItem selected
through its pattern included, and reports Unchecked with the foreground's owner when it
fails, as the Document case did. A pattern act that succeeded (the tab selected) should
not stand in for the keyboard half that was never delivered.

Falsified when a case whose press lands while another window holds the foreground
reports that step Ok.

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing
