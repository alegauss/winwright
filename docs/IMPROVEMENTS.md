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

### §WW487 The grammar as a page, derived from the parser

Every case file in every adopting repository is written in the locator grammar, and the
only place it is written down is a section of a README a thousand lines long. A reader
learning it has no page to be sent to, no anchor per form, and no search — and the forms
they most need are the ones a skim passes over: the brace that reads the project's own
strings, the union at the type position, and `nameStarts` for a control carrying its own
state in its text.

The page is a form per row: what it addresses, what it matches, and what it refuses. The
refusals belong on it rather than in a footnote, because half of what the grammar is
worth is in what it will not parse — `name` and `nameStarts` named together, an empty
prefix, a type repeated inside a union, a brace whose key the project declares nowhere.

Derived and not typed. `Locator.Parse` is the one authority on what parses, and a table
retyped beside it is wrong at the first predicate added. The generator reads the parser
the way `scripts/product.mjs` reads the enum, and the suite already holds a case per
form, so an example can come off the case that proves it rather than off an author's
memory.

### §WW488 The case format, generated from the loader's own schema

`winwright_format` already answers every field of a file, a case, a step and a fixture,
whether it is required, and the closed list of what it accepts. It answers it to an
agent, in a repository that has installed the plugin. Somebody reading the site to
decide whether to adopt this has neither, so the most precise description of the format
is the one they cannot reach.

The page is that payload rendered: a table per shape, required marked as required, and
every closed list printed in full rather than described as "one of several". The fields
a first case never touches — `sameCountdownAs`, `notEndsWithLabel`, `discloses` — belong
on it too, because the reason to read a reference is the field you did not know existed.

Generated, and refusing to build where it cannot be. A page retyping a field name is
wrong at the first rename and reports nothing when it happens; the loader's schema is
the same declaration the tool enforces, so the build derives the page from it and fails
where the two disagree. The prose that says *why* a field exists stays hand-written
beside the generated table — that reasoning is in the source's own summaries and in the
README, and it is the half a schema cannot carry.

### §WW489 The verbs, published from the catalogue the suite already checks

Two tables in the README list the verb families: the ones that drive controls, and the
ones that read the application and the desk it is on. They are the answer to "can this
tool do the thing I need", and they are twenty-odd rows deep inside a file nobody can
link a row of.

What the page adds is the column the tables leave implicit: what each verb needs. A
pattern act needs nothing of the desk and asks the control through its own accessibility
peer; a synthesised act needs the foreground, and a run that did not get it answers a
hole. That distinction is the difference between a suite that runs on a build agent and
one that only runs at somebody's desk, and it is currently discovered on a red run.

It is generated, because the catalogue exists already: every verb is entered in the
suite against what it needs of the application and of the desk, and that entry is
checked against the engine in both directions — a verb added without one is a red. So
the page is a read of a list this repository is already gated on, and the cost of adding
a verb does not go up by one hand-edited row.

### §WW490 The project declaration, key by key, off ProjectDeclaration

`winwright.json` is the first file an adopting repository writes, and what the README
gives it is one example with nine keys in it. An example is a good start and a bad
reference: it cannot say which keys exist beside the ones it shows, what each one
defaults to when it is absent, or which of them refuse a value that looks perfectly
reasonable.

Two of those refusals cost an afternoon each if they are met rather than read. `loading`
takes the keys of strings and not the strings, and a key none of the language files
carries refuses the run — because a check that silently matches nothing reports every
page as finished forever. `destructive` refuses a bare name in a project shipping more
than one language, a name being exactly the field a translation rewrites.

The page is a row per key: what it declares, what happens when it is absent, and what it
refuses. Absent matters as much as present here — a reading whose key was never declared
is recorded as not taken rather than skipped, and that is the behaviour that makes an
incomplete declaration safe to start from. Generated off `ProjectDeclaration`, which is
the type that already decides all three.

### §WW491 A page for the third verdict, and for every hole that earns it

An adopter's first surprising run answers `2`. Nothing failed, nothing passed, and the
summary names an assertion that never ran. That is the whole argument of this project
arriving at the worst possible moment to have to go looking for it — and today it is
spread over four separate parts of the README, none of them titled anything a person
would search for.

The page collects it: what a hole is, why it is neither a pass nor a failure, and every
condition that produces one. The four about the desk first, because they are the ones an
adopter meets and none of them is their code being wrong — a foreground Windows would
not grant, a focus that left while a walk was polling, a flyout the shell would not
open, a window standing over the region a capture was about. Then the ones about the
declaration: a reading whose key the project never declared, a capture step in a project
declaring no `captures`.

Beside each, what to do about it, because a verdict a reader cannot act on is a verdict
they learn to ignore. And the precedence, which is not the enum's order: broken outranks
failed, failed outranks degraded, and a run of no cases is degraded rather than passed.

### §WW492 The in-app half, argued as a shipping decision

`Winwright.InApp` is the only thing this project asks an adopter to put inside an
application their users run. That is a different kind of decision from taking a test
dependency, and the README answers it with a list of types.

The page answers the question actually being asked: what does this do in a release
nobody is testing? Nothing — it reports nothing and writes no file unless the run that
started the application set the variable that asks it to, and that is the property that
makes the protocol safe to leave in. Saying so plainly is worth more than any feature
list.

Then what it buys, each with the harness-side failure it prevents. A render, because a
screen copy carries whatever stood in front of the window and a render cannot. A popup's
own tree, which is the one surface no copy of the screen can take — a popup is layered
for the shadow it draws, so the soft edge of a copy is the desktop behind it. The
coordinates sentence, because a picture drawn by a system-aware process on a scaled
display has a size that does not mean what it says.

And the failure an adopter meets first: a render asked for on an application with no
in-app half answers a hole naming the package, which is the message that page should be
findable by.

