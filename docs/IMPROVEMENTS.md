# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

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
