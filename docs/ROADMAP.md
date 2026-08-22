# Roadmap (active backlog)

## Block A — The verdict (a run is data, and "not observed" is an answer)

- 📋 **WW108** (deps: —) **the summary names an assertion and the trace numbers a step, and nothing joins the two** — Each result carries the ordinal of the step that settled it, so a failure named in the summary is one grep away from the line recording what was read back. → §WW108
- 📋 **WW109** (deps: —) **a trace line that does not parse refuses without naming the file or the line it is on** — The reader raises the JSON parser's own error, so a truncated or hand-edited trace is diagnosed by opening the file and counting lines to a byte offset. → §WW109
- 📋 **WW117** (deps: —) **a test host that dies mid-run still prints a pass** — The run compares what was discovered against what executed, so a suite that quietly loses 22 tests is a red instead of a green nobody counted. → §WW117

## Block B — Attach, launch, and leave nothing behind

- 📋 **WW110** (deps: —) **nothing enumerates what a run measured, so a runner that forgets a precondition stops checking it** — One reading collects the conditions this tool measures and the sentences they produce, so what a run checked is a list it can print rather than five calls to remember. → §WW110
- 📋 **WW111** (deps: —) **the suite's own window fixtures take the foreground from whoever is working at the machine** — Fixture windows are created off-screen, since a suite that flashes over the desk also perturbs the one reading a task in this block exists to measure. → §WW111
- 📋 **WW114** (deps: —) **the suite's input tests compete for a foreground Windows will not always grant them** — The window fixtures run on a desktop of their own, where they are the only windows and the foreground is theirs by definition rather than by contest. → §WW114
- 📋 **WW119** (deps: —) **the tray fixture returns before the shell has put the icon anywhere a test can find it** — The fixture waits for the icon to be findable rather than for the shell to take the message, so a test never races the placement every one of its assertions depends on. → §WW119
- 📋 **WW125** (deps: —) **a test class that touches the desktop can be written outside the serial collection and nothing says so** — Membership is asserted rather than remembered, so a class creating a window or launching a process runs beside the foreground checks only when somebody meant it to. → §WW125

## Block C — Locate — the locator grammar and the tree an agent reads

- 📋 **WW112** (deps: —) **nothing makes an act go through the actionability check, so a verb that forgets it still runs** — An act reaches its element only through a gate that has already judged it, the way a launch reaches a process only through the register. → §WW112
- 📋 **WW113** (deps: —) **the first line inspect prints is a locator step that matches nothing when it is used as one** — A locator written from any line inspect printed resolves, including the first one, which today addresses the root and matches nothing. → §WW113
- 📋 **WW124** (deps: —) **an automation id with a space in it renders as a locator step the grammar then refuses** — An id is quoted the way a name already is, so every line inspect prints parses - which is the property the whole verb exists for and the one nothing was checking. → §WW124

## Block D — Act — patterns before pointers

- 📋 **WW115** (deps: —) **a pointer act says what it clicks and never why the pattern route was unavailable** — Each declared pointer act carries the reason it needs a desktop, so what a scenario costs reads as a list of reasons rather than as a list of locators. → §WW115
- 📋 **WW116** (deps: —) **nothing declares which entries are destructive, and invoke will press any of them** — Destructive entries are named per project and refused by invoke unless the act says it means that one, so no check can end the run by pressing something. → §WW116

## Block E — Capture — the picture that proves what it photographed

- 📋 **WW38** (deps: Block K) **sampled points cannot answer whether a region is covered** — The z order above the window is enumerated and each frame intersected with the copy rectangle, which answers for the whole area in one pass and names the intruder. → §WW38
- 📋 **WW40** (deps: Block K) **a copy trimmed around an intruder is a picture of something nobody asked for** — An overlap fails rather than crops, and the refusal names the intruder, its process and the rectangle it covers, so the cause is actionable instead of mysterious. → §WW40
- 📋 **WW41** (deps: Block K) **a window with a system backdrop transmits what is behind it through the glass** — Z-order reasoning cannot answer for that, so the compositor is asked directly and a window that opted into one is refused rather than merely warned about. → §WW41
- 📋 **WW42** (deps: Block K) **a copy of exactly one colour is written and reported as a capture** — A flat rectangle is not a picture of a window, and a session where nothing was rendering produced one that exited zero. → §WW42
- 📋 **WW43** (deps: Block K) **a page still computing is photographed and announced as a picture of a report** — The loading strings are read from the project's own language files before anything launches, and a key none of them carries refuses the run instead of matching nothing. → §WW43
- 📋 **WW46** (deps: Block K) **a change meant to be invisible has no cheap way to prove it was** — Two renders of unchanged code are byte-identical, so a difference is a real difference and no tolerance has to be chosen for a comparison to mean anything. → §WW46