### §WW493 One adoption, captured from a run rather than remembered

Every code block in the area today was typed by whoever wrote the page. That is normal
and it is also the thing this project refuses everywhere else: a figure true on the day
it was typed and silent about the day it stopped being.

The page is one adoption executed against a real application, with every command and
every line of output captured. A throwaway repository is built, the driving project is
added, `winwright.json` is written, one case is run, and the output is what the run
actually printed. A message whose wording changed then fails the build rather than
misleading a reader halfway through.

The refusals are the point, not the happy path. Being refused on something that has
always worked is the moment somebody decides a tool is not worth the trouble, and what
the refusal *says* is the whole difference. The capture should include at least the two
an adopter meets first: the build that fails with duplicate-attribute errors naming a
wpftmp project, and the run that answers a hole because the desk would not grant the
foreground.

This is what `site/docs/` exists for rather than the README: a captured run is generated
content, and a file that regenerates it is a build step, not a paragraph somebody keeps
in step by hand.

### §WW494 Troubleshooting addressed by the message, not by the cause

Documentation is organised by cause and readers arrive by symptom. Somebody stuck has a
string on their screen and nothing else, and every explanation this project has written
is filed under the thing they do not yet know is wrong.

The page is a row per message: what it says, what it actually means, and the one edit
that clears it. The three that stop adoptions outright come first. `CS0579`
duplicate-attribute errors naming a `<YourApp>_<random>_wpftmp` project, which reads
like a XAML problem and is a folder the SDK's globs swallowed. A launcher writing a
missing surface and a build command to stderr, which is the plugin's two commands run
without the `dotnet build -c Release` that follows them. And a run answering holes about
a foreground it never got, which is a desk somebody is using rather than a flaky suite.

Then the ones a first case meets: a selector matching nothing, a case name declared
twice, a fixture naming an environment nothing carries to the launch, a capture asked of
an application with no in-app half.

Each row is a search term. The value of this page is entirely in whether the words on it
are the words a reader pastes into a search box, so it is written from real messages
rather than paraphrased.

### §WW495 A plain-text twin per page, and the area in llms.txt

The pitch page writes a Markdown twin per route and an `llms.txt` that lists them,
because a model reading this project should not have to render a web page to learn what
it is. The documentation area beside it publishes HTML and a search index, and nothing
else.

That is the same defect one page at a time, and it lands on the pages an agent most
needs: the format, the grammar, the verbs. An agent driving an adopting repository has
the MCP tools, but an agent deciding whether to suggest this tool at all has exactly
what a search engine has.

Converted from the built HTML, never authored twice. Half these pages will be generated
— a verb table off the catalogue, a format table off the loader's schema — so a twin
written from the MDX source would carry the prose and none of it. One render, two
outputs, so the two cannot disagree. It runs after the Astro build, because Astro
empties its output directory before it writes.

And the area's pages join the site's `llms.txt` rather than starting a second one: two
indexes under one base is one answer a crawler takes and one nobody asked for.

### §WW496 One sitemap for one base

The prerender writes `dist/sitemap.xml` from the route table and `robots.txt` names it.
Starlight writes `dist/docs/sitemap-index.xml` from its own pages, and nothing points at
it. So the site publishes two sitemaps under one base, and the one a crawler is told
about lists the pitch routes alone.

That is backwards for what the area is: the pitch page is one scroll designed to be
arrived at, and the area is where the reads that decide an adoption live — the ones
somebody arrives at from a search for a message, a field name or a locator form.

Either the site's sitemap gains the area's pages or it points at the area's index; one
of the two, decided once, with the other turned off rather than left writing a file
nobody reads. The site's existing test says the sitemap lists every route exactly once
and nothing else, so whichever way it goes, that test is what has to be taught the new
rule — and it is the same test that stops the area's pages being added and then quietly
dropped when a route is renamed.

Small, and it is the difference between the work in this block being found and being
published.

### §WW497 A gate on the links the area does not own the other end of

The area is deliberately thin about the long form: it links the README rather than
holding a second copy, and several of those links carry an anchor — `#writing-a-case`,
`#what-needs-the-application-to-cooperate`. An anchor is a heading spelled as a slug,
and this repository renames headings whenever the argument under one changes.

Nothing notices. The link still resolves, GitHub lands the reader at the top of a
thousand-line file, and the page that sent them there reads exactly as it did when it
worked. It is the quietest kind of rot, and it lands on the reader who was already being
sent somewhere else for the detail.

The gate is small because both ends are in this repository: read every link the built
area emits at this repository's own README, take the headings the README actually
declares, and fail the build where an anchor names one that is not there. It runs with
the site's other tests, which already read the built output rather than the source.

External links are out of scope — nuget.org and github.com are not this project's to
keep — and so is the reverse direction: a heading with no page pointing at it is not a
defect.

### §WW498 The words, defined once, before the pages that use them

The area is written in a vocabulary it never defines. A *case* is a data file and not a
test method. A *hole* is a check that could not be evaluated, which is neither a pass
nor a failure. The *desk* is the interactive session a run needs to itself. A *reading*
is what an element reports, and also what a run takes of the machine before it starts. A
*fixture* is what an application is launched with, and a *project* is the declaration
and not the csproj.

Every one of those is a word a reader already owns, used here to mean something
narrower. That is worse than a term they have never seen: a new word is looked up and a
familiar one is assumed, so the misreading survives the whole page.

The page is a definition per word, one paragraph each, each ending in the page where the
word does its work. It goes first in the sidebar, before installing, because it is the
read that makes the other pages parse — and it is short on purpose, since a glossary
long enough to need its own navigation is one nobody reaches the end of.

It is also the page to link a word from, once the area is more than three pages deep.
