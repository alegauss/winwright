---
name: winwright
description: "Drive or check a Windows desktop application — a WPF or WinForms window, a dialog, a tray icon, a menu. Use when a window is in play: reading what a control says, pressing or setting one, proving a layout, proving that two renders are the same bytes, writing or fixing a `.cases.json` scenario, or reading a run's verdict. Says which loop answers which question and which verb family to reach for. Trigger words: window, WPF, WinForms, UI Automation, locator, control, dialog, tray, screenshot, scenario, case file, winwright."
---

# winwright — which loop answers which question

Three questions about a window, three loops that answer them. Reaching for the wrong loop is how a
check that proves nothing reads green.

| The question | The loop | Reach for |
| --- | --- | --- |
| **Layout** — is it where it should be, and is anything over it? | take a picture and read the geometry | `PaintedFrame`, `Obstruction`, `Pictures`, `GeometryDump` |
| **Input** — does the control take the act, and does the reading change? | act, then read back | `Act`, `Resolve`, `PatternValues`, `Pointer`, `Keyboard` |
| **Determinism** — does the same input come out the same? | render twice and compare bytes | `Unchanged`, `RenderMatch`, `FrameRun` |

A picture cannot prove input, and an interaction cannot prove determinism. A screenshot that shows the
right thing says nothing about whether the button works; one interaction that worked says nothing
about whether it works the same way twice.

## Do not write a harness

A case is a data file the engine runs, not a script you repeat. The engine owns the waits, the
retries and the verdict — write those yourself and you write them differently every time.

A write naming `Winwright.Acting`, `Winwright.Locating` or `Winwright.Asserting` is **denied** by the
plugin's hook. That is not an obstacle to route around; it is the format arriving before the work.

Instead:

1. Ask **`winwright_format`** what a case may say — every field, whether it is required, and the
   closed list of what it accepts.
2. Ask **`winwright_vocabulary`** what a step may do — every act, what each needs said beside it, and
   whether the engine may repeat it.
3. Write the `.cases.json`, then ask **`winwright_check`** whether it loads. The refusal is addressed
   into the file (`cases[0].steps[1].act`), so you fix a field and not a file. Once the project
   exists, name it as `project` in the same call: that also answers what the door of a run would
   refuse, which is the class of fault that loads cleanly and then never runs.
4. Ask **`winwright_run`** to run it. It answers the verdict, a line per case that ran and per case
   it left alone, the exit code, and what outlived the run — so *did it pass* does not go back to a
   shell. A desk that cannot observe answers a hole, which is neither a red nor a green.

Do not type a field name from memory. The tools carry the loader's own schema, so a key you cannot
spell is a key you cannot send — take the constraint rather than guessing at it.

## Addressing an element

One grammar, read the same way by every verb: `Button#save`, `Edit#profileName`,
`TabItem[name="Profiles"]`, `Text#status`. A locator that matches two elements is ambiguous and is
refused — narrow it rather than taking the first.

## Two things that decide whether an answer can be trusted

**A pattern act needs no foreground.** `Act.Invoke`, `Act.Toggle`, `Act.SetValue` ask the control
through its own accessibility peer. Only the verbs that synthesise input — `Pointer`, `Keyboard`,
`Traversal`, `Menu` — need the window in front, and those are the ones another person at the same
desk can break. If a check went red on foreground contention, that is the desk and not the code.

**A check that could not run is a third verdict and never a pass.** A desk with no interactive
session, no display or no automation cannot observe anything, so the run says *hole* rather than
*green*. Do not read a hole as a pass, and do not turn one into a red: neither is what happened.

## Reading a verdict

The exit code is the gate; the sentence says what the run did not do before it says what it
concluded. A filtered run qualifies its pass — if the sentence names cases it left alone, the green
covers less than the whole suite, and that is the half worth reading first.