## Block F — Assert — the expectation is derived, never typed

- 📋 **WW118** (deps: —) **a derived set can carry a placeholder value that no exact read will ever match** — A set refuses a placeholder the way a label already does, so a key that cannot pass is a red at derivation rather than a member nothing in the tree ever equals. → §WW118

## Block G — The scenario — a case is a data file

- 📋 **WW57** (deps: Block A, Block C, Block D) **a case is two hundred lines of script that mostly repeats the previous case** — A case is a data file: steps, locators, acts and expectations as fields, with the loop, the waits and the verdicts owned by the engine instead of by the author. → §WW57
- 📋 **WW58** (deps: Block A, Block C, Block D) **the format is a convention the author is asked to remember** — Every field is validated at insertion, so a refusal costs a retry and never a deletion, which is roadkeep law one applied to a scenario instead of to a line. → §WW58
- 📋 **WW59** (deps: Block A, Block C, Block D) **there is no way to run one case, or one file, without running the rest** — Run takes a file, a case or a tag and says what it did not run, so a single case is ten seconds when a single act is what changed. → §WW59
- 📋 **WW60** (deps: Block A, Block C, Block D) **a case that needs a fixture reaches for this machine instead** — Fixtures and sampled environments are declared per case and passed to every launch it makes, or the expectations describe one environment and the window renders another. → §WW60
- 📋 **WW61** (deps: Block A, Block C, Block D) **a case with an absent precondition goes red for a reason about the desk it ran on** — The precondition is declared, so its absence is unchecked and named instead of a failure nobody reading it can act on. → §WW61
- 📋 **WW62** (deps: Block A, Block C, Block D) **three cases driving the same window each pay their own launch** — A window is declared shareable and lent to the cases that only read it, while a case run alone still owns its process and its first paint. → §WW62
- 📋 **WW63** (deps: Block A, Block C, Block D) **a scenario says what to do and never why it is worth doing** — Each case carries the defect it exists to catch, so a case nobody can justify is visible and a case removed by accident is missed. → §WW63

## Block H — The Claude Code surface — plugin, tools, skill, hook

- 📋 **WW65** (deps: Block G) **adopting the tool is a per-machine install somebody has to remember** — It ships as a Claude Code plugin, so two commands in the repository wire it and every clone is wired, with nothing added to any path. → §WW65
- 📋 **WW66** (deps: Block G) **the schema of a case arrives as flag names typed from memory** — The tools carry this project's scenario schema as their input schema, which is the difference between a refusal and a guess. → §WW66
- 📋 **WW67** (deps: Block G) **a hand-written harness script is the path of least resistance** — A hook denies one and names the verb that replaces it, which is the same guard roadkeep puts in front of a governed file. → §WW67
- 📋 **WW69** (deps: Block G) **the skill is loaded on every turn against a budget it does not need** — It loads when a window is in play and says which loop answers which question, which is the whole of what an agent needs to reach the right verb. → §WW69

## Block I — The in-app half — the app cooperates with the harness

- 📋 **WW122** (deps: —) **the in-app half is a project reference, so an adopter cannot pin it or take it at all** — It builds a package with an id and a version, so adopting it is one reference the agreement check can pin rather than a path into somebody else's source tree. → §WW122
- 📋 **WW123** (deps: —) **nothing refuses a reference between the two halves, and one edit would merge them** — The suite asserts the engine references no in-app package and the in-app package references no engine, since that separation is what makes each half safe to ship where it goes. → §WW123

## Block J — Adoption — the proof is the deletion

