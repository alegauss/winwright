# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW529 Filling a JSON store

Left by WW527. A staged file asking to be filled gets `{files}` replaced with the staged
directory as Windows spells it, `C:\Users\...\Temp\winwright\...`. Inside a JSON string
each backslash starts an escape, so a store such as `{ "store": "{files}\\cache" }`
comes out as invalid JSON, or as a path with its separators silently eaten. The
application then refuses the store, or reads a different one, and the case reports
something about the application. Most stores an adopter stages are JSON, quickshell's
`sessions.json` among them. `{reachN}` is safe, since a host:port carries no backslash.

What to build: the escaping belongs to the file, not to the token. A staged file whose
destination ends in `.json` and asks to be filled has every replacement JSON-escaped
before it is written, so `{files}` lands as `C:\\Users\\...` inside a string. Any other
extension is filled raw, as today. The rule goes on the `files` field's schema text and
in the README beside `fill`, so an adopter staging an `.ini` or `.xml` knows the value
arrives unescaped.

Falsified when a filled `.json` store naming `{files}` inside a string parses, and
round-trips to the staged directory's own path.

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
