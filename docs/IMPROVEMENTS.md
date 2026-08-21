# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

### §WW1 Three outcomes, three exit codes

Two verdicts cannot describe what actually happens on a desk, and claude-tray measured
that and grew a third: 0 where every assertion ran and passed, 1 where one failed, 2
where everything that ran passed but something could not be evaluated at all - one
profile registered, no report rendered, a tray already resident so the menu case had to
refuse. Every one of those used to be an info line, and an assertion that quietly
stopped running is the defect two of that project's tasks were filed for. xUnit has no
vocabulary for it, which is why pportal fails instead and says so in as many words.

### §WW2 Unchecked is a missing precondition, not a missing check

The distinction is load-bearing and easy to lose. Unchecked means it should have been
checked and was not - a second profile that does not exist here, a foreground this run
does not own. It is deliberately not for an assertion that can never run: the Settings
window binds nothing to Escape, so there is no Escape behaviour to lose and nothing is
reported. A check that matches nothing on any machine is worse than an absent one,
because it reads green forever; that one is refused when the scenario loads rather than
counted as a hole at run time.

### §WW3 The trace is what the run saw

A verdict with no record makes every failure a re-run, and a re-run on a different desk
answers a different question. One line per step: the locator as written, what it
resolved to, the pattern used, what was read back, how long it waited and how many polls
saw it. Written as JSONL beside the run, because the reader is usually an agent and a
viewer nobody can grep is a viewer nobody opens.

### §WW4 Printed every time, counted once

claude-tray's environment sweep walks one submenu per sampled mode, so an assertion
absent in all three modes was counted three times and read as three holes. The line
still prints at every occurrence, because where it did not run is part of the reading,
and the tally dedupes by name. Two different properties, and collapsing them either
hides an occurrence or inflates the count.

### §WW5 The declaration is per project

roadkeep's sixth law, applied here. The executable, the source root the staleness check
compares against, the language files, the default timeouts and the store to fingerprint
are all facts about a project and none of them about a case. A scenario carrying one is
a scenario that runs on exactly one checkout, which is how a harness becomes unmovable
and then unowned.

### §WW6 The word every is earned

A run where an assertion could not be evaluated is not the same run as one where all of
them passed, and printing the same green line for both is how a timing assertion got
dropped into an info line nobody reads. The summary refuses the word while the unchecked
list is non-empty, and names what is on it rather than counting it.

### §WW7 A broken harness is not a broken build

A pattern that throws, an assembly that will not load, a locator that cannot be parsed -
none of these is a statement about the code under test, and reporting them as a failed
assertion sends whoever reads it to the wrong file. The outcome carries the step and the
exception and is its own colour, so the reader knows which repository to open.

## Block B — Attach, launch, and leave nothing behind

### §WW8 Every launch is registered by construction

Measured in claude-tray: two trays a failing case had started were still alive
afterwards, the next build died on a file lock naming their process ids, and the command
after that ran the previous executable and reported on code that was not in the tree.
The register is total because every launch goes through one place - there is no per-case
list to keep, so a case returning early down a path nobody thought about still has its
process stopped - and whatever survived is named in the summary rather than cleaned up
in silence.

### §WW9 A build that failed leaves the old exe in place

The same wrong reading as driving an unnamed binary, arrived at by accident rather than
by flag. The binary's write time is compared against the newest source file; older means
this run is about the previous build. Unchecked rather than failed: everything that ran
did run and did pass, on a binary. What could not be evaluated is the claim the caller
actually came for.

### §WW10 Two keys, because one is not enough

A harness once reported that every check passed against a tray published the previous
afternoon, before the submenu entry being verified existed in it. The file version
catches the ordinary case and cannot catch that one, because a Debug build and an
installed Release carry the same version between releases - so the write time is read
second, and a version difference is reported in preference to it because it is the more
useful sentence.

### §WW11 A window with no main window handle

A borderless toast, a balloon or a menu is a top-level window the process owns, and the
process object reports none of them. Top-level windows are enumerated by process id and
filtered by a sane size, which skips the tool and message windows every process carries.
claude-tray's frame capture already does exactly this, and it is the one path in the
launcher that reaches an animation.

### §WW12 Another window of the same app is the wrong picture

The failure this refusal exists for returned a picture of another instance's Settings
window when Statistics had been asked for, printed the size it captured and exited zero.
Only a windowed instance counts: a resident tray showing nothing is running on every
developer machine here, and a check that fired on it would make every routine capture
take an override to work.

### §WW13 The foreground is a precondition, not a retry

Windows refuses the foreground to a process that does not already own it, so a run
started from an editor drives somebody else's window. Measured while verifying a task in
claude-tray: the same case failed at three different points on three runs and passed
unchanged either side of stashing the code under test. It is asked once, answered as
unchecked with the intruder named, and deliberately not retried - a case that passes on
the second attempt cannot tell a busy desktop from a broken build.

### §WW14 Attach is a different promise from launch

Attaching is convenient on a developer machine, where a single-instance mutex makes a
second launch exit silently. It is also a different claim: what gets checked is whatever
binary is up. So attach says which one it reached, and every assertion that depended on
a launch argument is unchecked by construction rather than compared against a value this
process never received. It is never implied when a running instance is found, because
implying it moves the check onto a binary nobody named.

### §WW15 A launch argument does not survive an attach

Measured in claude-tray: verifying a task against a Portuguese tray with the default
English produced four failures for labels that were all present, in another language.
There is no command line to read, so the language is resolved the way the app resolved
it - saved preference first, then the display language - and reported out loud. A
language explicitly asked for that this process cannot be in is unchecked rather than
quietly replaced.

## Block C — Locate — the locator grammar and the tree an agent reads

### §WW16 One locator grammar

The same three conditions are rebuilt at every call site today, in PowerShell in one
project and in C# in another. One grammar parsed once - the automation id, the name, the
control type, the class name, the pattern it must carry, an index and chaining - and
resolved by every verb the same way. It is also what the scenario file writes, so an
agent learns one thing rather than one per language.

### §WW17 A deadline, and a form with no wait at all

Both exist in the harnesses already and the second matters more than it looks: a helper
that retries for two hundred milliseconds folds that sleep into every miss, so a loop
doing its own waiting ends up measuring its own helper. Asking whether something arrived
needs a deadline; asking whether it is gone needs a single look, and using the first for
the second is how a timing observation loses its resolution.

### §WW18 Actionability, for Windows

Playwright waits for visible, stable, enabled and able to receive events. The Windows
equivalent is present in the tree, not offscreen, enabled, and carrying the pattern the
act needs - and the fourth is the one no browser has, because a control offering no
invoke pattern cannot be pressed without the foreground. The refusal names which of the
four was missing, since each one has a different remedy.

### §WW19 Inspect is a verb

The control view dump exists in claude-tray and prints only on a failure, after a
throwaway script had already been written to get the same output while diagnosing a
missing template part. Making it a verb is what lets a locator be written from the tree
instead of from the markup - and the markup is the thing being asserted, so reading the
expected name out of it is how a check comes to agree with the defect.

### §WW20 A collapsed pane is not in the tree

A control on a page that is not showing cannot be found by any id, which reads exactly
like a control that was renamed or removed. The miss says which of the two it is by
naming what would have to be navigated first, so the answer is a route rather than a
puzzle. This is the second of the five traps claude-tray's harness header records, and
it costs a session every time it is rediscovered.

### §WW21 Disambiguation is geometry

The sidebar items in claude-tray are bare borders with no automation peer, so they are
matched by the text inside them - and the page title carries the same words. Sorting by
the rectangle picks the one on the left. It is a real property of the window and belongs
in the locator, where the next reader can see the choice was made, rather than in a sort
call at one call site.

### §WW22 A pattern's current value is a live view

Holding a pattern and comparing its value before against its value after compares the
reading with itself and can never fail. claude-tray's slider check carries a note about
it and casts the numbers out first. The engine reads values into snapshots and
re-resolves per act, so the trap is closed once rather than remembered by every author
who writes an assertion about a change.

### §WW23 What a control offers, before the act is written