- 📋 **WW78** (deps: Block G) **the keyboard case is the only observable of a live input path and it lives in a script** — Migrated first: navigate, type, read back, traverse and drive a slider, which is the shortest path through locate, act and assert. → §WW78
- 📋 **WW79** (deps: Block G) **the panes case proves a report is readable and needs no second profile** — Migrated as the case that runs on every machine, which is what makes it the one that would notice a template part going missing again. → §WW79
- 📋 **WW80** (deps: Block G) **the sessions case drives the one surface a capture provably cannot finish checking** — Migrated with its waits, its expansion and the popup it opens, since that popup is the argument for the whole interaction loop existing. → §WW80
- 📋 **WW81** (deps: Block G) **the profiles case is the only thing in that repository that drives the picker** — Migrated with the round trip and the timing observation, both of which need the hop count the walk reports to mean anything. → §WW81
- 📋 **WW82** (deps: Block G) **the menu case reads the notification area and nothing else does** — Migrated with the overflow flyout, the keyboard expansion and the expectations derived from the app's own read-out. → §WW82
- 📋 **WW83** (deps: Block G) **the switch case drives the one path that rewrites a real setting** — Migrated inside the store comparison, so the promise that a run touches nothing is asserted where it is most likely to be broken. → §WW83
- 📋 **WW84** (deps: Block G) **the names case observes what no screenshot can** — Migrated with the derived panel sweep and the exact-label reads, including the rule that must fire and the one that must not. → §WW84
- 📋 **WW85** (deps: Block G) **the environment sweep walks a submenu per sampled mode** — Migrated last, because it is the case that proves a fixture reaches every launch a case makes rather than only the first. → §WW85
- 📋 **WW86** (deps: WW78, WW79, WW80, WW81, WW82, WW83, WW84, WW85, Block E) **claude-tray still carries two harness scripts nobody should extend** — Both are deleted once every assertion in them is a case, and the run reports the line count removed so the saving is a measurement. → §WW86
- 📋 **WW87** (deps: Block E, WW77 ✅) **freewilly carries its own copy of the capture script and a layout probe** — Both migrate: the capture as a scenario, the probe as the geometry dump this framework already owns. → §WW87
- 📋 **WW88** (deps: Block G, WW76 ✅) **pportal carries an interaction harness and twenty-seven copies of one runner** — The harness becomes scenarios and the runner becomes a package reference, which is the largest single deletion the adoption produces. → §WW88

## Block K — The proving ground — a fixture app built to be hard to test

