# Shipped Ledger

## Block A — The verdict (a run is data, and "not observed" is an answer)

- ✅ **WW1** **an assertion that could not be evaluated reports the same green as one that passed** — A run reads as one of three outcomes whose enum values are the exit codes themselves - 0, 1, 2 - and the degraded summary names every assertion that never ran.
- ✅ **WW2** **nothing separates an assertion that should have run from one this project can never observe** — A hole is now constructed from an absent named precondition and from nothing else, and an assertion naming no subject, or a requirement no run measures, is refused when the scenario loads.
- ✅ **WW3** **a run records what it concluded and not what it observed** — Each step is written as one JSONL line the moment it happens, carrying the locator, what it resolved to, the pattern, the read-back, the wait and the polls, flushed so a run that dies keeps it.
- ✅ **WW4** **the same unrun assertion is tallied once per environment a sweep walks** — A sweep tallies each assertion once by name and keeps every occurrence under it, so one hole met in three environments reads as one hole and still prints three lines, each naming its environment.
- ✅ **WW5** **every path, timeout and language file is typed into the scenario that needs it** — A winwright.json found by walking up from the scenario carries the executable, source root, language files and timeouts, resolved against its own directory, and refuses by name what it omits.
- ✅ **WW6** **a summary can say every check passed while one of them never ran** — The summary refuses the word while anything is unchecked and names what is on the list, and Coverage.RequireEvery is the gate an adopting project's own green asks first.
- ✅ **WW7** **a step that threw is indistinguishable from one that failed its assertion** — A throw is recorded as a HarnessError carrying its step and exception, gives the run a fourth outcome that exits 3 and outranks the other three, and reads as threw in the trace.

## Block B — Attach, launch, and leave nothing behind

- ✅ **WW8** **a case that returns early leaves its process running and locks the next build** — A LaunchedProcess can only be made by the register, so an early return still stops the process, and whatever was alive at the end is named with its pid and whether it stopped.
- ✅ **WW9** **a build that failed leaves the previous exe in place and the run reports on code that is not in the tree** — The binary's write time is compared against the newest source outside build output, and older yields an absent precondition carrying both stamps rather than a failure.
- ✅ **WW10** **attaching to a running instance checks whatever binary is up, not the one that was named** — The running instance is identified by file version and then write time, and a mismatch becomes an absent precondition naming both, with the version difference preferred.
- ✅ **WW11** **a borderless window is invisible to the launcher, because its main window handle stays zero** — Top-level windows are enumerated by process id with a size floor, largest first, so an owned borderless popup the main window handle skips is found with its class, title and bounds.
- ✅ **WW12** **a second window of the app under test can sit on top of the one being driven** — Only an instance showing a top-level window stops the run, and the refusal names each one and the override; a resident instance showing nothing is reported and never refused.
- ✅ **WW13** **a synthesized key press goes to whatever owns the foreground, which is usually the editor** — The foreground is asked once and answered as a met or absent precondition naming what holds it, and the type carries no wait, poll or retry for anything to route around it.
- ✅ **WW14** **there is no way to drive a process somebody else started** — Attach takes a pid or a window, names the binary it reached, and is its own type with no arguments to read, so a check needing one is a hole rather than a comparison.
- ✅ **WW15** **a launch argument silently does not survive an attach** — The language is resolved saved preference first and display language second, reported out loud with where it was read, and one asked for that the app is not in is a hole.
- ✅ **WW125** **a test class that touches the desktop can be written outside the serial collection and nothing says so** — Seven classes that put a window or a process in front of the machine now declare the serial collection, and a check reads the suite's own sources so the eighth cannot be written without one.

## Block C — Locate — the locator grammar and the tree an agent reads

- ✅ **WW16** **an element is addressed by hand-built automation conditions at every call site** — One grammar parses id, name, type, class, pattern, index and descendant chaining, round-trips, and refuses a control type or pattern UI Automation itself does not have.
- ✅ **WW17** **a scenario sleeps because there is no way to wait for a condition** — Once looks a single time and sleeps never, Until takes its deadline as a required argument and reports what it spent, and neither is reachable from the other.
- ✅ **WW18** **an act runs against an element that is present and not yet actionable** — Presence, on screen, enabled and the pattern the act needs are judged over one snapshot, and the refusal names which of the four was missing with the remedy that is its own.
- ✅ **WW19** **the control view only prints when a check has already failed** — Inspect walks the control view of a live window and prints one line per element that begins with the locator step addressing it, then its rectangle, state and patterns.
- ✅ **WW20** **a control missing because its pane is collapsed reads the same as one that is gone** — A miss says whether the step matched elsewhere, whether what it stopped under is shut and must be opened, or that nothing in the window is shut and the control is gone.
- ✅ **WW21** **two elements carrying the same name cannot be told apart by any condition** — The grammar takes order=left, right, top or bottom and sorts matches by rectangle, and a step matching several without saying which is refused with what it matched.
- ✅ **WW22** **a resolved element goes stale and the act runs against a handle nobody holds** — A subject holds a locator and never an element, resolving again per act, and every pattern is read into plain values so two readings either side of an act can differ.
- ✅ **WW23** **there is no way to ask what patterns an element actually offers** — What an element offers is asked for by locator, and every declared act is checked against it before the run, refusing with the element and the pattern both named.

## Block D — Act — patterns before pointers

- ✅ **WW24** **an act needs the foreground, so a run started from an editor drives somebody else's window** — Invoke, toggle, value, range, selection and expand each re-resolve, require actionability, act through the pattern and read back, proven with the foreground held elsewhere.
- ✅ **WW25** **a pointer act happens implicitly wherever a pattern was missing** — A pointer act is its own declared kind that nothing falls back to, it sends nothing unless the window owns the foreground, and what needs a desktop is summarised from the declaration.
- ✅ **WW26** **typing is asserted by a screenshot, which cannot see whether a key arrived** — Text goes in as real keys and the act waits for the control's own value to say so, refusing a control that reports no value because typing nobody can read back is the screenshot.
- ✅ **WW27** **keyboard traversal has no observable at all** — A traversal key answers with what holds the focus rather than whether it moved, and a nudge presses away from whichever end the range sits at and says it did.
- ✅ **WW28** **a picker walked to a value passes through every value on the way** — A pick reports the route it took and every value it stopped on, and the keyboard walk anchors at whichever end is nearer so the count stays as small as the picker allows.
- ✅ **WW29** **a menu is driven by clicks a shell does not always deliver** — A menu is entered with F10, walked with Down and expanded with Right against a polled deadline, nothing resets between attempts, and this surface has no invoke at all.
- ✅ **WW30** **a flaky act is hidden behind a retry that runs until it passes** — Attempts are capped, a cap large enough to hide a failure is refused, and the count is stamped onto the trace step so a green that took three goes still says so.
- ✅ **WW31** **the notification-area icon has no clickable point and no reliable right-click** — Icons are addressed by rectangle since every taskbar button refuses a clickable point, the overflow is opened through the chevron found by id, and the menu route is focus plus the application key.
- ✅ **WW32** **a selection that silently does not land leaves the pane it should have built unrealised** — A selection is confirmed against the control and a condition the caller names, the pointer is reached for only after that did not pass, and nothing reports a landing it did not confirm.
- ✅ **WW33** **an act leaves the window in a state the next case did not ask for** — Surfaces are recorded as a case found them and put back on leaving the scope, only where they moved, and a restore that did not take is reported rather than assumed.

## Block E — Capture — the picture that proves what it photographed

- ✅ **WW36** **a copy of the window rectangle carries a strip of the desktop down every edge** — The extended frame bounds is what a capture copies, and the run prints the four trims, which on an overlapped window are eleven a side and none at the top.
- ✅ **WW37** **a capture is offset or scaled on any display that is not at one hundred percent** — Per-monitor awareness is set when the engine assembly loads, so every rectangle and every synthesised click is in the space the window lives in on a display at any scaling.
- ✅ **WW39** **a window can be visible by its style bits and painted by nothing** — Windows the compositor is not drawing are left out of a listing and carry the reason when one is asked for anyway, so a stock desktop stops reporting 12 intruders in 27 windows.
- ✅ **WW45** **an animation has no observable, so a transition ships unlooked-at** — Frames land in slots computed from the first one and held against a stopwatch, so one slow capture costs its own frame and not the run, and the sequence reports the drift it measured.
- ✅ **WW47** **a wrong capture is caught only because a person looked at the picture** — A capture states the window, the process and the arguments behind it, and refuses outright when the window belongs to another process or nothing is drawing it.
- ✅ **WW34** **a screen copy can photograph anything that happens to be in the rectangle** — A capture routes to the off-screen render by default and reaches the screen copy only for a menu, a balloon or an owned popup - or where a caller forced it and said why, which the receipt keeps.
- ✅ **WW35** **a tree that failed to build writes a file that looks like a successful capture** — A picture is scanned for any pixel carrying an alpha of its own, so a blank is a red - and one with no alpha channel to read is unchecked rather than passed, because that scan never ran.
- ✅ **WW44** **nothing checks that a capture contains the surface it was taken for** — A capture is asserted against the rectangle the application said it drew, telling clipped from elsewhere because they are two repairs, and a surface nobody reported is a hole rather than a red.

## Block F — Assert — the expectation is derived, never typed

- ✅ **WW48** **an assertion is a boolean, so a failure says nothing about what was there instead** — An expectation reports every reading it saw, the wait and the polls, so a subject that answered the wrong thing throughout is never reported as one that never answered.
- ✅ **WW49** **a hardcoded expected set silently stops covering what it was written for** — Expected sets come from the project's own strings and never from the tree being asserted, and a key that declares nothing is refused rather than passing against an empty window.
- ✅ **WW50** **labels are matched in English against a window rendering another language** — A label resolves to the language the window is rendering, a value carrying a placeholder is refused outright, and a language nobody declared strings for is named instead of answered in English.
- ✅ **WW52** **a control announcing its glyph codepoint satisfies every check for a non-empty name** — A name is read for what it is, so a glyph codepoint, an echoed automation id and an unfilled template each fail as themselves and print as escapes rather than as nothing.
- ✅ **WW53** **a run mutates the store of the user who launched it** — The store is hashed around the case and every file, directory and variable that moved is named, so a rewrite of the same length and the same date is still caught.
- ✅ **WW54** **no reading and a window that was talking are reported with the same sentence** — A read that ended with nothing says whether the window was working or blank, how many polls saw it, and what to look at, so only one of the two points at the deadline.
- ✅ **WW55** **diagnosing a failure costs a throwaway script that dumps the tree** — A red carries the control view it had in hand - bounded, saying what it cut, marking the element the check read - so the dump that took a throwaway script arrives with the failure.
- ✅ **WW56** **an assertion is trusted without ever being watched fail** — A case declares the defects its check must catch and each one is run against it, so a check that stayed green with a declared defect in place is a red rather than a line that passes forever.
- ✅ **WW51** **a page renders correctly above a screenful of blank space and nothing notices** — A dump is checked against itself for the four faults and measured for how much of the surface was drawn on, with every expectation derived from the dump rather than typed into a case.

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

- ✅ **WW64** **an answer has to be verified by reading the file the command already read** — Every answer carries where it came from as fields - file, line, key, element, pattern - so a missing header reads as strings.en.json:6 and the trace keeps it, rather than a reader opening the file.
- ✅ **WW68** **a machine that can observe nothing reports a build failure** — Six conditions are read before anything launches - session, desktop, display, coordinates, automation, foreground - and a desk lacking one excuses a check as a named hole instead of a red.
- ✅ **WW70** **three copies of the engine can be in play and quietly disagree** — The copies in play are read together and answer agreed, behind or unpinnable, exiting non-zero on the two that are not agreement - and one copy nobody can pin makes the whole reading unpinnable.

## Block I — The in-app half — the app cooperates with the harness

- ✅ **WW71** **the render verb is written again in every project that wants one** — One package renders a tree with no window shown - measure, arrange, update, target, background, encode - with the arrange inside, and a size of nothing refused rather than written as an empty file.
- ✅ **WW72** **a capture is rendered on no background and comes back unreadable** — The application's own key is asked first and the observed window colour is the fallback, the classic palette is never consulted, and neither answering is stated rather than turned into white.
- 🗑 **WW120** **the in-app package fixes the process DPI awareness before the engine can ask for its own** — superseded by WW121: Measured before deciding and the premise did not survive: the process is system-aware before any code here runs, so the package is not what fixes it and no initializer could win.
- ✅ **WW121** **a render says nothing about whether this process can see the display it drew on** — The in-app half asks for per-monitor awareness at load and every receipt now carries what the process actually sees, so a size drawn in the wrong space says so instead of looking correct.
- ✅ **WW73** **a brush shared between capture threads belongs to whichever one reached it first** — A brush handed to a capture is frozen as a copy or refused by name, with both threads stated, instead of the drawing raising an error that names neither the brush nor either thread.
- ✅ **WW74** **the app knows what it drew and nothing asks it** — The application writes a name and four physical-pixel numbers per surface and the engine reads them back, with a surface nobody reported answering as a named hole rather than a throw.
- ✅ **WW75** **a popup closes the moment the window is raised to be photographed** — The host walks the logical tree, holds every popup it finds open and puts each one back, so a closed popup is reached at all and no page has to remember the rule for itself.
- ✅ **WW76** **the same single-threaded runner is copied into every test file that touches a control** — One bounded runner rethrows what the work threw as itself, and this suite's own eight copies of it were deleted in the same commit, which is the saving stated as a deletion.
- ✅ **WW77** **a surface with no accessibility tree can only be checked by reading its source** — The application dumps every laid-out rectangle with its depth in physical pixels and the engine reads it back, so a surface with no accessibility tree is assertable without reading its source.

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

- ✅ **WW89** **there is no window to develop against that is not somebody's shipping product** — A WPF fixture belongs to this repository and the suite launches it, reads its tree and stops it, with nothing on it read off the machine it runs on.
- ✅ **WW90** **every refusal the framework makes is asserted only against a defect nobody can reproduce** — The fixture takes flags and refuses one it does not know before the presentation stack starts, naming its whole catalogue, so a misspelt shape is a red exit rather than a silent default.
- ✅ **WW91** **a window hosted under two different message pumps is the difference no picture can see** — One flag ships the same window under a dispatcher that runs and one that never does, so a window that composes perfectly and answers no message is producible on demand.
- ✅ **WW92** **a control that announces nothing has to be found inside somebody else's product** — One flag puts all five namings on one page - nothing, a glyph, an echoed id, a neighbouring label and a button that keeps its text - so the check has its whole case set on demand.
