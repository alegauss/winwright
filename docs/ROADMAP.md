# Roadmap (active backlog)

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

- ⏳ **WW158** (deps: a session that can be disconnected and reconnected) **the display condition counts monitors and measures the virtual screen, which the session that rendered nothing passed** — Its own criterion is unmet: no desk that draws nothing has taken the reading. → §WW158
- 📋 **WW289** (deps: —) **a toast takes the desk and the run excuses 43 checks, reading as green as one that excused 8** — The ledger counts every excuse and nothing compares the count to what a run usually spends, so the weakest evidence a suite produces is spelled exactly like the strongest. → §WW289

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

- ⏳ **WW288** (deps: —) **a search whose flyout shut mid-look gives up, so the icon case is a hole on the runs the shell decides** — Whether a search should reopen a flyout that shut mid-look is still undecided, and the rate is still only in one run's excuse ledger. → §WW288

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

- ⏳ **WW248** (deps: —) **a test class holding an in-process dialog cannot also drive a launched window, and nothing says so** — A structural excuse is still not a red, and telling one from circumstance needs a history across runs. → §WW248
- ⏳ **WW249** (deps: —) **the case proving that typing reaches a WPF box fails about one guest run in four** — Nine reds, and the substituted character is always the last one sent — one or more of them, at no fixed position, with the array ruled out. → §WW249
- 📋 **WW269** (deps: —) **a caption counting down in real time cannot survive an exact comparison across a round trip** — `sameAs` compares two readings exactly, and a run that straddles a minute boundary would then be a red build about a clock rather than about the application. → §WW269

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

- ⏳ **WW78** (deps: WW230 ⏳) **the keyboard case is the only observable of a live input path and it lives in a script** — The runner now carries claude-tray to the guest, and the restore there fails: the engine's package exists only in a folder this repository does not ship. → §WW78
- 📋 **WW82** (deps: WW257 ✅, WW258 ✅, WW259 ✅, WW260 ✅, WW291 ✅, WW292 ✅) **the menu case reads the notification area and nothing else does** — Migrated with the overflow flyout, the keyboard expansion and the expectations derived from the app's own read-out. → §WW82
- 📋 **WW83** (deps: Block G ✅, WW291 ✅) **the switch case drives the one path that rewrites a real setting** — Migrated inside the store comparison, so the promise that a run touches nothing is asserted where it is most likely to be broken. → §WW83
- 📋 **WW85** (deps: Block G ✅, WW291 ✅) **the environment sweep walks a submenu per sampled mode** — Migrated last, because it is the case that proves a fixture reaches every launch a case makes rather than only the first. → §WW85
- 📋 **WW86** (deps: WW78 ⏳, WW79 ✅, WW80 ✅, WW81 ✅, WW82, WW83, WW84 ✅, WW85, Block E ✅) **claude-tray still carries two harness scripts nobody should extend** — Both are deleted once every assertion in them is a case, and the run reports the line count removed so the saving is a measurement. → §WW86
- 📋 **WW87** (deps: Block G ✅, Block E ✅, WW77 ✅) **freewilly carries its own copy of the capture script and a layout probe** — Both migrate: the capture as a scenario, the probe as the geometry dump this framework already owns. → §WW87
- 📋 **WW88** (deps: Block G ✅, WW76 ✅) **pportal carries an interaction harness and twenty-seven copies of one runner** — The harness becomes scenarios and the runner becomes a package reference, which is the largest single deletion the adoption produces. → §WW88
- ⏳ **WW230** (deps: —) **the only place the engine's package exists is a folder inside this repository** — Nothing is published yet, so the adopter's `nuget.config` is still a path across two clones. → §WW230

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
  the hook, the tools and the skill, and nothing is added to any path. No slash
  commands: a verb reachable from a tool does not also need a name typed with a slash.
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
