# Roadmap (active backlog)

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

- 🛠 **WW487** (deps: —) **the locator grammar lives in a section of a 1,000-line README, so learning it means reading the file** — Every case in every adopting repository is written in that grammar, and a section of a README is not a page anybody can be sent to or search. → §WW487
- 📋 **WW488** (deps: —) **the case format answers only to an installed plugin, so a reader deciding has no field list to read** — Evaluation comes before installation, and the schema winwright_format serves an agent is the one a person weighing the format needs first. → §WW488
- 📋 **WW489** (deps: —) **the verb catalogue is a README table, so no row can be linked and no verb says what it needs of the desk** — The suite already checks that catalogue against the engine in both directions, so publishing it costs a generator rather than a second list to keep. → §WW489
- 📋 **WW490** (deps: —) **winwright.json is described by an example, so a reader cannot tell which keys this build accepts** — It is the first file an adopter writes, and an example with nine keys says nothing about the tenth or about what each one refuses. → §WW490
- 📋 **WW491** (deps: —) **what earns a hole rather than a red is spread over the README, so a reader who met a 2 cannot look it up** — Degraded is the finding this project exists for and the verdict an adopter meets without understanding it, which is the moment a scattered explanation is worth nothing. → §WW491
- 📋 **WW492** (deps: —) **the in-app half is described as a list of types, so nobody can tell what shipping it costs their users** — It is the one package that goes into an application real people run, and that decision needs what it writes, when, and what a release does with no harness attached. → §WW492
- 📋 **WW493** (deps: —) **every example in the area was written rather than run, so no page shows one real adoption end to end** — A walkthrough pasted from memory misleads at the first message whose wording changed, and the refusals are where somebody decides a tool is not worth the trouble. → §WW493
- 📋 **WW494** (deps: —) **a first run's refusals are written up by cause, and a stuck reader has only the words on their screen** — CS0579 naming a wpftmp project, a desk that grants no foreground and a guard nobody built are what stop an adoption, and none of the three is searchable today. → §WW494
- 📋 **WW495** (deps: —) **the area publishes HTML alone, so an agent renders three pages to learn what the tool is** — The pitch page already writes a plain-text twin per route and a llms.txt beside it, and the area next door publishes neither. → §WW495
- 📋 **WW496** (deps: —) **the site ships two sitemaps under one base, and the one robots.txt names lists no page of the area** — A crawler is handed the pitch routes and never the pages an adoption is decided on, which are the reads the area was built to publish. → §WW496
- 📋 **WW497** (deps: —) **the area links README anchors by hand, so a renamed heading breaks them with nothing going red** — Those links carry every reader the area sends to the long form, and they point into a file this repository owns and renames freely. → §WW497
- 📋 **WW498** (deps: —) **every page assumes the vocabulary, so a first-time reader meets case, hole, desk and reading undefined** — These words carry the design and none of them means here what it means elsewhere, so a reader guessing at one misreads every page that uses it. → §WW498

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
- **Every arm of a capture refusal has something that provokes it** Each of the eight is
  paired with a fixture shape or a written reason no shape can be, checked against the
  engine's enum both ways and against the built article's own flags. WW199 widened this
  from "a fixture": three arms have one, and the rest name a defect this proving ground
  cannot be.

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