- 📋 **WW92** (deps: —) **a control that announces nothing has to be found inside somebody else's product** — The fixture carries one deliberately unnamed control, one announcing a glyph codepoint and one whose label is a neighbouring element, which is the naming rule in one page. → §WW92
- 📋 **WW93** (deps: —) **a collapsed pane, a popup and a submenu are three different kinds of absence** — All three are on the fixture, since the tree reports each one differently and each cost a real defect somewhere to learn. → §WW93
- 📋 **WW94** (deps: —) **the backdrop refusal has only one arm to be tested against** — The fixture ships one window that opted into a system backdrop and one that never did, so both the refusal and the pass beside it are observable. → §WW94
- 📋 **WW95** (deps: —) **a borderless window with no main window handle exists in only one real product** — The fixture raises a toast that has none, which is what the enumerating launcher is for and what nothing else available here can produce on demand. → §WW95
- 📋 **WW96** (deps: —) **a page that is still loading cannot be produced when the check needs one** — The fixture takes the duration as a flag, so the loading refusal is asserted at a moment the run chose rather than on a machine that happened to be slow. → §WW96
- 📋 **WW97** (deps: —) **an animation with a known length and a known frame count does not exist to capture** — The fixture plays one, so a frame sequence is checked against a number instead of against a picture somebody had to look at. → §WW97
- 📋 **WW98** (deps: —) **a byte-identical render has nothing to be identical to** — The fixture draws a surface fixed by construction - no clock, no machine name, no real data - which is what makes the comparison mean anything at all. → §WW98
- 📋 **WW99** (deps: —) **a second instance has to be started by hand to test the refusal that exists for it** — The fixture opens a second window on request, so the other-instance refusal and the override beside it are both actually driven. → §WW99
- 📋 **WW100** (deps: —) **the store a run must not touch belongs to whoever is running it** — The fixture writes a store of its own and offers to mutate it, so the fingerprint check is asserted without putting anybody's real settings at risk. → §WW100
- 📋 **WW101** (deps: —) **the surface protocol is implemented in one product and copied into the next** — The fixture is the reference implementation, and this framework's own suite drives it rather than reaching into a real application's flags. → §WW101
- 📋 **WW102** (deps: —) **a localized window is needed to develop the label rule and no fixture has one** — The fixture ships several language files, one of them carrying a key whose value has a placeholder, which is the case the label rule has to refuse. → §WW102
- 📋 **WW103** (deps: —) **an intruder covering a rectangle has to be arranged by hand every time** — The fixture puts a topmost window over a named region on request, so the region check is driven rather than reasoned about. → §WW103
- 📋 **WW104** (deps: —) **a surface with no accessibility tree is only found inside an installer** — The fixture carries one drawn without automation peers, which is what the geometry dump exists for and what nothing else here can produce. → §WW104
- 📋 **WW105** (deps: —) **the catalogue of what the fixture can do lives only in its own source** — The app lists every flag it has and the list is asserted against the flags that exist, so a shape added later is never one nobody can find. → §WW105
- 📋 **WW106** (deps: —) **a fixture that drifts from the products it stands in for is worse than none** — Each shape it carries names the real defect it reproduces, so a shape nobody can justify is removed rather than maintained forever. → §WW106
- 📋 **WW107** (deps: —) **a fixture is only ever driven by the framework that ships with it** — It is also driven by hand: every flag opens the surface it names, so a person can look at the thing a failing case is talking about without writing anything. → §WW107

## Done when — Block A

- **A degraded run is legible without reading the log** Run any scenario on a machine
  missing a precondition: the exit code is 2, and the summary lists every assertion that
  did not run, by name.
- **Nothing about this machine is typed into a scenario** Move any scenario to another
  checkout and run it: it behaves the same, or it refuses naming the declaration that is
  missing.
- **A failure is diagnosed from the record and not from a re-run** The trace of a failed
  run carries the locator, what it resolved to, what was read back and the verdict for
  every step before the one that broke.

## Done when — Block B

- **No process outlives the run that started it** After any scenario ends - passing,
  failing, throwing or interrupted - nothing it launched is alive, and the summary names
  whatever had to be stopped.
- **A run says which binary it drove** Every summary carries the executable, its version
  and its write time, so a run against a build older than the change is visible without
  being asked for.
- **Nothing about the desk is reported as a defect in the code** A busy foreground, a
  resident instance or a display that renders nothing each end as a named unchecked
  assertion rather than as a failure.

## Done when — Block C

- **An element is addressed without reading the markup** Every locator in the migrated
  scenarios was written from what inspect printed against a live window, and the task
  that wrote it says so.
- **No scenario carries a sleep** Every wait is a deadline on a condition, and how long
  it actually took is in the trace for whoever wants to tune it.
- **An act never runs against an element that cannot take it** Actionability is checked
  first, and a refusal names which of the four properties - present, on screen, enabled,
  carrying the pattern - was missing.

## Done when — Block D

- **The default act needs no foreground** Every act that can go through a pattern does;
  the ones that cannot are declared as pointer acts and carry the reason for it in the
  file.
- **A retry is bounded and said out loud** No act retries until it passes, the attempt
  count reaches the trace, and an act that only ever works on the third attempt is
  visible in the output.
- **A destructive entry is never invoked by accident** Entries that launch or quit are
  named in the scenario and reached only by traversal, so no run ends because a check
  pressed something.

## Done when — Block E

- **A capture proves what it photographed** Every written image names the window, the
  process and the arguments behind it, and every refusal names what it saw instead of
  writing a file.
- **An off-screen render is the default** The screen copy runs only where a case
  declares a surface a render cannot reach, and the output says which of the two
  produced the file.
