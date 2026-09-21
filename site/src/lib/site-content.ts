// The copy lives here and nowhere else. Every section component imports a value from this
// module and only renders it — so a claim is an array element a reviewer can check against
// the product, not a string welded into the markup that displays it. The composition (which
// section, in which order, and the illustrative SVGs) lives in the JSX; this file is the
// words.
//
// Fragments carrying inline code or emphasis are modelled as a small tagged run list
// (`Rich`) rather than raw HTML, so a section renders them without dangerouslySetInnerHTML
// and the Markdown twin generator has a structure to convert rather than markup to parse.
//
// Every figure below comes from `product`, and none of it is typed. The copy states the
// reason and the generator states the number: a count typed beside the list it describes is
// true on the day it is typed and silent about the day it stops being — which is the same
// defect class this product exists to refuse in a test suite.
import {
  Spelled,
  harnessPackage,
  inAppPackage,
  spelled,
  verdict,
  verdictCount,
  version,
} from "./product";

export type Run =
  | string
  | { code: string }
  | { b: string }
  | { i: string };

export type Rich = Run[];

/* ------------------------------------------------------------------ meta + chrome */

export const meta = {
  title: "winwright — a green never covers a check that did not run",
  description:
    `A Windows UI test framework for .NET 10, on nuget.org. It answers in ${spelled(verdictCount)} verdicts rather than two, drives a desktop application through UI Automation and Win32, derives its expectations from the application's own declarations, and names anything it could not evaluate instead of leaving it out of the count.`,
  og: {
    title: "winwright",
    description:
      `Drive a Windows desktop application from a test, and report what was actually observed. ${Spelled(verdictCount)} verdicts, one locator grammar, patterns before pointers, and a picture that proves what it photographed.`,
    url: "https://alegauss.github.io/winwright/",
  },
} as const;

export const repoUrl = "https://github.com/alegauss/winwright";
export const parentUrl = "https://alegauss.github.io/";

// The release page rather than a file: the packages carry their version in their names, so
// there is no version-independent URL for an asset itself, and a hard-coded
// `Winwright.0.1.0.nupkg` would 404 on the day 0.1.1 ships.
export const releasesUrl = `${repoUrl}/releases/latest`;
export const changelogUrl = `${repoUrl}/blob/main/docs/CHANGELOG.md`;

// The documentation area, which is a second build under the same base (site/docs/, an Astro
// project writing into this one's dist/docs). Written as a path and not as a route: there is
// no entry for it in routes.tsx because nothing here renders it.
export const docsUrl = "/winwright/docs/";

// The README is still the long form — every verb family, every field a case may carry — and
// the area links to it rather than holding a second copy. Named separately so a link meaning
// "the whole reference" cannot quietly become a link to the area's three pages.
export const readmeUrl = `${repoUrl}#readme`;
export const issuesUrl = `${repoUrl}/issues`;
export const licenceUrl = `${repoUrl}/blob/main/LICENSE`;

/** A package's own page on nuget.org, which is where a version is actually published. */
export function nugetUrl(id: string): string {
  return `https://www.nuget.org/packages/${id}`;
}

// Section anchors (#x) act on the landing page; the page links are base-absolute so they
// resolve the same from every route. The brand and footer link home the same way.
export const navLinks = [
  { href: "#install", label: "Get it" },
  { href: "#verdict", label: "The verdict" },
  { href: "/winwright/claude-code/", label: "Claude Code" },
  { href: "/winwright/compare/", label: "Compare" },
  { href: docsUrl, label: "Docs" },
] as const;

export const footer = {
  groups: [
    {
      heading: "Take it",
      links: [
        { href: nugetUrl(harnessPackage().id), label: `${harnessPackage().id} on nuget.org` },
        { href: nugetUrl(inAppPackage().id), label: `${inAppPackage().id} on nuget.org` },
        { href: releasesUrl, label: "Releases" },
        { href: changelogUrl, label: "Changelog" },
      ],
    },
    {
      heading: "Read it",
      links: [
        { href: docsUrl, label: "Documentation" },
        { href: readmeUrl, label: "The README" },
        { href: "/winwright/claude-code/", label: "Claude Code plugin" },
        { href: "/winwright/compare/", label: "Compare" },
      ],
    },
    {
      heading: "Ask about it",
      links: [
        { href: repoUrl, label: "GitHub" },
        { href: issuesUrl, label: "Issues" },
        { href: licenceUrl, label: "Apache-2.0 licence" },
      ],
    },
  ],
  // What this is and what it is not, in the shape a reader can check rather than in
  // adjectives. The licence is named because the packages declare it and a reader deciding
  // whether to take one is entitled to read it without cloning anything.
  disclaimer:
    "winwright drives applications through Windows UI Automation and Win32 and adds no external dependency to either half. Both packages are published under the Apache-2.0 licence. It is not affiliated with, endorsed by, or sponsored by Microsoft; “Windows”, “Win32” and “UI Automation” are Microsoft's. © 2026 Alexandre Oliveira.",
} as const;

/* --------------------------------------------------------------- sponsor */

// Mirrors alegauss.github.io/sponsor.json — the canonical sponsor declaration for these
// projects. Transcribed here rather than fetched at runtime: this site is prerendered, and
// the whole point of naming a sponsor is that crawlers and LLMs read it in the served HTML.
export const sponsor = {
  label: "Sponsored by",
  name: "Viglet",
  url: "https://www.viglet.org",
  siteLabel: "viglet.org",
  logo: "/winwright/viglet/viglet-logo.png",
  summary:
    "Open source search and content tools for organisations with a lot to publish — run on your own servers, with no per-user licence.",
  products: [
    {
      name: "Viglet Turing ES",
      url: "https://turing.viglet.org",
      logo: "/winwright/viglet/turing-logo.png",
      inline:
        "so visitors find what they came for, with AI answers drawn only from your own content",
    },
    {
      name: "Viglet Shio CMS",
      url: "https://shio.viglet.org",
      logo: "/winwright/viglet/shio-logo.png",
      inline:
        "so a new page goes live the same day, reviewed and approved by your own team",
    },
  ],
} as const;

/* ------------------------------------------------------------------ hero */

