# winwright

Driving a Windows desktop application from a test, and reporting what was actually observed.

It exists because of one measurement. A suite reported a pass with no failures and a total of 352
where the run before it had 374 — twenty-two tests gone, the host had died partway through, and the
only sign was a number nobody had a reason to read. Everything here follows from refusing that: a
green may not cover a check that never ran, and anything this tool could not evaluate is named in
the output rather than left out of it.

## What it needs

- Windows. It is not cross-platform and is not going to be — the whole engine is UI Automation and
  Win32.
- .NET 10, `net10.0-windows`. The in-app half needs `<UseWPF>true</UseWPF>`.
- Two packages, and an application under test takes at most one of them.

```xml
<!-- In the test project that drives the application. -->
<PackageReference Include="Winwright" Version="0.1.0" />

<!-- In the application under test, only if you want the readings it can only take from inside. -->
<PackageReference Include="Winwright.InApp" Version="0.1.0" />
```

`Winwright.InApp` is optional, and deliberately so: every reading and every pattern act runs against
an application that references nothing. See [what needs cooperation](#what-needs-the-application-to-cooperate).

## Addressing an element

One grammar, written once, read the same way by every verb.

```
#saveButton                              the automation id
Button                                   the control type
Button#saveButton                        both
Button[name="Save as..."]                the name
Pane[class=Chrome_WidgetWin_1]           the window class
Button[pattern=Invoke]                   it must carry that pattern
Text[name="Statistics"][order=left]      the leftmost of the ones that match
MenuItem[order=top][index=2]             the second from the top
Window#main > Pane > Button#save         a descendant of, at any depth
```

`>` means **a descendant of**, not a direct child. That is a decision, not a shorthand: UI Automation
wraps controls in panes that differ between frameworks, between versions of one framework, and
between a maximised window and a restored one, so a direct-child locator is the one that breaks on
somebody else's machine.

`Locator.Parse` refuses a locator that does not parse, naming the position and the reason;
`Locator.TryParse` answers without throwing, for a caller collecting refusals rather than stopping.

## The verbs

Grouped by what they are. Every one of them is catalogued in the suite against what it needs from
the application and from the desk, and that catalogue is checked against the engine in both
directions — a verb added without an entry is a red.

| Family | What it does |
| --- | --- |
| `Resolve` | one look for a locator, the same polled to a deadline, or every element a step matches |
| `Inspect` | the control view under a window or an element, as a tree or as lines a person reads |
| `Locator` | parse a locator, or try to |
| `UiaVocabulary` | whether a name is a control type or a pattern, and the nearest name to a misspelt one |
| `ElementFacts` / `PatternValues` | what UI Automation says about one element, and what its patterns read |
| `ActionabilityCheck` | whether an element can take an act at all, and why not where it cannot |
| `Admitted` | the door an act reaches its element through |
| `Subject` | a locator bound to a root, a deadline and a project |
| `Attempt` / `Retry` | a deadline on a sighting or a condition; an act attempted to a cap and counted |
| `Preflight` | what each declared act needs, checked against the tree before anything is pressed |
| `Act` | invoke, toggle, set a value or a range, select, expand, collapse — through the control's own patterns |
| `Selecting` / `Pick` | select and confirm; every value a picker holds, and reaching one |
| `Surface` | record controls as a case found them and put them back |
| `Pointer` | synthesised mouse input, and the declared readings about why an act needs it |
| `Keyboard` / `Traversal` | synthesised keys, traversal keys at a window, and what holds the focus |
| `Focus` | what holds the focus, read against the application under test rather than the whole desk |
| `Menu` | enter a menu bar the way a keyboard user does, walk to an entry, open a submenu, dismiss |
| `NotificationArea` | the tray, the overflow flyout, the icons on either, and an icon's context menu |

**A pattern act is the default and needs no foreground.** It asks the control through its own
accessibility peer rather than asking the desktop to move a mouse. The verbs that do need the
foreground are the ones that synthesise input, and they are marked as such in the catalogue rather
than discovered on a red run.

## What needs the application to cooperate

Nothing in the list above needs `Winwright.InApp`. What the in-app half adds is the readings a
harness cannot take from outside the process:

- `Coordinates` — whether this process's idea of the display is trustworthy, in a sentence a report
  prints. A picture drawn by a system-aware process on a scaled display has a size that does not
  mean what it says, and nothing else about the file would ever say so.
- `Render` — an element to a PNG, measured, arranged and updated in that order. The arrange is why
  the verb exists: a tree that was measured and never arranged renders as a fully transparent
  picture of exactly the right size, which looks like a drawing bug and is a calling bug.
- `Backgrounds` — what a capture should be drawn on, from a brush the application declares under
  `WinwrightCaptureBackground`, or the window's own. The system palette is not consulted at all: it
  answers white on a machine whose window is dark.
- `Geometry` / `Surfaces` — the laid-out tree and what was drawn, written only where the harness
  asked. An application shipped to its users reports nothing and writes no file, which is what makes
  the protocol safe to leave in a release.
- `Popups` — every popup under a window held open for as long as a run lasts. A preview has no hand
  to click with, and fixing that at one call site leaves the next popup to rediscover it.
- `Freezables` / `Apartment` — a brush that may cross to a capture thread, and bounded work on the
  application's own dispatcher.

## Declaring a project

`winwright.json`, found by walking up from where a run starts. Every key is optional; a reading that
needs one this file does not declare is **recorded as not taken**, never quietly skipped.

```json
{
  "executable": "bin/Debug/net10.0-windows/YourApp.exe",
  "sourceRoot": "src/YourApp",
  "sourceIgnore": ["bin", "obj"],
  "fingerprintStore": "%APPDATA%/YourApp",
  "languageFiles": ["strings.en.json", "strings.pt-BR.json"],
  "loading": ["report.computing", "common.pleaseWait"],
  "language": { "preferenceFile": "settings.json", "preferenceKey": "ui.language", "fallback": "en" },
  "timeouts": { "resolve": 5000, "stop": 5000 },
  "attempts": 3,
  "destructive": [{ "id": "quitCommand" }, { "key": "menu.exit" }]
}
```

`loading` names the **keys** of the strings your application shows while a page is still computing,
never the text: a phrase written here is one a translation rewrites, and a check comparing against it
starts matching nothing the day somebody ships another language. `Loading.In` resolves each key out
of `languageFiles` for whichever language the run resolved and asks the tree, so a page that is still
saying it is loading is a failure rather than a photograph. A key none of those files carries
**refuses the run** — a check that silently matches nothing reports a page as finished forever.

`destructive` names the entries that end the run, and it is the one key with a refusal of its own: a
bare name is refused where the project ships more than one language, because a name is the field a
translation rewrites. Write `{"id": …}` or `{"key": …}` instead — a safety check compared against
text a person sees has an expiry date, and the expiry is whenever somebody translates the
application.

## The verdict, and the exit code

The member values **are** the process exit codes. A mapping written twice is a mapping that drifts,
and CI reads the number rather than the word.

| Code | Outcome | What it means |
| --- | --- | --- |
| `0` | `Passed` | Every assertion ran, and every one of them held. |
| `1` | `Failed` | At least one assertion ran and did not hold. |
| `2` | `Degraded` | Everything that ran passed, and something could not be evaluated at all. |
| `3` | `Broken` | The harness threw. What it says is about this tool, not about your application. |

`2` is the reason this project exists. An assertion whose precondition was absent did not pass and
did not fail — it never ran, it is named in the summary by name, and collapsing it into either of the
other two is the thing winwright will not do. `3` outranks the rest, because a reader told the build
failed opens the wrong repository.

Four things a scenario meets often are holes rather than failures, and all of them are about the
desk rather than about your application: a foreground Windows would not grant, a focus that left the
application while a menu walk or a traversal was polling, a notification-area flyout the shell would
not open, and a window somebody else left standing over the region a capture was about. None of them
is your code being wrong, so none goes red — the answer names what the desk did instead.

That last one is a region and never a sample. `Obstruction.Reading` walks the z order down to the
window being photographed, intersects every frame above it with the capture rectangle, and answers
how many pixels are taken and by which windows — named, with their process, because a reader handed
a covered capture needs to know which window to move. Nine sampled points were what this replaced,
and the capture that verified them carried two windows of another process across its corner.

Hand that reading to `CaptureReceipt.Of` and an overlap is **refused rather than cropped**. The
copied rectangle is the painted frame, so there is no invisible border left for a foreign window to
hide in — an overlap is inside real content, and a file quietly trimmed to dodge one is a picture of
something nobody asked for. Leave the reading off and the receipt says nothing about the region
rather than claiming it was clear: a caller who never looked and one who looked and found nothing
are two different facts.

A window's own glass is the other way a copy stops being a picture of it. `Glass.Of` asks the
compositor which system backdrop the window opted into — mica, acrylic and tabbed all composite what
is behind the window into it — and a receipt handed that reading refuses too. Z-order reasoning
cannot answer for this: the intruder is not in front of the window, it is showing through it. A
menu, a balloon or an owned popup is exempt, because those carry a backdrop by design and the copy
route exists for them.

And a third question the picture answers about itself: `Colours.In` counts distinct colours and
refuses a capture that is exactly one. A flat rectangle is not a picture of a window — the session
that produced the measured one had everything present and nothing rendering, so the file was written
and the run exited zero. This is a separate reading from the blank check on purpose: that one scans
the alpha channel and a screen copy has none, so it cannot answer for the very picture this is
about. Counting stops as soon as the answer cannot change, and says when it stopped early.

That last one reaches the verbs above it. Looking for a tray icon answers a reading rather than an
icon-or-nothing, and where it found none it says whether every place it could have been was looked
at. Not found everywhere is an answer about your application; not found because the flyout would not
open is an answer about the desk, and the two never arrive as the same value.

Asking that icon for its menu carries the same distinction up. A menu the icon never showed is a
failure you can act on; a shell that hid the icon, an icon that vanished between being found and
being asked, and a desk that would not give it the focus are holes, because the route to a tray menu
is focus and then the application key and none of those let the run get that far. The verdict and
the trace step agree, so a record never disagrees with the summary beside it.

Before the assertions, a run takes one reading of the machine: the desk it is on, which binary it is
driving, whether that binary is stale, the resolved language, the foreground, the launch arguments,
whether anything else is showing the application, and whether the desk is this run's alone. Each is
reported as measured, absent, or **not read** — an absent line and a missing line read the same to
somebody skimming, and only one of them is a statement.

That reading is on the same page as the verdict, and above it. `VerdictSummary.Render(verdict,
reading)` prints what the run read first and what it concluded second, because a reader who has just
been told four assertions never ran wants the absent precondition before the tally rather than
after. A reading that opened a store fingerprint and never closed it is refused rather than printed:
it shows the machine as it was before the run touched it, and the verdict beside it is about what
happened after.

That reading has an end as well as a beginning. Where the project declares a store the run must not
change, the fingerprint is taken with the rest of the readings and read again when the run finishes,
and what moved is reported beside them. Wrap the run in `Preamble.Around` and neither half is a call
anyone has to remember; a run that threw takes no closing reading, because a machine left dirty by a
run that never finished is not a fact worth reporting over the failure that caused it.

## What it refuses

The value here is concentrated in the refusals, and every one of them is paired in the suite with the
thing that provokes it — a fixture flag, or a stated reason no flag can. Among them: a locator that
does not parse, two elements matching one step, an element that cannot take the act, a declared
destructive entry reached without saying you meant it, a picture nothing drew, a render of a tree
that lays out to nothing, a capture of a window this run is not driving, a run that changed the
machine of whoever ran it, a verdict assembled wrongly, and a trace that is not a trace.

## What it is not

- Not cross-platform.
- No external dependency in the engine.
- No assertion about individual pixels.
- The tool never writes the test.
- No recorder that turns clicks into a scenario.
- No service, no daemon, no database.
- A green never covers an assertion that did not run.

## Not built yet

Written against what has shipped, so it does not promise a line that is still a line:

- **There is no scenario file.** A case is C# against the verbs above. The data-file format, its
  loader and the refusals at load are designed and not built.
- **Nothing composes a run.** The pieces have callers now — a verdict carries the step behind it,
  `Expect.Of` attaches to a red the control view it failed to read, and `Preamble.Around` closes the
  store fingerprint the same reading opened. What is still missing is the thing above them: no
  runner walks a case end to end, so a caller assembles these itself.
- **There is no Claude Code plugin.** The MCP tools, the skill and the hook are designed and not
  built.

## Building it here

```
run-tests.cmd            build and run the suite, taking the roll call as part of the run
run-tests-vm.cmd         the same in a VMware guest, so the host stays usable
```

The suite creates real windows, takes the foreground and synthesises input, which is why the second
one exists. A bare `dotnet test` takes the roll call too: a run short of what discovery found is not
reported as a pass.

`docs/` holds the roadmap, the ledger and the rationale behind each decision. They are written for
whoever is building winwright, and they are governed — the files are written through `roadkeep`
rather than by hand.
