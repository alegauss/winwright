# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

### §WW149 Two verdicts about different runs

Seen the moment the check became part of the ordinary command. The roll call's entry
point is exercised directly by the suite - which is right, since the exit codes are the
thing being asserted - and it writes what it found to the console as it goes. Those
sentences now appear in the middle of a real run, above the real one.

A reader skimming a failed run sees "4 of 4 were recorded and never ran" and then "all
957 discovered cases ran", and only the second is about the run they are looking at. The
first is a fixture answering about four names in a temporary file. This block's
criterion asks that a degraded run be legible without reading the log, and two
contradicting verdicts in one output is the opposite of legible.

The entry point should write where the caller says rather than to the console it happens
to find. A writer passed in, defaulting to the console, costs one parameter and lets the
cases assert what was written instead of leaking it - a better test as well as a quieter
run, since nothing today checks the words that reach a reader at all.

Worth keeping in mind for the next tool: anything with a Main that a test calls directly
will do this, and the reason to fix it here rather than tolerate it is that the
sentences are about the same subject and differ only in which run they describe.

### §WW150 One directory, however many runs

Filed against the check the moment it stopped being optional. The roll compares a
discovery listing with a results file, and both now default to one fixed directory at
the root of the repository. One run is fine. Two are not: a developer running the suite
while an agent runs it in another shell has both writing discovered.txt and
winwright.trx, and whichever finishes second reads a listing from one run against
results from the other.

What comes out of that is a phantom - names discovery found in one run missing from the
other's results - and the shape of the answer is exactly the shape of a host that died.
So the check invented to stop a false green would produce a false red, and the reader's
correct response is to run it again, which is the habit this whole thing exists to
break.

The repair is a directory per run rather than a lock: a lock makes the second run wait
for the first, which is slower and still wrong if either is killed. What names it is the
task's to decide - the process, a stamp, whatever the runner already has - so long as
two runs started a second apart cannot collide.

Worth noting the check has no way to notice this today. A listing and a results file
carry nothing that ties them to one run, and if they carried it the mismatch would be a
refusal rather than a phantom shortfall.

## Block B — Attach, launch, and leave nothing behind

### §WW152 Survivors nobody reads

Read against this block's criterion once its last line shipped. The criterion says that
after any scenario ends - passing, failing, throwing or interrupted - nothing it
launched is alive, and the summary names whatever had to be stopped. The first half is
built and tested: the register stops what it launched and records each one as stopped,
killed or already gone. The second half has no reader. Survivors is called by nothing
outside the register's own tests.

So a run that had to kill a window says so to nobody. That matters most in the case the
criterion was written for: a process that ignored a close and had to be killed is the
difference between a scenario that tidied up and one that left the machine in a state
the next run inherits, and today both print the same nothing.

The place is the same one the pointer reasons went to. A preamble carries findings
beside its conditions, and what had to be stopped is a finding rather than a
precondition: it does not excuse an assertion, it is not a failure of the application,
and it is exactly the sort of thing a reader wants in front of them when the next run
behaves oddly.

Worth saying where this really lands. Block G builds the thing that takes a run, and its
dependencies are now all finished; whoever builds it inherits this, and this line is
what tells them the claim is owed rather than met.

## Block C — Locate — the locator grammar and the tree an agent reads

### §WW144 The line is not a structure

Noticed twice in one block, which is the signal. Inspect renders each element as a
string that begins with the locator step and continues with the rectangle and the
patterns, and anything wanting the locator back has to find where it ends. Two test
files now carry the same helper for that, and the second was rewritten mid-task when a
name turned out to contain a run of spaces: the separator is two spaces, and two spaces
occur inside a name somebody else wrote.

The helper that works scans for a double space outside the quotation marks, honouring
backslash escapes. That is a small parser, written twice, to recover something the
renderer had in its hand a moment earlier. It is the shape of code that is correct until
the day the format gains a field.

What is wanted is that the rendered form carries its parts. A rendered element with the
step, the indent and the whole line on it costs nothing to produce, keeps the line
exactly as it prints today, and means nothing downstream has to know where the fields
meet - the diagnosis view, the copied-line checks and anything an adopter writes.

There is a second reader worth converting at the same time: the check that every printed
line parses is the check most likely to be defeated by the format changing under it, and
it is the one that should be reading a field rather than a substring.

## Block D — Act — patterns before pointers

### §WW147 A count that exists for one expression