export const hero = {
  badge: `${harnessPackage().id} ${version} · on nuget.org · Windows · .NET 10`,
  titleLead: "A green never covers",
  titleAccent: "a check that did not run.",
  sub: [
    "winwright is a UI test framework for Windows desktop applications. It drives one from a test and reports ",
    { b: "what was actually observed" },
    " — answering in ",
    { b: `${spelled(verdictCount)} verdicts rather than two` },
    ", so a check whose precondition was absent is named in the summary instead of disappearing from the count.",
  ] as Rich,
  meta: [
    "🧩 No external dependency in either half",
    "🪟 UI Automation and Win32, nothing else",
    "✍️ The tool never writes the test",
  ],
  pills: [
    [{ b: ".NET 10" }, " · ", { code: "net10.0-windows" }] as Rich,
    ["One ", { b: "locator grammar" }, ", read the same by every verb"] as Rich,
    [{ b: "Pattern acts" }, " need no foreground"] as Rich,
    ["Two packages, and an app takes ", { b: "at most one" }] as Rich,
  ],
};

/* ------------------------------------------------------------------ hero run */
// The hero is a run, not a screenshot: what is sold here is the shape of the answer. The
// transcript is a real scenario's output — the preamble, the steps, the reading that could
// not be taken, and the verdict that names it — and it is the origin story restated as
// output. Rendered as an autoplaying transcript that scrolls its own list.
//
// The numbers in the footer are the measurement this project exists because of: a suite
// that reported a pass with a total of 352 where the run before it had 374.

export const heroRun = {
  eyebrow: "What a run says",
  question:
    "check the report pane after the language is switched, on whatever desk this is",
  // The tool call, because that is what a run is asked for by: the four MCP tools the
  // plugin wires, and this one takes a project, a directory of cases and a selector. From a
  // test project the same run is `Suite.Launch`, which the scenario section names.
  command: 'winwright_run  { "project": ".", "cases": "cases", "case": "language round trip" }',
  // The preamble: one reading of the machine, taken before any assertion. Each line is
  // measured, absent, or *not read* — and only one of those three is a statement. The
  // conditions are named the way the engine names them, because that name is what a case's
  // `needs` refers to and what the summary prints.
  preamble: [
    "this run measured 5 conditions, 1 not read.",
    "  ok      the running instance is the binary this run named",
    "  ok      a binary built from this source",
    "  agrees  the application is in the language this scenario is written for",
    "  ok      the foreground belongs to the window under test",
    "  not read an application that renders its own tree when asked",
  ],
  steps: [
    {
      cmd: "resolve  Window#main > Pane#reportHost > Text",
      mark: "ok" as const,
      out: "1 element  ·  Text “Relatório mensal”  ·  38ms",
    },
    {
      cmd: "invoke   MenuItem#languagePtBR",
      mark: "ok" as const,
      out: "InvokePattern  ·  no foreground needed  ·  attempt 1 of 3",
    },
    {
      cmd: "read     Pane#reportHost > Text   covers: report.labels",
      mark: "ok" as const,
      out: "27 strings derived from strings.pt-BR.json  ·  27 read, none left over",
    },
    {
      cmd: "capture  Pane#reportHost           with: report.pt-BR",
      mark: "unread" as const,
      out: "unchecked — “an overflow flyout this run can work” absent (the desk's): the shell would not open the flyout, so the pane could not be brought to the front. Named, not dropped.",
    },
    {
      cmd: "close    the machine is as this run found it",
      mark: "ok" as const,
      out: "store fingerprint unchanged  ·  nothing this run launched outlived it",
    },
  ],
  before: "Before: 352 of 374 ran · reported green · nobody had a reason to read the total",
  after: `After: ${verdict("Degraded").name} (exit ${verdict("Degraded").code}) · everything that ran held · 1 unchecked, and whose it was`,
  note: [
    "The last line is the point. An assertion whose precondition was absent did not pass and did not fail — it ",
    { b: "never ran" },
    ", it is named in the summary along with whose the absence was, and collapsing it into either of the other two is the thing winwright will not do. The exit code is ",
    { code: `${verdict("Degraded").code}` },
    ", so CI can tell the difference without reading a word.",
  ] as Rich,
};

/* ------------------------------------------------------------------ the two halves */

export const halves = {
  eyebrow: "Who takes what",
  heading: "Two packages, and your application takes at most one of them",
  intro: [
    "The harness half is referenced by the project that drives the application. The in-app half is optional, and deliberately so: ",
    { b: "every reading and every pattern act runs against an application that references nothing" },
    ". What the second package buys is the handful of readings a harness cannot take from outside the process.",
  ] as Rich,
  actors: [
    {
      who: harnessPackage().id,
      sub: "the test project",
      iface: `<PackageReference Include="${harnessPackage().id}" Version="${version}" />`,
      job: "Locate, act, assert, capture, and assemble the verdict. Referenced by whoever drives the application — never by the application.",
      primary: true,
    },
    {
      who: inAppPackage().id,
      sub: "the application under test, and only if you want it",
      iface: `<PackageReference Include="${inAppPackage().id}" Version="${version}" />`,
      job: "Coordinates, render, backgrounds, geometry, popups — the readings that can only be taken from inside. It references the engine not at all, so nothing here ships a test harness to your users.",
      primary: false,
    },
  ],
  actorsNote: [
    "An application shipped to its users reports nothing and writes no file, which is what makes the in-app protocol safe to leave in a release.",
  ] as Rich,
};

/* ------------------------------------------------------------------ the laws */

export const laws = {
  eyebrow: "What it guarantees",
  heading: "Ten rules, in the order a run meets them",
  intro: [
    "Every one of these is paired in the engine's own suite with the thing that would break it, and each names the defect it prevents rather than the value it expresses.",
  ] as Rich,
  items: [
    {
      id: "L1",
      title: "A green never covers a check that did not run",
      body: "All of it follows from one measurement: a suite reported a pass with a total of 352 where the run before it had 374. Twenty-two checks were gone and the only sign was a number nobody had a reason to read.",
    },
    {
      id: "L2",
      title: "“Not observed” is an answer",
      body: `Absent and not read are different facts and they read the same to somebody skimming. Every reading is reported as measured, absent, or not taken — and only one of those three is a statement about your application.`,
    },
    {
      id: "L3",
      title: "The desk is not your application",
      body: "A foreground Windows would not grant, a focus that left mid-poll, a flyout the shell would not open. None of them is your code being wrong, so none of them goes red — the answer names what the desk did instead.",
    },
    {
      id: "L4",
      title: "One grammar, written once",
      body: "Every verb reads the same locator string the same way, and the combinator means a descendant of, at any depth — because UI Automation wraps controls in panes that differ between frameworks and between a maximised window and a restored one.",
    },
    {
      id: "L5",
      title: "Patterns before pointers",
      body: "An act asks the control through its own accessibility peer, which needs no foreground and no mouse. The verbs that do synthesise input are marked as such in the catalogue rather than discovered on a red run.",
    },
    {
      id: "L6",
      title: "The expectation is derived, never typed",
      body: "An expected label read from the application's own language file cannot drift from it. A string retyped into a test is a second copy of the truth, and the second copy is the one that goes stale.",
    },
    {
      id: "L7",
      title: "A picture proves what it photographed",
      body: "A capture that a window covered, that a backdrop transmitted through, that came back one flat colour, or that photographed a page still computing is refused — and the refusal names the intruder, its process and the rectangle.",
    },
    {
      id: "L8",
      title: "A refusal is an instruction",
      body: "A locator that does not parse names the position and the reason. A misspelt control type is answered with the nearest name UI Automation actually has. An error that costs a round trip to understand is a defect.",
    },
    {
      id: "L9",
      title: "A run leaves the machine as it found it",
      body: "The store a case writes through is fingerprinted before and compared after, so the promise that a run changed nothing of yours is asserted where it is most likely to be broken rather than stated in a readme. Leftover processes are stopped and named in the same breath.",
    },
    {
      id: "L10",
      title: "The tool never writes the test",
      body: "There is no recorder that turns clicks into a scenario, and no assertion invented on your behalf. What the engine owns is the loop, the waits and the verdicts; what you own is what is worth asserting.",
    },
  ],
};

/* ------------------------------------------------------------------ the verdict */

export const verdictSection = {
  eyebrow: "The answer",
  heading: `${Spelled(verdictCount)} outcomes, and the member values are the exit codes`,
  // The figure's own title bar: the file a run was over, in the extension the loader walks
  // for. It sat here as an invented command for as long as the invented command did.
  terminalTitle: "winwright — report.cases.json",
  intro: [
    "A mapping written twice is a mapping that drifts, and CI reads the number rather than the word — so the enum's values ",
    { b: "are" },
    " the process exit codes rather than being translated into them. ",
    { code: `${verdict("Degraded").code}` },
    " is the one the rest of this framework was built to make possible.",
  ] as Rich,
  // The cards are generated from RunOutcome itself: the name, the number and the first
  // sentence of the member's own summary. What is typed here is the gloss under each — the
  // part that is prose about the decision rather than a restatement of it.
  glosses: {
    Passed: [
      "And it means the roll call agreed: a run short of what discovery found is not reported as a pass, because that is exactly the shape the original defect had.",
    ] as Rich,
    Failed: [
      "One assertion ran and did not hold. The failing step carries the view the diagnosis built — the tree as it was, the element's facts, what its patterns read — so nobody writes a throwaway script to see what the window had.",
    ] as Rich,
    Degraded: [
      "The one the rest of this was built for. Each unevaluated reading is named in the summary, and so is ",
      { b: "whose the absence was" },
      " — the desk's, this run's, or nobody's yet. A reader told three checks never ran has to know whether to clear a machine or open a repository.",
    ] as Rich,
    Broken: [
      "It outranks the rest, because a reader told the build failed opens the wrong repository. What the message says is about this tool, and nothing after the throw was observed at all.",
    ] as Rich,
  } as Record<string, Rich>,
  note: [
    "Before the assertions, a run takes one reading of the machine: the desk it is on, which binary it is driving, whether that binary is stale, the resolved language, the foreground, the launch arguments, whether anything else is showing the application, and whether the desk is this run's alone. Each is reported as measured, absent, or ",
    { b: "not read" },
    " — an absent line and a missing line read the same to somebody skimming, and only one of them is a statement.",
  ] as Rich,
};

/* ------------------------------------------------------------------ locate */

export const locate = {
  eyebrow: "Addressing an element",
  heading: "One grammar, read the same way by every verb",
  intro: [
    "A locator is a string, it parses or it is refused, and the refusal names the position and the reason. ",
    { code: "Locator.TryParse" },
    " answers without throwing, for a caller collecting refusals rather than stopping at the first.",
  ] as Rich,
  lines: [
    ["#saveButton", "the automation id"],
    ["Button", "the control type"],
    ["Button#saveButton", "both at once"],
    ['Button[name="Save as..."]', "the name"],
    ["Pane[class=Chrome_WidgetWin_1]", "the window class"],
    ["Button[pattern=Invoke]", "it must carry that pattern"],
    ['Text[name="Statistics"][order=left]', "the leftmost of the ones that match"],
    ["MenuItem[order=top][index=2]", "the second from the top"],
    ["Window#main > Pane > Button#save", "a descendant of, at any depth"],
  ] as [string, string][],
  notes: [
    [
      { b: "“>” means a descendant of, not a direct child." },
      " That is a decision, not a shorthand: UI Automation wraps controls in panes that differ between frameworks, between versions of one framework, and between a maximised window and a restored one — so a direct-child locator is the one that breaks on somebody else's machine.",
    ] as Rich,
    [
      { b: "Two elements matching one step is a refusal" },
      ", not a first-match. A locator that quietly picked one of two is a test that passes against the wrong control until the day the order changes.",
    ] as Rich,
    [
      { b: "A misspelt control type is answered with the nearest real one." },
      " The vocabulary is UI Automation's own rather than a list kept here by hand, and a list kept by hand is a list that drifts from the thing it describes.",
    ] as Rich,
    [
      { b: "The tree is readable." },
      " The control view under a window or an element comes back as a tree, or as lines a person reads — which is what turns “it did not find it” into a question with an answer.",
    ] as Rich,
  ],
};

/* ------------------------------------------------------------------ act */

