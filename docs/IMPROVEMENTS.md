# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW508 The directory a fixture starts in

Found by quickshell (QS181). Its import case needed the dialog to look the same on every
desk, so the fixture was to pass the client a session file committed beside the cases:
`"arguments": ["--import", "cases/fixtures/MobaXterm.ini"]`. That path means nothing to
the launched application. `FixtureDeclaration` starts it with a `ProcessStartInfo` that
sets no working directory, so it inherits whatever directory the test runner happened to
be in, and a relative path resolves somewhere different under `run-tests.cmd`, under
`dotnet test` and in the guest.

An adopter is left with two workarounds, both bad: changing the runner process's own
current directory before `Suite.Launch`, which is global state in a test assembly, or
writing an absolute path into a data file that is committed and read on other machines.

What to build: the fixture's application starts in the project's root, the directory
`winwright.json` is found in, unless the fixture names a `workingDirectory` of its own,
resolved against that same root. The run's trace says which directory it started in, so
a fixture whose file was not found can be read as a path problem rather than a missing
file.

Falsified when a fixture argument naming a project-relative file is resolved differently
by two runners on the same checkout.

### §WW509 A fixture's own environment

Found adopting a case for quickshell's QS217. The client keeps its saved sessions in a
file under the user's AppData, and a case that searches the session list needs a store
holding known sessions. A fixture declares `arguments` and `shareable` and nothing else,
so it cannot give the launched application an environment variable naming another store,
nor place a file where the application will look, and the case has to stop at "the entry
is listed".

The workaround on the adopter's side is a command-line flag that exists only for the
harness, which is a surface the application's users then see, or a test runner that
writes into the real user profile before launching, which is global state another case
can trample.

What to build: a fixture may declare `environment`, a map of names to values merged into
the launched process's environment, values resolved against the project root the way a
file argument is once WW508 lands. Shared fixtures key on it, so two fixtures differing
only in environment are two launches. Optionally `files`, copied into a scratch folder
the environment can name as a token, so a case gets an isolated store without a fixed
path.

Falsified when a case can launch an application against a file of its own choosing
without the application growing a flag for it.

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

### §WW510 The swap nothing loads a page to watch

Both halves of the site now state a version the browser asks nuget.org for: the landing
page since WW500, the area since WW506. Nothing exercises the swap on either.

What is asserted is the parts. `nuget-feed.test.mjs` runs the picker against versions it
makes up, which is where the real failure lived — a numeric prerelease counter, a
release outranking a prerelease of the same core. `docs-area.test.mjs` asserts the block
carries the version it was built with and the ids to ask, and that the picker reached
the page. Every one of those can hold while a reader sees the built number forever: a
selector matching nothing, a text node the highlighter split differently, a promise
nobody awaited.

The copy button is the specific worry rather than a general one. The page rewrites two
things for one number — the text a reader sees and the `data-code` the button hands over
— and a reader who copies getting a different version from one who reads is worse than
both being stale.

It needs a page loaded in something that runs scripts, with the feed answered by a stub
so the case is about the swap rather than about nuget.org being up. That is another
devDependency, which is the cost to weigh: the no-dependency rule is the engine's, and
nothing holds the site's build tooling to it. Weigh it against the alternative, which is
the one behaviour here published on trust.