Read against this block's criterion once its last line shipped. The criterion says a
retry is bounded and said out loud: no act retries until it passes, the attempt count
reaches the trace, and an act that only ever works on the third attempt is visible in
the output. The first clause holds and the other two do not.

The retry type carries both halves. Bounded runs an act to a cap and answers how many
attempts it took; Recorded stamps that count onto the step a trace records, and says in
its own comment that a step which took three goes is a different step from one that took
one even when both are green. Recorded is called by nothing.

The one site that retries is the surface restorer pressing a toggle back to where it
was. It calls Bounded and drops the answer, so the number exists for the length of one
expression and then does not. A control that only comes round on the third press is
invisible, which is the finding the criterion asks to see.

What is owed is the join and not a new mechanism: the restorer keeps what Bounded
answered, and whatever it reports carries the count. The wider question belongs in the
same task - whether a restore belongs in a trace at all, being the harness tidying up
rather than the scenario acting - and if not, the count belongs in what the restore
reports instead.

### §WW148 The other weaker route, still unnamed

Left standing by WW134 and pointed at by WW135, which closed the same shape one type
over. A destructive list is built two ways. One takes what the project declared,
resolves a key across every language it ships, and refuses a bare name where more than
one is shipped. The other takes plain strings, makes every one of them a name, and
refuses nothing.

The second exists because a caller with no project still needs one - the same honest
need that left a subject constructible without a declaration. WW135 answered that need
by keeping the shape and naming it for what it gives up, so a reader meets the word
before the consequence. This one is still spelled as an ordinary overload of the same
name, which is the spelling that made the subject's version a trap.

So it is the same repair: name it, and let the name carry the loss. Whether it is
renamed or removed depends on whether anything outside a test wants it, which the task
should check first rather than assume.

Worth stating the rule this is the third instance of, because a fourth will come. Where
a guard can be declined, the declining is a named thing a reader sees at the call site,
and never a second overload that differs from the guarded one by which arguments
happened to be to hand.

## Block E — Capture — the picture that proves what it photographed

### §WW38 A region, not a handful of points

Nine sampled points cannot cover a window: the capture taken to verify one task passed
all of them while carrying two windows of another process across its lower-right corner.
More points only move the threshold - the number that finally covers a window is the
number of pixels in it - so the question is asked about the region instead. The z order
above the window is enumerated and each frame intersected with the copy rectangle, which
answers for the whole area in one pass and names the intruder rather than merely
refusing.

### §WW40 Fail, never crop

An overlap on the edge is now inside real content, because the copied rectangle is the
painted frame and there is no invisible border left for a foreign window to hide in. A
file quietly trimmed to dodge an intruder is a picture of something nobody asked for.
The refusal names the intruder, its process and the rectangle it covers, because
something else was in the way is not actionable and a title with a pid is.

### §WW41 A backdrop transmits what is behind it

Measured in freewilly: with nothing overlapping, the copy still carried a blurred image
of the desktop behind the window - another application's content legible through the
frame - because a Fluent window's backdrop composites what is behind it by design.
Z-order reasoning cannot answer for that: the intruder is not in front of the window, it
is showing through it. The refusal is positive evidence rather than a name, so the
compositor is asked which backdrop the window opted into, and a popup - the one thing
the screen copy exists for - is not refused by it. A printed warning was the first
response and was not enough, because a warning is not a refusal and the file gets
written either way.

### §WW42 One colour is not a window

Measured in freewilly while shipping a task: a copy of the notification area came back
as exactly one distinct colour, with the session present, the shell running and the
environment reporting an interactive desktop. The display was simply not rendering
anything a copy could read. Without this assertion the script would have written that
file and exited zero, and the reader would have had a picture of nothing that claimed to
be a picture of something.

### §WW43 The page is still saying it is loading

Measured in claude-tray: a report on a machine with 213 recent transcript files took
about 25 seconds to build, and at the default wait the copy came back as a heading, a
subtitle and the words computing your consumption pace. Two variants captured that way
are near-identical for the same reason, so comparing them proves nothing, and it was
caught only because somebody looked. A longer wait is the wrong answer twice - it slows
every capture and still passes the page that needed longer still - so the loading
strings are read from the project's own language files and asked of the tree instead. A
key none of those files carries refuses the run, because a check that silently matches
nothing is the shape of defect this whole path exists to stop.

### §WW46 Byte-identical is the cheapest visual assertion