Reaching for a pattern a control does not carry is a run-time failure today, discovered
on a red run and usually far from the line that caused it. Inspect names the patterns
per element, and the scenario loader checks each act against them, so the same mistake
becomes a refusal at load with the element and the pattern both named.

## Block D — Act — patterns before pointers

### §WW24 Invoked, not clicked

pportal's harness states the rule and the reason in its own header: a synthesised mouse
click lands on whatever is drawn at a point, so it needs the window in the foreground,
and Windows refuses the foreground to a process that does not already own it - which
means a run started from an editor or an agent drives somebody else's window. The invoke
pattern asks the control directly: no pointer, no foreground, nothing on top to be
confused with. That is what lets these run unattended, which is the whole point of them.

### §WW25 A pointer act is declared, never inferred

Some controls have no pattern at all: a bare border with no automation peer, a
notification-area icon, a segment of a custom template. Reaching for the mouse there is
right, and doing it silently is not, because the act then carries a precondition the
file never mentions. Declaring it puts the cost where the reader is, and makes the set
of acts that need a real desktop countable rather than discovered.

### §WW26 Typing, read back through the control

The windows in claude-tray accepted no keyboard input at all from the day the first one
shipped, while every screenshot ever taken of them looked perfect - because mouse input
travels the window procedure and keyboard input travels the component dispatcher, and
those are different input environments. Text typed and then read back through the value
the control reports is the only observable that separates the two, and it is the reason
an interaction loop exists beside the picture loop at all.

### §WW27 Traversal has an observable

Tab moving focus is a property of the window and nothing in a picture shows it. What
holds focus after the key is read and named, so a failure says where focus actually went
rather than only that it did not move. The same shape covers an arrow key driving a
slider, with one measured detail: at the maximum the press in that direction is a
legitimate no-op, so the other one is used and the assertion stays about the control
rather than about the starting value.

### §WW28 The route and the hop count are part of the answer

A claim about one switch is void when the walk made several, because each intermediate
stop is a switch of its own and the line observed belongs to some other value.
claude-tray's picker used to normalise to the top and walk down, which silently voided
the timing assertion whenever the pattern route threw - and that fallback exists
precisely because the pattern route sometimes does. Anchoring at whichever end is nearer
costs at most one change for a two-item picker, so the assertion holds on both routes,
and the count is reported so a reader can tell which was taken.

### §WW29 A menu is opened the way a keyboard user opens it

Down to the item, Right to expand, and never invoke - invoking one entry launches a
terminal and another ends the run. The submenu appearing is an event, so it is polled to
a deadline rather than slept at for a fixed interval that is either too short on the day
it matters or paid on every run. Nothing is pressed to reset between attempts, and that
is deliberate: Left on a top-level entry dismisses the whole menu, and retrying after
one walked a menu that was no longer there and failed all three times.

### §WW30 Bounded, counted, and never until green

One walk and one read is a coin toss against a shell that drops synthesised input: three
runs in ten reported a submenu that did not expand, against a build with nothing wrong
with it, wearing the wording of a real defect. What is deliberately not done is retrying
until it passes - the attempts are capped, so a submenu that genuinely stopped expanding
still goes red and merely stops doing so at random. The count reaches the output,
because an act that only ever works on the third attempt is itself a finding.

### §WW31 The notification-area icon

It has no clickable point - asking for one throws - so its bounding rectangle is used
instead. It may sit inside the overflow flyout, which has to be opened before the icon
exists in the tree at all, and the chevron that opens it is identified by its class
rather than by its position among the other tray buttons. Worse on the current shell: a
synthesised right-click on the icon does not open the menu at all, observed on Windows
11 build 26200, and the route that works is focus through automation plus the
application key - the path a keyboard user already has.

### §WW32 Select, then confirm, then click

A tab control builds a tab's content on its first visit, so a selection that silently
does not land leaves the list inside it never realised - and the case then blames a
forty-second scan for a tab it never opened. Seen alternating pass and degrade while the
case was being written, which is the shape that teaches a reader to re-run rather than
to look. Confirming that the selection took, and only then falling back to the pointer,
is what makes the next step's failure mean what it says.

### §WW33 A case hands the window back as it found it

