# Shipped Ledger

## Block A — The verdict (a run is data, and "not observed" is an answer)

- ✅ **WW1** **an assertion that could not be evaluated reports the same green as one that passed** — A run reads as one of three outcomes whose enum values are the exit codes themselves - 0, 1, 2 - and the degraded summary names every assertion that never ran.
- ✅ **WW2** **nothing separates an assertion that should have run from one this project can never observe** — A hole is now constructed from an absent named precondition and from nothing else, and an assertion naming no subject, or a requirement no run measures, is refused when the scenario loads.
- ✅ **WW3** **a run records what it concluded and not what it observed** — Each step is written as one JSONL line the moment it happens, carrying the locator, what it resolved to, the pattern, the read-back, the wait and the polls, flushed so a run that dies keeps it.
- ✅ **WW4** **the same unrun assertion is tallied once per environment a sweep walks** — A sweep tallies each assertion once by name and keeps every occurrence under it, so one hole met in three environments reads as one hole and still prints three lines, each naming its environment.
- ✅ **WW5** **every path, timeout and language file is typed into the scenario that needs it** — A winwright.json found by walking up from the scenario carries the executable, source root, language files and timeouts, resolved against its own directory, and refuses by name what it omits.
- ✅ **WW6** **a summary can say every check passed while one of them never ran** — The summary refuses the word while anything is unchecked and names what is on the list, and Coverage.RequireEvery is the gate an adopting project's own green asks first.
- ✅ **WW7** **a step that threw is indistinguishable from one that failed its assertion** — A throw is recorded as a HarnessError carrying its step and exception, gives the run a fourth outcome that exits 3 and outranks the other three, and reads as threw in the trace.

## Block B — Attach, launch, and leave nothing behind

- ✅ **WW8** **a case that returns early leaves its process running and locks the next build** — A LaunchedProcess can only be made by the register, so an early return still stops the process, and whatever was alive at the end is named with its pid and whether it stopped.
- ✅ **WW9** **a build that failed leaves the previous exe in place and the run reports on code that is not in the tree** — The binary's write time is compared against the newest source outside build output, and older yields an absent precondition carrying both stamps rather than a failure.

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

