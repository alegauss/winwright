import type { Rich } from "./site-content";
import { Spelled, spelled, verdict, verdictCount } from "./product";

// The depth pages, one record each. The route, the title and the description are all read
// off the same record (in routes.tsx), so a new pillar cannot ship half-declared or
// untitled: add a record here and its route, its <head> and its page all appear together,
// or none of them do.
//
// The counts in these records are the same counts the landing page states, and they come
// from the same generated module: a depth page is where a reader goes to check the summary,
// and the two disagreeing is worse than either being stale alone.

export interface FeatureSection {
  heading: string;
  body?: Rich;
  list?: Rich[];
}

export interface FeatureRecord {
  slug: string;
  title: string;
  description: string;
  ogTitle: string;
  ogDescription: string;
  eyebrow: string;
  heading: string;
  lead: Rich;
  /** a diagram key resolved to markup in the page component */
  figure?: "tree" | "capture" | "verdictTerminal" | "scenarioFile";
  sections: FeatureSection[];
}

export const features: FeatureRecord[] = [
  {
    slug: "verdict",
    title: `The verdict — ${spelled(verdictCount)} outcomes, and the values are the exit codes`,
    description:
      `A run ends in one of ${spelled(verdictCount)} outcomes whose enum values are the process exit codes. ${verdict("Degraded").name} (${verdict("Degraded").code}) is what an assertion that could not be evaluated produces, named rather than dropped.`,
    ogTitle: "winwright — the verdict",
    ogDescription: `${Spelled(verdictCount)} outcomes, one of which exists so a check that never ran cannot be reported as one that passed.`,
    eyebrow: "The answer",
    heading: "The verdict",
    lead: [
      "A suite reported a pass with no failures and a total of 352, where the run before it had 374. Twenty-two checks were gone, the host had died partway through, and the only sign was a number nobody had a reason to read. Everything here follows from refusing that.",
    ],
    figure: "verdictTerminal",
    sections: [
      {
        heading: "The member values are the exit codes",
        body: [
          "Not translated into them — a mapping written twice is a mapping that drifts, and CI reads the number rather than the word. ",
          { code: "0" },
          " passed, ",
          { code: "1" },
          " failed, ",
          { code: `${verdict("Degraded").code}` },
          " degraded, ",
          { code: `${verdict("Broken").code}` },
          " broken. The last outranks the rest, because a reader told the build failed opens the wrong repository.",
        ],
      },
      {
        heading: "Three holes that are about the desk, not your application",
        list: [
          ["A foreground Windows would not grant."],
          ["A focus that left the application while a menu walk or a traversal was polling."],
          ["A notification-area flyout the shell would not open."],
          [
            "None of them is your code being wrong, so none goes red — the answer names what the desk did instead.",
          ],
        ],
      },
      {
        heading: "One reading of the machine, before anything is asserted",
        body: [
          "The desk it is on, which binary it is driving, whether that binary is stale, the resolved language, the foreground, the launch arguments, whether anything else is showing the application, and whether the desk is this run's alone. Each is reported as measured, absent, or ",
          { b: "not read" },
          " — an absent line and a missing line read the same to somebody skimming, and only one of them is a statement.",
        ],
      },
      {
        heading: "The roll call is part of the run",
        body: [
          "A run short of what discovery found is not reported as a pass. That is the original defect's own shape, so it is the one check that cannot be left to a convention.",
        ],
      },
    ],
  },
  {
    slug: "locate",
    title: "Locators — one grammar, read the same way by every verb",
    description:
      "A locator is a string that parses or is refused with the position and the reason. The combinator means a descendant of, at any depth, because the panes UI Automation inserts differ between frameworks and between a maximised window and a restored one.",
    ogTitle: "winwright — the locator grammar",
    ogDescription: "One grammar, a refusal that names the position, and a tree a person can read.",
    eyebrow: "Addressing an element",
    heading: "Locators",
    lead: [
      "One grammar, written once, read the same way by every verb — and refused at parse time rather than answered with a null at run time.",
    ],
    figure: "tree",
    sections: [
      {
        heading: "What a locator can say",
        list: [
          ["The automation id, the control type, or both."],
          ["The name, the window class, or a pattern the control must carry."],
          ["An order and an index — the leftmost of the ones that match, the second from the top."],
          ["A descendant chain, where each step is a descendant of the last at any depth."],
        ],
      },
      {
        heading: "Why the combinator is not a direct child",
        body: [
          "UI Automation wraps controls in panes that differ between frameworks, between versions of one framework, and between a maximised window and a restored one. A direct-child locator is therefore the one that passes here and breaks on somebody else's machine — which is the worst failure mode available to a locator, because it looks like flakiness rather than like a decision.",
        ],
      },
      {
        heading: "Refusals, not first matches",
        list: [
          ["A locator that does not parse names the position and the reason."],
          [
            { code: "Locator.TryParse" },
            " answers without throwing, for a caller collecting refusals rather than stopping at the first.",
          ],
          ["Two elements matching one step is a refusal — a quiet first-match is a test that passes against the wrong control until the order changes."],
          ["A misspelt control type or pattern is answered with the nearest name UI Automation actually has, because the vocabulary is read from UI Automation rather than from a list kept here by hand."],
        ],
      },
      {
        heading: "And the tree is readable",
        body: [
          "The control view under a window or an element comes back as a tree, or as lines a person reads. Which is the difference between “it did not find it” and a question with an answer.",
        ],
      },
    ],
  },
  {
    slug: "act",
    title: "Acts — patterns before pointers",
    description:
      "Invoke, toggle, set a value or a range, select, expand, collapse — through the control's own patterns, which needs no foreground. The verbs that synthesise input are marked as such in the catalogue rather than discovered on a red run.",
    ogTitle: "winwright — acting",
    ogDescription: "A pattern act needs no foreground; a preflight checks what every declared act needs before anything is pressed.",
    eyebrow: "Acting on it",
    heading: "Acts",
    lead: [
      "A pattern act is the default. It asks the control through its own accessibility peer rather than asking the desktop to move a mouse — so a run does not need the screen to itself.",
    ],
    sections: [
      {
        heading: "What an act does",
        list: [
          ["Invoke, toggle, set a value or a range, select, expand, collapse."],
          ["Select and confirm; every value a picker holds, and reaching one."],
          ["Record the controls as a case found them, and put them back."],
          ["Each attempt is capped and counted, so “attempt 3 of 3” is in the record rather than in somebody's memory."],
        ],
      },
      {
        heading: "Checked before, not discovered after",
        body: [
          "Actionability answers whether an element can take an act at all, and where it cannot, why not. The preflight takes every ",
          { b: "declared" },
          " act in a case and checks what it needs against the tree before anything is pressed — which turns a red run halfway through into a refusal at the start.",
        ],
      },
      {
        heading: "The verbs that do need the foreground",
        body: [
          "Synthesised mouse and keyboard input exist for the controls with no peer worth asking, and every one of them is marked as needing the foreground in the catalogue. The catalogue is checked against the engine in both directions: a verb added without an entry is a red.",
        ],
      },
      {
        heading: "Menus, traversal and the notification area",
        list: [
          ["Enter a menu bar the way a keyboard user does, walk to an entry, open a submenu, dismiss."],
          ["The walk reports its hop count, which is what makes a timing observation mean anything."],
          ["The tray, the overflow flyout, the icons on either, and an icon's context menu."],
          [
            "Finding an icon says ",
            { b: "which" },
            " of the two happened — the icon is absent, or the flyout would not open — because a caller reading a null cannot tell them apart.",
          ],
        ],
      },
      {
        heading: "The one act that has to be meant",
        body: [
          "A declared destructive entry reached without saying you meant it is refused. And a bare name is refused as a declaration where the project ships more than one language: a name is the field a translation rewrites, so a safety check compared against text a person sees expires the moment somebody translates the application.",
        ],
      },
    ],
  },
  {
    slug: "assert",
    title: "Assertions — derived, never typed",
    description:
      "Expectations are read from what the application itself declares — its language files, its layout, its own read-out — so switching the resolved language switches the expectation with it and a retyped string cannot go stale in silence.",
    ogTitle: "winwright — asserting",
    ogDescription: "A string retyped into a test is a second copy of the truth, and the second copy is the one that goes stale.",
    eyebrow: "Asserting",
    heading: "Assertions",
    lead: [
      "The expected set is derived from what the application declares about itself. A string retyped into a test is a second copy of the truth, and the second copy is the one that goes stale.",
    ],
    sections: [
      {
        heading: "Derived from what, exactly",
        list: [
          ["The project's own language files, for every label a person reads."],
          ["The laid-out tree, for anything about arrangement."],
          ["The application's own read-out, where it publishes one."],
          ["And a derived set names what it derived from, because an expectation nobody can trace is one somebody will eventually “fix” by editing the number."],
        ],
      },
      {
        heading: "Falsifiable, or it is not an assertion",
        body: [
          "A check that cannot fail is worth exactly as much as the one that never ran — which is the same defect this project's verdict exists to expose, one layer down. So an expectation that no input could falsify is refused rather than counted.",
        ],
      },
      {
        heading: "A read that timed out is a reading",
        body: [
          "Not a value, and not a default that looks like data. It is recorded as the thing that did not arrive, and it reaches the summary as unobserved — which is the only honest place for it.",
        ],
      },
      {
        heading: "A failing step carries its diagnosis",
        body: [
          "The tree as it was, the element's facts, and what its patterns read at the moment the expectation did not hold. Which is what a reader would otherwise write a throwaway script to find out, on a window that has since closed.",
        ],
      },
      {
        heading: "And the machine is compared to itself",
        body: [
          "The store a case writes through is fingerprinted before the run and compared after it, so the promise that a run leaves the machine as it found it is asserted on the path most likely to break it rather than stated in a readme.",
        ],
      },
    ],
  },
  {
    slug: "capture",
    title: "Capture — a picture that proves what it photographed",
    description:
      "Every way a screenshot can lie is a refusal: something over the rectangle, a system backdrop transmitting through the glass, one flat colour, a page still computing, or a window this run is not driving.",
    ogTitle: "winwright — capture",
    ogDescription: "An overlap fails rather than crops, and the refusal names the intruder, its process and the rectangle.",
    eyebrow: "Capture",
    heading: "Capture",
    lead: [
      "A screenshot is evidence only if something rules out the ways it can lie. Each refusal below was a real session that exited zero with a file nobody could use.",
    ],
    figure: "capture",
    sections: [
      {
        heading: "What is checked before a file exists",
        list: [
          ["The z order above the window is enumerated and each frame intersected with the copy rectangle — one pass that answers for the whole area, where sampled points cannot."],
          ["An overlap fails rather than crops, and the refusal names the intruder, its process and the rectangle it covers."],
          ["A window that opted into a system backdrop is refused rather than warned about: it transmits what is behind it, and z-order reasoning cannot answer for glass."],
          ["A copy of exactly one colour is not a picture of a window — that is what a session where nothing was rendering produced, and it exited zero."],
          ["A page still computing is not a report: the loading strings are read from the project's own language files before anything launches."],
          ["A capture of a window this run is not driving is refused, because a picture of somebody else's desktop is not evidence about your application."],
        ],
      },
      {
        heading: "Two renders of unchanged code are byte-identical",
        body: [
          "Which is what makes a difference a real difference, and means nobody has to choose a tolerance for a comparison to mean anything. A change meant to be invisible finally has a cheap way to prove it was.",
        ],
      },
      {
        heading: "Measured, arranged, updated — in that order",
        body: [
          "The arrange is why the render verb exists. A tree that was measured and never arranged renders as a fully transparent picture of exactly the right size, which looks like a drawing bug and is a calling bug.",
        ],
      },
    ],
  },
  {
    slug: "scenario",
    title: "Scenarios — a case is a data file",
    description:
      "Steps, locators, acts and expectations are fields; the loop, the waits and the verdicts belong to the engine. Every field is validated at insertion, so a refusal costs a retry and never a deletion.",
    ogTitle: "winwright — the scenario",
    ogDescription: "A case is a data file that carries the defect it exists to catch.",
    eyebrow: "The scenario",
    heading: "Scenarios",
    lead: [
      "A case used to be two hundred lines of script that mostly repeated the previous case. What is left in the file now is the part that is actually about your application.",
    ],
    figure: "scenarioFile",
    sections: [
      {
        heading: "What the engine owns",
        body: [
          "The loop, the waits, the retries and the verdicts. Which is why a fix to any of them is applied once rather than to some of twenty-seven copies of a runner while the rest keep the bug.",
        ],
      },
      {
        heading: "What the file owns",
        list: [
          ["Steps, locators, acts and expectations, as fields."],
          ["The precondition, declared — so its absence is named as unchecked rather than going red for a reason about the desk."],
          ["The fixture and the sampled environments, passed to every launch the case makes rather than only the first."],
          ["Whether the window is shareable, so three cases that only read it do not each pay their own launch."],
          ["The defect the case exists to catch, so a case nobody can justify is visible and a case removed by accident is missed."],
        ],
      },
      {
        heading: "Validated at insertion",
        body: [
          "Every field, as it is written. A refusal costs a retry and never a deletion — which matters most for the caller writing the file a field at a time rather than pasting it whole.",
        ],
      },
      {
        heading: "Run one, and be told what you did not run",
        body: [
          "A file, a case or a tag. A single case is ten seconds when a single act is what changed, and the run says what it left out rather than reporting a total that quietly moved.",
        ],
      },
    ],
  },
  {
    slug: "in-app",
    title: "The in-app half — what only the application can answer",
    description:
      "An optional second package for the readings a harness cannot take from outside the process: coordinates, render, backgrounds, geometry, popups. It references the engine not at all, so nothing here ships a test harness to your users.",
    ogTitle: "winwright — the in-app half",
    ogDescription: "Optional, and deliberately so: every verb works against an application that references nothing.",
    eyebrow: "What needs cooperation",
    heading: "The in-app half",
    lead: [
      "Optional, and deliberately so. Every reading and every pattern act elsewhere on this site runs against an application that references nothing — this package buys the handful that cannot be taken from outside a process.",
    ],
    sections: [
      {
        heading: "What it adds",
        list: [
          ["Whether this process's idea of the display is trustworthy, in a sentence a report prints."],
          ["An element to a PNG — measured, arranged and updated in that order."],
          ["What a capture is drawn on, from a brush the application declares rather than from the system palette, which answers white on a machine whose window is dark."],
          ["The laid-out tree and what was drawn, written only where the harness asked."],
          ["Every popup under a window held open for as long as a run lasts — a preview has no hand to click with."],
          ["A brush that may cross to a capture thread, and bounded work on the application's own dispatcher."],
        ],
      },
      {
        heading: "Why it is a separate package",
        body: [
          "It references the engine not at all. An application that referenced the engine would ship a test harness to its users, and the two halves are separate assemblies precisely so an application takes the one it needs without inheriting the other.",
        ],
      },
      {
        heading: "And it is silent in a release",
        body: [
          "An application shipped to its users reports nothing and writes no file. That is what makes the protocol safe to leave in, which is the only version of this idea that survives contact with a release branch.",
        ],
      },
    ],
  },
];
