# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

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

### §WW512 The directory nothing starts in

WW508 set the launch's working directory to the project's root and let a fixture name
its own. What holds it is four cases over `ProcessStartInfo`: what a declaration
resolves to, and that `Starting` puts it on the object it returns.

That is the declaration about the launch and not the launch. Nothing starts a process
and asks what a relative argument resolved to, which is what an adopter relies on — and
it is the falsification WW508 wrote for itself: *a fixture argument naming a
project-relative file is resolved differently by two runners on the same checkout*. The
fix makes that true by construction and no run visits it.

The gap is WW510's shape: every part asserted, the behaviour not. A `WorkingDirectory`
that `UseShellExecute` ignores, or a later edit dropping the assignment while the cases
pass over `StartsIn` alone, both read green.

What it needs is a mode on `Winwright.Fixture` and a case over it. The application reads
no command line today — every fixture here is told apart by what it draws, never by what
it was passed — so this is a flag making it report where it is, in a label a locator
reads, which is how everything else here is observed. A case against a fixture naming
`workingDirectory` reads that label back; one naming none reads the project root.

Worth having beyond this task: the suite's first case where an argument a fixture passes
changes what the window says — the half of `FixtureDeclaration` nothing observes.

### §WW513 The read-out's own directory

WW508 gave the fixture launch a working directory: the project's root, or what the
fixture named, resolved against that root. The application has a second launcher and it
did not get one.

`DerivedSet.Printed` runs the application to capture what it prints, which is where a
derived set comes from when it comes from the application rather than from the strings
files. It builds its own `ProcessStartInfo` — redirected stdout, `CreateNoWindow`, UTF-8
— and sets no working directory, so it inherits whichever one the runner happened to be
in. It holds `declaration`, so the root is one field away.

The exposure is narrower than the fixture launch's and is the same kind. A project's
`reports` arguments are usually flags, but nothing stops one naming a file, and a
read-out that resolves it against the runner's directory is WW508's defect in the
launcher WW508 did not name.

What makes this its own line rather than a line in that commit is that it cannot be
held. `Printed` builds the start info and starts the process in one call, so there is no
seam to assert against — which is why `FixtureDeclaration.Starting` returning a
`ProcessStartInfo` is the shape to copy. Nor does any case exercise a read-out through a
project's `reports`, so even the one-line change would go in unheld, and an unheld line
is what this backlog keeps filing tasks about.

So the work is the seam first, then the directory, then a case over both.

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

### §WW511 The matcher nothing reports on

`documented` says nothing about a line its matcher did not match. It clears the doc
comment it had accumulated and moves on — right for a brace or a blank line, silence for
a member it failed to recognise.

Three callers match with `/^([A-Z][A-Za-z]*),$/` — `grammar.mjs` twice, `verbs.mjs` and
`holes.mjs`. That recognises a valueless member with a trailing comma and nothing else.
Give one of those enums a member with a value, or write the last one without its comma,
and the member leaves the page with nothing said.

What makes this more than a shape worth tidying is that the pairing meant to catch it
asks the same question. `verbs.test.mjs` finds `Cooperation`'s members with `/^
{4}([A-Z][A-Za-z]*),$/gm` — the generator's own regex, copied — and checks one
direction: every kind published is declared. So the page would explain one kind of
cooperation where the engine has two, and the suite would be green.

WW507 repaired exactly this in the fourth caller, and its shape is the one to carry
across: the matcher recognises any member, and the caller refuses after the walk what it
cannot use. A line that does not match is a line nothing reports, so nothing may depend
on a matcher being complete.

The pairings are the other half, and a second regex over the same lines is not an
independent read. Counting the members a body declares is a different question from
which lines look like one, and it is the question a case should ask.
