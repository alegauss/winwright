# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

### §WW545 The substitution, on a terminal

Found by quickshell (QS361) on 2026-10-16. Its case "clicking a tab on the strip puts
that tab's terminal on screen" types `title first-tab-marker` into a terminal, a
`Document[class="Terminal"]` that publishes the Text pattern, and presses Enter as its
own step. It failed in two guest runs out of about twenty, reading `first-tabrmarker`
and then `first-tab-markrr`: WW310's signature exactly, the last code unit of the string
put where an earlier one belongs, length intact.

WW249's backstop could not reach it. It compares a value with the string expected, and a
terminal's text is its whole screen.

What landed (2026-10-18): a control with Text and no Value is read as a document.
`Keyboard.Run` takes it to `InADocument`, which counts it arrived where the screen holds
what was sent (`Substitution.Holds`), and where the screen carries the substitution of
it (`Substitution.InDocument`: a run of the same length, differing only by the last code
unit sent, more often than before the send) erases exactly what was sent with Backspace
and sends it again, up to three times. Nothing has run before Enter, so this is safe;
text carrying a control character is never repaired. `DocumentTypingTests` holds both.

What is left is the proof, and it is an adopter's: quickshell's tab case passing three
hundred guest runs in a row on a released engine, with no substitution reaching the
title. A case's trace cannot show the resends yet, which is its own line.

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing
