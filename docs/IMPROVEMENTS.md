# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

### §WW108 The verdict and the trace do not reference each other

Block A shipped both halves and joined neither. A summary line says `failed the report
renders - the file was never written`; the trace says step 7 was an assert on `#status`
that waited 240 ms and polled three times. Reading one against the other is a person
matching prose to prose, which is the re-run this block exists to make unnecessary - the
criterion "A failure is diagnosed from the record and not from a re-run" is met by what
the trace contains and not by how a reader reaches it.

What is missing is one field in each direction: a result carrying the ordinal of the
step that settled it, and a step carrying the assertion's name where the step was an
assert. The ordinal is already assigned by the writer and returned from the write, so a
runner holds it at the moment the result is built and nothing has to be looked up
afterwards.

It is filed under this block rather than under the runner's, because it is a property of
what a verdict is. A runner left to invent the join would invent a different one, and
the next runner another.

### §WW109 A trace that does not parse says which line

The trace reader hands the JSON parser's exception straight out. What a reader gets is a
complaint about an invalid start of a value and a byte offset into a string that is no
longer on screen: no path, no line number, and nothing saying the file was a trace at
all. The test written for it can only assert that something was thrown, which is the
tell - nothing about the refusal was worth naming.

A trace is read after a run that already went wrong, and often after one that was
truncated, so this is the second bad moment in a row for whoever is reading it. The
refusal should carry the file, the ordinal of the line and that line's own text cut to
something a terminal can show, which is the shape the scenario refusal already has for a
file that will not load.

The blank line is deliberately skipped and stays skipped: a trace ended by a crash
finishes on one, and that is the reader working rather than failing. What this is about
is the line that has content and is not a step.

## Block B — Attach, launch, and leave nothing behind

### §WW110 The run has no preamble

Block B shipped five measurements and joined none of them. Staleness, the running
binary, the foreground, the launch arguments and the resolved language each answer with
a precondition and a sentence, and each is reached by its own call on its own type.
Nothing lists them.

Two things follow, and the second is the one that matters. The block's criterion "a run
says which binary it drove" is currently met three times over by three sentences, which
is to say it is not met once: a reader gets whichever of them the caller remembered to
print. And a runner assembling the precondition set by hand will one day be edited by
somebody who does not know all five are there - at which point the forgotten one stops
being measured and every assertion that needed it silently starts passing. That is WW6's
defect with a different subject, and this block is where it can still be closed cheaply.

What is wanted is one reading, taken once at the start of a run: the target, the binary
with both its keys, the language and where it was read, the staleness comparison, the
foreground at the moment input was first synthesised, and what other instances were
open. It renders as the preamble a summary opens with, and it hands over a set the
assertions are then resolved against, so that adding a sixth measurement is one file and
not an audit of every runner.

### §WW111 The suite leaves the foreground somewhere else

Found while writing WW13. Creating a top-level window with WS_VISIBLE activates it, so
every fixture in this suite that needs a visible window takes the foreground for as long
as it lives. On a developer machine that is a flash over whatever was being typed into,
several times a run.

It is filed under this block rather than dismissed as test hygiene for two reasons. The
theme here is leaving nothing behind, and a foreground handed to a window that has since
been destroyed is something left behind. And the tool measures the foreground: a suite
that moves it is a suite whose own readings of it are taken on a desk the suite
disturbed, which is the shape of a test that agrees with itself.

The fix is small. A fixture window can be created at coordinates outside every monitor's
bounds, which keeps it visible to the enumeration under test and invisible to the person
at the keyboard. Where a test genuinely needs a window on screen, it can say so and
place it deliberately. What should not survive is the current arrangement, where forty
by forty is the default because it was the first pair of numbers typed.

## Block C — Locate — the locator grammar and the tree an agent reads

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

## Block K — The proving ground — a fixture app built to be hard to test

### §WW89 A window that belongs to this repository

Every loop in this framework is currently developed against somebody's shipping product,
which means a real account, a real transcript directory, a real controller and a machine
somebody set up by hand. The fixture removes all of it. It is not a demo and not a
sample: it is the surface this framework's own tests drive, and its design goal is to be
hard to test in the specific ways Windows is hard.

### §WW90 Every refusal has a flag that provokes it

This framework's value is concentrated in its refusals, and a refusal nobody can provoke
is a refusal that will quietly stop working. Each one gets a fixture flag: cover this
region, opt into a backdrop, render nothing, stay loading this long, draw a control with
no name. The framework's own suite then asserts the red, which is the only thing that
keeps a refusal real rather than remembered.

### §WW91 The same window under two pumps

The difference between hosting a window under one message pump and another is invisible
in every picture and decides whether keyboard input arrives at all. claude-tray
discovered it by shipping windows that took no keystrokes while every screenshot of them
looked perfect. The fixture ships both hosts behind two flags, which is what makes the
check for it developable without inheriting a real product's hosting decisions.