export const act = {
  eyebrow: "Acting on it",
  heading: "A pattern act is the default, and it needs no foreground",
  intro: [
    "It asks the control through its own accessibility peer rather than asking the desktop to move a mouse. So a run does not need the screen to itself, and an act does not fail because something else took the foreground while it was polling.",
  ] as Rich,
  cards: [
    {
      icon: "🎛️",
      title: "Through the control's own patterns",
      body: [
        "Invoke, toggle, set a value or a range, select, expand, collapse. Each act is attempted to a cap and counted, so ",
        { b: "attempt 3 of 3" },
        " is in the record rather than in somebody's memory of the run.",
      ] as Rich,
    },
    {
      icon: "🚦",
      title: "Actionability is checked before, not discovered after",
      body: [
        "Whether an element can take an act at all — and where it cannot, why not. A preflight takes every declared act and checks what it needs against the tree ",
        { b: "before anything is pressed" },
        ".",
      ] as Rich,
    },
    {
      icon: "🖱️",
      title: "Pointers exist, and say why",
      body: [
        "Synthesised mouse input is there for the controls that have no peer worth asking. Every verb that needs the foreground is marked as such in the catalogue, and the declared reading says why the act could not be done any other way.",
      ] as Rich,
    },
    {
      icon: "⌨️",
      title: "Keys, traversal, and what holds the focus",
      body: [
        "Synthesised keys, traversal keys at a window, and a focus read ",
        { b: "against the application under test" },
        " rather than against the whole desk — because “something else has the focus” and “your window lost it” are different facts.",
      ] as Rich,
    },
    {
      icon: "📋",
      title: "Menus the way a keyboard user walks them",
      body: [
        "Enter a menu bar, walk to an entry, open a submenu, dismiss. The walk reports its hop count, which is what makes a timing observation about a picker mean anything.",
      ] as Rich,
    },
    {
      icon: "🔔",
      title: "The notification area, including the overflow",
      body: [
        "The tray, the overflow flyout, the icons on either, and an icon's context menu. Finding an icon says ",
        { b: "which" },
        " of the two happened — the icon is not there, or the flyout would not open — because a caller reading a null cannot tell them apart.",
      ] as Rich,
    },
  ],
  destructive: [
    "A declared destructive entry reached without saying you meant it is refused. ",
    { code: "destructive" },
    " is the one project key with a refusal of its own: a bare name is rejected where the project ships more than one language, because a name is the field a translation rewrites — and a safety check compared against text a person sees has an expiry date.",
  ] as Rich,
};

/* ------------------------------------------------------------------ assert */

export const assert = {
  eyebrow: "Asserting",
  heading: "The expectation is derived, never typed",
  intro: [
    "A string retyped into a test is a second copy of the truth, and the second copy is the one that goes stale. So the expected set is read from what the application itself declares — its language files, its layout, its own read-out — and the comparison is against that.",
  ] as Rich,
  list: [
    [
      { b: "Labels come from the project's own language files." },
      " Switch the resolved language and the expected set switches with it; a key none of the files carries refuses the run rather than matching nothing.",
    ] as Rich,
    [
      { b: "A derived set names what it derived from." },
      " An expectation nobody can trace back to a source is an expectation somebody will eventually “fix” by editing the number.",
    ] as Rich,
    [
      { b: "Every expectation is falsifiable." },
      " A check that cannot fail is a check that is not being run, and it is worth exactly as much as the one that never ran at all.",
    ] as Rich,
    [
      { b: "A timed-out read is a reading, not a value." },
      " It is recorded as the thing that did not arrive, so it reaches the summary as unobserved rather than as a default that looks like data.",
    ] as Rich,
    [
      { b: "The store is fingerprinted before and compared after." },
      " The promise that a run leaves the machine as it found it is asserted on the path most likely to break it — the one case that rewrites a real setting.",
    ] as Rich,
    [
      { b: "A failing step carries its diagnosis." },
      " The tree as it was, the element's facts, and what its patterns read at the moment it did not hold.",
    ] as Rich,
  ],
};

/* ------------------------------------------------------------------ capture */

export const capture = {
  eyebrow: "Capture",
  heading: "A picture that proves what it photographed",
  intro: [
    "A screenshot is evidence only if something rules out the ways it can lie. Each of these was a real session that exited zero with a file nobody could use, so each is a refusal rather than a warning.",
  ] as Rich,
  steps: [
    {
      n: "1",
      title: "Nothing is over the rectangle",
      body: [
        "The z order above the window is enumerated and every frame intersected with the copy rectangle, which answers for the whole area in one pass — sampled points cannot — and ",
        { b: "names the intruder, its process and the rectangle it covers" },
        ".",
      ] as Rich,
    },
    {
      n: "2",
      title: "An overlap fails rather than crops",
      body: [
        "A copy trimmed around an intruder is a picture of something nobody asked for. So the refusal is the answer, and it is actionable instead of mysterious.",
      ] as Rich,
    },
    {
      n: "3",
      title: "The compositor is asked about the glass",
      body: [
        "A window with a system backdrop transmits what is behind it, and z-order reasoning cannot answer for that. A window that opted into one is refused rather than merely warned about.",
      ] as Rich,
    },
    {
      n: "4",
      title: "One flat colour is not a picture",
      body: [
        "A copy of exactly one colour is what a session where nothing was rendering produced — and it was written, reported as a capture, and exited zero.",
      ] as Rich,
    },
    {
      n: "5",
      title: "A page still computing is not a report",
      body: [
        "The loading strings are read from the project's own language files ",
        { b: "before anything launches" },
        ", so a key none of them carries refuses the run instead of matching nothing.",
      ] as Rich,
    },
    {
      n: "6",
      title: "Two renders of unchanged code are byte-identical",
      body: [
        "Which is what makes a difference a real difference, and means nobody has to choose a tolerance for a comparison to mean anything.",
      ] as Rich,
    },
  ],
  note: [
    "A render is measured, arranged and updated in that order. The arrange is why the verb exists: a tree that was measured and never arranged renders as a ",
    { b: "fully transparent picture of exactly the right size" },
    ", which looks like a drawing bug and is a calling bug.",
  ] as Rich,
};

/* ------------------------------------------------------------------ scenario */

export const scenario = {
  eyebrow: "The scenario",
  heading: "A case is a data file, not two hundred lines of script",
  intro: [
    "A scenario file is a ",
    { code: ".cases.json" },
    " — an object with ",
    { code: "cases" },
    " in it, and optionally the ",
    { code: "fixtures" },
    " they are launched against. A run is pointed at a directory and walks it. Steps, locators, acts and expectations are fields; the loop, the waits, the attempts and the verdict belong to ",
    { code: "CaseRun" },
    ". What is left in the file is the part that is actually about your application — including the defect the case exists to catch.",
  ] as Rich,
  fileTitle: "cases/report.cases.json",
  list: [
    [
      { b: "Every field is validated against the loader's own schema" },
      ", so a refusal costs a retry and never a deletion — which is what matters most to whoever is writing the file a field at a time.",
    ] as Rich,
    [
      { b: "Run everything, one case by name, or one tag" },
      " — and the run names every case it left alone, so a pass over two of nine never reads as a pass. A selector matching nothing is refused with the names there are.",
    ] as Rich,
    [
      { b: "A fixture carries the launch" },
      " — its sampled environment, the flag that environment arrives through, the arguments, the variables, and the language the window it opens is in. Every launch a case makes reads the same one.",
    ] as Rich,
    [
      { code: "needs" },
      " ",
      { b: "declares what the machine must have" },
      ", by the name the engine gives the condition — so an absent one is named as unchecked rather than going red for a reason about the desk it ran on.",
    ] as Rich,
    [
      { code: "shareable" },
      " ",
      { b: "on the fixture and" },
      " ",
      { code: "onlyReads" },
      " ",
      { b: "on the case" },
      " are the two halves of lending one window to the cases that merely read it, while the same case run alone still owns its process and its first paint.",
    ] as Rich,
    [
      { code: "forEach" },
      " ",
      { b: "runs a case once per string a key declares" },
      ", derived from the project's own language files, with the member reaching a locator through ",
      { code: "{}" },
      " — so twenty-seven panels are one case rather than twenty-seven.",
    ] as Rich,
    [
      { code: "catches" },
      " ",
      { b: "is the defect the case exists for" },
      ". A case that names none is counted in the run's own reading, so a check nobody can justify is visible and one removed by accident is missed.",
    ] as Rich,
  ],
};

/* ------------------------------------------------------------------ project file */

export const project = {
  eyebrow: "Declaring a project",
  heading: [
    { code: "winwright.json" },
    ", found by walking up from where a run starts",
  ] as Rich,
  intro: [
    "Every key is optional. A reading that needs one this file does not declare is ",
    { b: "recorded as not taken" },
    ", never quietly skipped — which is the same rule as everything else here, applied to configuration.",
  ] as Rich,
  note: [
    { code: "destructive" },
    " names the entries that end the run. Write ",
    { code: '{"id": …}' },
    " or ",
    { code: '{"key": …}' },
    " rather than a bare name where the project ships more than one language: a name is the field a translation rewrites, and a safety check compared against text a person sees expires the moment somebody translates the application.",
  ] as Rich,
};

/* ------------------------------------------------------------------ in-app */

export const inApp = {
  eyebrow: "What needs the application to cooperate",
  heading: "Nothing above this line does",
  intro: [
    "Every reading and every pattern act so far runs against an application that references nothing. What the in-app half adds is the handful of readings a harness ",
    { b: "cannot" },
    " take from outside the process — and it references the engine not at all, so nothing here ships a test harness to your users.",
  ] as Rich,
  list: [
    [
      { b: "Coordinates" },
      " — whether this process's idea of the display is trustworthy, in a sentence a report prints. A picture drawn by a system-aware process on a scaled display has a size that does not mean what it says, and nothing else about the file would ever say so.",
    ] as Rich,
    [
      { b: "Render" },
      " — an element to a PNG, measured, arranged and updated in that order.",
    ] as Rich,
    [
      { b: "Backgrounds" },
      " — what a capture is drawn on, from a brush the application declares under ",
      { code: "WinwrightCaptureBackground" },
      ", or the window's own. The system palette is not consulted at all: it answers white on a machine whose window is dark.",
    ] as Rich,
    [
      { b: "Geometry and surfaces" },
      " — the laid-out tree and what was drawn, written only where the harness asked. An application shipped to its users reports nothing and writes no file.",
    ] as Rich,
    [
      { b: "Popups" },
      " — every popup under a window held open for as long as a run lasts. A preview has no hand to click with, and fixing that at one call site leaves the next popup to rediscover it.",
    ] as Rich,
    [
      { b: "Freezables and apartment" },
      " — a brush that may cross to a capture thread, and bounded work on the application's own dispatcher.",
    ] as Rich,
  ],
};

/* ------------------------------------------------------------------ refusals */

export const refusals = {
  icon: "⛔",
  heading: "The value is concentrated in the refusals",
  body: [
    [
      "Every one of them is paired in the suite with the thing that provokes it — a fixture flag, or a stated reason no flag can. A refusal nobody has ever seen fire is a refusal nobody can rely on.",
    ] as Rich,
    [
      "Among them: a locator that does not parse, two elements matching one step, an element that cannot take the act, a declared destructive entry reached without saying you meant it, a picture nothing drew, a render of a tree that lays out to nothing, a capture of a window this run is not driving, a run that changed the machine of whoever ran it, a verdict assembled wrongly, and a trace that is not a trace.",
    ] as Rich,
  ],
};

/* ------------------------------------------------------------------ non-goals */

const nonGoalItems = [
  {
    title: "Cross-platform",
    body: "The whole engine is UI Automation and Win32. Portability would mean a different tool wearing the same name, and it is not coming.",
  },
  {
    title: "An external dependency in the engine",
    body: "A package in the engine is a package every adopting project inherits. The two halves are separate assemblies precisely so an application takes the one it needs without inheriting the other.",
  },
  {
    title: "An assertion about individual pixels",
    body: "A picture is evidence that something was drawn and that nothing was over it. What a pixel is worth arguing about belongs to a design review, not to a suite.",
  },
  {
    title: "A recorder that turns clicks into a scenario",
    body: "A recorded case asserts what happened rather than what matters, and nobody can tell the two apart afterwards. The tool never writes the test.",
  },
  {
    title: "A service, a daemon or a database",
    body: "A run is a process that starts, observes, reports and stops. Nothing here outlives it, and nothing here is installed on the machine that hosts it.",
  },
];

export const nonGoals = {
  eyebrow: "Scope",
  heading: "What it is not",
  intro: [
    `${Spelled(nonGoalItems.length)} things winwright has decided against, written down where they can be pointed at. A tool with no stated non-goals is a tool that will eventually be asked for all of them.`,
  ] as Rich,
  items: nonGoalItems,
};

/* ------------------------------------------------------------------ the agent teaser */

export const agent = {
  eyebrow: "For the agent driving it",
  heading: "It ships as a Claude Code plugin",
  intro: [
    "Two commands in the repository wire every clone, and nothing is added to any path. Four MCP tools answer the format, the vocabulary, whether a case would load, and what a run of it did — and the one that reads a case back carries the ",
    { b: "loader's own schema as its input schema" },
    ", so a misspelt key is not a thing the caller can send. The skill loads when a window is in play rather than on every turn, and a hook denies a hand-written harness script and names the verb that replaces it.",
  ] as Rich,
  note: [
    "Three of the four launch nothing and press nothing, which is the distinction a session needs first: whether a case would load is a claim nothing about the machine can change, and whether it passed is not.",
  ] as Rich,
  cta: "What the plugin does, tool by tool →",
};

/* ------------------------------------------------------------------ feature index */

export const featureIndex = {
  eyebrow: "In depth",
  heading: "One page per pillar, for whoever is deciding",
  intro: [
    "Each of these is the argument for one part of the tool, at the length the argument actually takes. The summaries above are the same claims, shorter.",
  ] as Rich,
  go: "Read it →",
};

/* ------------------------------------------------------------------ install */

export const install = {
  eyebrow: "Get it",
  heading: "Two package references, and one of them is optional",
  intro: [
    "Both halves are on nuget.org at ",
    { code: version },
    ". The harness half goes in the project that drives the application; the in-app half goes in the application, and only if you want the readings that can only be taken from inside — every verb on this page works without it.",
  ] as Rich,
  facts: [
    "🪟 Windows only — the engine is UI Automation and Win32",
    `📦 .NET 10, ${"net10.0-windows"} · the in-app half needs UseWPF`,
    "🧩 No external dependency in either half",
  ],
  cta: "⬇ Get the packages",
  ctaShort: "⬇ Get it",
  secondary: "Release notes",
  // The one adoption step that is genuinely easy to miss, named here rather than left to be
  // discovered: a project at the repository root compiles the driving project into itself.
  note: [
    "One line if your application's ",
    { code: ".csproj" },
    " sits at the repository root: add ",
    { code: String.raw`<DefaultItemExcludes>$(DefaultItemExcludes);tests\**</DefaultItemExcludes>` },
    " to it. Without it the SDK's default globs walk the whole tree below the project file and compile the driving project ",
    { b: "into the application" },
    " — and the build stops with duplicate-attribute errors that point at your own generated file rather than at the folder that caused them.",
  ] as Rich,
  agentLead: "And in the repository that drives the application, if you use Claude Code:",
  agentCommands: [
    "claude plugin marketplace add alegauss/winwright --scope project",
    "claude plugin install winwright@alegauss --scope project",
  ],
  agentNote: [
    "Both write into that repository's ",
    { code: ".claude/settings.json" },
    ", so committing that file wires every clone. There is no per-machine install and nothing added to any path.",
  ] as Rich,
};

/* ------------------------------------------------------------------ release */

// The band a finished product owes a reader: which version is published, what it is
// licensed under, where the notes are, and where to say something is wrong. Every figure
// here is the generated one, so the section cannot state a version the tree does not.
export const release = {
  eyebrow: "Release",
  heading: `${harnessPackage().id} ${version}`,
  intro: [
    "Both halves are published together and carry the same version, and the release is checked against the version the source declares — so a package and the commit it was built from cannot disagree about which build it is.",
  ] as Rich,
  facts: [
    { k: "Version", v: version },
    { k: "Frameworks", v: "net10.0-windows · Windows only" },
    { k: "Licence", v: "Apache-2.0", href: licenceUrl },
    { k: "Dependencies", v: "none in either half" },
  ],
  links: [
    { href: nugetUrl(harnessPackage().id), label: `${harnessPackage().id} on nuget.org` },
    { href: nugetUrl(inAppPackage().id), label: `${inAppPackage().id} on nuget.org` },
    { href: changelogUrl, label: "Changelog" },
    { href: docsUrl, label: "Documentation" },
    { href: issuesUrl, label: "Report a problem" },
  ],
  note: [
    "The suite that holds every guarantee on this page creates real windows, takes the foreground and synthesises input — so a bare ",
    { code: "dotnet test" },
    " over it takes the roll call as part of the run: a suite short of what discovery found is not reported as a pass. The rule this framework exists for is applied to this framework.",
  ] as Rich,
};

/* ------------------------------------------------------------------ /claude-code page */

