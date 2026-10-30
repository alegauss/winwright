# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW586 Processes the application starts

Met in modelado, MOD46 (2026-10-30). The Export card is to gain "Open in Blender", which
starts Blender importing the model shown. Its case wanted to be:

```json
{ "locator": "Button#OpenInBlender", "act": "invoke", "named": "Open in Blender is pressed" },
{ "locator": "Window[nameStarts=\"Blender\"]", "act": "read", "reads": "name", "matches": "cube", "named": "Blender opens on the model" }
```

and then the program the application started has to go when the case ends. winwright
stops only what it launched itself (ProcessRegister.Ending): it asks the window to close
and kills the tree only when that fails. An application that closes cleanly leaves its
children running, and even the kill misses a grandchild whose parent already exited. The
Blender outlives the case, holds the desk for every case after it, and is never named
among the survivors. Nor can a case read what was started.

The fix sits in ProcessRegister.Launch, so it covers every launch, a fixture's before
commands (WW582) included: a Job object per launch, stopped with it, a member that will
not stop named as a survivor. Not descendants by parent id, which lose orphans and risk
a reused pid. Tests: a child started before the assignment; an adopted process never
joins a job; a lent fixture's children stop at the run's end; an instance older than the
case is never stopped. A case reads a child's executable and arguments through
NtQueryInformationProcess, never WMI, a package the engine may not take. That read says
what started; keeping a GUI from coming up is the fixture's, pointing the application at
a stub.

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system
