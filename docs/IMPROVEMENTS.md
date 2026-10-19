# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW549 A launched window comes forward without help

Found by quickshell (QS368), soaking one case on the guest desk: `Suite.Launch` over
"clicking a tab on the strip puts that tab's terminal on screen" alone, whose first step
types. It was Degraded three of three, twice: "the foreground belongs to explorer
'Program Manager', which no case in this run started ... still true 5002ms later". The
same case passes in the full run of 31, after earlier cases sent synthesised input.

So the foreground a launched window gets depends on what ran before it. On an unattended
desk Windows' foreground lock refuses a process started by one that is not the
foreground, and nothing the engine does at launch lifts it; a case first in its run, or
alone in it, cannot type or press.

The workaround is quickshell's, in `tests/Quickshell.Cases/CasesRun.cs` (`Nudge`): a
`SendInput` mouse move of no distance before each launch. With it the same soak went
three of three green.

What winwright should offer: at launch, before waiting for the window under test to come
forward, lift the lock the way the adopter had to (a zero-distance synthesised input, or
`AllowSetForegroundWindow` for the launched pid), and say in the trace that it did. Then
a case's verdict no longer depends on its position in the run, and an adopter deletes
`Nudge`.

Falsified when a single typing case, launched first on a fresh guest login with no input
since, comes forward without any help.

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

The proof came on 2026-10-19: quickshell's tab case passed 300 of 300 guest runs in a
row on 1.1.2 (QS368), and the repair fired on two of them, one resend each, per WW546's
trace. This line ships on that run.

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing
