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
<PackageReference Include="Winwright" Version="0.1.0-alpha.2" />

<!-- In the application under test, only if you want the readings it can only take from inside. -->
<PackageReference Include="Winwright.InApp" Version="0.1.0-alpha.2" />
```

`Winwright.InApp` is optional, and deliberately so: every reading and every pattern act runs against
an application that references nothing. See [what needs cooperation](#what-needs-the-application-to-cooperate).

### One line if your application's project is at the repository root

The project that drives the application is a project of its own, and it usually goes in a folder
underneath. If the application's `.csproj` sits at the repository root, that folder is inside its
reach: every default glob the SDK applies — `Compile`, and with `UseWPF` also `Page`, `Resource` and
`EmbeddedResource` — walks the whole tree below the project file, so the driving project's sources and
its `obj\` are compiled **into the application**.

```xml
<!-- In the application's own project, if it lives at the repository root. -->
<DefaultItemExcludes>$(DefaultItemExcludes);tests\**</DefaultItemExcludes>
```

**It is named here because the failure names something else.** Without it the build stops with a
row of `CS0579` duplicate-attribute errors — and every one of them points at your *own* generated
`obj\…\AssemblyInfo.cs`, because the duplicate it found is the nested project's copy of the same
attributes. Nothing in the message mentions the folder that caused it. Under `UseWPF` it is worse
again: the errors arrive attributed to a `<YourApp>_<random>_wpftmp` project, which reads like a XAML
problem.

Measured twice. In claude-tray it took moving the folder out of the tree and building again to find
out what it was; here it is a negative control — delete the line from `samples/Adopter` and the build
fails that way on purpose, which is what keeps this a rule rather than a note somebody wrote after
losing an afternoon.

## Adopting it in Claude Code

Two commands, run once in the repository that drives the application:

```
claude plugin marketplace add alegauss/winwright --scope project
claude plugin install winwright@alegauss --scope project
```

Both write into that repository's `.claude/settings.json`, so **committing that file wires every
clone**. There is no per-machine install, nothing added to any path, and no instruction that differs
by whose desk it is. A clone that has the file has the plugin.

The version the plugin carries is the version this tree declares, and the two are read against each
other by the same gate that compares the packages — `Winwright.Concordance --declared … --manifest …`.
A plugin an adopter installed that is a version behind is the hazard worth naming: nothing goes red,
and the run answers a question somebody stopped asking.

### What the tools answer

The plugin wires an MCP server, so the format arrives as a schema rather than as prose somebody
loaded and then typed a key out of:

- **`winwright_format`** — every field of a file, a case, a step and a fixture, whether it is
  required, and the closed list of what it accepts.
- **`winwright_vocabulary`** — every act, what each one needs said beside it, and whether the engine
  may repeat it.
- **`winwright_check`** — a case read back *before* the file exists. Its input schema **is** the
  loader's schema, so a misspelled key is not a thing the caller can send; what comes back is either
  the loader's own refusal, addressed as `cases[0].steps[1].act`, or what a run of it would do.
- **`winwright_run`** — the cases a selection asks for, run. It answers the verdict, a line per case
  that ran and per case it left alone, the exit code, and what outlived the run. **A desk that cannot
  observe answers a hole**, naming which of the six conditions is missing — not a red, and not a
  green either. That is the whole reason it is a separate verb from `winwright_check`: whether a file
  parses is a claim nothing about the machine can change, and whether it passed is not.

The server and the guard are .NET processes the plugin launches, so they need building once —
`dotnet build -c Release` in the plugin's own clone. That is the one step the two commands above do
not cover.

**Skip it and you are told, not left guessing.** Both are wired through a launcher that looks for its
assembly — Release first, then Debug — and where there is none it writes the missing surface and the
build command to stderr and exits. What that replaces is the failure mode worth naming: a `dotnet
exec` on a path a fresh clone does not have produced a .NET assembly error on every write, and a
guard that is not there refuses nothing. The one surface whose whole job is to be in the way was the
one that went quiet. The launcher exits 1 and never 2: denying every write because a build is missing
would put the guard in front of everything instead of in front of a harness script.

### What the guard refuses

A hand-written harness script is always available and always faster in the moment, and that is
exactly how a 2,732-line one happens. So the plugin registers a `PreToolUse` hook: a write whose
content names `Winwright.Acting`, `Winwright.Locating` or `Winwright.Asserting` is **denied**, and the
refusal names the case file and `winwright_check` that replace it. The refusal arrives before the
work rather than after it, which is the difference between being asked to write the other thing and
being asked to delete what you just wrote.

It stays out of its own way in three places. A `.cases.json` write is never denied — that is the verb.
A project referencing the engine's *source* is never denied — the suite here drives windows on
purpose, and a guard you turn off to work on the tool is a guard whose false denies nobody hears
about. And anything it cannot read it allows: a hook that denies what it did not understand is one
that gets removed, after which nothing is guarded at all.

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
| `Synthesised` | the acts a case can name that put real input on the desk — type, click, nudge, press, pick — each carrying what it needed of the machine |
| `Selecting` / `Pick` | select and confirm; every value a picker holds, and reaching one |
| `Surface` | record controls as a case found them and put them back |
| `Pointer` | synthesised mouse input, and the declared readings about why an act needs it |
| `Keyboard` / `Traversal` | synthesised keys, traversal keys at a window, and what holds the focus |
| `Focus` | what holds the focus, read against the application under test rather than the whole desk |
| `Menu` | enter a menu bar the way a keyboard user does, walk to an entry, open a submenu, dismiss |
| `NotificationArea` | the tray, the overflow flyout, the icons on either, and an icon's context menu |

Those drive controls. These are about the application itself and the desk it is running on — what a
case reaches for before it has a control to name, and what it reads to know whether an answer it
just took can be trusted.

| Family | What it does |
| --- | --- |
| `AppTarget` | attach to a running application by process or by window, or launch one and keep what it was launched with |
| `TopLevelWindows` | every top-level window a process owns, and the largest of them, which is the frame where there is one |
| `ProcessRegister` | what this run started, and stopping it inside the budget the project declares |
| `Desk` | whether there is an interactive desk to drive at all, and whether a throw is that desk refusing rather than the code failing |
| `Foreground` | what holds the keyboard, read straight from Windows, and whether a named window does |
| `ForeignInput` | whether anybody but this run touched the machine while a case was working |
| `Obstruction` | what stands over a region, read off the z order |
| `PaintedFrame` | what a window actually paints inside the rectangle it owns |
| `Loading` | whether a page has finished computing, against the loading label the project declares |
| `CaseRun` | one declared case, run: the loop, the waits, the attempts and the verdict, none of which the case carries |
| `Suite` | the cases a selection asked for, run, with every case it left alone named rather than counted |

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

## Writing a case

A scenario file is an object with `cases` in it, and optionally the `fixtures` those cases are
launched against. A case is a data file, not a script: the steps, their locators, their acts and
their expectations are fields, and the loop, the waits, the attempts and the verdict belong to
`CaseRun` — so two cases that drive the same window do not carry two copies of the same loop.

```json
{
  "cases": [
    {
      "name": "renaming a profile writes it back",
      "catches": "a rename that updates the list and never the file",
      "filed": "WW63",
      "tags": ["smoke", "profiles"],
      "needs": ["a second profile"],
      "steps": [
        { "locator": "TabItem[name=\"Profiles\"]", "act": "select" },
        { "locator": "Edit#profileName", "act": "set value", "with": "Beta", "expect": "Beta", "reads": "value" },
        { "locator": "CheckBox#autosave", "act": "toggle", "expect": "On", "reads": "toggle" },
        { "locator": "Button#save", "act": "invoke", "named": "save the profile" },
        { "locator": "Text#status", "act": "read", "expect": "Saved", "reads": "text" }
      ]
    }
  ]
}
```

A case has a `name` and its `steps`, and may carry `tags`, `needs`, `catches` and `filed`.
`locator` and `act` are the two fields every step has. `act` is one of `read`, `invoke`, `toggle`,
`set value`, `set range`, `select`, `expand`, `collapse`, `type`, `click`, `nudge`, `press`, `pick`, `pick at`. `expect` is what the element should read
once the act has landed, and `reads` says which reading that is — one of `anything`, `value`, `range`,
`toggle`, `selected`, `picked`, `expanded`, `text`, `name`, `focused`, defaulting to `anything`, the one value the element
reports, in the order a reader looks at them. `selected` asks whether *this* element is chosen and
`picked` asks which one a container chose — the reading every claim about a picker is about, and the
only one that answers on a ComboBox offering no value. `name` is what a label says: a caption's words are in its
name and in no pattern, so it is the only reading that answers for one — and a step may not read it
where its own locator matched on the name, because then the reading is fixed before the act runs. `with` is required exactly where the act takes something and
refused where it does not. `named` renames a step in the report; `meansIt` is the sentence a step
needs before it may touch an entry the project declared destructive. `moves` is the other kind of
expectation: that the reading ended up different, for the claim a case cannot name a value for. `answers`
is the third — that the reading said something rather than nothing, for the value a case cannot
know. `matches` is the fourth: the pattern the reading should match, for the value a case cannot name
but whose shape it can — a note carrying the date its figure came from, say. A pattern that matches
the empty string is refused, because that is `answers` in a field that reads as though it checked
more. `discloses` is the fifth: that the act put something under the locator that was not in the tree
before it — an expander, a tree view, a details pane, a search that fills a list. Never a count the
case types; the engine compares the subtree against what it read a moment earlier. `sameAs` is the
sixth and is `moves` with a memory: it names the `named` of an earlier step in the same case and
claims this step's reading is **back to what that one read** — the round trip, for the value a case
cannot know at either end. `never` is the seventh and is the only claim about the **wait** rather
than about what it ended on: it names a key whose string must not be showing anywhere in the window
at any moment while this step waits for its locator. And `covers` is the eighth, which is one claim
over many elements — see below.

`sameAs` is judged where the case knows all its steps, so a pointer at a name nobody wrote, at a
step further down, at a name two steps share, or at a step reading something else is refused before
the run. It has to say which reading it is about: comparing a value to a name says nothing.

`never` exists because some claims cannot be read at the end. claude-tray's report comes back from a
per-profile cache in 12ms, and is rebuilt from scratch in 961ms with a *no readings yet* line shown
on the way — and once the waiting is over those two windows read identically. So the locator says
when to stop looking rather than what to look at, the key is a key and never the text for the same
reason the project's `loading` strings are, and the result says **how many times it looked**, because
that number is the whole strength of a negative claim. A locator that never arrives fails rather than
holding on having seen nothing, and an absence found by a walk that did not reach the whole window is
a hole and not a pass.

A step with no `expect` is an act and not a check: it moves the window into the state a later step
reads. An act that survives being repeated is attempted again where its read-back does not arrive; one
that does not — `toggle`, `invoke` — gets a single go, because a retried toggle fails about the
opposite state.

### An expectation nobody types

`covers` names a key in the project's strings, and the claim is that **every string declared under it
reads somewhere this step's locator matches**:

```json
{ "locator": "Text", "act": "read", "covers": "stats.tab" }
```

The set is derived and never listed, and that is the whole point. claude-tray's harness named three
tab keys by hand; the window grew a fourth, and the case went on reporting *all three tab headers
read* against a four-tab window. A list stops covering what it was written for and says nothing when
it does. Add a string to the file and this step fails until the window carries it — with no edit here.

One claim over many elements, so it takes no `expect`, no `reads` and no `moves`: those are about one
element, and a step answers one thing. The act must be `read`, because one act over many of them is
not a claim. A key that declares no strings is **broken and not failed** — an empty expected set is
met by an empty window, which is the hole the derivation exists to close.

### The five that can put real input on the desk

`type`, `click`, `nudge` and `press` put real input on the desk instead of asking the control. Each has a
pattern act beside it that reads almost the same and proves something else, and **which one a case
names is the whole of what an interaction loop is for**: `set value` writes through ValuePattern and
passed on the day a WPF window under a WinForms pump took no keyboard input at all; `type` presses
keys and did not. Likewise `set range` against `nudge`, and `invoke` against `click`.

`nudge` presses an arrow key at a range control, in whichever direction can actually move it — at the
maximum a press upward is a legitimate no-op, so it goes the other way and the check stays about
whether the control responds rather than about where it started.

`pick` is the fifth and the only one that tries not to be. It reaches a value in a picker by name —
the selection pattern first, which needs nothing of the desk, and the keyboard where that refuses,
anchored at whichever end of the list is nearer. What comes back says which route it took **and how
many selection changes it cost**, because a claim about one switch is void when the walk made
several: each intermediate stop is a switch of its own. It is also the one act whose landing the
engine can see, so a `pick` that claims nothing of what it reached is refused where it was written —
every step after it would be read against whichever value the walk happened to stop at.

`pick at` is the same walk told **where to go rather than what to reach**, for the picker whose
values are the machine's data rather than the application's vocabulary — a profile list, an account,
a device. Naming one of those is the hardcoded expectation with the worst possible scope: it passes
on the desk it was written on and fails on every other. A position is what the picker's own order
supplies and no machine's data changes. Its own verb and not a second meaning for `with`, because a
picker may hold a value spelled `1`.

They cost something the other eight do not. A synthesised act needs the window in the foreground,
which Windows does not always grant — so its result carries **what it needed**, and a step that was
never attempted comes back as a hole naming the absence rather than as a reading that did not move.
Those two are indistinguishable from the outside, and reporting the first as the second is a red about
the application on a fact about the desk.

`click` requires its reason in `with`, out of `NoAutomationPeer`, `NotificationArea`, `CustomTemplate`,
`PointerIsTheAct` and the escalation. That is not ceremony: a click whose justification defaults is a
click nobody had to justify, and then every act quietly escalates and the suite is driving the desktop
instead of asking controls.

`read` is the one verb that touches nothing. It resolves, reads, and claims what it read, which is
what a case checking a label after a save actually wants — and what it stops the case from doing is
naming `select` on a text label to get there, which says the case moved something and turns a check
into a harness error on a control that offers no such pattern. A `read` of something nothing drew is
a **failure naming the locator**, not a break, because a read need not have found anything the way an
act must. It expects something or it is refused, it is never retried (the wait already polled to the
deadline), and it never passes the destructive guard — reading the name of the entry that ends the
run does not press it.

**Every field is judged where it is written, and the refusal names the field.** A locator that does
not parse, an act that is not an act, a number a range could never take, a key nobody recognises —
each is refused at `cases[2].steps[1].act`, before the rest of the file is read. A key is refused
rather than ignored on purpose: `"expects"` beside `"expect"` would load, run, check nothing and read
green, which is a check the author wrote and the run never made.

Two refusals are about a case that cannot fail. One with no steps drives nothing. One whose steps all
expect nothing acts and never looks, so it passes on a build with the defect still in it — the same
unearned green the third verdict exists to prevent, arriving as a file instead.

### What a case needs, and why it exists

`needs` names what this machine has to have before there is anything to observe — a second profile,
a pad plugged in, a display that renders. A case whose requirement the run measured as **absent does
not act at all**: every check in it comes back *unchecked*, carrying the absence, and the run is
degraded rather than red. That is the third verdict applied to a whole case, and it is the answer
xUnit has nowhere to put: a case that fails because the machine could not run it sends the reader
looking for a defect in the application. A case that declares a requirement nothing measured is
refused — a run answering "it needs two profiles" with silence does not know whether it looked.

`catches` is the defect the case exists to catch, and `filed` the task it was filed under. Neither is
required, deliberately: asked for a sentence they do not have, an author writes one, and the field
stops meaning anything for every case that has a real one. What happens instead is that the run
**counts the cases that say nothing** and names them, because a check nobody can justify is a check
nobody dares delete and nobody dares change.

### What a case is launched against

A file declares `fixtures`, and a case names one with `fixture` — from any file in the suite, not
only its own. A fixture is what the application is started with: `arguments`, `variables`, and the
`environment` it samples reached through a `flag`:

```json
{
  "fixtures": [
    { "name": "pt-BR", "environment": "pt-BR", "flag": "--language", "shareable": true }
  ]
}
```

**One declaration decides both what the application is launched with and what the expectations are
read from.** The states a menu exists to report are the ones where the environment disagrees with the
application, and on a developer's machine it never does — so without a sampled environment those
assertions are only ever unchecked. A fixture that names an environment nothing carries to the launch
is refused, and so is one that names it twice: an argument spelling `--language=en` beside
`"environment": "pt-BR"` is two places deciding one thing, and whichever the application reads last
wins while the expectations still describe the other.

Names resolve across the whole suite, so the launch three files need is declared once and a name two
files declare is **refused, naming both** — before any case has resolved against either. Without that,
the second copy is where the flag gains a value the first does not have, nothing compares them, and
every expectation in that file describes an environment nothing set up. Same rule as case names, one
level up.

`shareable` says the application leaves a window the next case would accept. `Suite.Launch` lends one
window to several cases only when three separate things agree: the fixture says it may be lent, every
case using it declares `onlyReads`, and the invocation asked for sharing. Sharing is opted into per
invocation rather than merged into the cases, because **a case run alone still owning its process is
what keeps it worth running alone** — and the first case through a lent fixture pays the launch and
owns the window, so its reading is the reading it would take alone.

### Running one of them

A case declares `tags` as well as a name, and `Selection` takes either: `Selection.Case("renaming a
profile writes it back")`, `Selection.Tag("smoke")`, or `Selection.All`. `Suite.Run` runs what the
selection asked for and **names every case it did not run**, in the sentence it opens with:

```
Passed: 1 of 9 cases, 8 not run, 3 assertions over case 'renaming a profile writes it back'.
```

A selector that matches nothing is **refused** with the names or tags there are, rather than
producing a run of no cases — a run of no cases has no failure and no hole in it, so it reads as a
pass, and the pass is about nothing. A case name declared twice, in one file or across two, is
refused for the same reason: a name has to select one case.

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

**Or let the capture ask for you.** `CaptureReceipt.Taking(path, window, target, write)` runs the
write between the readings and composes the receipt from all of them, so none of these questions
depends on a caller remembering it — a reading reached by its own call is one that stops being taken
while every check that needed it starts passing. Which questions apply is the route's business: a
render is asked only about what was written, because nothing else can reach it. The file is written
either way, since a picture nobody may trust is still evidence about what went wrong; what a refusal
withdraws is the claim that it is a capture.

A window's own glass is the other way a copy stops being a picture of it. `Glass.Of` asks the
compositor which system backdrop the window opted into — mica, acrylic and tabbed all composite what
is behind the window into it — and a receipt handed that reading refuses too. Z-order reasoning
cannot answer for this: the intruder is not in front of the window, it is showing through it. A
menu, a balloon or an owned popup is exempt, because those carry a backdrop by design and the copy
route exists for them — and so is an off-screen render, which draws the visual tree with the
compositor not involved and so carries nothing from behind the window at all. It is the screen copy
that a backdrop reaches.

And a third question the picture answers about itself: `Colours.In` counts distinct colours and
refuses a capture that is exactly one. A flat rectangle is not a picture of a window — the session
that produced the measured one had everything present and nothing rendering, so the file was written
and the run exited zero. This is a separate reading from the blank check on purpose: that one scans
the alpha channel and a screen copy has none, so it cannot answer for the very picture this is
about. Counting stops as soon as the answer cannot change, and says when it stopped early.

For a change meant to be invisible, `Unchanged.Between` compares two renders **byte for byte**. No
tolerance is chosen, which is the argument every other image comparison eventually turns into — and
choosing one is choosing how much of a change to stop reporting. Where the files differ it also says
whether the *picture* did: two files that differ and draw the same thing is an encoder writing
something of its own, and a reader told only that the render changed would go looking for a visual
difference, find none, and conclude the check is broken.

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
whether anything else is showing the application, and whether the desk is this run's alone. The
instance reading passes over a process that will not say which binary it is running — refusing on
those would refuse on an elevated shell somebody left open — and **names how many it passed over**,
so "nothing else is running this application" is never a claim about a candidate nobody could read. Each is
reported as measured, absent, or **not read** — an absent line and a missing line read the same to
somebody skimming, and only one of them is a statement.

That reading is on the same page as the verdict, and above it. `VerdictSummary.Render(verdict,
reading)` prints what the run read first and what it concluded second, because a reader who has just
been told four assertions never ran wants the absent precondition before the tally rather than
after. A reading that opened a store fingerprint and never closed it is refused rather than printed:
it shows the machine as it was before the run touched it, and the verdict beside it is about what
happened after.

A sweep carries one per environment. `EnvironmentRun` takes the reading that environment earned
beside the verdict it earned, and the summary prints a sentence for each machine that had something
to explain — a sweep is read to find out *which* machine behaved differently, and a name alone
cannot answer that. A sweep that read some machines and not others names the ones it did not; one
that read none says nothing, because it claimed nothing.

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

- **A case runs; a suite does not.** `CaseRun.Of` walks one case end to end and owns the loop, the
  waits, the attempts and the verdict. What is still missing is above it: nothing selects a case by
  name or a file by path, nothing declares the fixture a case needs, and nothing lends one window
  to the several cases that only read it.
- **A suite runs; a suite does not report to anywhere but the caller.** `winwright_run` launches,
  runs and answers, and what it answers is the verdict — there is no file it writes, no watch mode and
  no history. A second run tells you nothing about the first. There are no slash commands either, and
  none are planned: a verb reachable from a tool does not also need a name typed with a slash.

## Building it here

```
run-tests.cmd            build and run the suite, taking the roll call as part of the run
run-tests-vm.cmd         the same in a VMware guest, so the host stays usable
pack-local.cmd           pack into packages\ for a side-by-side adopting clone, and evict the old copy
```

The suite creates real windows, takes the foreground and synthesises input, which is why the second
one exists. A bare `dotnet test` takes the roll call too: a run short of what discovery found is not
reported as a pass.

The third exists only until the engine is published, and it is a trap rather than a convenience.
The version in `packages\` never changes, and NuGet extracts a package once per version — so a plain
`dotnet pack` over the same number leaves an adopting clone restoring exactly what it already had.
What that looks like from over there is every case file refusing to load, naming a field of the case
that is perfectly correct. Measured three times in one session. `pack-local.cmd` packs and evicts
together so the sequence cannot be half-done.

### Running an adopting project's cases off the desk

The guest runner carries a tree, not this tree. An adopting repository points it at itself:

```
tools\run-tests-vm.ps1 -Tree D:\path\to\yours -Run "run-cases.cmd" -Bring @('yours.trx')
```

`-Name` defaults to the tree's own folder, so it lands in `C:\src\<name>` and two projects cannot
collide in one guest; `-ResultsIn` says where the command left what it wrote. **That the runner can
carry two trees is why it prints which one it took** — otherwise a green is a green about whichever
tree the caller believed they named.

This matters more for an adopter than it does here. Every reason this exists — a host run that
reported eight failures of which two were only the desk, and a negative control that passed because
the host wrote a file faster than the guest could — applies to anybody driving a window from a test,
and until this took a tree they had nowhere to run but the machine they were working at.

`docs/` holds the roadmap, the ledger and the rationale behind each decision. They are written for
whoever is building winwright, and they are governed — the files are written through `roadkeep`
rather than by hand.
