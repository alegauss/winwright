// The verbs, read out of the catalogue the suite is already gated on. WW489.
//
// Two tables in the README list the verb *families* in prose. They are the answer to "can this
// tool do the thing I need", and they are twenty-odd rows deep inside a file nobody can link a
// row of — and they leave out the column that decides an adoption: what each verb needs.
//
// A pattern act needs nothing of the desk and asks the control through its own accessibility
// peer. A synthesised act needs the foreground, and a run that did not get it answers a hole
// rather than a failure. That is the difference between a suite that runs on a build agent and
// one that only runs at somebody's desk, and today it is discovered on a red run.
//
// One source, and it is in the suite rather than the engine:
//   tests/Winwright.Tests/Cooperating.cs   every verb, against what it needs
//
// That is the point rather than an accident. The catalogue is checked against the engine in
// both directions — a verb added without an entry is a red — so this page is a read of a list
// this repository already fails over, and adding a verb does not cost a hand-edited row here.
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

import { bodyOf, code, constructions, documented, splitTop } from "./csharp.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const siteDir = join(here, "..");
const repoDir = join(siteDir, "..");

const catalogue = readFileSync(
  join(repoDir, "tests", "Winwright.Tests", "Cooperating.cs"),
  "utf8",
);

// What a verb may need of the application. Two members, and the page says what each means in
// the enum's own words: "the in-app half" is a package an adopter has to decide to ship.
const cooperation = documented(
  bodyOf(catalogue, "internal enum Cooperation", "{", "}", "Cooperating.cs"),
  (line) => /^([A-Z][A-Za-z]*),$/.exec(line)?.[1],
  "Cooperating.cs's Cooperation",
).map((one) => ({ kind: one.name, means: one.means }));

const known = new Set(cooperation.map((one) => one.kind));

/** A quoted literal, or several concatenated across the lines a long sentence wraps over. */
function sentence(expression, where) {
  return splitTop(expression, "+")
    .map((part) => {
      const literal = /^"([\s\S]*)"$/.exec(part.trim());
      if (!literal) throw new Error(`verbs: ${where} is not a sentence this can read: ${part.trim()}`);
      return literal[1].replace(/\\"/g, '"').replace(/\\\\/g, "\\");
    })
    .join("");
}

const verbs = constructions(
  bodyOf(code(catalogue), "public static IReadOnlyList<VerbNeeds> Known", "[", "]", "Cooperating.cs"),
).map((one) => {
  const args = splitTop(one, ",");
  if (args.length !== 4) {
    throw new Error(`verbs: an entry has ${args.length} arguments and a VerbNeeds has 4: ${one.trim().slice(0, 60)}`);
  }

  const named = /^"([A-Za-z]+)\.([A-Za-z]+)"$/.exec(args[0]);
  if (!named) throw new Error(`verbs: ${args[0]} is not a verb spelled Type.Method`);

  const needs = /^Cooperation\.([A-Za-z]+)$/.exec(args[1]);
  if (!needs || !known.has(needs[1])) {
    throw new Error(`verbs: ${named[0]} needs ${args[1]}, which Cooperation does not declare`);
  }
  if (args[2] !== "true" && args[2] !== "false") {
    throw new Error(`verbs: ${named[0]} says ${args[2]} about the desk, which is neither true nor false`);
  }

  return {
    family: named[1],
    member: named[2],
    named: `${named[1]}.${named[2]}`,
    needs: needs[1],
    desk: args[2] === "true",
    does: sentence(args[3], `${named[1]}.${named[2]}`),
  };
});

if (verbs.length === 0) throw new Error("verbs: Cooperating.Known catalogues nothing");

// A verb catalogued twice is two answers about one word, which is the drift the catalogue
// exists to stop — and the page would publish both rows under one anchor.
const seen = new Set();
for (const one of verbs) {
  if (seen.has(one.named)) throw new Error(`verbs: ${one.named} is catalogued twice`);
  seen.add(one.named);
}

// The three figures the page opens with, counted here rather than typed there. `Cooperating`
// prints the same sentence to a reader of the suite, and a number on a page that disagrees
// with it is the second spelling this whole file exists to avoid.
const anywhere = verbs.filter((one) => one.needs === "None" && !one.desk).length;
const counts = {
  verbs: verbs.length,
  families: new Set(verbs.map((one) => one.family)).size,
  anywhere,
  desk: verbs.filter((one) => one.desk).length,
  inApp: verbs.filter((one) => one.needs !== "None").length,
};

const out = join(siteDir, "docs", "src", "data", "verbs.generated.json");
mkdirSync(dirname(out), { recursive: true });
writeFileSync(out, `${JSON.stringify({ cooperation, verbs, counts }, null, 2)}\n`);

console.log(
  `verbs: ${counts.verbs} verb(s) in ${counts.families} famil(ies), ${counts.anywhere} against any`
    + ` application, ${counts.desk} needing a desk, ${counts.inApp} needing the in-app half`
    + " -> docs/src/data/verbs.generated.json",
);
