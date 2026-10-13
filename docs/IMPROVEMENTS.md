# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

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

### §WW527 Filling tokens in a staged file

Left by WW522. A fixture's `reaches` addresses are offered to `arguments` and
`variables` as `{reach0}`, `{reach1}` and on, and WW522's design also offered them to
`files` so a staged store can name the server. quickshell's QS269 is that shape: its
session store, staged under `{files}`, names the host and port the session connects to.
Staging copies bytes as they are, so the store has to carry a literal address, which is
a second spelling of what `reaches` already says. The two can disagree, and then the
probe checks one server while the client connects to another.

What to build: a staged file may ask for its tokens to be filled. In WW524's object form
that is `{ "from": ..., "fill": true }`; until that exists, a fixture-level `"fill":
["sessions.json"]`. Filling replaces `{reachN}` and `{files}` in the file's text, read
and written as UTF-8 without a BOM change. A file asked to be filled that is not valid
UTF-8 is refused, naming it, and a file not asked is copied byte for byte as today.
Copying silently is the default because most staged files are not text.

Falsified when a staged store names the server only as `{reach0}` and the launched
application reads the address the probe reached.

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

### §WW528 Claiming a reading is empty

Found shipping WW523. Its case wanted to say that a tab publishing no state publishes
none: `"reads": "status"` on an element with no ItemStatus answers the empty string, and
that is the claim, "no state to report". No field can make it. `expect` is not a claim
when blank, `answers` takes only `true`, and `"matches": "^$"` is refused because a
pattern matching the empty string holds for every answer. That refusal is right, because
a pattern cannot tell empty from absent. So WW523's control had to be a test reading the
facts in process, the stand-in the evidence rule exists to avoid. The same gap stands
for an Edit a form must leave blank, and for a description an entry must not carry.

What to build: `"empty": true`, a claim that the reading resolved and said the empty
string. It is distinct from `absent` (the locator matches nothing) and from a hole (the
reading never answered): an element that did not resolve fails it, naming the locator,
and a reading that answered anything else fails it, naming what it read. It is refused
beside the readings marked `Always`, which never answer empty.

Falsified when a case can claim a tab publishes no status, and the claim fails both on a
tab that publishes one and on a locator that matches nothing.

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing
