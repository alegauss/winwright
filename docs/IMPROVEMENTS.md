# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

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

### §WW568 A read-out launch takes its own streams too

Found while building WW564, on 2026-10-20, reading the launches that do not go through
`ProcessRegister`. WW563 made the register take every stream a caller leaves alone: the
input is closed, and the output and error are drained into the launch. So nothing a
case's application does can reach the MCP server's protocol channel.

A derived set's read-out is a launch of the same application, and it does not go through
the register. `DerivedSet.Reading` builds its own `ProcessStartInfo`, and `Printed`
starts it with `Process.Start`. It redirects standard output, which it reads, and leaves
standard input and standard error alone. Under the MCP server the child inherits the
server's stdin. That stdin is the protocol's input, so an application that reads its
input during a read-out takes the next request. Its stderr goes to the server's stderr,
which is harmless but is lost to the refusal a non-zero exit already writes.

What to build: `Reading` redirects standard input and closes it once the process starts.
It also redirects standard error and keeps what arrived. `Printed` drains both, so a
full error pipe cannot block the read. A read-out that exits non-zero then names the
last lines the application wrote on its error stream in the `UnderivableSetException`,
which is where a reader needs them. A test runs a read-out of an application that reads
its input, and asserts that it meets the end of input instead of waiting.

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system