- **Every capture refusal has a fixture that provokes it** Each named refusal is
  asserted against the proving ground, so one that quietly stopped working is a red run
  rather than a silence.

## Done when — Block F

- **No expectation is typed twice** Every set a scenario checks against is derived from
  the project's own strings or read-outs, and adding a tab, a panel or a profile needs
  no scenario edit.
- **A red step carries its diagnosis** The control view it failed to read is attached to
  the failure, so no throwaway script is written to find out what the window actually
  had.
- **A run leaves the machine as it found it** The fingerprint taken before the run
  matches the one taken after, on every scenario, including the ones that drive a real
  setting.

## Done when — Block G

- **A case is data** Every migrated case is a scenario file, and the count of harness
  code lines in the adopting project is zero.
- **The format refuses before the prose exists** An invalid case is refused at insertion
  with the offending field named, never reported by a linter after somebody already
  wrote it.
- **One case runs alone** Any single case runs by name in seconds, and the run says what
  it did not run, so a single act that changed costs a single case.

## Done when — Block H

- **The plugin is the whole installation** Two commands in an adopting repository wire
  the hook, the tools, the commands and the skill, and nothing is added to any path.
- **An answer costs no context** Every verb answers as machine-readable output carrying
  the file and line it came from, so nothing is verified by reading what the command
  already read.
- **The skill fits its budget** It loads only when a window is in play, and what it
  costs a session is measured and written down rather than assumed to be small.

## Done when — Block I

- **The in-app half is one package reference** A project adopting it deletes its own
  capture, surface and thread helpers, and the deletion is what the adopting task
  reports.
- **A project that cannot take the package still works** Every verb needing no
  cooperation runs against an application that references nothing, which is what keeps
  this usable on a product nobody here owns.

## Done when — Block J

- **The proof is a deletion** claude-tray, freewilly and pportal each lose their harness
  scripts, and the number of lines removed is reported rather than described.
- **Nothing was lost in the move** Every assertion the replaced scripts made is present
  as a case, and the migration names any that was dropped along with the reason.
- **The migrated suite is not slower than what it replaced** The run time of the
  replaced script and of the scenarios replacing it are both measured and written down
  beside each other.

## Done when — Block K

- **Every refusal has something that provokes it** Each named refusal in this framework
  maps to a fixture flag, and a refusal with no flag is a finding the fixture closes
  rather than a gap nobody sees.
- **The fixture needs nothing from the machine** It runs with no account, no network, no
  second display and no real data, on a clean checkout of this repository alone.
- **A shape exists because a defect existed** Every surface the fixture carries names
  the real defect it reproduces, and one that can name none is removed instead of
  maintained forever.

## Non-goals

- **Not cross-platform** The problem is Windows-shaped: UI Automation, DWM, per-monitor
  DPI, the notification area and Win32 menus. A Linux or macOS target would dilute every
  one of those decisions into an abstraction that serves none of them well.
- **No external dependency in the engine** UIAutomationClient and UIAutomationTypes are
  in-box in the Windows Desktop framework. A package here is a package every adopting
  project inherits, and two of the three target projects exist partly to delete
  dependencies.
- **No assertion about individual pixels** Comparing colours or regions is brittle by
  nature and survives neither a theme, a DPI nor a font. What is claimed about an image
  is that it drew something, that it photographed the right window, and that it is
  byte-identical when nothing changed.
- **The tool never writes the test** It validates, resolves and runs. A generator that
  invented the assertion would reintroduce exactly the drift the declarative format
  exists to stop — roadkeep's law L4, applied to scenarios instead of prose.
- **No recorder that turns clicks into a scenario** Playwright's codegen records
  selectors nobody reviewed. The verb here is inspect: it prints the control view so an
  agent picks the locator, and the choice stays in the file with the reason for it.
- **No service, no daemon, no database** The store is the repository: scenarios in
  versioned files, traces as JSONL beside the run. A test tool that asks for
  infrastructure is a test tool that does not run on the machine of whoever is fixing
  the defect.
- **A green never covers an assertion that did not run** This is the third verdict's
  whole reason to exist. Any proposal that collapses DEGRADED into a pass or into a
  failure is undoing the central finding, however much simplicity it offers in exchange.