A popup is a toggle and a tab is a position, and the next case sharing that window asked
for neither. Restoring is what makes sharing safe; without it, sharing produces
order-dependent failures that appear only when the whole suite runs and vanish when the
case is run alone. claude-tray's picker walk already goes out and back for this reason,
and the rule generalises to every act that changes a surface.

## Block E — Capture — the picture that proves what it photographed

### §WW34 The off-screen render is the default

A render of a visual tree cannot photograph anything else: there is no foreground, no z
order and no second instance to be confused with. claude-tray keeps both loops and its
own notes say which it prefers; pportal chose only the render and wrote down why. The
screen copy exists for the one case a render cannot reach - a popup, a context menu or a
balloon, each its own top-level window and none of them in the main window's visual
tree.

### §WW35 A blank is not a picture

A tree that failed to build, or one that was never arranged, renders as a rectangle of
transparent pixels, and a caller that checked only that a file was written cannot tell
the two apart. Scanning for any pixel carrying an alpha of its own is the whole
assertion. It is not a claim that the screen is correct, and pportal's suite
deliberately renders an empty element beside it, because a check that has never seen a
blank cannot claim to tell one.

### §WW36 The painted frame, not the window rect

The window rectangle spans a window's invisible resize border and its drop-shadow
margin, so every copy of it carries a strip of whatever is behind the window down its
edges - measured in claude-tray as slivers of the editor along the left, the right and
the bottom of every capture. The extended frame bounds is the visible frame, in the same
physical-pixel space. Measured at two hundred percent: 1760 by 1896 owned against 1738
by 1885 painted, asymmetric because the top has no invisible border to spare. The run
prints how much it trimmed, because nobody can see the difference by looking at one
file.

### §WW37 Per-monitor awareness, in every process that reads a rectangle

Without it the rectangle and the copy sit in different coordinate spaces, and the
capture is offset or scaled on any display at 125 to 200 percent. It matters as much for
synthesised input: a bounding rectangle is reported in physical pixels and the cursor
call takes them, so every click lands somewhere else on a scaled display. Both harnesses
set it as the first thing they do, with a fallback to the older call before giving up,
and it belongs in the engine so no author has to remember it.

### §WW38 A region, not a handful of points

Nine sampled points cannot cover a window: the capture taken to verify one task passed
all of them while carrying two windows of another process across its lower-right corner.
More points only move the threshold - the number that finally covers a window is the
number of pixels in it - so the question is asked about the region instead. The z order
above the window is enumerated and each frame intersected with the copy rectangle, which
answers for the whole area in one pass and names the intruder rather than merely
refusing.

### §WW39 Cloaked is visible and painted by nothing

A window can be visible by its style bits and drawn by nobody: the compositor cloaks
suspended packaged apps, the shell's hidden host windows and every window belonging to
another virtual desktop. Without this filter a run on a stock Windows 11 desktop reports
a screenful of intruders that are not on screen and refuses every capture it is asked
for, which is a check that has stopped being usable rather than one that found
something.

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

### §WW44 Only the app knows what it drew

Verifying one task in claude-tray cost three captures and a full-screen grab, and none
of the three failed: the script reported success, named the right window each time, and
the file simply did not contain the note the flag exists to show. Nothing was checking
that the capture contained the surface it was taken for, and nothing could - because
only the app knows what it drew and where. So the app prints the rectangle, in physical
pixels, and the copy asserts it lies inside the area it is about to read.

### §WW45 Frames against a clock

An entrance, a fill or a confetti burst has no observable and ships unlooked-at. Frames
are copied at a fixed rate into a numbered sequence, with each frame's target time held
against a stopwatch rather than accumulated out of sleeps, so the sequence records when
as well as what. What comes out is what an encoder takes, and the frame count is a
number an assertion can be written against instead of a picture somebody has to open.

### §WW46 Byte-identical is the cheapest visual assertion

freewilly's window skill states the rule: a change meant to be invisible must produce a
byte-identical file, and the render is deterministic, verified by re-capturing unchanged
code. Three findings about theme handling in that project came from this and from
nothing else, and its test suite saw none of them. It also avoids choosing a tolerance,
which is the argument every other image comparison eventually turns into.