freewilly's window skill states the rule: a change meant to be invisible must produce a
byte-identical file, and the render is deterministic, verified by re-capturing unchanged
code. Three findings about theme handling in that project came from this and from
nothing else, and its test suite saw none of them. It also avoids choosing a tolerance,
which is the argument every other image comparison eventually turns into.

## Block F — Assert — the expectation is derived, never typed

### §WW151 A fingerprint nobody takes

Read against this block's criterion once its last line shipped. The criterion says the
fingerprint taken before the run matches the one taken after, on every scenario,
including the ones that drive a real setting. The type that takes a fingerprint exists,
it is thorough, and outside its own tests nothing calls it.

So the claim holds exactly as often as an author remembers to write both halves, and the
half that gets forgotten is the second one - the run is over, the assertions passed, and
nobody is looking. A settings file rewritten to the same length is the accident the
whole type was built for, and it is invisible unless somebody asked twice.

The place is decided now in a way it was not when this was filed. The preamble carries
findings beside its conditions, and a store that changed is exactly a finding: not a
failure of the scenario, since the application did what it was driven to do, and not a
precondition either, since nothing may be excused by it. The before reading belongs
where the run describes the machine and the after reading beside it.

What must stay true is the difference between the two absences. A run that took no
fingerprint because the project declared no store is a run with nothing to say; a run
that took one and found it moved is a run with something to say. Reporting them the same
way is the shape this project keeps refusing.

## Block G — The scenario — a case is a data file

### §WW57 A case is data

The interaction harness in claude-tray is 2,732 lines for eight cases, and most of what
is in it is the same loop written eight times. The steps, their locators, their acts and
their expectations are fields; the loop, the waits, the retries, the process register
and the verdicts belong to the engine. This is the whole reason for a framework here
rather than the library it would otherwise have been.

### §WW58 Refused at insertion, not linted afterwards

roadkeep's first law, transferred. A linter reports after the text exists, and by then
the work is done and the author is being asked to delete what they just wrote. A field
validated at the point of insertion refuses before the case is composed, which converts
an analytical act into a procedural one - and the saving is the analysis rather than the
characters.

### §WW59 One case runs alone

The value of a small case is partly that it costs ten seconds when a name is what
changed. Run takes a file, a case or a tag, and says what it did not run - because a
filtered run reporting success without qualification is the same silent pass the third
verdict exists to prevent, one level up.

### §WW60 A fixture reaches every launch a case makes

The states a menu exists to report are the ones where the environment disagrees with the
application, and on a developer's machine it never does - so without a sampled
environment those assertions are only ever unchecked. One declaration decides both what
the app is launched with and what the expectations are read from, so the two cannot be
given different modes and a sampled menu is never compared against a real environment.

### §WW61 A precondition is declared

pportal's interaction tests fail rather than skip when no controller is plugged in, and
say so, because xUnit gives them no third outcome to use. With one, the precondition
belongs in the case: this needs two profiles, this needs a pad, this needs a display
that renders. Its absence is then named and counted rather than argued about, and the
case stays honest on a machine that cannot run it.

### §WW62 One launch, lent to the cases that only read

Three cases in claude-tray drive the same window and each used to own its process, so a
full run paid the launch, the first layout pass and the wait for the first poll three
times over - seconds each, for a window none of them leaves in a state the next would
reject. Sharing is opted into per invocation rather than being a merge of the cases, so
a case run alone still owns its process, which is the property that keeps it worth
running alone.

### §WW63 A case names the defect it exists to catch

Every case in these harnesses carries a task id and a sentence about what went wrong
without it, and that is why they survive: a check nobody can justify is a check nobody
dares delete and nobody dares change. The field is part of the schema, so the
justification is written when the case is, rather than reconstructed a year later out of
a commit message.

## Block H — The Claude Code surface — plugin, tools, skill, hook

### §WW65 The plugin is the installation

Two commands in the adopting repository write both declarations into its settings, and
committing that file wires every clone - no per-machine step, no path entry, no
instruction that differs by operating system. What arrives with it is the hook, the
tools, the commands and the skill. This is roadkeep's adoption story, and it is the
reason that tool gets used rather than admired.

### §WW66 The schema arrives as the tool's input schema

Flag names typed from memory are guesses, and a guess costs a refusal and a retry at
best. A tool whose input schema is this project's scenario schema makes the fields
arrive already named, already typed and already constrained, which is the difference
between being told what is wrong and being unable to express it in the first place.

### §WW67 The hook is what makes the verb the easy path