### §WW92 One page holding the whole naming rule

A control with no name, one announcing a glyph codepoint, one whose label is a
neighbouring element, and beside them a button that must keep its own text - both
branches of the rule on one surface. That is the case set the naming check needs, and
assembling it out of a real product means waiting for that product to happen to have all
four at once.

### §WW93 Three kinds of absence

A collapsed pane, a closed popup and an unopened submenu are all missing from the tree
and all mean different things, and each one cost a real defect somewhere to learn.
Having the three behind flags is what lets the classification of a miss be developed and
asserted rather than reasoned about from memory.

### §WW94 Both arms of the backdrop refusal

A refusal with only one arm tested is half a check: it can be right about the window it
refuses and wrong about everything it lets through. The fixture ships one window that
opted into a system backdrop and one that never did, so the refusal and the pass beside
it are both driven - which is what proves the check reads the compositor rather than the
window's name.

### §WW95 A borderless window with no handle

A toast, a balloon or a menu is a top-level window the process object never reports, and
that shape exists today in exactly one product here. The fixture raises one on request,
which makes the enumerating launcher and the frame sequence both developable without
waiting for somebody's notification to fire on its own schedule.

### §WW96 A page that is loading for as long as the check needs

The loading refusal was discovered on a machine that happened to be slow, and
reproducing it means finding another one. The fixture takes the duration as a flag, so
the refusal is asserted at a moment the run chose - and the other arm is covered too, a
page that finishes inside the wait and must not be refused for it.

### §WW97 An animation with a known length

A frame sequence is currently checked by opening the frames, which is the thing this
framework exists to avoid. The fixture plays an animation of a declared duration with a
declared number of visible states, so the sequence is checked against numbers: how many
frames, at what interval, and that the states arrive in the order they were declared in.

### §WW98 Something to be identical to

The byte-identical assertion needs a surface fixed by construction: no clock, no machine
name, no real data, and no theme that follows the desktop unless the case asked for it.
Producing one is a design constraint rather than an accident, and it doubles as the
reference for what an adopting project has to do to make its own surfaces comparable at
all.

### §WW99 A second instance on request

The other-instance refusal and its override are both tested today by remembering to
leave a window open. The fixture opens a second window on request, so both arms are
driven - and the distinction that matters is covered too: a resident process showing no
window must not trip the refusal, because that is the ordinary state of every developer
machine here.

### §WW100 A store the run is allowed to break

The fingerprint check protects the store of whoever is running it, which makes it the
one assertion that cannot be developed against a real product without putting somebody's
settings at risk. The fixture writes a store of its own and offers to mutate it on
request, so both the clean run and the caught mutation are observable without anything
real being touched.

### §WW101 The reference implementation of the surface protocol

The protocol exists in one product and would be copied into the next, which is exactly
how two implementations of one line format come to disagree. The fixture implements it
and this framework's own suite drives that implementation, so the protocol has an owner
- and an adopting project has something to copy that is known to be current.

### §WW102 A localized window, including the key that must be refused

The label rule needs several languages to be developed at all, and it needs one specific
pathological case: a key whose value carries a placeholder, which an exact-name read can
never match and which has to be refused rather than skipped. Real products have the
languages and rarely have the pathological key on purpose.

### §WW103 An intruder over a named region

The region check is the most intricate piece of the capture stack, and today it is
exercised by moving a window by hand and hoping. The fixture puts a topmost window over
a rectangle the caller names, so the intersection, the naming of the intruder and the
raise-then-refuse loop are all driven - including the case that must pass, an intruder
that overlaps nothing.

### §WW104 A surface drawn without automation peers

The geometry dump exists because some surfaces have no tree, and the only example
available today is an installer page in another repository, behind a compiler that has
to be installed first. The fixture draws one, so the dump and the layout invariants over
it are developable here rather than borrowed.

### §WW105 The fixture says what it can do

A catalogue that lives only in source is a catalogue nobody consults, and a flag nobody
knows about is a shape nobody tests against. The application lists every flag it has and
the list is asserted against the flags that exist - the same rule claude-tray applies to
its own preview catalogue, where an unknown name prints the whole table and exits
non-zero.

### §WW106 A shape exists because a defect existed

A fixture that grows shapes nobody can justify becomes a second product to maintain, and
then it drifts from the things it stands in for and starts producing false confidence.
Each surface names the real defect it reproduces. One that can name none is removed, and
the removal is itself a reading about what this framework no longer has to defend
against.

### §WW107 The fixture is also for a person

When a case fails, the fastest way to understand it is to look at the thing it is
talking about, and that must not require writing anything first. Every flag opens the
surface it names in a window somebody can see, which is the property claude-tray's
preview flags have and the reason its harness is debuggable at all.
