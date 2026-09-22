# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

### §WW504 The generator list, spelled twice

Six generators now read this repository's C# to build the documentation area, and the
list of them is written twice: `generate` in `site/package.json` and `prebuild` in
`site/docs/package.json`. Both are hand-chained `&&` sequences, and nothing compares
them. WW491 added the sixth and had to remember two places to do it.

The failure is asymmetric, which is what makes it worth a line rather than a comment. A
generator missing from `prebuild` is loud on a clean checkout — the area imports a
payload that was never written and the build stops — and silent on the machine of
whoever added it, because their `docs/src/data/` still holds the file from the last time
they ran it by hand. So the author sees green, CI sees red, and the distance between
those two is a push.

`docs-area.test.mjs` already holds the joins of this shape: the base, the output
directory, the build order. This is the same kind of fact — two spellings of one list —
and the same case can hold it, by reading the `.mjs` files that write into
`docs/src/data/` and asserting each is named in both scripts. That reading is worth more
than the equality: a generator nobody chained at all would be a file in `scripts/` that
runs nowhere, and neither list would say so.

### §WW505 The task ids the generated sentences carry onto the site

Four sentences on the published area cite a task id. `/verbs/` carries WW317, WW470 and
WW483, and `/verdicts/` carries WW450. They arrive honestly: the generators publish the
engine's own sentences, and this repository's prose cites the task the reasoning started
in — which is right for a reader of the source and resolves to nothing an adopter can
open.

Stripping them is not one rule, which is why this is a line rather than a patch. Three
of the four come out cleanly: `(WW483)` is parenthetical, `WW450:` opens a sentence, and
`— WW470, for a window…` loses a clause marker. The fourth does not: *a traversal key at
the window, or WW317's chord* reads the id as a noun, and taking it out leaves *or 's
chord*. So either the stripper knows four shapes and will meet a fifth, or those
sentences are rewritten where they are declared — which is the same decision
`DeskFact.Because` and `VerbNeeds.Because` already make about their audience, made once
rather than per sentence.

WW492 avoided a fifth by taking the first sentence rather than the first paragraph, and
its own case refuses a task id outright. That case is the shape the others want: the
page that published one should say so before somebody reads it and looks for a WW483
they cannot find.

### §WW506 The documentation area's own copy of the version

WW500 gave the landing page a version it asks nuget.org for. The area did not get one,
and the reason is structural rather than an omission: `site/docs` is its own npm project
with its own build, and it cannot import `src/lib/nuget-feed.ts` from the project next
door.

So it goes on rendering `product.generated.json`, which is right on the day it is built
and a version behind from the next release onward — in `installing` and in `in-app`, the
two pages carrying a `PackageReference` somebody pastes into a csproj. The area is the
half a reader reaches once they have decided to adopt, and it is now the half that
disagrees.

Copying the picker across is refused. The comparison is where the real failure lives —
prerelease counters are numeric, a release outranks a prerelease of the same core, text
ordering gets both wrong — and a second copy is two things to keep true where the suite
asserts one.

What it wants is the picker somewhere both builds read, the shape `scripts/product.mjs`
already has: one producer, two consumers. The area then needs a small client script,
because Starlight ships no JavaScript by default — which is simpler than the landing
page's case rather than harder, there being no hydration to tear.

Until then the area is correct at deploy and stale after, and WW500's page is the one to
trust where the two disagree.

### §WW507 The reader product.mjs still has, and the value it needs

WW502 made `csharp.mjs` the one reader every generator walks C# with, and
`csharp.test.mjs` refuses a second copy by name. It excepts `product.mjs`, which is the
oldest of them and still reads a doc comment its own way: `outcomes()` walks
`RunOutcome`'s members, accumulates the `///` lines above each, and takes the first
sentence of the summary — which is what `documented` does, written out again.

The exception is honest rather than a shortcut. `documented` answers with a member's
name and its summary, and this generator needs a third thing the others never want: the
value the member declares, because `RunOutcome`'s member values *are* the process exit
codes. A member with no explicit value is refused rather than rendered as a guess, and
that refusal is the whole reason the generator exists.

So the repair is a parameter rather than a deletion: the shared reader learns to answer
with the value where a member declares one, `product.mjs` asks for it, and the exception
in `csharp.test.mjs` goes with the copy. What has to survive the move is the refusal — a
code nobody wrote must still stop the build — and `product.test.mjs` already holds the
published figures against the enum in both directions, so it is the case that says the
exit codes did not change while it happened.

Small, and the reason to do it is that this is the reader every page's figures now come
through.