### §WW47 The success line names what it captured

The assertions decide whether a file is written; the line decides whether a wrong one
reports itself. Naming the window title, the process id and the arguments behind it is
worth as much as the checks, because the failure that started all of this was caught
only by a person reading the picture. A capture that says what it is is a capture
somebody can disbelieve, which is the property a silent success does not have.

## Block F — Assert — the expectation is derived, never typed

### §WW48 An expectation reports what it read

The first version of a timed-out read in claude-tray reported no panes and no status
line after 25 seconds, while the status line had been up for the whole 25 seconds saying
it was computing - and the real fault was elsewhere entirely, a missing template part.
The message pointed at timing and cost a throwaway script to get past. Every expectation
now carries what it read, how long it waited and how many of its polls saw it, because
the reason is what decides between a re-run and a hunt.

### §WW49 Derived, never listed

A hardcoded expected set silently stops covering the thing it was written for.
claude-tray's panes case named three tab headers by hand and the window had carried four
for some time, so it reported all three tab headers read against a four-tab window, and
the newest pane had never been asked whether it was in the tree at all. The set is
derived from the project's own strings - and from the strings rather than from the tree,
because the tree is what is being asserted and an expectation read out of it could never
notice a header that had gone missing.

### §WW50 Labels come from the project's own strings

An assertion matching English words against a window rendering another language is loud
when it fails and silent one step over, where it matches nothing and passes. Labels
resolve through the project's language files with the same fallback the app itself uses.
A value carrying a placeholder is refused rather than skipped: an exact-name read of a
tree holding it already filled in can never match it, and skipping it in silence is the
failure this whole rule exists to prevent.

### §WW51 Geometry, checked

freewilly's installer page was built four times and verified every time by reading the
script, and the failures that misses are the ones it had already produced: a caption
assigned before its width wrapped at column zero, a page that rendered correctly above a
screenful of blank space, and a button standing nine pixels below the box it belongs to,
because an edit sizes itself to its font and a button does not. Each was found by
running an installer, which is the most expensive place to find anything. Nothing
overlaps, nothing starts off the surface, nothing ends past it, nothing measures zero.

### §WW52 A name is its label, not merely non-empty

A placeholder, an automation id echoed back or a glyph codepoint would all satisfy
non-empty, and none of them is what a screen reader should say. claude-tray found two
controls carrying empty names while every neighbouring button read fine, because a
control derives its name from its own content and both of those had none - one's label
was a separate text block, the other's content was a font glyph. A name the console
cannot draw is printed as escapes, or the worst case in the whole check reads as the
empty case it is not.

### §WW53 The run leaves the machine as it found it

A harness that drives a real picker, a real setting or a real environment variable can
change the machine of whoever ran it, and the change outlives the run. The store is
fingerprinted before and compared after, which is the strongest form that promise can
take - and the comparison is wrapped around the case most likely to break it, rather
than around the ones that only read.

### §WW54 Working is not blank

Two timed-out reads look identical to whoever is reading the output and mean opposite
things. Working means the window was talking the whole time: a slow machine, a cold
cache, or a report that built and could not be read - which is what a missing template
part looks like. Blank means nothing was ever in the tree and the window is not being
read at all. Collapsing the two cost a defect hunt that started at timing and ended
somewhere else entirely.

### §WW55 The diagnosis ships with the failure

Diagnosing a missing template part in claude-tray took a throwaway script that dumped
the control view - id, type and name per element - and the defect was obvious the moment
somebody looked at the output. That is work the check was already supposed to have done.
Attaching the dump to the failure is the difference between a red run that ends an
investigation and one that starts it.

### §WW56 A check that cannot fail

Several tasks across these repositories record assertions being watched go red before
being trusted, and one of them found a defect in the check itself rather than in the
code under test. A case may declare the injection that must turn it red - a value
removed, a name changed, a panel hidden - so the engine can run it and report a check
that no longer fails. Opt-in, because not every assertion has a cheap injection, and
naming the ones that do is itself a reading.

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

### §WW64 Query instead of read

