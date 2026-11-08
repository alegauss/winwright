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

### §WW616 An in-app render the size of what the tree drew

Seen while building WW611, in RendersTests: a 240x160 WPF window with an opaque orange
background, rendered through Renders.Drawn, read fully transparent at pixel (238, 158).
Drawn renders the root visual at its RenderSize, and for a Window that size is the outer
size, frame included, while the tree draws only the client area. So the strip the frame
takes, along the right and bottom edges, is transparent in every in-app picture of a
window, and a viewer shows it as white.

Measured so far: that one pixel. Still to measure: the width of the band on each edge at
100% and at a scaled display, and whether a window with no frame (WindowStyle None) has
one at all.

What to build: render the window's client area at its client size, or crop the picture
to the area the tree drew, so the picture is the window a person sees inside its frame.
The receipt's size should then be the client size, said as that. Done when a picture of
an opaque window has no transparent pixel, with a test reading both bottom corners.

## Block F — Assert — the expectation is derived, never typed

### §WW615 The file-arrives case has room for a loaded guest

Met in the guest on 2026-11-08, in the full run that gated WW610 (a change that touches
no file step): FileSubjectTests.A_file_that_arrives_while_the_step_waits_passes failed
after 3 s, and every full guest run before it that day had passed it.

The case writes the file from Task.Run after Task.Delay(300) and gives the step a 3000
ms resolve budget. ReadFile polls every 50 ms up to that deadline and compares the whole
bytes, so the engine side is sound. What can miss is the writer: a pool continuation on
a guest busy with the whole suite can arrive after the budget, and the step then reads
as a file that never came.

What to build: give the case a budget a loaded guest cannot miss, as WW612 and WW613 did
for their windows (the step returns as soon as the bytes match, so a wider budget costs
nothing on a pass), and write the file from a dedicated thread rather than the pool.
Keep the control beside it: the same step with nothing arriving still fails.

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system

## Block N — The name — winwright becomes paneproof
