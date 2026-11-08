# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

### §WW617 An act whose application exits during it has landed

Met in the guest on 2026-11-08, in the full run that gated WW611: WW606's own case
invoked Button#exitOnly on the fixture's relaunch pane, whose click shuts the
application down, and the step broke with "COMException: Could not open the process
token". The process was gone while UI Automation was still returning from
InvokePattern.Invoke. The same invoke had passed in the runs before, so it is a race the
application wins or loses.

An application whose button exits it is the case WW575 and WW606 exist for: the step
after the invoke claims 'process: exits'. Breaking on the invoke stops the case before
that claim is read, and reads as the harness failing.

What to build: in Act.Through, read the element's process id before the act, and where
the act throws a COMException or ElementNotAvailableException and that process is no
longer running, return the act as landed with nothing read after it, saying the
application exited during it. Any other throw stays a throw. Done when an invoke on a
button that exits synchronously lands every time, with a test whose handler exits inside
the click.

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system

## Block N — The name — winwright becomes paneproof