export const claudeCode = {
  meta: {
    title: "winwright for Claude Code — the plugin, the tools, the skill and the hook",
    description:
      "Two commands wire every clone: four MCP tools, one of which carries the scenario loader's own schema as its input schema, a skill that loads when a window is in play, and a hook that denies a hand-written harness script and names the verb that replaces it.",
    ogTitle: "winwright for Claude Code",
    ogDescription:
      "A case an agent writes is refused at insertion rather than at run time, and a hand-rolled harness script is denied with the verb that replaces it.",
  },
  eyebrow: "For the agent driving it",
  heading: "Wired by the repository, not by each machine",
  intro: [
    "Adoption that depends on somebody remembering a per-machine install is adoption that stops at the person who set it up. winwright ships as a Claude Code plugin: two commands in the repository wire it, every clone is wired, and nothing is added to any path.",
  ] as Rich,
  statusLead: "tools, arriving as schemas rather than as prose somebody has to read and retype",
  status: [
    "Which is the difference between a refusal and a guess: an agent that writes a case field by field is corrected at insertion, where the fix costs a retry, rather than at run time, where it costs a red run somebody has to read.",
  ] as Rich,
  setupHeading: "The two commands, and the one step they do not cover",
  setupLead: "Run once, in the repository that drives the application:",
  setupCommands: [
    "claude plugin marketplace add alegauss/winwright --scope project",
    "claude plugin install winwright@alegauss --scope project",
  ],
  setupNote: [
    "Both write into that repository's ",
    { code: ".claude/settings.json" },
    ", so committing that file wires every clone. The server and the guard are .NET processes the plugin launches, so they are built once — ",
    { code: "dotnet build -c Release" },
    " in the plugin's own clone. ",
    { b: "Skip it and you are told, not left guessing" },
    ": each is wired through a launcher that looks for its assembly, and where there is none it writes the missing surface and the build command to stderr and exits 1. Never 2 — denying every write because a build is missing would put the guard in front of everything instead of in front of a harness script.",
  ] as Rich,
  readHeading: "Reading — nothing is launched, nothing is pressed",
  read: [
    { k: "winwright_format", d: "every field of a file, a case, a step and a fixture, whether it is required, and the closed list of what it accepts" },
    { k: "winwright_vocabulary", d: "every act, what each one needs said beside it, and whether the engine may repeat it" },
    { k: "winwright_check", d: "a case read back before the file exists — either the loader's own refusal, addressed as cases[0].steps[1].act, or what a run of it would do" },
  ],
  doHeading: "Running — it launches the application",
  do: [
    { k: "winwright_run", d: "the cases a selection asks for: the verdict, a line per case that ran and per case it left alone, the exit code, and what outlived the run" },
  ],
  splitNote: [
    "The split is the whole reason the last one is a separate tool. Whether a file parses is a claim nothing about the machine can change; whether it passed is not — and a desk that cannot observe answers a ",
    { b: "hole" },
    ", naming which condition is missing, rather than a red.",
  ] as Rich,
  hookHeading: "The hook that keeps the shortcut closed",
  hookBody: [
    "A hand-written harness script is always available and always faster in the moment, and that is exactly how a 2,732-line one happens. So the plugin registers a ",
    { code: "PreToolUse" },
    " hook: a write whose content names the engine's acting, locating or asserting namespaces is ",
    { b: "denied" },
    ", and the refusal names the case file and the tool that replace it. It arrives before the work rather than after it — the difference between being asked to write the other thing and being asked to delete what you just wrote.",
  ] as Rich,
  hookStaysHeading: "And it stays out of its own way in three places",
  hookStays: [
    [
      "A scenario-file write is never denied — that is the verb it is pointing at.",
    ] as Rich,
    [
      "A project referencing the engine's ",
      { b: "source" },
      " is never denied: a suite that drives windows on purpose is the one place a harness belongs, and a guard you turn off to work on the tool is a guard whose false denials nobody hears about.",
    ] as Rich,
    [
      "Anything it cannot read, it allows. A hook that denies what it did not understand is one that gets removed, after which nothing is guarded at all.",
    ] as Rich,
  ],
  skillHeading: "The skill that is not always loaded",
  skillBody: [
    "It loads when a window is in play rather than on every turn, and what it says is which loop answers which question — which is the whole of what an agent needs in order to reach the right tool instead of the nearest one.",
  ] as Rich,
  refusesHeading: "What it deliberately refuses",
  refusesLead: [
    "winwright is the substrate; the intelligence is the caller's. It reads trees, presses controls through their own patterns and assembles a verdict — it does not decide what is worth asserting.",
  ] as Rich,
  refuses: [
    { t: "No model", b: "It calls no LLM." },
    { t: "No prompts", b: "It stores none." },
    { t: "No recorder", b: "Clicks never become a case." },
    { t: "No invented assertion", b: "The tool never writes the test." },
    { t: "No second desk", b: "It drives the one you are on." },
    { t: "No telemetry", b: "Nothing is measured or sent." },
  ],
};

/* ------------------------------------------------------------------ /claude-code: friction */

export const friction = {
  eyebrow: "The friction this removes",
  heading: "What a hand-rolled harness costs, and what replaces it",
  intro: [
    "None of this is a defect in the tools it names. Every one of them is what a general-purpose automation library does to somebody who has to answer, at the end of a run, whether the thing they were checking was actually checked.",
  ] as Rich,
  todayLabel: "the script you have",
  hereLabel: "here",
  items: [
    {
      t: "A pass and a check that never ran are the same colour",
      today: {
        cmd: "Assert.True(found is not null)",
        body: [
          "A precondition that was absent throws, is caught, and turns into a skip nobody counts — or worse, into a branch that returns early and reports nothing at all. The total moves and the colour does not.",
        ] as Rich,
      },
      here: {
        cmd: `${verdict("Degraded").name.toUpperCase()} (exit ${verdict("Degraded").code}) - 1 unchecked (all the desk's)`,
        body: [
          "The unevaluated reading is named in the summary, whose the absence was is named beside it, and the process exits ",
          { code: `${verdict("Degraded").code}` },
          " — a number CI can act on without anybody reading a word of the output.",
        ] as Rich,
      },
    },
    {
      t: "The locator is a different string in every verb",
      today: {
        cmd: 'FindFirst(TreeScope.Children, new AndCondition(...))',
        body: [
          "A condition tree is written per call site, so the same control is addressed four different ways in four places, and the one that breaks is the one nobody recognises as the same control.",
        ] as Rich,
      },
      here: {
        cmd: "Window#main > Pane > Button#save",
        body: [
          "One grammar, read the same way by every verb, refused at parse time with the position and the reason rather than at run time with a null.",
        ] as Rich,
      },
    },
    {
      t: "Every act is a click, so every act needs the screen",
      today: {
        cmd: "SetForegroundWindow(h); MoveTo(x, y); Click()",
        body: [
          "Which means the run owns the desk, a notification that steals the foreground is a failure, and the same script cannot be run on the machine somebody is working on.",
        ] as Rich,
      },
      here: {
        cmd: "Act.Invoke(subject)",
        body: [
          "A pattern act asks the control through its own accessibility peer and ",
          { b: "needs no foreground" },
          ". The verbs that do synthesise input are marked as such in the catalogue rather than discovered on a red run.",
        ] as Rich,
      },
    },
    {
      t: "The expected string is typed into the test",
      today: {
        cmd: 'Assert.Equal("Relatório mensal", label)',
        body: [
          "A second copy of the truth, and it is the copy that goes stale — silently, on the day somebody edits the language file and nobody edits the test.",
        ] as Rich,
      },
      here: {
        cmd: '"covers": "report.labels"',
        body: [
          "The expected set is derived from the application's own language files, so switching the resolved language switches the expectation with it — and a key none of the files carries refuses the run rather than matching nothing. Every value in the set carries the file and line it came from.",
        ] as Rich,
      },
    },
    {
      t: "A screenshot is written whatever it contains",
      today: {
        cmd: "CopyFromScreen(bounds); bmp.Save(path)",
        body: [
          "A dialog over the window, a backdrop transmitting what is behind it, a page still computing, or nothing rendering at all — each writes a file, and each exits zero.",
        ] as Rich,
      },
      here: {
        cmd: "1 window(s) stand over 376x166 at 62,90, taking 15360 of its 62416 pixel(s): 'Update available' (pid 14820) over 240x64 at 194,92",
        body: [
          "Every way the picture can lie is checked, and the reading names the intruder, its process and the rectangle it covers — rather than cropping around it and writing the file anyway.",
        ] as Rich,
      },
    },
    {
      t: "A case is two hundred lines that mostly repeat the previous case",
      today: {
        cmd: "// twenty-seven copies of one runner",
        body: [
          "The loop, the waits and the verdict logic are re-authored per case, so a fix to any of them is applied to some of the copies and the rest keep the bug.",
        ] as Rich,
      },
      here: {
        cmd: "report.cases.json",
        body: [
          "Steps, locators, acts and expectations are fields; the loop, the waits, the attempts and the verdict belong to the engine. What is left in the file is the part that is about your application.",
        ] as Rich,
      },
    },
  ],
  footer: [
    "The run on the home page is a case going through these verbs. The line that matters is the last one — and it is the line a hand-rolled harness has no way to print.",
  ] as Rich,
};