A hand-written harness script is always available and always faster in the moment, and
that is exactly how 2,732 lines happen. The guard denies the write and names the verb
that replaces it, which is the same shape roadkeep puts in front of a governed file -
and the reason it works is that the refusal arrives before the work rather than after
it.

### §WW69 The skill loads when a window is in play

The whole content of an instruction file is loaded on every turn against a budget, which
is why claude-tray keeps its flag catalogue in a skill and only the rules in the file
the harness reads. The skill says which loop answers which question - a picture proves
layout, an interaction proves input, a render proves determinism - and what it costs a
session is measured rather than assumed to be small.

## Block I — The in-app half — the app cooperates with the harness

### §WW153 A report that spells a version no file holds

The agreement reading drops the build metadata the SDK appends - `+694044e...` - in
exactly one place, and its own comment says why: the metadata is already not part of the
version, so a sentence carrying it would name one copy's decoration as the answer.
`Versions` drops it. `Render` does not.

So the gate WW142 stood up prints both spellings at once. Run against this repository
the sentence says all five copies are `0.1.0`, and the line under it says the assembly
being called is `0.1.0+694044e37cdff1f8ad593f1fa3735e05af09d218` - a version string no
file in the tree holds, beside four that hold theirs. A reader comparing the two has to
already know which half of that is decoration.

The alignment goes with it. `Render` writes the version into a twelve-wide column, which
a forty-seven character informational version overruns, so the one copy most in need of
being read against the others is the one whose row does not line up with them. Measured
rather than supposed: it is in the run that shipped WW142.

What it owes is one decision applied in both places - either the report drops the
metadata as the sentence does, or it keeps it and says it is keeping it, in a column
wide enough for what it prints. The reading already knows how to drop it; nothing here
needs a new rule, only the same one twice.

## Block J — Adoption — the proof is the deletion

### §WW78 The keyboard case, first

It is the shortest path through the whole framework - launch under a named host,
navigate by clicking a control with no automation peer, resolve by id, type, read back
through a pattern, traverse, and drive a range - and it is the case whose absence let a
window ship with no keyboard input at all. Migrating it first means the engine is
exercised end to end before anything else about it is claimed.

### §WW79 The panes case runs on every machine

This assertion used to live inside the profiles case, which opens by counting profiles
and skipping below two. That is right for a round trip and wrong for the property that
made the round trip readable: a tab body being in the tree has nothing to do with
profiles, and behind that skip it did not run on a single-profile machine - which is
most machines and every hosted runner. Migrating it separately is what keeps the two
apart.

### §WW80 The sessions case is the argument for the whole loop

A popup is its own top-level window, so no render over a page's content can photograph
it and no published screenshot ever will. Whether that note is readable at all is a
question only the accessibility tree can answer. The case also waits out an asynchronous
scan, expands a row into a tree and puts the surface back afterwards, which makes it the
widest single test of locate, act, wait and restore in one place.

### §WW81 The profiles case is the only thing that drives the picker

Every capture renders one profile, which is structurally incapable of seeing three
defects that all need a second switch and two of which need it to come back. It also
carries a timing claim - a line that must never be observed on the way back - and that
claim is void unless the walk reports how many selection changes it took. Migrating it
proves the hop count and the watch-while-waiting shape both survive the move.

### §WW82 The menu case reads the notification area

Nothing else in any of these repositories opens a tray menu, and everything hard about
it is Windows-specific: an icon with no clickable point, an overflow flyout that has to
be opened before the icon is in the tree at all, a right-click the current shell does
not deliver, and a submenu that expands only by keyboard. It is also where the
expectations are derived from the application's own read-out instead of typed by hand.

### §WW83 The switch case rewrites a real setting

Until that case existed, the path that rewrites the setting, re-keys the stores and
takes the other account's token ran under no check at all. It is refused against a
resident process, because a pick there would repoint the real icon for real. Migrating
it inside the store comparison asserts the promise that a run touches nothing at the one
place most likely to break it.

### §WW84 The names case observes what no screenshot can

A picture cannot see an accessible name, and an unnamed control is invisible to every
other check. The case sweeps every panel the page declares - derived from the navigation
labels, so a panel added later is covered with no edit here - and reads every control
the naming rule is responsible for, covering both the branch that must fire and the
branch that must not. Getting the second wrong gives three controls in one row the same
name, which is worse for a screen reader than one unnamed.

### §WW85 The environment sweep, last