roadkeep's fifth law. An answer an agent cannot audit gets verified by reading the file,
which is the cost the command existed to remove - reading a backlog end to end to find
one ready task cost about five thousand tokens in that repository. Every verb answers
with provenance: which file, which line, which element, which pattern. The same
arithmetic applies here, where the alternative to a verb is reading a 2,732-line script.

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

### §WW68 Doctor answers for the desk

A machine with no interactive session, no foreground to take, a display that renders
nothing, or missing automation assemblies cannot observe anything at all - and every one
of those currently arrives dressed as a failing assertion about the code. Asked once and
up front, the answer is a report about the machine. On a hosted runner it either changes
nothing or names a condition that was being suffered in silence, and which of the two is
a reading nobody has taken.

### §WW69 The skill loads when a window is in play

The whole content of an instruction file is loaded on every turn against a budget, which
is why claude-tray keeps its flag catalogue in a skill and only the rules in the file
the harness reads. The skill says which loop answers which question - a picture proves
layout, an interaction proves input, a render proves determinism - and what it costs a
session is measured rather than assumed to be small.

### §WW70 Three copies can disagree

The version the plugin carries, the one continuous integration gates on and the one
being called are allowed to differ, and a stale copy does not fail - it agrees with a
rule that has moved. roadkeep reads all three and answers agreed, behind or unpinnable,
exiting non-zero on the two that are not agreement. The same hazard arrives here the
moment a project vendors the engine instead of taking the plugin.

## Block I — The in-app half — the app cooperates with the harness

### §WW71 One render, in one package

The measure, the arrange, the update, the render target, the composed background and the
encoder are the same six steps in every project that wants a picture. pportal's version
already carries the note that a tree which was never arranged renders as nothing at all
- an empty picture that looks like a drawing bug and is a calling bug - so the arrange
belongs inside rather than being expected of the caller. A size of nothing is refused
rather than written as an empty file.

### §WW72 The background is not a decoration

The classic system palette was the obvious source and is measurably wrong: it answers
white on a machine whose application window is dark, so the first capture taken that way
came back as pale text on nothing, correct in every respect and unreadable. The theme's
own key is asked first and the observed window colour is the fallback, and which of the
two answered is printed on every run, because the difference between them is a picture
nobody can read.

### §WW73 A shared brush is frozen or it belongs to one thread

A brush is a freezable and an unfrozen one belongs to the thread that made it, so a
static one belongs to whichever thread reached the class first and every capture thread
after that is refused. Captures run on their own single-threaded apartment by nature, so
the second one throws. Found by the first run of pportal's capture tests rather than by
reading, and asserted there ever since.

### §WW74 The app says what it drew

The rectangle is reported in physical pixels, because layout happens in
device-independent units and the copy works in pixels - so a rectangle handed over in
the wrong one is right at one hundred percent and wrong at every scaling a developer
here actually runs. Deliberately dull and machine-first: a name, then four numbers. A
preview that cannot report is caught by the never-reported arm rather than by an
exception nobody sees.

### §WW75 The host holds popups open

A popup that closes when it loses mouse capture is right for a person and fatal for a
capture: the window is raised to the foreground, the popup goes, and the copy is a
correct picture of a window without it. Fixing that at one call site left the next popup
preview to rediscover it. A preview has no hand to click with, so the rule belongs to
the host - and it walks the logical tree, because a closed popup's child is not in the
visual tree at all, which is exactly the state it has to be reached in.

### §WW76 One runner, not twenty-seven

pportal carries the same eight-line single-threaded runner in twenty-seven test files,
each with its own timeout and its own message for a thread that does not finish.
Controls cannot be constructed off that apartment, and a suite that hangs on a UI
primitive reports nothing at all, so the runner is load-bearing - which is exactly why
it should exist once, bounded, and surfacing whatever the thread threw.

### §WW77 A surface with no tree

An installer page, a custom-drawn control or an immediate-mode surface has no
accessibility tree to read, and the only check available today is reading the source -
which misses the caption that wrapped, the page that rendered above blank space and the
button nine pixels out of place. A geometry dump the harness reads is what makes those
surfaces assertable at all, and freewilly already built one, for exactly one page.

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

