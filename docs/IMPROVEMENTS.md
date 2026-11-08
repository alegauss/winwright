# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

## Block C — Locate — the locator grammar and the tree an agent reads

### §WW614 A locator's order is read by name

Found while fixing WW605, which was the same defect in the press verb. Locator.cs parses
a step's order with Enum.TryParse<MatchOrder>(value, ignoreCase: true, ...), and
Enum.TryParse takes a numeric string as an enum value. So [order=1] parses as Left, and
[order=9] parses as the undefined value 9, which no sort handles. The refusal beside it
only turns away Tree, so neither number is refused, and the message that lists the
orders (left, right, top, bottom) is never reached for them.

What to build: read the order by name only, the way ActVerb.Traversal now reads a
traversal key, and refuse anything else with the existing LocatorFault.UnknownOrder
message. Done when [order=1], [order=9] and [order=-1] are each refused with that
message, beside a test that the four names still parse.

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

### §WW611 A backdrop window rendered in-app

Met in claude-tray T449, in dark mode: the app's windows set `ThemeMode="System"`, which
leaves their background to the system backdrop (Mica). Every in-app render of them is
washed out: `preview.cmd shell`, `panels` and `context` all write a white page with
near-white labels. Only controls that paint their own fill (an accent button, a selected
row) read. The cases pass.

Seen with the harness at 0.1.0-alpha.18 and again at 1.1.7, which carries WW583's wait
for a fade, so it is not a picture taken mid-animation. The window's own background is
transparent under the backdrop, and DWM draws the backdrop outside the WPF tree the
render reads. Dark-theme foreground brushes are then composed onto white, because white
is what an empty pixel becomes.

What would close it, in the order a reader of the picture would want:
- When the render's root is a window whose background is transparent or a backdrop, paint the
  theme's window background (the resolved `ApplicationBackgroundBrush`, or the system window colour
  for the active theme) under the tree before rendering it. That is what a person sees there.
- Failing that, refuse the picture as one the engine cannot vouch for, which is the rule `capture`
  already states, instead of writing it and passing.

No workaround in claude-tray: giving the windows an opaque background would change what
users see (no Mica) only to make pictures right, which is the wrong way round.

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
