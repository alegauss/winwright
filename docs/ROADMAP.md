# Roadmap (active backlog)

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

- ⏳ **WW158** (deps: a session that can be disconnected and reconnected) **the display condition counts monitors and measures the virtual screen, which the session that rendered nothing passed** — Its own criterion is unmet: no desk that draws nothing has taken the reading. → §WW158

## Block C — Locate — the locator grammar and the tree an agent reads

- 📋 **WW206** (deps: WW207 ✅) **the sweep rule is checked per file, so a file holding two sweeps passes on the strength of the one that reads code** — The rule is checked at the sweep, because a file with a repaired sweep and a raw one beside it was reported as clean. → §WW206

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

- 📋 **WW57** (deps: Block A ✅, Block C, Block D ✅) **a case is two hundred lines of script that mostly repeats the previous case** — A case is a data file: steps, locators, acts and expectations as fields, with the loop, the waits and the verdicts owned by the engine instead of by the author. → §WW57
- 📋 **WW58** (deps: Block A ✅, Block C, Block D ✅) **the format is a convention the author is asked to remember** — Every field is validated at insertion, so a refusal costs a retry and never a deletion, which is roadkeep law one applied to a scenario instead of to a line. → §WW58
- 📋 **WW59** (deps: Block A ✅, Block C, Block D ✅) **there is no way to run one case, or one file, without running the rest** — Run takes a file, a case or a tag and says what it did not run, so a single case is ten seconds when a single act is what changed. → §WW59
- 📋 **WW60** (deps: Block A ✅, Block C, Block D ✅) **a case that needs a fixture reaches for this machine instead** — Fixtures and sampled environments are declared per case and passed to every launch it makes, or the expectations describe one environment and the window renders another. → §WW60
- 📋 **WW61** (deps: Block A ✅, Block C, Block D ✅) **a case with an absent precondition goes red for a reason about the desk it ran on** — The precondition is declared, so its absence is unchecked and named instead of a failure nobody reading it can act on. → §WW61
- 📋 **WW62** (deps: Block A ✅, Block C, Block D ✅) **three cases driving the same window each pay their own launch** — A window is declared shareable and lent to the cases that only read it, while a case run alone still owns its process and its first paint. → §WW62
- 📋 **WW63** (deps: Block A ✅, Block C, Block D ✅) **a scenario says what to do and never why it is worth doing** — Each case carries the defect it exists to catch, so a case nobody can justify is visible and a case removed by accident is missed. → §WW63

## Block H — The Claude Code surface — plugin, tools, skill, hook

- 📋 **WW65** (deps: Block G) **adopting the tool is a per-machine install somebody has to remember** — It ships as a Claude Code plugin, so two commands in the repository wire it and every clone is wired, with nothing added to any path. → §WW65
- 📋 **WW66** (deps: Block G) **the schema of a case arrives as flag names typed from memory** — The tools carry this project's scenario schema as their input schema, which is the difference between a refusal and a guess. → §WW66
- 📋 **WW67** (deps: Block G) **a hand-written harness script is the path of least resistance** — A hook denies one and names the verb that replaces it, which is the same guard roadkeep puts in front of a governed file. → §WW67
- 📋 **WW69** (deps: Block G) **the skill is loaded on every turn against a budget it does not need** — It loads when a window is in play and says which loop answers which question, which is the whole of what an agent needs to reach the right verb. → §WW69

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

- 📋 **WW78** (deps: Block G) **the keyboard case is the only observable of a live input path and it lives in a script** — Migrated first: navigate, type, read back, traverse and drive a slider, which is the shortest path through locate, act and assert. → §WW78
- 📋 **WW79** (deps: Block G) **the panes case proves a report is readable and needs no second profile** — Migrated as the case that runs on every machine, which is what makes it the one that would notice a template part going missing again. → §WW79
- 📋 **WW80** (deps: Block G) **the sessions case drives the one surface a capture provably cannot finish checking** — Migrated with its waits, its expansion and the popup it opens, since that popup is the argument for the whole interaction loop existing. → §WW80
- 📋 **WW81** (deps: Block G) **the profiles case is the only thing in that repository that drives the picker** — Migrated with the round trip and the timing observation, both of which need the hop count the walk reports to mean anything. → §WW81
- 📋 **WW82** (deps: Block G) **the menu case reads the notification area and nothing else does** — Migrated with the overflow flyout, the keyboard expansion and the expectations derived from the app's own read-out. → §WW82
- 📋 **WW83** (deps: Block G) **the switch case drives the one path that rewrites a real setting** — Migrated inside the store comparison, so the promise that a run touches nothing is asserted where it is most likely to be broken. → §WW83
- 📋 **WW84** (deps: Block G) **the names case observes what no screenshot can** — Migrated with the derived panel sweep and the exact-label reads, including the rule that must fire and the one that must not. → §WW84
- 📋 **WW85** (deps: Block G) **the environment sweep walks a submenu per sampled mode** — Migrated last, because it is the case that proves a fixture reaches every launch a case makes rather than only the first. → §WW85
- 📋 **WW86** (deps: WW78, WW79, WW80, WW81, WW82, WW83, WW84, WW85, Block E ✅) **claude-tray still carries two harness scripts nobody should extend** — Both are deleted once every assertion in them is a case, and the run reports the line count removed so the saving is a measurement. → §WW86
- 📋 **WW87** (deps: Block G, Block E ✅, WW77 ✅) **freewilly carries its own copy of the capture script and a layout probe** — Both migrate: the capture as a scenario, the probe as the geometry dump this framework already owns. → §WW87
- 📋 **WW88** (deps: Block G, WW76 ✅) **pportal carries an interaction harness and twenty-seven copies of one runner** — The harness becomes scenarios and the runner becomes a package reference, which is the largest single deletion the adoption produces. → §WW88

## Block K — The proving ground — a fixture app built to be hard to test

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
- **Every arm of a capture refusal has something that provokes it** Each of the six is
  paired with a fixture shape or a written reason no shape can be, checked against the
  engine's enum both ways and against the built article's own flags. WW199 widened this
  from "a fixture": three arms have one, and the other three name a defect this proving
  ground cannot be.

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

- **Every refusal has something that provokes it** Each named refusal maps to a fixture
  flag or to the case that builds it, both checked against the assemblies and against
  the built fixture. WW146 settled the difference: a receipt about the wrong window is
  the harness handing over the wrong handle, so faking one would reproduce that bug in
  the thing it is pointed at.
- **The fixture needs nothing from the machine** It runs with no account, no network, no
  second display and no real data, on a clean checkout of this repository alone.
- **A shape exists because a defect existed** Every surface the fixture carries names
  the real defect it reproduces, and one that can name none is removed instead of
  maintained forever.

## Done when — WW158

- **Proven on a desk that draws nothing, not on a mock** The condition goes absent
  against a real session that reports everything present and renders nothing - the WW42
  desk - and stays met on an ordinary one. A rendering check verified only against a
  substitute has been verified against the one desk that was never the problem.

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
