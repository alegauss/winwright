<div align="center">

<img src="https://alegauss.github.io/winwright/logo.svg" alt="winwright logo" width="120">

# winwright

**Tests for a Windows desktop app that Claude Code writes as data, and a verdict that never calls a check green when it did not run.**

```
claude plugin marketplace add alegauss/winwright --scope project
claude plugin install winwright@winwright --scope project
```

Windows · .NET 10 · two NuGet packages, no external dependency · Apache-2.0

[![Winwright on nuget.org](https://img.shields.io/nuget/v/Winwright?label=Winwright)](https://www.nuget.org/packages/Winwright)
[![Winwright.InApp on nuget.org](https://img.shields.io/nuget/v/Winwright.InApp?label=Winwright.InApp)](https://www.nuget.org/packages/Winwright.InApp)

**[alegauss.github.io/winwright](https://alegauss.github.io/winwright/)**: the short version of
the argument. This file is the long one.

</div>

- **A case is a data file, not a script.** Steps name an element, an act and what to expect, in a
  `.cases.json` file. The waits, the retries and the verdict belong to the engine, so two cases never
  carry two copies of the same loop.
- **It asks the control, not the desktop.** An act goes through the control's own UI Automation
  pattern, so most cases need no mouse and no foreground.
- **A third verdict.** A check that could not run is neither a pass nor a fail: it is named, and a
  run where everything else passed exits `2`, not `0`. A suite that lost 22 of its 374 tests and
  still reported a pass is why this exists.
- **Checked before it runs.** `winwright_check` reads a case against the loader's own schema before
  the file exists, so a misspelled key is refused with its address, not discovered on a red run.
- **No harness scripts.** The plugin's guard denies a hand-written script that drives the engine
  directly, and names the case file and the check that replace it.

---

> **Unofficial / community project.** Not affiliated with, endorsed by, or sponsored by
> Anthropic. "Claude" and "Claude Code" are trademarks of Anthropic.

## Install

### The plugin

Run the two commands above in the repository that drives the application. Both write into that
repository's `.claude/settings.json`, so **committing that file wires every clone**.

The MCP server and the guard are .NET processes, so they need building once, in the plugin's own
clone: `dotnet build -c Release`. That is the one step the two commands do not cover. Skip it and
you are told which surface is missing and the command to run, rather than left with a guard that
silently refuses nothing.

The marketplace is named `winwright`, after the plugin, so it sits beside roadkeep's `alegauss`
instead of replacing it. If your settings still carry `winwright@alegauss`, remove that entry and
its marketplace, then run the two commands above.

### The packages

Windows only, .NET 10 (`net10.0-windows`). The in-app half needs `<UseWPF>true</UseWPF>`.

```xml
<!-- In the test project that drives the application. -->
<PackageReference Include="Winwright" Version="1.1.5" />

<!-- In the application under test, only if you want the readings it can only take from inside. -->
<PackageReference Include="Winwright.InApp" Version="1.1.5" />
```

`Winwright.InApp` is optional: every reading and every pattern act runs against an application
that references nothing. See [what needs cooperation](#what-needs-the-application-to-cooperate).
The badges above are read from the feed; the version in these lines is typed, so trust the badge
where the two disagree.

**If your application's `.csproj` is at the repository root**, add one line to it:

```xml
<DefaultItemExcludes>$(DefaultItemExcludes);tests\**</DefaultItemExcludes>
```

Without it the SDK's default globs compile the driving project's sources into the application,
and the build stops with `CS0579` duplicate-attribute errors that point at your *own*
`obj\…\AssemblyInfo.cs` (under `UseWPF`, at a `<YourApp>_<random>_wpftmp` project), with nothing
naming the folder that caused it. `samples/Adopter` fails that way on purpose when the line is
removed.

## First session

Declare the application once, in `winwright.json` at the repository root (every key is optional;
[Declaring a project](#declaring-a-project) has them all):

```json
{ "executable": "bin/{configuration}/net10.0-windows/YourApp.exe", "configuration": "Debug" }
```

Then ask Claude Code for the behaviour you want held, in plain words:

> Write a case that opens the Profiles tab, renames the profile to Beta, saves, and checks the
> status reads Saved.

> Run the smoke cases and tell me what did not run.

Claude reads the format and the acts from the plugin's tools, writes the case into a
`.cases.json` file, checks it with `winwright_check` and runs it with `winwright_run`. The answer
is the verdict, a line per case, and for a case that did not pass, each check that did not: the
step, the locator it resolved, what it read and how long it waited, so a red names what to change
without a second run. [Writing a case](#writing-a-case) is the full format.

### What the tools answer

- **`winwright_format`**: every field of a file, a case, a step and a fixture, whether it is
  required, and the closed list of what it accepts.
- **`winwright_vocabulary`**: every act, what each one needs said beside it, and whether the engine
  may repeat it.
- **`winwright_check`**: a case read back *before* the file exists. Its input schema **is** the
  loader's, so what comes back is either the loader's own refusal, addressed as
  `cases[0].steps[1].act`, or what a run of it would do. Name a `project` beside it and it also
  answers what the run would refuse, such as a `capture` step in a project declaring no `captures`.
- **`winwright_run`**: the cases a selection asks for, run. It answers the verdict, a line per case
  that ran and per case it left alone, the exit code, and what outlived the run. The application's
  own output is kept, and a red case ends with the last lines it wrote. **A desk that cannot observe
  answers a hole**, naming which of the six conditions is missing: not a red, and not a green
  either.

### What the guard refuses

A hand-written harness script is always faster in the moment, and that is how a 2,732-line one
happens. So the plugin's `PreToolUse` hook **denies** a write whose content names
`Winwright.Acting`, `Winwright.Locating` or `Winwright.Asserting`, and names the case file and
`winwright_check` that replace it. It never denies a `.cases.json` write, never denies a project
that references the engine's *source*, and allows anything it cannot read: a hook that denies
what it did not understand is one that gets removed.

---

**Everything below is the reference**: the locator grammar, every verb family, every key a project
may declare, every field a case may carry, the exit codes and the reason behind each refusal. The
suite holds each section against the engine, so a verb or a field added without its line here is a
red.

## Addressing an element

One grammar, written once, read the same way by every verb.

```
#saveButton                              the automation id
Button                                   the control type
Button#saveButton                        both
Button[name="Save as..."]                the name
MenuItem[nameStarts="Pessoal "]          the name, where the rest of it is decoration
Pane[class=Chrome_WidgetWin_1]           the window class
TabItem[status="Connected"]              the state it publishes for a screen reader
Button[pattern=Invoke]                   it must carry that pattern
ComboBox|Slider|Edit                     any one of several control types
Text[name="Statistics"][order=left]      the leftmost of the ones that match
MenuItem[order=top][index=2]             the second from the top
Text[name="{settings.nav.about}"]        what the project's strings call it
MenuItem[name="{report:activeProfile}"]  what the application says it is
Group[name="{}"]                         the member, in a case that repeats
Window#main > Pane > Button#save         a descendant of, at any depth
```

A brace is a hole the run fills. `{a.key}` is read out of the project's own strings in the language
the fixture says its window is in, and `{}` is the member of the set a repeating case walks. A
locator naming an element in words is the hardcoded set at its smallest: it goes stale the day
somebody edits the strings file, and it is wrong in every other language the application ships from
the moment it is written. claude-tray's settings sidebar is six bare `Border`s with no automation
peer, so the words are the only thing that addresses one — and the case that had to type them said
so in a comment, that the label happens to be the same in all four languages.

`nameStarts` matches the **front** of a name, for the control that carries its own state in its text:
a tray entry reading `Pessoal — used 41%  · active now` is a label, a reading and a suffix that comes
and goes, so equality addresses it on no machine. A prefix and never a containment, so a profile
called `one` cannot address the entry for `twenty-one`; a step naming `name` and `nameStarts` both is
refused, because a name equal to something already begins with it, and an empty prefix is refused
because every name begins with nothing.

`status` matches UI Automation's ItemStatus whole: the state an element publishes in words, such as
*Connected* on a tab, which is what a screen-reader user hears. It finds the connected tab without
knowing its title. An empty status is refused, because every element that publishes no state
publishes that, and a step that found its element by `status` may not then claim its `status`
reading, since the locator already fixed it.

Both braces are refused at declaration where the locator will not parse with something in it, and a
key that declares nothing is refused before the first act, naming the key and the file. What the trace
records is the substituted locator: the words the run actually looked for are what a red is about,
and the key is one line away in the case file.

**A `nameStarts` brace reads past a trailing placeholder, and a `name` brace never will.** It is the
same argument `beginsWithLabel` makes below, in the field that addresses rather than the one that
claims: a label spelt `"CLAUDE_CONFIG_DIR points outside these profiles — {0}"` has a front that every
filled-in tree drawing it begins with, and that front is what the locator is filled from. `name` is
equality and keeps the refusal, because a locator hunting the literal `{0}` matches nothing. Narrow
the same way — one placeholder, at the end, and the brace must be the *whole* of the value:
`nameStarts="{a.key} — "` is refused rather than stitched together, a prefix assembled out of two
places being one no window draws in one piece.

`|` at the type position means **any one of these**, and it is there because a rule under test governs
a family of controls as often as it governs one. claude-tray's settings rows name every control with
no content of its own to derive a name from — a ComboBox, a Slider, a TextBox — and exclude the rest
by *what they are* rather than by a list of ids. Written as one step per type, most steps match
nothing on any given panel, each of those is a hole, and the run is a page of holes. The predicates
after a union apply to the whole of it, and a type named twice is refused: that is a step written
twice, not a wider set.

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
| `Resolve` | one look for a locator, the same polled to a deadline, every element a step matches, or what a sweep looks under |
| `Inspect` | the control view under a window or an element, as a tree or as lines a person reads |
| `Locator` | parse a locator, or try to |
| `UiaVocabulary` | whether a name is a control type or a pattern, and the nearest name to a misspelt one |
| `ElementFacts` / `PatternValues` | what UI Automation says about one element, and what its patterns read |
| `ActionabilityCheck` | whether an element can take an act at all, and why not where it cannot |
| `Admitted` | the door an act reaches its element through |
| `Subject` | a locator bound to a root, a deadline and a project |
| `Attempt` / `Retry` | a deadline on a sighting or a condition; an act attempted to a cap and counted |
| `Preflight` | what each declared act needs, checked against the tree before anything is pressed |
| `Act` | invoke, toggle, set a value or a range, select, expand, collapse, resize the window — through the control's own patterns or, for a window's size, `SetWindowPos` |
| `Synthesised` | the acts a case can name that put real input on the desk — type, click, drag, drop file, nudge, press, pick — each carrying what it needed of the machine |
| `Selecting` / `Pick` | select and confirm; every value a picker holds, and reaching one |
| `Surface` | record controls as a case found them and put them back |
| `Pointer` | synthesised mouse input, and the declared readings about why an act needs it |
| `Drag` | a press held across moves past the drag threshold and released over a second element, or at a second point of the same one |
| `ElementPoint` | a point inside an element as a case writes it, a percentage of the element or device-independent pixels from its corner |
| `FileDrop` | a file dragged in from outside the application and released over an element, with the effect the target answered |
| `Keyboard` / `Traversal` | synthesised keys, traversal keys at a window, and what holds the focus |
| `Chord` | a key with modifiers, parsed where the case wrote it and sent as one batch — the only route to an application whose commands have no button |
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
| `ProcessRegister` | what this run started, and stopping it inside the budget the project declares. A process that outlived its case is named with the case it was launched for, and with whether that launch was lent to the cases after it |
| `Desk` | whether there is an interactive desk to drive at all, and whether a throw is that desk refusing rather than the code failing |
| `Foreground` | what holds the keyboard, read straight from Windows, and whether a named window does |
| `ForeignInput` | whether anybody but this run touched the machine while a case was working |
| `Obstruction` | what stands over a region, read off the z order |
| `DropShadows` | whether this desk draws the shadow a menu asks for — a setting of the machine, off on a runner tuned for speed, so a menu with no shadow behind it is the desk and not the application |
| `PaintedFrame` | what a window actually paints inside the rectangle it owns |
| `OwnRender` | ask the application to draw its own tree into a named file, and read back whether it did — the one verb here that needs `Winwright.InApp` on the other end |
| `Loading` | whether a page has finished computing, against the loading label the project declares |
| `CaseRun` | one declared case, run: the loop, the waits, the attempts and the verdict, none of which the case carries |
| `Suite` | the cases a selection asked for, run, with every case it left alone named rather than counted |

**A pattern act is the default and needs no foreground.** It asks the control through its own
accessibility peer rather than asking the desktop to move a mouse. The verbs that do need the
foreground are the ones that synthesise input, and they are marked as such in the catalogue rather
than discovered on a red run.

## What needs the application to cooperate

One verb in the list above needs `Winwright.InApp` — `OwnRender`, and it is the whole reason the
default capture route works. Everything else answers against an application that references nothing.
What the in-app half adds is what a harness cannot take from outside the process:

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
  to click with, and fixing that at one call site leaves the next popup to rediscover it. `Picture`
  draws a popup's own tree, which is the one surface no copy of the screen may have: a framework
  layers a popup for the drop shadow it draws itself, and the soft edge of that is the desktop.
- `Renders` — one line that answers a harness asking for a render, over the message loop the
  application already runs. No thread, no watched directory, and nothing at all unless the run that
  started it set `WINWRIGHT_RENDERS` to somewhere it may write — and where a render does not happen,
  the run asks the application why and prints its answer, so "no in-app half" and "the half is here
  and nobody set the variable" stop reading alike. That matters most on an attached run, which
  launched nothing and so can only ever have the second. Write `Renders.Everywhere()` and
  every window the application ever shows is covered, including the ones it has not shown yet;
  `Renders.Answer(window)` covers exactly the window you name and nothing else, which is a second
  window — a dialog, a wizard page — coming back as an application that does not take the message.
- `Freezables` / `Apartment` — a brush that may cross to a capture thread, and bounded work on the
  application's own dispatcher.

## Declaring a project

`winwright.json`, found by walking up from where a run starts. Every key is optional; a reading that
needs one this file does not declare is **recorded as not taken**, never quietly skipped.

```json
{
  "executable": "bin/{configuration}/net10.0-windows/YourApp.exe",
  "configuration": "Debug",
  "sourceRoot": "src/YourApp",
  "sourceIgnore": ["bin", "obj"],
  "fingerprintStore": "%APPDATA%/YourApp",
  "captures": "TestResults/captures",
  "lastRun": "TestResults/last-run.json",
  "languageFiles": ["strings.en.json", "strings.pt-BR.json"],
  "reportedSets": { "profiles": ["--print-profiles"] },
  "reportedValues": { "activeProfile": ["--print-active-profile"] },
  "loading": ["report.computing", "common.pleaseWait"],
  "language": { "preferenceFile": "settings.json", "preferenceKey": "ui.language", "fallback": "en" },
  "timeouts": { "resolve": 5000, "stop": 5000 },
  "attempts": 3,
  "destructive": [{ "id": "quitCommand" }, { "key": "menu.exit" }]
}
```

`{configuration}` in a declared path stands for the build being tested, so one declaration serves a
local Debug run and a Release run in CI. A run fills it from, in order: the configuration a caller
passes to `ProjectDeclaration.Load(path, configuration)`, then the `WINWRIGHT_CONFIGURATION`
environment variable, then `configuration` in this file. CI sets the variable for its Release job, and
a developer's runner gets the file's default without saying anything. A path naming
`{configuration}` that none of the three fills **refuses when it is asked for**, naming the
`configuration` key and the variable, so a run never launches a path with a brace in it, or a Debug
build left over from another job.

`loading` names the **keys** of the strings your application shows while a page is still computing,
never the text: a phrase written here is one a translation rewrites, and a check comparing against it
starts matching nothing the day somebody ships another language. `Loading.In` resolves each key out
of `languageFiles` for whichever language the run resolved and asks the tree, so a page that is still
saying it is loading is a failure rather than a photograph. A key none of those files carries
**refuses the run** — a check that silently matches nothing reports a page as finished forever.

`lastRun` names a file where a run keeps the names of the cases it actually ran, per selection. The
next run over the same selection compares against it, and the line under the verdict's sentence
names each case that **appeared** and each that **vanished**. A count catches 374 becoming 352. Only
this catches a case swapped for another at the same total, for example one test turned into a skip
and another renamed, which still exits 0 with the line people read unchanged. Each case's `catches`
is kept beside its name, and that is what the line judges. A rename that kept its claim **holds**. A
claim no case that ran makes any more is named as **lost**. A case that vanished without a `catches`
is named as one whose loss cannot be told. Two cases making the same claim cover each other. It is a
finding and never moves the exit code. The file is rewritten on every run, so a deliberate change is
named once, on the run that made it. Without the key, that line says nothing remembers the last run.

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
Every step says what it acts on and what to do: `act`, plus **exactly one of `locator` or `tray`**.
`tray` names a notification-area icon by the name the shell gives it — a tooltip — for the surface no
locator reaches: the notification area is in the shell's tree rather than the window's, and an icon
has no clickable point either, so it is addressed by name and not by the grammar. A tray step's claim
is that the icon **can be found**, so it carries no `expect` and no `reads`: an icon is a rectangle
and a tooltip, it has no patterns to read, and a search that could not open the overflow is a hole
naming the desk rather than an application that placed no icon. Naming both a `locator` and a `tray`
is refused, and so is naming neither.

A tray step takes three acts and no others, because an icon is not an element: `read` asks whether the
shell is showing it, `open tray menu` asks for its menu — by focus and the application key, which
is the only route that reaches one on this shell — and `click tray icon` clicks it with the primary
button, which is how a person opens what a tray application opens on a left-click. Every other act asks
a control through its patterns, and a step naming one against a `tray` is refused where it was written;
the two tray acts named against a `locator` are refused the same way, since there is no control to hand
them. `act` is one of `read`, `invoke`, `toggle`,
`set value`, `set range`, `select`, `add to selection`, `remove from selection`, `choose file`, `expand`, `collapse`, `resize`, `type`, `click`, `drag`, `drop file`, `nudge`, `press`, `pick`, `pick at`, `open submenu`, `open tray menu`, `click tray icon`, `capture`.

`select` replaces whatever its list had selected. A list that takes several rows at once, such as a
sidebar where Ctrl+click adds a row, takes **`add to selection`** and **`remove from selection`**,
which leave the rest of the selection as it was. Both go through the item's own selection pattern, so
they need no foreground, and both are read back the way `select` is, with `"reads": "selected"`:

```json
{ "locator": "List#sessions > ListItem[name=\"Beta\"]", "act": "add to selection", "reads": "selected", "expect": "selected" }
```

On a list that takes one row at a time, both are refused, naming the list, because there an add is a
replace that reads as an add.

An application may answer the gesture and offer no pattern. For that, a `click` takes **`holding`**:
`ctrl`, which adds the row to the selection, or `shift`, which takes the range to it. The key goes down
and up in the same batch of input as the click, so nothing can arrive between them, and the step's name
in the report says the key (`ctrl click List#sessions > ListItem[name="Beta"]`). A key that is not one
of the two is refused, and `holding` beside any act but `click` is refused.

**`resize`** sizes the window the step's `locator` is in. `with` is the outer width and height in
device-independent pixels, written `640x720`, so the case means the same size on a display scaled to
150%. Any element of the window will do as the locator: the window itself is what a locator searches
under, so it never matches one. It is how a case reaches a layout that changes with width, such as a
toolbar whose groups move into its overflow:

```json
{ "locator": "ToolBar#MainToolBar", "act": "resize", "with": "640x720", "named": "the window is narrowed" },
{ "locator": "Button#OverflowButton", "act": "read", "reads": "enabled", "expect": "enabled" }
```

A maximised window is restored first. The act activates nothing, so it needs no foreground. Its
read-back is the size the window ended at, beside the size it had (`resize ... via SetWindowPos:
1024x768 -> 640x720`). A window with a minimum size refuses part of a request, and the trace then
shows the size it kept rather than the one asked for. Whether the layout answered is the next step's
claim. A size that is not two whole numbers above 0 joined by `x` is refused where it is written.

A case that **stops short** (a step threw, or an act was never sent) collapses whatever its `expand`
steps opened and its own `collapse` steps did not close again, last opened first, through the
pattern so the tidy needs no foreground. Each one is a line in the trace. Without it, a drop-down
left open by one red case held the desk for the next one. A case that runs to its end is left
exactly as its last step left it.

`drag` presses on the step's `locator`, moves past the system's drag threshold with the button held,
and releases over a second element named in **`to`**, which is looked for in the same window. It is
for the gesture no pattern expresses, such as a tab strip reordered by dragging one tab across its
neighbours:

```json
{ "locator": "TabItem#firstTab", "act": "drag", "to": "TabItem#secondTab", "named": "First is dragged onto Second" },
{ "locator": "Text#dragOrder", "act": "read", "reads": "name", "expect": "Second, First, Third" }
```

Its claim is that the gesture was made. Where the item ended up is the next step's read, as it is
after a `click`. Like `click`, it needs the foreground, and a desk that will not give it is a hole
rather than a red. `with` takes nothing, because the pointer is the act. A `drag` with no `to` is
refused, and so is a `to` beside any other act, since nothing else would read it. It is attempted
once: a second drag moves the item again from wherever the first one left it.

A drop onto another application's window, such as a file dragged onto an Explorer folder, says
**`"toDesktop": true`**. The `to` locator is then looked for from the desktop rather than in the
window under test, so its first step names the other window:

```json
{ "locator": "ListItem[name=\"report.txt\"]", "act": "drag", "to": "Window[name=\"Downloads\"] > List", "toDesktop": true }
```

Only the release lands there. The foreground the drag needs is still the window under test, and
whether the file arrived is the next step's to check. For a drop onto Explorer that is a file on
disk, not an element. `toDesktop` with no `to` is refused.

A drag can also stay inside one element, for something drawn on a canvas: a crop handle, a map, a
slider track, a custom editor. Its handles are pixels, not elements. **`from`** names the point the
drag presses at, and `to` is then the point of the same element it releases at instead of a
locator. A point is `x,y`. Each half is a percentage of the element (`12%`) or whole
device-independent pixels from its top-left corner (`40`), so it means the same spot on a display
scaled to 150%:

```json
{ "locator": "Pane#cropSurface", "act": "drag", "from": "10%,10%", "to": "40%,30%", "named": "the top-left handle is dragged in" },
{ "locator": "Text#cropSaid", "act": "read", "reads": "name", "expect": "crop: 120,60 150x120" }
```

A point the case cannot have meant is refused where it is written: half a point, a percentage above
100, a `from` with no `to`, a `from` beside any act but `drag`, or `toDesktop` beside `from`. A
point in pixels that falls outside the element is refused by the act, which then names the element's
size, and nothing is pressed. Like any `drag` it needs the foreground and is attempted once.

The other direction, a file dragged in from Explorer and dropped on the application, is
**`drop file`**. `with` names the file the way a `file` step does: relative to the project's root, or
in the fixture's staged directory through `{files}`. The step's `locator` is where it is released:

```json
{ "locator": "Pane#DropZone", "act": "drop file", "with": "{files}/photo.png", "named": "the photo is dropped" },
{ "locator": "Text#ImageSize", "act": "read", "reads": "name", "expect": "1920 x 1080" }
```

The drag is a real one. winwright opens a small window of its own in a corner of the window under
test, presses on it, and starts the system's drag loop with the file list Explorer offers
(`CF_HDROP`). It then carries the pointer to the target with the button held and releases it there.
The application's own drop handler runs, the same as for a drop from Explorer. The effect the target
answers is the act's evidence. A target that answers with no effect is a hole that says so, not a
pass. A target that refuses files and one the drop could not reach (an application running elevated,
for instance) look the same from outside. Like `drag`, it needs the foreground and is attempted once.
A file that is not there stops the case. A `drop file` with no `with` is refused.

The system's common **Open** and **Save** dialogs are reached from the window that opened them: a
dialog the application owns is a child of its window in the tree, `#32770` by class. **`choose file`**
puts the path from `with` (written the way `drop file` writes it) into the dialog's name box and
confirms. Both go through the controls' own patterns, so it needs no foreground. Its locator is the
dialog. Address the dialog's controls by id and never by name, because the names are in the shell's
language. The type list is `ComboBox#1136` in Open and `ComboBox#FileTypeControlHost` in Save, and
the existing `pick` chooses an entry from it by the filter's description:

```json
{ "locator": "Button#ExportButton", "act": "invoke" },
{ "locator": "Window[class=\"#32770\"] > ComboBox#FileTypeControlHost", "act": "pick", "with": "WebP image" },
{ "locator": "Window[class=\"#32770\"]", "act": "choose file", "with": "{files}/out" },
{ "locator": "Window[class=\"#32770\"]", "act": "read", "absent": true, "named": "the dialog closed" },
{ "file": "{files}/out.webp", "act": "read", "sameBytesAs": "cases/expected/out.webp" }
```

The path is typed into the name box as characters sent to the box's own window, not set as its
value. A Save dialog the application gave a default name keeps that name until the box is edited, and
a value set from outside does not count as an edit. The dialog would then save under its default name
with every step green. If it already existed, the dialog would ask to replace it, and that question
is a second `#32770`. Typed characters need no foreground either. The box is read back before the
confirm. If it does not hold the path, the act is refused, naming what the box held, and nothing is
confirmed.

Whether the dialog accepted the path is not the act's claim. The `read` after it claims, with
`"absent": true`, that the dialog closed. If the dialog stays open because the file is not there, or
because a save asks before replacing, that step is red. The question the dialog asks is a second
`#32770`, and an absence claim that matches two windows fails and names both, instead of refusing
the locator as ambiguous. It is attempted once. A locator that matches a window
with no name box or no confirm button is refused, naming what was missing.

A file on disk is the third thing a step can be about, beside a `locator` and a `tray` icon. A step
naming **`file`** takes `read` and makes one of two claims. The first, **`sameBytesAs`**, is that the
file holds the bytes of another one. Both paths are written the same way, relative to the project's
root or naming the fixture's staged directory with `{files}`:

```json
{ "file": "{files}/landing/report.bin", "act": "read", "sameBytesAs": "cases/fixtures/report.bin" }
```

It is waited for over `timeouts.resolve`, because Explorer writes a dropped file after the release
returns, and a read that collides with the writer is a look that saw nothing. A file still missing
then is a failure naming what the folder holds; one that differs names both lengths and the first
byte that differs. A file step that makes neither claim is refused, because a file that merely exists
is what a truncated copy is too, and so is every claim about a control's reading beside it. A
`sameBytesAs` file that is not there stops the case as the case's own fault, not the application's.

The second, **`image`**, is for a picture the application wrote whose bytes vary from desk to desk,
such as a render sized to the viewport. The file is decoded instead of compared. The claim is a list of
facts joined by commas: `width N`, `height N`, `longSide N`, `shortSide N` (in pixels), and
`transparent` (some pixel is less than fully opaque) or `opaque`:

```json
{ "file": "{files}/view.png", "act": "read", "image": "longSide 2048, transparent" }
```

It is waited for the same way. A failure names what was read, such as *view.png is 1024x576, with no
transparent pixel, and the step claimed longSide 2048, transparent*, or says the file could not be
decoded as an image. Windows' own imaging decodes it: PNG, JPEG, BMP, GIF and TIFF, and WebP where
the system has that codec. A fact it does not know is refused where it is written, and so is a claim
that is both `transparent` and `opaque`.

`click tray icon` is a pointer at the icon's centre, and on purpose: every taskbar button refuses a
clickable point, so the rectangle is the only address an icon has. What it claims is the click — sent
at the icon, with the icon the thing standing at that point — and what the application did with it is
the next step's to read, as it is after an `invoke`. A window it opened is a top-level window, so a
resident fixture's next step finds it from the desktop it resolves against:

```json
{ "tray": "[check]", "act": "click tray icon", "named": "the icon is clicked" },
{ "locator": "Text[name=\"{main.nav.statistics}\"]", "act": "read", "reads": "name", "answers": true }
```

It is attempted once, like every act a second go of which is a second request: whether a second click
brings a window back or toggles a flyout shut is the application's business. A flyout that would not
open, an icon that moved away, and something standing over the icon's centre are holes naming the
desk; an icon that is on neither the taskbar nor the overflow is a failure.

`capture` is the one act that produces a file, and the one that acts on a window rather than on a
control: it photographs the window its locator is inside, and its argument is what to **call** the
picture rather than where to put it. Where pictures go is `captures` in the project's own
declaration, and the folder inside it is the case's name — so no case carries a path, which is the
same rule every other field here follows. Its claim is the receipt and it takes no `expect` and no
`reads`. A window a render could reach is a **hole** rather than a picture: a render draws the
application's own visual tree and only the application can take one, so the answer names
`Winwright.InApp` instead of reaching for the screen.

A capture may also name a **`popup`** — the surface inside the window whose tree the picture is of,
by the name the application's own author gave it. It is the one picture no copy of the screen can
ever take: an open popup is its own layered window whose soft edge is a strip of whatever it stands
in front of, and a closed one has no window at all — while to the application it is an element in a
tree it owns either way, so a preview of a flyout nobody has clicked is a picture that can be asked
for. Only a capture takes the field, because every other act reaches an element a locator matched.
The refusals divide the way the verdict does: an application that never took the message is a hole,
and a name no popup has — or that two popups share, or whose popup is holding nothing — is a **red**,
because the run asked, the application answered, and what is wrong is the word the case wrote.

`expect` is what the element should read
once the act has landed, and `reads` says which reading that is — one of `anything`, `value`, `range`,
`toggle`, `selected`, `picked`, `expanded`, `text`, `name`, `description`, `status`, `enabled`, `focused`, defaulting to `anything`, the one value the element
reports, in the order a reader looks at them. A reading that goes through a pattern the element does
not offer (`value` on a ComboBox that is not editable, which has no Value pattern) **fails at once**,
naming the missing pattern and the readings that element does give, rather than polling its deadline
out and saying nothing answered. `selected` asks whether *this* element is chosen and
`picked` asks which one a container chose — the reading every claim about a picker is about, and the
only one that answers on a ComboBox offering no value. `name` is what a label says: a caption's words are in its
name and in no pattern, so it is the only reading that answers for one — and a step may not read it
where its own locator matched on the name or on the front of it, because then the reading is fixed
before the act runs. `description` is the sentence an element says *beside* its name, which is where
an application puts what will not fit in a label — and, where a framework's own accessible object had
to be replaced to carry that sentence, the state it stopped exposing as a pattern. `status` is the
state an element publishes in words for a screen reader, UI Automation's ItemStatus (WPF's
`AutomationProperties.ItemStatus`): *Connected*, *Reconnecting*, the accessible half of a coloured
dot. An element that publishes none reads as the **empty string**, not as nothing observed, because
an unset status is an answer, so a case expecting *Reconnecting* against a tab with no status fails
naming what it read rather than standing down as a hole. `enabled` is
whether the control will take input at all — the state a half-finished form is supposed to be in,
and not a locator predicate, because a locator selecting only enabled controls makes the greyed case
match nothing and *not there* and *there and refusing* are opposite findings about a form. Like
`focused`, it answers for every element that resolved, so a step claiming it **answers** is refused
where it is written. `press` takes either a traversal key or a **chord** — `Ctrl+Shift+I`, modifiers then the key, held
and released in one `SendInput` so nothing can arrive between them. The two are one verb because a
case means one thing either way, *send this key at the window*; what differs is the claim underneath,
since a traversal key moves the focus and is read for it, and a chord invokes a command whose
consequence the next step is the check for. It is what an application with no menu and no toolbar
needs: the commands **are** the application, the keyboard is the only route to them, and `click`
needs a target that does not exist. A modifier on its own, `"with": "Alt"`, is a **tap**: the key goes
down and up with nothing between, which is how Windows shows a classic menu bar. The report calls it
`Alt, alone`, so it does not read as a chord that lost its key. Two modifiers and no key (`Ctrl+Alt`)
are still refused. `with` is required exactly where the act takes something and
refused where it does not. `named` renames a step in the report; `meansIt` is the sentence a step
needs before it may touch an entry the project declared destructive. `moves` is the other kind of
expectation: that the reading ended up different, for the claim a case cannot name a value for. `answers`
is the third — that the reading said something rather than nothing, for the value a case cannot
know. `matches` is the fourth: the pattern the reading should match, for the value a case cannot name
but whose shape it can — a note carrying the date its figure came from, say. A pattern that matches
the empty string is refused, because that is `answers` in a field that reads as though it checked
more. `empty` is its opposite and says what that pattern could not: the element is there and the
reading says nothing, such as a field a form must leave blank or a tab with no state to report
(`"reads": "status", "empty": true`). Unlike `absent`, a locator that matches nothing fails it, and it
is refused beside a reading such as `focused` that answers for every element. `discloses` is the fifth: that the act put something under the locator that was not in the tree
before it — an expander, a tree view, a details pane, a search that fills a list. Never a count the
case types; the engine compares the subtree against what it read a moment earlier. `sameAs` is the
sixth and is `moves` with a memory: it names the `named` of an earlier step in the same case and
claims this step's reading is **back to what that one read** — the round trip, for the value a case
cannot know at either end. `unlike` is the seventh and is that read the other way: it names an earlier
step and claims this step's reading **differs** from what that one read, for the change whose value a
case cannot know at either end either.

`sameCountdownAs` is `sameAs` for a reading that **ticks while the case runs**. A caption naming when
a quota window turns over counts down as you watch it, so a run crossing a minute boundary reads it
one lower and nothing about the application is wrong — and an exact comparison there is a red build
about a clock. It reads the numbers and ignores the words: every number must match except the last,
which may be lower by one, never higher, since a caption that counted *up* is the window having turned
over rather than the clock having moved. A different count of numbers is a different caption. Two
readings with no digits at all are refused rather than matched, because a claim that a countdown came
back is not settled by two strings that never counted.

Its own field and never a tolerance on `sameAs`, deliberately: a percentage is the same number or it
is not, and a general tolerance would soften every exact claim in every project to serve one caption.
One limit worth knowing before you rely on it — it does not span a unit rolling over, so `3h 00m`
reading later as `2h 59m` fails though it is a minute apart. That is one minute in sixty of the ones
this tolerates, and the alternative is teaching the engine what `h` means: a format it would then have
to be kept in step with, which is what the derived expectation refuses everywhere else.

`label` is the eighth and is `expect` with the value derived rather than typed: it names the key
whose declared string the reading should be, read out of the language the fixture says its window is
in. `notLabel` is the ninth and is its mirror — the reading must **not** be that string, for the
states an application has a word for and must not be showing. `beginsWithLabel` is the third of that
family: the reading must **begin** with the declared string, for a state announced as a word in front
of a sentence. A prefix and never a containment, and that is the announcing application's own rule
rather than a convenience — the word goes in front precisely because the sentence behind it is free
text that may contain either word, so a containment would report a switch as on because its
explanation says what turning it on would do. `endsWithLabel` is the fourth and the same claim at the
other end, for a state an application **appends**: a menu entry reading `Pessoal — used 41% · set in
Windows` carries one mark in front and another behind, and the two ends is what reading both takes.
The suffix is as exact as the prefix and for the same reason — what stands before it is a name and a
figure no case can predict.

`notEndsWithLabel` is the fifth and the one that closes the family: the reading must **not** end with
the declared string. Without it a state an application appends could be claimed present and never
claimed absent, so a mark a window drew unconditionally would pass every claim that could be written
about it. Measured on the same submenu: the case for the state where no mark is drawn had nothing to
say. A locator cannot stand in — `nameStarts` addresses the front and no predicate addresses a suffix
— and `never` asks about the whole window, which for a tray case is the desktop. This end only:
`notBeginsWithLabel` has nothing measured behind it, and a field nothing drives is a field no case
proves.

**The three end-anchored claims read past a placeholder; `label` and `notLabel` never will.** A
declared string carrying one is otherwise refused before the run, because a tree holding it already
filled in can never *equal* a format string — which is right for equality and was too wide for a
claim about one end. `notEndsWithLabel` reads the same end its positive does, and a negative against
the fixed part is the stronger claim rather than the weaker one: a reading that does not end with the
fixed part cannot end with the whole label either. Where the
placeholder is the whole of one end, the fixed part is the label as far as a prefix or a suffix is
concerned: `beginsWithLabel` against `"CLAUDE_CONFIG_DIR points outside these profiles — {0}"`
compares everything up to the `{0}`, and `endsWithLabel` against `"{name} is using it"` compares
everything after it. Anything looser is still refused, and the refusal names which end had nothing
to read: a placeholder at the same end as the claim leaves it nothing, and one in the middle would
have the claim comparing a fragment while the file it names looks much longer than what was checked.

`spoken` is the tenth and is about the tree under the locator rather than about any one reading: that
everything under it which announces anything announces a **name** — never a font glyph, a template
nobody filled in, or an automation id handed back — and that something does. `eachSpoken` is the
eleventh and is the same predicate over a different set: every element the locator **matches**
announces a name. `ownHeader` is the twelfth and is the half neither of those can see: of the rows
the locator matches, no control inside one announces a **different** row's header.

`absent` is the claim a locator makes by matching **nothing** — for the window that argues by what
it does not hold: no toolbar, no status bar, no sidebar, and not hidden ones waiting to be switched
on. Every other claim reads a subject and this one says there is none, so it is the only claim on the
step and no `reads` may sit beside it. Two things keep it from being the easiest unearned green in
the format: the wait runs the other way round, polling until the locator matches nothing rather than
until it matches, so a control on its way out is waited for and one that never leaves fails naming
what it found; and the **region** the last step is looked for under has to be there, so
`Pane#capture > Button#close` matching nothing because the pane never opened is reported as a claim
that could not be evaluated rather than as a pass.

**`within`** is about where an element is drawn. It names a second locator in the same window, and
claims this step's element lies inside that one's rectangle and is not empty:

```json
{ "locator": "Button#closeTab", "act": "read", "within": "TabItem#second" }
```

It exists for a control that is in the tree and paints nothing: a close button 22 by 22 whose glyph
the theme's padding left no room for passed every case, because a case could read that the element
exists and what it is called, but not where it is drawn. A failure names both rectangles, and an
element laid out to nothing (zero wide, zero high, or offscreen) fails as **empty**, since that was
the shape of the defect. It is a claim and never a reading of numbers, because a case that pins pixels
breaks when the display's scale changes. Half a pixel of slack on each edge absorbs the rounding to
device pixels. The act is `read` and no `reads` sits beside it.

**`fillsMonitor`** is the same claim against the one rectangle that is no element: the monitor. It
claims this step's element covers the whole of the monitor it is mostly on:

```json
{ "locator": "Document#terminal", "act": "read", "fillsMonitor": true }
```

It exists for a full-screen mode. A case pressing F11 could read that the title bar had gone, but not
that what was left filled the screen, so a full screen that kept a border passed. Point it at the
element that should fill the screen (the terminal, the video, the canvas) rather than only at what
went away. A failure names both rectangles and each edge left uncovered, in pixels: `8px at the
right, 8px at the bottom` is a size taken from the wrong rectangle, and a strip at the bottom alone is
usually the taskbar. Half a pixel of slack again, `read` and no `reads`, and the case never says which
monitor, so it passes on any display the window was put on.

`never` is the thirteenth and is the only claim about the **wait** rather
than about what it ended on: it names a key whose string must not be showing anywhere in the window
at any moment while this step waits for its locator. And `covers` is the fourteenth, which is one claim
over many elements — see below.

`contains` is the fourth of that family and the one a dialog needs: this step's reading **holds**
what an earlier step read, rather than equalling it. A capture prompt quotes the pad it opened for,
and neither string can be typed — one is whatever device is plugged into the desk and the other is
built out of it. `coversWithin` is the near miss and answers a different question: it compares a
*derived set* against many elements, where this compares one reading with one earlier reading.

`sameAs` and `unlike` are judged where the case knows all its steps, so a pointer at a name nobody
wrote, at a step further down, at a name two steps share, or at a step reading something else is
refused before the run. Both have to say which reading they are about: comparing a value to a name
A brace reads one of two wells and the spelling says which. `{a.key}` is answered out of the project's
strings **before the window is** and reads the same on every desk. `{report:name}` is answered by
running the application the way `reportedValues` declares — for the element no case can name, like the
menu entry for whichever profile this machine's icon happens to follow. The application is asked once
per run however many braces name it, so a locator carrying one costs a launch and not a launch per
step. A name the project declares in neither well is refused where it was written, because a locator
that matched nothing would send the reader to the application for something the file got wrong.

**A read-out is asked about the machine the fixture put the window on.** Where a fixture declares an
`environment`, the argument that carries it is on the read-out's launch too — the same one the window
got. Without that the case compares a window drawn on a sampled environment against an application
answering about the real one, which is what the single `environment` field exists to prevent: it is
one declaration precisely so the two cannot be given different modes. A fixture that samples nothing
adds nothing, so every other project's read-outs are the launches they always were.

says nothing. A pointer at the step's own name is refused too — `sameAs` would hold whatever the
window did and `unlike` would fail whatever it did, and neither is a reading. And an earlier step
that read nothing settles neither: an element that says nothing is not evidence a value changed, it
is evidence nobody read it.

`label` and `notLabel` exist because a label typed into a case is the hardcoded set with one member:
it goes stale the day somebody edits the string, and it is wrong in every other language the
application ships from the moment it is written. `covers` is not the answer for one control — it
derives the strings *under* a key, and a leaf key has no children, so the sweep comes back broken
rather than failed. The failure sentence carries the key **and** the string, so a control announcing
the wrong label reads differently from one announcing the right label in the wrong language.

`eachSpoken` is `covers` on the other axis. That one derives a set of **strings** and asks whether
each reads somewhere the locator matched; this asks whether every **element** the locator matches
announces a label. It is what a settings page needs: thirty-odd rows under one naming rule, where an
assertion written against three named controls covers the rule exactly where it was already known to
work.

`ownHeader` catches what a name-existence check is blind to by construction. A rule that pairs a
row's control with the wrong row's header gives several controls one name — every one of them a real
label somebody wrote, every one non-empty, and a screen reader reading the same words over each. The
headers are derived and never listed: they are the names of the rows the locator matched, so a row
added to the page joins the set with no edit here. A control announcing its own row's header is
right, and one keeping its own text is right — that second branch is the only one that can produce
the duplicate, which is why a case checking the first alone is checking the easy half.

Both of them sweep the **window**, so a locator that matched nothing is neither held nor failed but
**counted as a hole** naming the locator. That is not where `covers` goes, and the difference is
which side the set comes from: `covers` derives its set from what the project declares, so an empty
one is a fact about the file and wrong on every machine. These two find what is on the page, and a
page with no rows is a fact about the application — claude-tray's About panel holds prose and links
and not one settings row, so a walk over every panel the navigation declares would otherwise red on
a page behaving exactly as designed.

`spoken` is what a screen reader gets, and no capture can tell it from a picture. It is never a
count: claude-tray's script asserted *four or more* named fields on a conversation row, which is the
stale literal a derived set exists to refuse — the row grows a column and the case goes on asserting
four. Two count-free halves instead: something under here speaks, so a row of pictures fails, and
nothing under here announces a glyph or an id, so a row of codepoints fails. An element that
announces nothing at all is not counted against it, because a container legitimately does not.

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

An act finds its element the way an expectation does, polling its locator to the deadline. A look
that matched an element gone before it could be read counts as no look, so an item a WPF combo box
is still realising after `expand` is waited for. Where nothing arrives, the refusal says which kind
of absence it was: *nothing matched, in 80 looks over 2000 ms*, or *it matched on 3 of 80 looks and
was gone each time before it could be read*. The first points at the locator, the second at the
application replacing the element.

Where nothing matched, the refusal goes on to say why, the way a red expectation does: the step
is under a different parent somewhere else in the window, or something it would sit behind is shut
(a collapsed drop-down, a tab not selected). A toolbar holding items in its **overflow** is named
first, with the button that opens it: *ToolBar #editorToolbar (opened by Button #OverflowButton)
holds items in its overflow*. A toolbar's overflow is in no tree until it is opened, so a button a
narrower window pushed there reads as missing. That usually means the desk running the case is
narrower than the one it was written on. Size the window with `resize`, or open the overflow first.

Both instants reach the trace. An act reads once, the moment it returns; the expectation beside it
polls after that to the deadline, and it is the second reading the verdict turns on. Where the two
differ the act's own line carries **what the run settled on** as well as what the act read, because
the line a reader lands on is the one naming the verb they wrote — and a read-back there that
disagrees with the verdict above it sends them to the control when the answer was the timing.

### An expectation nobody types

`covers` names a key in the project's strings, and the claim runs **both ways**: every string declared
under it reads somewhere this step's locator matches, **and nothing else does**:

```json
{ "locator": "Text", "act": "read", "covers": "stats.tab" }
```

Both directions matter, and the second is easy to meet by accident. The tab set this was built for is
the whole of what a `TabItem` locator matches, so a window carrying one more tab than the expectation
had heard of is exactly the defect it exists to catch — and a step that only checked for missing
strings would pass over it. Where the locator cannot be narrowed to the container you mean, say so
with `coversAtLeast` instead:

```json
{ "locator": "Text", "act": "read", "coversAtLeast": "settings.panels" }
```

That claims only that every declared string is read here, and allows values the set does not declare.
It is the form a shared container needs: measured migrating a sidebar whose items are the only
elements addressable by their words, so the locator has to be `Text` — all six panels matched and the
step failed on nine strangers, because the panel beside the sidebar is full of Texts and no locator
separates the two. The strangers are still counted and still named in the sentence: allowed is not the
same as unrecorded.

A third form is for the reading that **decorates** what it is about. A menu entry for the profile
`Pessoal` renders as `Pessoal  active now`, or carries `pinned` or `sign-in needed` — so equality is
false of every entry and neither claim above can be written:

```json
{ "locator": "MenuItem", "act": "read", "coversWithin": "profiles" }
```

`coversWithin` claims each declared value appears **inside** the name of something the locator
matched. One-way only, because a submenu also carries entries about no declared value at all — the
toggles beside the profiles — and demanding every name hold one would fail on those. It is deliberately
not a substring option on `covers`: weakening the exact claim everywhere to catch this would take the
second direction away from every project that relies on it. The alternative a hand-written harness
reaches for is counting entries, and a count passes when the right number of *wrong* entries is
present — the hardcoded list's failure on a different axis.

Some sets are not in a strings file at all. Profiles, accounts, devices — that is the machine's data,
and the number is whatever this machine has, so there is nothing to derive from the product's
vocabulary. For those the project says how to ask the application itself:

```json
"reportedSets": { "profiles": ["--print-profiles"] }
```

and a step names it exactly as it names a strings key — `"covers": "profiles"`. Which well the set
comes out of is the project's business, for the same reason which strings file it is has always been:
a case naming the flag would be a case that runs on one checkout. The application prints one value per
line **in UTF-8**, and an empty report or a non-zero exit is **broken and not failed**, since an empty
expected set is met by an empty window. A name declared in both wells derives from the application and
says so — the set's source names the strings key it shadowed, because a collision is not necessarily a
mistake and a silent one is.

Where the read-out answers a translation rather than this machine's data, write `{language}` into the
arguments and the run fills it in with the language it resolved:

```json
"reportedSets": { "headers": ["--print-headers", "--lang", "{language}"] }
```

The project says *how* its application is asked and the run says *which* — the same language the
strings are read in. Without it the application answers in whatever the desk is set to while the
window under test was launched in the fixture's language, and a sweep whose whole purpose is that the
two agree reports a value missing from the window it was on. Writing `--lang en` into the declaration
instead holds exactly as long as every fixture launches in English.

Where nothing names the language — an attach, or a test calling `DerivedSet.Reported` without
`speaking` — `{language}` is the language of the strings file that answers for this machine, chosen as
a label's is: the nearest file, then `language.fallback`. So a runner in `en-US` asks `--lang en` of a
project shipping `strings.en.json`, never a language the application does not ship; and where no file
answers and no fallback is declared, the set is refused with a sentence naming both. A read-out that
exits non-zero is refused with the arguments it was run with and the first lines it printed on either
stream.

Most of what an application knows about itself is **not** a set, so `reportedValues` is the scalar
beside it and `expectReported` names one:

```json
{ "locator": "Text#profile", "act": "read", "reads": "name", "expectReported": "activeProfile" }
```

That is `expect` with the value read from the application rather than typed. `label` is the near miss
and answers a different question — it derives from the project's **strings**, which is right for a word
the product ships and wrong for a fact about this machine. Which account is in use, which one an
environment variable selects, whether a toggle is on: a case naming any of those passes on the desk it
was written on and fails on every other. The read-out must print exactly one line; several is a set and
says so, and none has told the case nothing. This is what makes a count derived rather than typed — a case asserting two
profile entries goes on asserting two after a third is added, and says nothing when it stops covering
what it was written for.

`DerivedSet` is the engine's side of this, and it has two doors: one derives the set from the strings
a project declares, the other from what the application prints when asked. The set is derived and
never listed, and that is the whole point. claude-tray's harness named three
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

`type` reads back what the control holds, and a control that reads as a document — a terminal, whose
Text pattern is its whole screen — has arrived when the screen **holds** what was typed, since it is
never only that. Synthesised Unicode input puts the last character typed where an earlier one belongs
about once a send in fifty, and `type` repairs that one fault by its shape: on a value it erases and
sends again, and on a terminal it takes back exactly what it sent with Backspace and sends it again,
which is safe because nothing on the line has run yet. A repaired step stays green and its trace line
says how many resends it took, so the rate can be read off your own runs. Text carrying Enter or Tab is never repaired —
the shell acted on those as they arrived — so a case that types a command and runs it presses Enter
as its own step.

A key or a click sent this way needs the window under test to hold the foreground, and Windows lets a newly started
application take it only when the process that started it could. A test host that has sent no
input can't, so a case first in its run, or soaked alone, used to wait out its whole budget behind
Program Manager. The launch now sends one mouse move of no distance before it starts anything, then
hands the started process the right by its pid. The cursor does not move, and the run counts the
move as its own input, never a person's. If your harness sends input of its own before each launch
to get around this, delete it. Where a window still does not come forward, the hole says whether the
launch could hand it the foreground. If it could, the application declined to come forward; if it
could not, the desk refused.

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

`open submenu` is the keyboard half of the pair `expand` is the pattern half of, and it exists
because the pattern half cannot ask the question. A WinForms submenu that is empty when the menu
opens exposes no ExpandCollapse at all and draws no arrow, and the shell then handles Right as
*activate a plain command* — which dismisses the whole menu. A mouse hover always worked, which is
why it went unnoticed until something drove it from the keyboard. A case naming `expand` there asks
a pattern that is not present and reports a control rather than the gesture. What comes back is the
entry the menu **landed on** rather than what the locator matched, so `reads: name` compares against
the submenu entry.

The locator names **the entry whose submenu is wanted**, and the walk goes there before Right is
pressed. A menu opens highlighting its first entry, so a step that named the window — or any element
in it — would expand whichever entry that happens to be: a step naming the fourth entry expanded the
first, which is how this was found. The entries are addressable because a popup is a top-level `Menu`
window of its own, and the step an adopter writes is the shape `Menu > MenuItem[name="…"]`.

They cost something the other eight do not. A synthesised act needs the window in the foreground,
which Windows does not always grant — so its result carries **what it needed**, and a step that was
never attempted comes back as a hole naming the absence rather than as a reading that did not move.
Those two are indistinguishable from the outside, and reporting the first as the second is a red about
the application on a fact about the desk.

The desk is **waited for**, over `timeouts.resolve`. An application that has just been launched is
often still coming forward when the first step asks — measured against claude-tray at 1453 ms — and a
single look at that moment names the desktop as an intruder, which is true about the instant and false
about the run. A desk somebody else is genuinely holding still comes back as the same hole, arrived at
when the budget runs out and saying how long it stood. Nothing is retried: what waits is the
precondition, and the act itself is sent once.

`click` requires its reason in `with`, out of `NoAutomationPeer`, `NotificationArea`, `CustomTemplate`,
`PointerIsTheAct` and the escalation. That is not ceremony: a click whose justification defaults is a
click nobody had to justify, and then every act quietly escalates and the suite is driving the desktop
instead of asking controls.

A click presses the left button unless the step says **`button`**: `middle`, which closes a tab in
every browser and terminal on Windows, or `right`, which opens a context menu. The step's name in the
report says the button too, so a trace about a middle click does not read as an ordinary one:

```json
{ "locator": "TabItem#second", "act": "click", "with": "PointerIsTheAct", "button": "middle" }
```

A button that is not one is refused with the three there are, rather than falling back to the left
one, because a typo that clicks with the primary button is a case about the wrong gesture that reads
green. `button` beside any act but `click` is refused.

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

### A case that runs once per string the application ships

`forEach` names a key, and the case runs its steps once for **each string declared under it**, with
the member reaching a locator through `{}`:

```json
{ "forEach": "settings.nav", "steps": [ { "locator": "Group[name=\"{}\"]", "act": "read", "eachSpoken": true } ] }
```

The number of runs is data the file must not carry. claude-tray's settings page has six panels under
one naming rule, and a case listing them is a case that reports a clean pass over the panels somebody
remembered — a panel added later is swept by nothing. That is the hardcoded-set defect one level up
from `covers`.

Two guards belong to the engine rather than to the case, and both were loops somebody wrote by hand
first. A key declaring **no** strings is refused rather than run: zero members makes every assertion
inside run zero times and report nothing at all. And a case repeating over a set no step's locator
reaches is refused too — it would drive the same window N times and report N times the confidence for
one reading.

**The claim is judged over the walk, not once per member.** Six panels asserting one rule are one
claim: red where any member that carried it failed, a **hole** only where *no* member carried it, and
otherwise a pass saying **how many of them did**. That last number is what a run reports apart from
what it asserted — claude-tray's About page holds prose and links and not one settings row, and a
panel that was reached and had nothing to check is not one that got away. The trace still carries a
line per member, so a red names the panel it came from.

What is *not* one of those holes is a member the window does not have. Where the last step of the
locator is the member itself, nothing matching means the strings declare a row this window does not
draw, and that is a red on every machine — a fact about the file, not about the page.

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
    { "name": "pt-BR", "environment": "pt-BR", "flag": "--language", "shareable": true, "language": "pt-BR" }
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

`language` is the other field and is not that shape. It decides nothing about the launch — it says
which language the window the launch produced is **in**, so a derived set reads the strings that
window is actually showing. Without it a project shipping five languages had to declare one of them
in `languageFiles` and pretend the other four were not there, because a set cannot be derived from
five files and picking the first would expect a language nobody is looking at. With it, a project
declares everything it ships and two fixtures in one file may be in two languages. A tag that is not
a language is refused where it was written.

`resident` says this launch draws **no window**, and it exists because a tray is a process that draws
none. A launch that draws nothing is otherwise refused, which is the right answer for every fixture
that meant to draw one — nothing about the case was observed, so nothing about the application is
being reported. claude-tray's tray is the counter-example: it puts an icon in the notification area,
and the window there is what a click on the icon is supposed to *produce*. Refusing the fixture makes
the one thing being asserted a reason not to run. A resident fixture's locators resolve against the
**desktop**, because that is where a tray icon lives; the refusal is kept for every fixture that did
not say so, and a resident launch that has already exited is refused too.

Names resolve across the whole suite, so the launch three files need is declared once and a name two
files declare is **refused, naming both** — before any case has resolved against either. Without that,
the second copy is where the flag gains a value the first does not have, nothing compares them, and
every expectation in that file describes an environment nothing set up. Same rule as case names, one
level up.

`workingDirectory` is the directory the application is started in. The default is the one thing worth
saying about it: **the project's root**, the directory `winwright.json` sits in, which is already what
every path that file declares resolves against. Before that default the launch set no directory at
all, so it inherited whichever one the test runner happened to be in — and a fixture passing
`"arguments": ["--import", "cases/fixtures/MobaXterm.ini"]` meant three different files under
`run-tests.cmd`, under `dotnet test` and in the guest. The two workarounds left were setting the
runner process's own current directory, which is global state in a test assembly, and committing an
absolute path into a file other machines read. Declare the field to start somewhere else; a relative
path resolves against that same root, and one that is already absolute is left where it is.

`unset` names variables the run inherited that the launch removes. `variables` can only set a value,
and empty is not unset to every application. The case that needed it is Electron launched from a
session inside VS Code: VS Code exports `ELECTRON_RUN_AS_NODE=1` to everything it starts, the
application inherits it, and Electron runs its main script as plain Node. No window opens, and the
error never names the variable. `"unset": ["ELECTRON_RUN_AS_NODE"]` gives the launch back the
environment a person's shell would have given it. A name under both `unset` and `variables` is
refused, and the fixture's line in a report says what it unset.

`opens` is a folder the fixture opens in Explorer beside the window under test, for a case that drops
something onto it. It is written the way `workingDirectory` is, relative to the project's root or
naming `{files}`, and created where it is not there. After the launch the two windows are put side by
side on the primary display, so a drop released over the folder lands in it and not in the window it
started from. The folder's window is closed when the case gives its own window back, and one still
standing after `timeouts.stop` is named among the survivors. A folder window that never arrives makes
the case a hole naming the desk, and `opens` beside `shareable` is refused, because a lent window
outlives the case that opened the folder:

```json
{
  "name": "with a landing folder",
  "files": ["cases/fixtures/report.bin"],
  "variables": { "YOURAPP_EXPORT": "{files}/report.bin" },
  "opens": "{files}/landing"
}
```

A drag then ends there with `toDesktop`, and a `file` step reads what landed:
`{ "file": "{files}/landing/report.bin", "act": "read", "sameBytesAs": "cases/fixtures/report.bin" }`.
Windows 11 may show the folder as a tab of an Explorer window that was already open, and that window is
used and never closed, because its other tabs are somebody's.

`files` is how a case gives the application a store of its own. Each is a path relative to the project
root, copied into a directory made for that launch and **emptied first**, and a `variables` value,
an argument or `workingDirectory` names that directory with the token `{files}`:

```json
{
  "fixtures": [
    {
      "name": "with sessions",
      "variables": { "APPDATA": "{files}" },
      "files": ["cases/fixtures/sessions.json"]
    }
  ]
}
```

So a Windows application needs **no flag for the harness**: point `APPDATA` at the staged directory
and the application finds the store this case put there. Before it, a case that needed a known data
file had two choices, both bad — a command-line flag that exists only for testing, which the
application's users then see, or writing into the real user profile before launching, which the next
case tramples. The directory is under the system temp rather than in the checkout, because an engine
that writes into your working tree is one that turns up in your `git status`, and it is per project
and per fixture, which is what a launch is keyed by. A fixture that stages files and names `{files}`
nowhere is **refused**: files nothing is looking at are files the case was written to read and never
read, and the reading it takes instead is the real machine's. Declaring no files is still a directory
of this launch's own, which is an empty store rather than a mistake. A value naming no token is
passed exactly as written — `"reader"` is not a path for being a string.

An application whose paths come in as flags names the directory in its **`arguments`**, which take
the same tokens. The application then opens the staged copy and writes beside it, where a file step
reads the result back. It still starts in the project's root, so its other paths resolve as they
always do:

```json
{
  "name": "exporting into its own folder",
  "arguments": ["--open", "{files}/photo.jpg", "--export", "{files}/out.png", "--models", "{root}/models"],
  "files": ["cases/fixtures/photo.jpg"]
}
```

**`{root}`** is the other token, and it names the project's root, the directory `winwright.json` sits
in. It is filled wherever `{files}` is, and also in a step's **`with`**, so a case can type a path in
the checkout without spelling one desk's checkout whole. `{root/}` is the same path with forward
slashes and a slash before the drive, `/C:/src/yourapp`, which is how Windows OpenSSH names a path:

```json
{ "locator": "Edit#remotePath", "act": "set value", "with": "{root/}/cases/fixtures", "reads": "value", "answers": true }
```

The tokens in `with` are filled when the step runs, because the root and the staged directory are
facts about the machine running it. A brace shaped like a token that is none of `{files}`, `{root}`
and `{root/}` is refused where it is written, naming the three. Any other brace, such as a line of
JSON a case types, is typed as written.

A plain path lands at the top of that directory under its own name. Where the application's store sits
a folder down, as `%AppData%\YourApp\sessions.json` does, say where it lands instead:

```json
"files": [
  { "from": "cases/fixtures/sessions.json", "to": "YourApp/sessions.json" },
  "cases/fixtures/theme.json"
]
```

An entry may also say `"fill": true`, so a store can name the server a fixture `reaches` without
spelling the address a second time. Its text gets `{reach0}`, `{reach1}` and so on replaced with the
addresses the probe reached, and `{files}` replaced with the staged directory. In a file that lands
as `.json`, every replacement is JSON-escaped, so `"store": "{files}"` parses and names the
directory whole. Any other file is filled raw, with the path as Windows spells it, because winwright
cannot know that file's quoting. The file is read as UTF-8 and keeps the byte-order mark it came with. A file asked to be filled that is not
UTF-8 is refused by name, and a file not asked is copied byte for byte.

A staged copy is **written now**, as a file the application was just given, whatever time its source
has in the tree. Before, it kept the source's time, so a rule about how recent a file is, such as a
client that opens a feed only when it was written in the last ten minutes, passed on the day the
fixture was made and failed the day after. An entry may say `"modified": "-1d"` to stage a stale one
on purpose, for the other side of a rule like that. An age is `now`, or a minus and a whole number of
`s`, `m`, `h` or `d`. Anything else is refused where the fixture is declared.

`to` is relative to the staged directory and its folders are made for you. A `to` that is absolute or
climbs out with `..` is **refused**, because that directory is emptied before every launch and a file
written outside it is one no run cleans up. Two entries that land on the same path are refused too,
naming both, and that includes two plain files with the same name from different folders, which used
to overwrite each other without a word.

`reaches` names what the application connects to outside itself, each a `host:port` or a
`%VARIABLE%` naming one:

```json
{
  "name": "connected",
  "reaches": ["127.0.0.1:2222"],
  "arguments": ["--connect", "{reach0}"]
}
```

Before every launch on that fixture the run opens a TCP connection to each address, with
`timeouts.reach` as the deadline (2000 ms when the project declares none). If every address answers,
the case runs, and `{reach0}`, `{reach1}` and so on in `arguments` and `variables` become the
addresses the probe reached. If one does not answer, the application is **never launched**: every
check in the case is a hole naming the address and what the connect said, never a red and never a
pass. winwright starts nothing itself. Bringing the server up (a container, a local sshd) is the
project's job; the fixture says what it needs and the run says whether it was there. An address
written out that is not a host and a port is refused at load.

**A variable reaches only an application that reads it.** Most Windows applications don't: .NET's
`GetFolderPath(SpecialFolder.ApplicationData)` asks Windows for the *known folder*, which comes from
the registry and ignores `APPDATA`. Such an application launches cleanly with the store staged and
reads the real profile's store anyway, so a case about it is green about the wrong file. winwright
cannot tell the two kinds apart from outside, so it does not refuse the fixture. Instead, wherever a
fixture sets `APPDATA`, `LOCALAPPDATA`, `USERPROFILE` or `HOME`, the fixture's line and every case
line run against it say so: *setting APPDATA as a variable only — an application asking Windows for
that known folder still reads the real profile*. If your application reads its known folder, it
needs a variable or argument of its own that names its data folder.

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
menu, a balloon or a popup is exempt, because those carry a backdrop by design and the copy
route exists for them — and so is an off-screen render, which draws the visual tree with the
compositor not involved and so carries nothing from behind the window at all. It is the screen copy
that a backdrop reaches.

A region drawn by a window of its own is not in the visual tree. That covers an `HwndHost` holding
a Direct3D swapchain, a WinForms control or a browser. The render copies each such region from its
window, asking the compositor for what it composed (so a DXGI swapchain is included), and draws it
over the picture where the tree reserved room for it. The receipt names each region it copied. This
is the capture route for a Mica window with Direct3D content: the screen copy is refused for the
glass, and the render carries no glass at all. Three cases are refused instead of written. A hosted
region in no window that is shown has nothing to copy from. A region that comes back black
throughout looks like a surface the print could not reach. A `D3DImage` that drew nothing into the
render is the third: a render is a software pass, and a `D3DImage` reaches one only when its back
buffer was set with `enableSoftwareFallback: true`. One that did draw is named on the receipt.

Layering is the other way the same thing happens, and `SeeThrough` is the reading for it — `Of`
takes a window handle and answers how its pixels reach the screen. A
layered window answers the backdrop question with *auto* — truthfully, it never asked the compositor
for anything — while being as much a window on to the desktop as any acrylic one. Nothing is exempt
from this one, and that asymmetry is the point: the shadow Windows draws behind a menu is a popup by
every test the route has, is layered with an alpha per pixel, and is nothing but a rectangle of
whatever the menu is standing in front of. A window layered at full alpha with no colour key is
*not* refused, because it composites to itself and hides the desktop perfectly.

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

Two of those readings are taken again when the run ends, and reported as **findings** rather than
preconditions — nothing is excused by either. The store is read back to say whether the run left the
machine as it found it. The foreground is read back because the opening look is a single one: an
application the run has just launched is often still coming forward, so the preamble can report a
desk that every act then had. Absent at the start and held at the close is a window that arrived;
absent at both is a desk somebody else had all along, and only the pair says which.

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
that lays out to nothing, a render of a page holding a window of its own (a Direct3D swapchain, a
WinForms control or a browser in an `HwndHost`) that is not shown, so there is nothing to copy the
region from, a `D3DImage` that drew nothing into the render, a capture of a window this run is not driving, a run that changed the
machine of whoever ran it, a verdict assembled wrongly, and a trace that is not a trace.

## What it is not

- Not cross-platform.
- No external dependency in the engine.
- The tool never writes the test.
- No recorder that turns clicks into a scenario.
- No service, no daemon, no database.
- A green never covers an assertion that did not run.

### And what a run deliberately does not keep

A run answers its caller and stops. It writes no report file, keeps no history and has no watch
mode, so a second run tells you nothing about the first — the pictures a `capture` step asks for are
the only files it leaves, and they go where the project declares under `captures`.

There are no slash commands either, and none are planned: a verb reachable from a tool does not also
need a name typed with a slash.

## Building it here

```
run-tests.cmd            build and run the suite, taking the roll call as part of the run
run-tests-vm.cmd         the same in a VMware guest, so the host stays usable
pack-local.cmd           pack into packages\ for a side-by-side adopting clone, and evict the old copy
```

The suite creates real windows, takes the foreground and synthesises input, which is why the second
one exists. A bare `dotnet test` takes the roll call on a run that passed and not on one that failed:
MSBuild skips an `AfterTargets` where the target it follows failed, so the roll goes quiet on exactly
the run whose reading is hardest. The first goes through a target that reaches it either way, which
is why it is the command rather than a convenience over one — a run short of what discovery found is
not reported as a pass, and a red run still says what it excused.

The third is for testing an **unpublished** change against a side-by-side adopting clone; adoption
itself restores from nuget.org and needs no path into this repository. It is a trap rather than a
convenience, which is why it is one command and not two. The version in `packages\` never changes
between two local packs, and NuGet extracts a package once per version — so a plain `dotnet pack`
over the same number leaves the adopting clone restoring exactly what it already had. What that
looks like from over there is every case file refusing to load, naming a field of the case that is
perfectly correct. Measured three times in one session. `pack-local.cmd` packs and evicts together
so the sequence cannot be half-done.

### Running an adopting project's cases off the desk

The guest runner carries a tree, not this tree. An adopting repository points it at itself:

```
tools\run-tests-vm.ps1 -Tree D:\path\to\yours -Run "run-cases.cmd" -Bring @('yours.trx')
```

`-Name` defaults to the tree's own folder, so it lands in `C:\src\<name>` and two projects cannot
collide in one guest; `-ResultsIn` says where the command left what it wrote. **That the runner can
carry two trees is why it prints which one it took** — otherwise a green is a green about whichever
tree the caller believed they named.

**One run holds the guest desk at a time.** Two runs on one desk take each other's foreground, and
each reads that, honestly, as a desk it could not get. So every run claims the desk for its whole
length through a host-wide mutex named `winwright-guest-desk-<hash of the .vmx path>`. A second run
waits for it, naming the run that holds it, and refuses if the wait outlasts the holder's bound. An
adopter with a runner of its own takes the same mutex. Inside a run, a foreground lost to another
window says whether that window is a process the run launched (and for which case) or one **no case
in this run started**, which on a shared desk is the other runner.

This matters more for an adopter than it does here. Every reason this exists — a host run that
reported eight failures of which two were only the desk, and a negative control that passed because
the host wrote a file faster than the guest could — applies to anybody driving a window from a test,
and until this took a tree they had nowhere to run but the machine they were working at.

`docs/` holds the roadmap, the ledger and the rationale behind each decision. They are written for
whoever is building winwright, and they are governed — the files are written through `roadkeep`
rather than by hand.

## More tools by alegauss

For Claude Code:

- **[Claude Code Tray](https://alegauss.github.io/claude-tray/)**: your rate-limit usage as a Windows tray icon that turns red before you run out.
- **[roadkeep](https://alegauss.github.io/roadkeep/)**: keeps ROADMAP, CHANGELOG and IMPROVEMENTS to one format, as a CLI and a Claude Code plugin.
- **[commitclerk](https://alegauss.github.io/commitclerk/)**: Conventional Commits messages from your staged diff, in a single-file Python CLI.
- **[polyweave](https://alegauss.github.io/polyweave/)**: a Claude Code plugin that makes a game's 3D assets, sounds and names by declaring what each has to satisfy.

For Windows developers:

- **[FreeWilly](https://alegauss.github.io/freewilly/)**: Docker on Windows without Docker Desktop, with no admin prompt and no telemetry.
- **[Quickshell](https://alegauss.github.io/quickshell/)**: an SSH client for Windows with sessions, keys, SFTP and tunnels, and without the rest of the suite.

All of them: [alegauss.github.io](https://alegauss.github.io/)

## Licence, and saying something is wrong

Apache-2.0, in [`LICENSE`](LICENSE), and both packages declare it. What has shipped is in
[`docs/CHANGELOG.md`](docs/CHANGELOG.md), one line per task with the reason it existed.

A report worth more than most is a refusal you think is wrong — a locator that should have parsed,
a capture refused over a window that was not there, a hole where your code was genuinely at fault.
The refusals are where the value is concentrated, so a false one is the defect worth hearing about
first. [Open an issue](https://github.com/alegauss/winwright/issues) with the verdict the run
printed: it names the condition, whose the absence was, and the step it belongs to, which is most
of the diagnosis already.