/* ------------------------------------------------------------------ /compare page */

export const compare = {
  meta: {
    title: "winwright vs Playwright, Appium, WinAppDriver, FlaUI and a hand-rolled harness",
    description:
      "What winwright does that a browser driver, an Appium stack, WinAppDriver, FlaUI or your own harness script does not — and what each of those is genuinely better at.",
    ogTitle: "winwright — the honest comparison",
    ogDescription:
      "Checkable rows grouped by the law each comes from, and a column for what every alternative wins.",
  },
  eyebrow: "Against the alternatives",
  heading: "What this does that the others do not — and where they win",
  intro: [
    "You arrive having already decided against something, or already using it. A matrix that wins every row is one nobody believes, so this one is grouped by the law each row comes from, and every alternative keeps the column it genuinely wins.",
  ] as Rich,
  columns: ["winwright", "Playwright", "Appium", "WinAppDriver", "FlaUI", "Your script"],
  legend: [
    { sym: "✓", label: "yes" },
    { sym: "~", label: "partial" },
    { sym: "✗", label: "no" },
  ],
  groups: [
    {
      law: "The verdict",
      rows: [
        { cap: "A third outcome for what could not be evaluated", cells: ["✓", "✗", "✗", "✗", "✗", "✗"] },
        { cap: "Harness failure ranked apart from a test failure", cells: ["✓", "~", "~", "✗", "✗", "✗"] },
        { cap: "A run short of what discovery found is not a pass", cells: ["✓", "✗", "✗", "✗", "✗", "✗"] },
      ],
    },
    {
      law: "Reaching a Windows desktop app",
      rows: [
        { cap: "Drives a native Windows desktop application", cells: ["✓", "✗", "~", "✓", "✓", "✓"] },
        { cap: "No driver process or server to install and keep alive", cells: ["✓", "✗", "✗", "✗", "✓", "✓"] },
        { cap: "The notification area, its overflow and an icon's menu", cells: ["✓", "✗", "✗", "✗", "~", "~"] },
      ],
    },
    {
      law: "Acting",
      rows: [
        { cap: "Acts through the control's own patterns, no foreground", cells: ["✓", "✓", "~", "~", "✓", "~"] },
        { cap: "What each act needs is checked before anything is pressed", cells: ["✓", "~", "✗", "✗", "✗", "✗"] },
      ],
    },
    {
      law: "Evidence",
      rows: [
        { cap: "A capture refuses when something covers the rectangle", cells: ["✓", "✗", "✗", "✗", "✗", "✗"] },
        { cap: "Expectations derived from the app's own declarations", cells: ["✓", "✗", "✗", "✗", "✗", "✗"] },
        { cap: "A run is asserted to have left the machine unchanged", cells: ["✓", "✗", "✗", "✗", "✗", "✗"] },
      ],
    },
    {
      law: "Breadth — where a rival wins",
      rows: [
        { cap: "Browsers, and a mature browser ecosystem", cells: ["✗", "✓", "~", "✗", "✗", "✗"] },
        { cap: "macOS, Linux, Android, iOS", cells: ["✗", "✓", "✓", "✗", "✗", "✗"] },
        { cap: "A WebDriver protocol other tools already speak", cells: ["✗", "✓", "✓", "✓", "✗", "✗"] },
        { cap: "A large existing body of examples and answers", cells: ["✗", "✓", "✓", "~", "~", "✗"] },
      ],
    },
  ],
  winsHeading: "What each alternative is genuinely better at",
  wins: [
    { name: "Playwright", body: "The browser, and everything around it — tracing, fixtures, a large ecosystem and years of answers. Pick it for web, and note that the name here is a nod rather than a comparison." },
    { name: "Appium", body: "One protocol across mobile and desktop, and a driver for platforms this will never reach. Pick it when the same suite has to drive more than Windows." },
    { name: "WinAppDriver", body: "WebDriver over a Windows app, so tools that already speak that protocol work unchanged. Pick it when the protocol is the requirement." },
    { name: "FlaUI", body: "A mature, well-liked .NET wrapper over UI Automation with a wide surface and no opinions to argue with. Pick it when you want the library rather than the discipline." },
    { name: "Your own script", body: "It already exists and it already knows your application. Pick it — right up to the run where the total moves and the colour does not." },
  ],
  winsFooter: [
    "What is left is the axis where this one wins: an answer that distinguishes a check that failed from a check that never ran, one grammar every verb reads, expectations derived rather than typed, and a picture that refuses rather than lies.",
  ] as Rich,
};