It walks one submenu per sampled mode, and it is the case that proves a fixture reaches
every launch a case makes rather than only the first. It is also the case that produced
the deduplication rule, by counting one absent assertion three times and reading as
three holes. Migrating it last means the fixture machinery is already in place and this
is a use of it rather than the reason to build it.

### §WW86 claude-tray loses both scripts

451 lines of capture and 2,732 lines of interaction, and the argument for this framework
is that neither should exist inside a product repository. They go once every assertion
in them is a case, and the line count removed is reported rather than described - a
saving nobody measured is a saving nobody can check.

### §WW87 freewilly loses its copy and its probe

Its capture script is 382 lines sharing most of their reasoning with claude-tray's 451,
and differing in two real ways - the backdrop refusal and the flat-colour refusal - both
of which belong in the engine and neither of which the other project has. The page probe
is the geometry dump this framework already owns, pointed at an installer surface
instead of at a window.

### §WW88 pportal loses the harness and the runner

The interaction file becomes scenarios and the twenty-seven copies of the
single-threaded runner become one package reference, which is the largest single
deletion the whole adoption produces. It is also the hardest, because a thousand other
tests sit around it and the migration must not disturb the parallelism setting the
runner config exists to hold in place.

### §WW154 The tool nobody outside this backlog can read about

There is no README. `docs/` holds the roadmap, the ledger and the rationale - all three
written for whoever is building this - and the repository root holds a solution, a props
file and two scripts. An adopting project meets winwright through a package id and a
path into somebody else's tree.

This is not a documentation preference. The shipping rule in
`.claude/skills/roadmap-docs` runs a decision on every task that ships: would an adopter
do something differently because this shipped, and if so, hit the surfaces that exist -
naming the README's feature list first. That clause has been answered by the surface not
existing, every time, for every task so far. The verbs, the locator grammar, the exit
codes, the refusals an adopter can hit and the two package ids they take are all
decided, and none of them are written anywhere a reader outside this backlog would look.

What it owes is one file at the root answering what this is, what it needs (Windows, the
Desktop framework, the two packages), what a scenario looks like, and what each exit
code means - written against what has actually shipped, read out of the ledger rather
than out of the roadmap, so it never promises a verb that is still a line.

It is then held by the same gate every other adopter-facing surface is: the task that
changes an exit code changes this in the same commit.

## Block K — The proving ground — a fixture app built to be hard to test

### §WW145 The pairing is checked one way only

Filed against WW132 the moment it shipped, which is where this kind of gap is cheapest
to see. The pairing now compares two lists that nobody compared before - the refusals
the assemblies name against the entries, and the entries against the flags the built
fixture prints - and both directions are real. The claim in the middle is not checked at
all.

An entry says --language reaches the unusable-label refusal. What holds that up is that
somebody wrote it down. Renaming a flag is caught; changing what the flag draws is not,
and neither is an entry that was wrong when it was written. Three of the four entries
naming a flag are provoked in the suite by building the situation directly rather than
by running the fixture with that flag, so the sentence and the evidence are about
different things.

What closes it is a case per entry: launch the fixture with the flag, drive the thing
the refusal is about, and assert the refusal. Four today, and the number is small
because the count of reachable refusals is small - which is the other half of why this
is worth doing now rather than at forty.

The shape to avoid is a case that catches any exception and calls the pairing proved.
The refusal type is named in the entry, so the case can insist on that type and nothing
else, and a refusal arriving for a different reason is a red rather than a pass.

### §WW146 The four the fixture cannot be

Counted by WW132 and left standing by it. Four refusals need a shape the fixture cannot
take: a receipt about a window other than the one captured, a picture nothing drew, a
capture of an element with no background above it, and a render of a tree that lays out
to nothing. The fixture always does the right thing, so it can never make any of them
happen, and each will quietly stop working with nothing to notice.

This block's criterion is explicit that a refusal with no flag is a finding the fixture
closes rather than a gap nobody sees, so the count being visible was the previous task's
work and closing it is this one's.

Three of the four are one shape apiece and the shapes are the point rather than the
refusal: a pane that lays out to nothing, a pane with no background above it, and a window
drawing nothing into the rectangle a capture would take. Each is a real product defect - a
page that renders empty, an element captured as transparent, a surface that came out blank
- and the fixture exists to be those on demand.

The fourth may not be a shape at all. A receipt about the wrong window is a harness
handing over the wrong handle, not an application misbehaving, and the honest answer may
be to move it to the reasons no flag can reach rather than invent a fixture that lies
about its own window.
