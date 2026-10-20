# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW564 A fixture can unset what the run inherited

Found launching polyweave's Electron window (polyweave `gui/`, run as `electron.exe <app
dir>`) from a session started inside VS Code. VS Code exports `ELECTRON_RUN_AS_NODE=1`
to every process it starts, the MCP server inherits it, and so does every application
the server launches. Electron given that variable runs its main script as plain Node: no
window, a quick exit, and an error naming a missing `BrowserWindow` export, never the
variable. polyweave hit the same thing in its own launcher and strips the variable there
(its PW438).

A fixture's `variables` can only set a value. Nothing in a fixture removes an inherited
one, and setting it to an empty string is a different state from unset for some
applications. The workaround lives outside the case: the MCP client that started the
server removed the variable from the server's environment first. An adopter running from
the plugin inside VS Code has no such place.

What to build: a fixture field naming inherited variables to remove before the launch,
for example `"unset": ["ELECTRON_RUN_AS_NODE"]`. Record it in the trace like
`variables`, and refuse a name given in both.  No default that strips a variable for one
kind of application: the field is generic, and an Electron adopter names
`ELECTRON_RUN_AS_NODE` in it like any other.

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

### §WW567 A tool that throws answers, and the server stays up

Found writing WW563's end-to-end test, on 2026-10-20. A project whose `executable` was
`cmd.exe` resolved it against the project root, and `ProcessRegister.Launch` threw
`Win32Exception (2)`: the system cannot find the file. Nothing between the throw and
`Program.Main` catches it. `Running.Over` catches `ScenarioRefusedException` and
`DeclarationMissingException` only, `Served.Called` catches nothing, and the server
exited with an unhandled exception. The client got no reply to the call, and every later
call in the session had no server to answer it.

So any exception the engine did not foresee costs the session its tools, not one answer.
That covers a missing executable, a COM failure in UI Automation, or an IO error on a
staged file. The client sees a closed pipe and no sentence saying why.

What to build: `Served.Called` answers any exception a tool throws as a refused answer.
The answer names the tool, the exception type and its message, and says the harness
broke rather than the application, so `isError` is true and the loop goes on. Keep the
stack trace on the server's stderr, which is not the protocol. Separately,
`Suite.Opened` could refuse a launch whose executable cannot be started, with a
`ScenarioRefusedException` naming the resolved path, since that is the declaration being
wrong. A test should call `winwright_run` on a project naming an executable that does
not exist, and assert that the reply is a refused answer and a second request is still
answered.

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system
