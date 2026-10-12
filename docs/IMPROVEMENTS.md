# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW519 Staged files and known folders

Found by quickshell's QS217 adopting 1.0.1. The case wanted the client over a session
store of its own: stage `cases/fixtures/sessions.json`, type part of a session's name
into "Open session", read it at the top. The schema says a variable naming `{files}`
gives an application a store "without the application growing a flag".

That holds only where the application reads its store through a variable. quickshell
reads `%AppData%\quickshell\sessions.json` via
`GetFolderPath(SpecialFolder.ApplicationData)`, which is `SHGetKnownFolderPath`:
resolved from the registry, never from `APPDATA`. So `"variables": { "APPDATA":
"{files}" }` launches cleanly, stages the file, and the client reads the real profile's
store: green about the wrong store, the failure WW509's refusal was written against,
invisible to the engine. Most Windows applications are in this position. Staging also
flattens to `{files}\<name>`, so a `<root>\quickshell\sessions.json` layout cannot be
reproduced.

Workaround: quickshell grew `QUICKSHELL_DATA`, a variable naming its data folder,
documented as a user feature; its only caller today is the case.

What winwright should offer:
1. Say it: the schema text and the run's report should name the known-folder case where a
   fixture sets `APPDATA`, `LOCALAPPDATA`, `USERPROFILE` or `HOME`.
2. Stage to a relative destination (`{ "from": …, "to": "quickshell/sessions.json" }`).
3. Only if those leave adopters adding variables anyway: a guest-only, restored-after
   known-folder redirect for the launch.

Falsified when a case can hand a known-folder-reading application a store without it
adding a variable, or the engine says plainly that it cannot.

### §WW521 An executable per configuration

Found adopting the engine in quickshell (QS265, 2026-10-11). `winwright.json` takes one
`executable`, a path, and quickshell's names
`src/Quickshell.App/bin/Debug/net10.0-windows/win-x64/quickshell.exe` because its local
runner builds Debug. CI builds and tests Release. So every CI run launches a file that
is not there and fails with `Win32Exception: The system cannot find the file specified`.
Where a Debug build is left over from another job, it runs the wrong build and passes.
The declaration has no way to say which build it means, so a project that tests two
configurations needs a second declaration or a script that rewrites this one, and
neither is a case file any more.

What to build: the declaration's `executable` may name the configuration as a
placeholder, `bin/{configuration}/...`, filled from what the runner is told. The default
is the one value the project declares, and an environment variable such as
`WINWRIGHT_CONFIGURATION` or a runner argument overrides it. A placeholder left unfilled
is refused, naming the key and how to fill it, and never launches a literal brace path.
The staleness check already reads `executable`, and it reads the filled path.

Falsified when a project built in two configurations still needs two declarations to run
its cases against each.

### §WW522 A fixture that names what it reaches

Found by quickshell's QS269 (2026-10-12). The session bar over a connected tab says
Connected, Reconnecting or Disconnected and carries a chip per running forward. Its
design asks for a UI case reading the chip's word and the forward chip after a forward
opens. No case can be written: the window shows the bar only while a pane holds a live
SSH session, and a fixture can stage files and pass arguments but cannot say the case
needs a server the application connects to. So the bar's evidence is an xunit test
against the project's own Docker sshd.

What to build: a fixture may declare what it reaches outside the application, as an
address (`"reaches": ["127.0.0.1:2222"]`) or a variable naming one. Before launching,
the runner probes each with a TCP connect under a short timeout. Reachable, the case
runs and the address is offered to `arguments` and `files` as `{reach0}` so a staged
store can name it. Unreachable, every assertion of every case on that fixture is
reported not observed, with the address and the probe's answer, never failed and never
passed: the verdict rule this engine already keeps for an absent precondition.

Not a service the engine starts: starting a container is the project's job, and the
README's "no service, no daemon" stands. The fixture only says what it needs and the run
says whether it was there.

Open choice: whether a probe answer is cached per run or per fixture launch.

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

### §WW520 Dragging one element onto another

Found adopting from quickshell (QS250, 2026-10-09). quickshell's tab strip reorders a
tab when it is dragged across its neighbours, a WPF TabControl driven by the mouse. A
case can click a TabItem and read whether it is selected, and there is no verb to press
on one element, move to another and release. So the reorder is proven only by an
in-process test calling the method the drag calls, and the gesture itself, the part a
user does, has no case.

What it needs: a `drag` act, spelled like `click` with a second locator for where it
ends, `"act": "drag", "locator": "TabItem[order=left]", "to": "TabItem[order=right]"`.
It uses real pointer input, because no UIA pattern moves an item along a list: press at
the source's clickable point, move in a few steps so the target's drag threshold
(SystemParameters.MinimumHorizontalDragDistance) is crossed and a PreviewMouseMove is
seen, then release over the destination. It needs the foreground as `click` with
PointerIsTheAct does, and it degrades the same way when the desk cannot give it.

Patterns before pointers still holds. A drag is exactly the case no pattern covers,
which is when this block reaches for the pointer.

Falsified when a case cannot reorder a WPF TabControl item by dragging it onto another.

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

### §WW523 Reading an element's item status

Found 2026-10-12 in quickshell, QS278 (the tab strip). Each tab publishes its connection
state as UIA ItemStatus (`AutomationProperties.ItemStatus` in WPF): "Connected",
"Reconnecting", "Disconnected" or "Local". That is the accessible half of a coloured
dot, and the one a screen reader announces. The design asked for `cases\tabs.cases.json`
to read it; the step it wanted is:

```json
{ "locator": "TabItem[order=left]", "act": "read", "reads": "status", "expect": "Local",
  "named": "a local shell's tab says it is one" }
```

`reads` offers `name`, `value`, `selected`, `toggle` and `text`, and nothing reads
`ItemStatusProperty`, so the step cannot be written. The claim settled for: quickshell's
`TabStripTests` reads the status in process, which is the in-process stand-in the
evidence rule exists to avoid. The sidebar (QS268) publishes "Connected" the same way
and has the same gap.

What to build: a `status` reading (`reads: "status"`) that returns the element's
`UIA_ItemStatusPropertyId`, empty where unset, usable with `expect` and `matches` like
`name`. Its absence should read as the empty string and not as unobserved, since an
unset status is an answer ("no state to report"). Also offer it as a locator attribute
(`TabItem[status="Connected"]`), so a case can find the connected tab without knowing
its title. The fixture app needs one element whose ItemStatus changes on a button press,
to prove the reading follows the live value and not a cached one.

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing
