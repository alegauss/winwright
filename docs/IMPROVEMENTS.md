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

### §WW503 The nine typed names, now that the catalogue exists

`ReadmeTests` holds the README's `winwright.json` block to the keys a project can
declare, and the list it holds it to is nine names typed into the case. This build reads
thirteen at the top level and three more under `language`. The example shows twelve:
`captures` is missing from it, and has been since the capture verb shipped — the gate
never looked, because `captures` is not one of the nine.

That is the exact shape the check was written against one file over. An adopter writes
this block by copying it, so a key that is not in it is a key nobody uses, which is the
same as a key that does not exist — and the case saying so was itself a hand-kept list
of what to look for.

WW490 removed the reason to keep one. `ProjectDeclaration.Keys` catalogues every key
with what it holds, what it means and what leaving it out does, and the suite holds that
catalogue against the deserialiser's own shape in both directions. So the case can read
`Keys` instead of naming nine, and the README gains the row it is missing in the same
commit — two lines of test for a gate that then covers every key this build will ever
read.

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

### §WW501 A sentence per reading, which the vocabulary has nowhere to keep

WW488 publishes the closed list `reads` accepts, in full, because a field described as
"one of several" sends a reader back to a tool they have not installed. Twelve words
came out, and nothing beside them says what any one of them is about. `value`, `text`
and `name` are guessable; `selected` and `picked` are not, and they are the pair an
author gets wrong.

The distinction is measured rather than stylistic, and the source already states it:
`selected` asks whether this element is the chosen one and `picked` asks which one a
container chose. WW266 found it missing on a profile picker offering no ValuePattern —
`value` answered nothing, `name` answered the picker's own label, and a round trip
comparing either would have held on every machine whatever the picker did. Choosing
wrong is not a red that names the mistake: a reading the element does not offer answers
null forever, and the failure sentence says nothing answered to it.

So the page needs a sentence per reading, and there is nowhere to read one from.
`ActVerb` and `ReadBack` are vocabularies of lambdas: the reasoning lives in comments
above the entries, which a generator cannot take and a rename does not move. Giving each
entry the sentence it already has in prose is what makes the column derivable — the same
shape `LocatorStep`'s summaries gave the predicate table.

### §WW502 The C# reader grammar.mjs was left out of

Four generators now read this repository's C# to build a page, and three of them read it
the same way: find a declaration's body by brace balance, split a collection expression
into its `new(...)` entries, split an argument list at the commas that are not inside a
string, and take a doc comment as data. WW489 extracted those into
`site/scripts/csharp.mjs` rather than write a third copy of them, which is WW193's rule
one language over — the walk is shared and the question never is.

`grammar.mjs` was left out of the move, and it is the one that matters most. It carries
its own `body` and its own `documented`, and they already disagree with the shared pair
about a doc comment: the extracted `plain` strips the `<para>` tags and keeps what is
inside them, while the copy in `grammar.mjs` splits on `<para>` and throws the rest
away. Both are right for what they were written for, and neither says so — so the next
person to fix a rendering bug in one has fixed it in the wrong half of a pair nothing
pairs.

The repair is the move rather than a comment explaining the difference. Whichever
paragraph rule survives becomes the argument the shared reader already takes, and
`grammar.test.mjs` is the case that says the predicate table did not change while it
happened.

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
