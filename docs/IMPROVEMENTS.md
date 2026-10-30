# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW586 Processes the application starts

Met in modelado, MOD46 (2026-10-30). The Export card is to gain "Open in Blender", which
starts Blender importing the model shown. Its case wants to say that Blender started, on
that model.

The job half landed: every launch in ProcessRegister.Launch holds a Windows job
(ProcessJob), what the application starts is asked to close and then ended with its
case, a member that will not go is a survivor named as started by the launch, and
LaunchedProcess.Started lists the members still running, conhost included.

What is left is the reading. A case reads a started program's executable and arguments,
through NtQueryInformationProcess (ProcessCommandLineInformation) on a pid from
LaunchedProcess.Started, never WMI, a package the engine may not take. It is a process
step beside WW575's `exits` and `relaunches`, a claim with its own wait, and it leaves
the console host out, since Windows gives one to every console program and no
application asked for it. That read says what started; keeping a GUI from coming up is
the fixture's, pointing the application at a stub. Tests: a fixture button that starts a
child with known arguments is read back; a claim naming a program that never started is
red after its wait; one naming arguments the child was not given is red.

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
