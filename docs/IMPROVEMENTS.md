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

### §WW563 A launched application never writes on the protocol channel

Found driving polyweave's Electron window through the MCP server from a terminal session
that has no plugin installed. The client was a small JSON-RPC reader over the server's
stdio (`tools/winwright-mcp.cmd`).

During `winwright_run`, which launched `electron.exe`, the server's stdout carried a
line that was not JSON-RPC: an empty line, before the result. A strict reader stopped
there with `JSONDecodeError: Expecting value: line 2 column 1`. A tolerant reader
skipped it and got the verdict. The most likely source is the launched application.
Electron writes to its own stdout, and a child started without redirecting its streams
inherits the server's, which is the protocol channel. An application that prints a lot,
or prints a `{`, would corrupt the stream for any MCP client, not only a hand-written
one.

What to build: every process the runner launches gets its stdout and stderr redirected
away from the server's own. Capture them, keep them in the case's trace as the
application's output, and name them in a red case's verdict where they say why it
failed. Nothing a launched application writes may reach the MCP channel. A test should
launch an application that prints to stdout and assert that the protocol stream stays
parseable.

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system
