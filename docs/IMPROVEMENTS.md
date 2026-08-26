# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW158 A display that renders is not a display that is attached

Display() reads three things and none of them is rendering: GetSystemMetrics for the
monitor count, for the virtual screen width and height, and SM_REMOTESESSION as a suffix
on a failure it has already decided. All three are proxies. The condition is named a
display that renders and answers a different question - is a display attached, and does
it measure something.

WW42 is the measurement that settles it. A copy of the notification area came back as
exactly one distinct colour, with the session present, the shell running and the
environment reporting an interactive desktop. This reading would have called that desk
met. It was caught by the capture, per capture and after the fact, and only because
somebody looked.

Read from the desk instead, the same fact answers once and answers first, which is what
the whole reading is for: the flat rectangle is refused before 999 cases run on the desk
that produces it, rather than once per picture afterwards.

The evidence is composition state and what the desk actually draws, never a named pixel
- the non-goal about individual pixels binds this as it binds Block E, and a colour
count is not a claim about a coordinate. WW42 stays where it is: the capture keeps its
own refusal, because a desk that renders can still be photographed while nothing is on
it.

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

### §WW225 Half the engine has no name in the format

The vocabulary is `read`, `invoke`, `toggle`, `set value`, `set range`, `select`,
`expand`, `collapse`. Every one reaches its element through the control's own pattern,
which is the right default and is the whole of what a case can say. The engine also has
`Pointer`, `Keyboard`, `Traversal` and `Focus` — synthesised input, and the readings
about why an act needs it — and none of them has a name a data file can write.

Not in the abstract. WW78's keyboard case asserts three things: typing reaches a WPF
TextBox, Tab moves focus off it, an arrow key moves a Slider. The first and third become
`set value` and `set range`, which go through the patterns that passed on the day those
windows took no keyboard input at all — checks that cannot fail for the reason they were
written. The second cannot be written: no reading answers what holds the focus.

Two more things need names. A pointer act, because the sidebar item the case clicks has
no automation peer and `invoke` needs an Invoke pattern. And a step's own foreground
precondition, since synthesised input goes wherever the foreground is and the script
asks before it drives.

WW82's sentence names a keyboard expansion, so it waits on this too. Past those two the
script drives the same way throughout — a pointer click to navigate, a keyboard walk
through a picker, Escape to close a menu — so this is not WW78's alone. It is what block
J waits on.

## Block H — The Claude Code surface — plugin, tools, skill, hook

### §WW221 The wiring is not the tool

WW65 shipped a claim: two commands, nothing added to any path, and committing one file
wires every clone. WW66 and WW67 then made both surfaces .NET processes, and the wiring
points at `bin/Release/net10.0-windows/*.dll` — paths a clone does not have until
somebody runs `dotnet build -c Release`. So the claim is now true about the wiring and
false about the tools. An adopter who runs the two commands gets a session whose server
fails to start and whose hook exits silently, which is the worse of the two: a guard
that is not there refuses nothing, and nothing goes red. The README names the build step
rather than hiding it, and naming a gap is not closing one. What closes it is the plugin
carrying something that runs without a prior build, or the install failing loudly when
the build is missing instead of degrading to a session with no tools and no guard. The
second is cheaper and is probably the right first move: a hook that cannot find its
assembly is the one case where silence is wrong, because the whole point of it is to be
in the way.

### §WW222 The tools stop one step short of the answer

`winwright_check` answers whether a file would load. That is the saving WW66 was about —
the analysis, before the prose exists — and it is not the question a session actually
has, which is *did it pass*. Today the answer to that comes from a shell: build, then
`dotnet test`, then read a trx. So the tool chain gets an agent to a correct case file
and then hands the run back to the same script-shaped path WW67 exists to deny, which is
the shape of a guard that closes one door and leaves the next one open.

What is missing is a verb that takes a selection, launches what the fixtures declare,
runs it and hands back the verdict — the sentence, the exit code, and the holes named
rather than counted. The reason it is not WW66's scope is that running one needs a desk,
and a desk is the one thing an MCP server cannot assume: the six conditions WW68 reads
have to be read before anything launches, and a tool that cannot observe has to answer
*hole* rather than a red or a green. That makes this a verdict-shaped task and not a
schema-shaped one, which is why it is filed apart rather than folded in.

### §WW224 Either the commands or the sentence

"The plugin is the whole installation" says in full that two commands wire the hook, the
tools, the commands and the skill. Three of the four landed. The commands did not, and
nothing in the block was ever filed for them — so the criterion closed over a surface no
task claimed, which is the failure mode WW176 exists to stop, arriving through the
criterion's own wording rather than through a missing pairing. `Criteria.cs` now says so
out loud rather than letting the entry read as satisfied.

Two ways out and they are not equivalent. Build them: a slash command per tool, which is
cheap and mostly redundant, because a verb reachable from a tool does not also need a
name typed with a slash. Or restate the criterion to name the three surfaces there are,
and record why commands were dropped. The second is more likely right, and it is the one
that needs a decision rather than an afternoon — which is why this is filed as a task
and not done in passing. What must not happen is the entry quietly keeping a word for a
thing nobody intends to build: that is how a list stops being read.

## Block I — The in-app half — the app cooperates with the harness

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

### §WW223 The repetition caught what a single green hides

`TrayPlacementTests.Adding_one_and_finding_it_holds_every_time_rather_than_most_times`
repeats five rounds precisely because a single green is what the old fixture produced
about half the time. On the guest run of WW67 it failed on round 1 — "it is on neither
the taskbar nor the overflow" — and passed on an immediate re-run of the same tree. So
the claim in the case's own name is false, and the evidence is the case doing exactly
what it was written to do.

Two things make round 1 the suspicious one. Round 0 ends with
`NotificationArea.CloseOverflow()`, and the next round's `TrayIconFixture.Add` runs
while the shell may still be tearing the flyout down; and the search was not excused, so
the desk was observable and the icon genuinely was in neither place. That points at
`Add` returning before the shell has placed the icon when the notification area is
mid-transition — the fixture's promise being read a moment too early rather than the
search looking in the wrong place.

Worth naming: the repair is not a retry around the assertion. A retry there would
restore the green and delete the measurement, which is the same trade the twenty-two
missing tests came out of.
