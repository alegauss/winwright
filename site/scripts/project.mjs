// The project declaration, read out of the type that decides it. WW490.
//
// `winwright.json` is the first file an adopting repository writes, and what described it was an
// example. An example is a good start and a bad reference: it cannot say which keys exist beside
// the ones it shows, what each one falls back to when it is absent, or which of them refuse a
// value that looks perfectly reasonable.
//
// WW490 gave `ProjectDeclaration` a catalogue of its own keys — name, what it holds, what it
// means, what absence does, what it refuses — held against the deserialiser's shape in both
// directions by the suite. This reads that catalogue, so a key this build reads and this page
// does not show is a red in the suite before it is a row missing here.
//
// Three sources, and the last two are the defaults the catalogue points at rather than repeats:
//   src/Winwright/Projects/ProjectDeclaration.cs   the keys, and what a project ignores by default
//   src/Winwright/Projects/Timeouts.cs             the waits a project gets without declaring any
//   src/Winwright/Acting/Retry.cs                  how many attempts an act gets by default
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

import { bodyOf, code, constructions, splitTop } from "./csharp.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const siteDir = join(here, "..");
const repoDir = join(siteDir, "..");

const read = (...parts) => readFileSync(join(repoDir, "src", "Winwright", ...parts), "utf8");

const declaration = read("Projects", "ProjectDeclaration.cs");

/** A quoted literal, or several concatenated across the lines a long sentence wraps over. */
function sentence(expression, where) {
  return splitTop(expression, "+")
    .map((part) => {
      const literal = /^"([\s\S]*)"$/.exec(part.trim());
      if (!literal) throw new Error(`project: ${where} is not a sentence this can read: ${part.trim()}`);
      return literal[1].replace(/\\"/g, '"').replace(/\\\\/g, "\\");
    })
    .join("");
}

// --- the keys ---
// Six fields in the order `DeclaredKey` declares them. The record is the contract, so an argument
// count that changed is a refusal here rather than a page whose columns quietly shifted along.
const keys = constructions(
  bodyOf(code(declaration), "public static IReadOnlyList<DeclaredKey> Keys", "[", "]", "ProjectDeclaration.cs"),
).map((one) => {
  const args = splitTop(one, ",");
  if (args.length !== 6) {
    throw new Error(`project: an entry has ${args.length} arguments and a DeclaredKey has 6: ${one.trim().slice(0, 60)}`);
  }

  const [name, under, holds, means, absent, refuses] = args.map((arg, at) =>
    sentence(arg, `argument ${at + 1} of a DeclaredKey`),
  );

  return {
    name,
    under,
    addressed: under.length > 0 ? `${under}.${name}` : name,
    holds,
    means,
    absent,
    refuses,
  };
});

if (keys.length === 0) throw new Error("project: ProjectDeclaration.Keys catalogues nothing");

for (const key of keys) {
  // The record's own contract, checked here too: the page renders `absent` into a cell of its
  // own, and an empty one reads as a key with no consequence either way — which is the one
  // thing that is never true of a key here.
  if (key.holds.length === 0 || key.means.length === 0 || key.absent.length === 0) {
    throw new Error(`project: '${key.addressed}' is catalogued without saying what it holds, means or does when absent`);
  }
}

const seen = new Set();
for (const key of keys) {
  if (seen.has(key.addressed)) throw new Error(`project: '${key.addressed}' is catalogued twice`);
  seen.add(key.addressed);
}

/** A `["a", "b"]` collection expression as its words. */
function words(body, where) {
  const found = splitTop(body, ",")
    .filter((one) => one.length > 0)
    .map((one) => sentence(one, where));

  if (found.length === 0) throw new Error(`project: ${where} lists nothing`);
  return found;
}

// --- the defaults the catalogue points at ---
// Named in `absent` rather than spelled there, because a sentence carrying eight directory names
// is a sentence that goes stale. They are read here and rendered beside the row.
const ignored = words(
  bodyOf(code(declaration), "public static IReadOnlyList<string> DefaultSourceIgnore", "[", "]", "ProjectDeclaration.cs"),
  "DefaultSourceIgnore",
);

const timeouts = [
  ...bodyOf(
    code(read("Projects", "Timeouts.cs")),
    // Past `{ get; } =` on purpose: the property's own accessor block is the first brace after
    // the name, and a body read from there is `get;` rather than the seeded dictionary.
    "public static IReadOnlyDictionary<string, int> Defaults { get; } =",
    "{",
    "}",
    "Timeouts.cs",
  ).matchAll(/\["([a-zA-Z]+)"\]\s*=\s*(\d+)/g),
].map((m) => ({ name: m[1], milliseconds: Number(m[2]) }));

if (timeouts.length === 0) throw new Error("project: Timeouts.Defaults seeds nothing");

const capped = /public const int DefaultCap = (\d+)/.exec(read("Acting", "Retry.cs"));
if (!capped) throw new Error("project: Retry no longer declares a DefaultCap");

const project = {
  file: /public const string FileName = "([^"]+)"/.exec(declaration)?.[1],
  keys,
  defaults: {
    sourceIgnore: ignored,
    timeouts,
    attempts: Number(capped[1]),
  },
};

if (!project.file) throw new Error("project: ProjectDeclaration no longer declares a FileName");

const out = join(siteDir, "docs", "src", "data", "project.generated.json");
mkdirSync(dirname(out), { recursive: true });
writeFileSync(out, `${JSON.stringify(project, null, 2)}\n`);

console.log(
  `project: ${keys.length} key(s) of ${project.file}, ${ignored.length} ignored by default,`
    + ` ${timeouts.length} seeded timeout(s), ${project.defaults.attempts} attempts`
    + " -> docs/src/data/project.generated.json",
);
