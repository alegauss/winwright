# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

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

### §WW524 Staged files keep only their name

Found with WW519, from quickshell's QS217. `FixtureDeclaration.Stage` copies every
declared file to `{files}\<file name>`, so the staged directory is always flat. An
application whose store sits one folder down from the directory a variable names, as
`%AppData%\quickshell\sessions.json` does, cannot be handed that layout. The case then
has to point the variable at the subfolder itself, which only works when the application
reads that exact variable and nothing above it. Two files with the same name from
different folders also overwrite each other without a word.

What to build: a `files` entry may be an object, `{ "from":
"cases/fixtures/sessions.json", "to": "quickshell/sessions.json" }`, beside the plain
string that keeps meaning "copy to the top". `to` is relative to the staged directory.
It is refused when absolute, when it climbs out with `..`, or when two entries resolve
to the same destination, and the refusal names both entries. The schema's `files` kind
changes from a list of words to a list whose members are a word or that object, and
`scripts/format.mjs` has to read the new kind.

Two plain entries sharing a file name are refused by the same rule, which turns today's
silent overwrite into a load-time answer.

Falsified when a fixture can reproduce `<root>\quickshell\sessions.json` under
`{files}`, and two entries landing on one path are refused at load.

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

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

### §WW525 A reading the element cannot give, said at once

Found 2026-10-12 in quickshell, QS280, `cases\home.cases.json`. A step read the Sign in
with drop-down, a WPF ComboBox that is not editable:

```json
{ "locator": "ComboBox#SignIn", "act": "read", "reads": "value",
  "expect": "Key or agent (automatic)" }
```

The ComboBox publishes no Value pattern, so `value` can never answer. The step polled 27
times over 5121 ms and failed with "nothing answered to it". Nothing said that this
element has no value to read, or that `picked` (WW266) is the reading for a picker's
selection, which the expand step on the same element read back in its trace ("Key or
agent (automatic)").

What it cost: a red case, then a step that expanded the drop-down, then a collapse that
never ran because the case had stopped. The next case (quick connect, a chord) ran
Degraded with "stopping 8 steps short", most likely because the open drop-down held the
foreground. The adopter rewrote the case to read the expanded list's entries before
finding `picked` in the changelog.

What to build: before polling, check whether the element supports the pattern the
reading needs. Where it does not, fail at once with a reason that names the missing
pattern and the reading that does fit ("ComboBox #SignIn has no Value pattern; its
selection reads as `picked`"). Separately, a case that stops after an expand should
collapse what it expanded, so one case's red does not degrade the next.

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing
