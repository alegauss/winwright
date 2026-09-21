// The third verdict, and every condition that earns it. WW491.
//
// An adopter's first surprising run answers `2`. Nothing failed, nothing passed, and the summary
// names an assertion that never ran. That is this project's whole argument arriving at the worst
// possible moment to have to go looking for it — and it was spread over four separate parts of a
// README, none of them titled anything a person would search for.
//
// What the page needs is a list of every condition a hole can name, and whose each one is. Both
// already exist as data in the engine, for the reason WW183 gives: the judgement about a
// condition belongs beside the reading it is about, or it gets made by whoever next has a red
// they cannot explain.
//
// Three sources:
//   src/Winwright/Verdicts/Holes.cs       the three buckets, and what a reader does about each
//   src/Winwright/Verdicts/DeskFacts.cs   the conditions that are the desk's, and why each is
//   src/Winwright/**/*.cs                 every condition this engine declares
//
// That last sweep is the one worth being careful about, because it is a second reading of what
// `Holes.Declared` gets by reflection and Node has no assembly to reflect over. `DeskFactTests`
// holds the two against each other, so a condition declared some third way is a red in the suite
// rather than a row missing here.
import { mkdirSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

import { bodyOf, code, constructions, documented, splitTop } from "./csharp.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const siteDir = join(here, "..");
const repoDir = join(siteDir, "..");
const engine = join(repoDir, "src", "Winwright");

const read = (...parts) => readFileSync(join(engine, ...parts), "utf8");

/** Every source of the engine, by the type its file is named for. */
const sources = readdirSync(engine, { recursive: true, withFileTypes: true })
  .filter((one) => one.isFile() && one.name.endsWith(".cs"))
  .filter((one) => !/[\\/](bin|obj)[\\/]/.test(one.parentPath ?? one.path))
  .map((one) => ({ type: one.name.slice(0, -".cs".length), path: join(one.parentPath ?? one.path, one.name) }));

// --- every condition this engine declares ---
// The two spellings `Holes.Declared` selects on: a constant named for the precondition it is, and
// `Named` where the reading it belongs to reads better that way.
// Comments off and strings kept, which is the reading the suite's own sweep makes: a doc comment
// quoting a declaration would otherwise be swept as a second one.
const declared = [
  ...new Set(
    sources.flatMap((one) =>
      [...code(readFileSync(one.path, "utf8")).matchAll(/public const string (?:[A-Za-z]*PreconditionName|Named) = "([^"]+)";/g)]
        .map((m) => m[1]),
    ),
  ),
].sort();

if (declared.length === 0) throw new Error("holes: the engine declares no precondition this can read");

// --- the three buckets ---
// What a reader does about a hole is the bucket's own sentence: the desk's is a machine to clear,
// this run's is a repository to open, and unclassified is somebody's to go and classify.
const kinds = documented(
  bodyOf(read("Verdicts", "Holes.cs"), "public enum Whose", "{", "}", "Holes.cs"),
  (line) => /^([A-Z][A-Za-z]*),$/.exec(line)?.[1],
  "Holes.cs's Whose",
  false,
).map((one) => ({ kind: one.name, means: one.means }));

// --- the conditions that are the desk's ---
// Each entry names its condition through the constant the reading declares, so the name has to be
// followed rather than printed: `Foreground.PreconditionName` is not a sentence anybody meets.
// Indexed by the type that declares each constant, and never by the file it sits in: `Focus.cs`
// declares `FocusReading` beside `Focus`, so a lookup by file name finds neither.
const held = new Map();
for (const one of sources) {
  let declaring = "";
  for (const line of readFileSync(one.path, "utf8").split(/\r?\n/)) {
    const type = /^\s*(?:public|internal)\s+(?:static\s+|sealed\s+|abstract\s+|partial\s+)*(?:class|record|struct|interface)\s+([A-Z][A-Za-z0-9]*)/.exec(line);
    if (type) {
      declaring = type[1];
      continue;
    }

    const constant = /public const string ([A-Za-z]+) = "([^"]*)"/.exec(line);
    if (constant && declaring.length > 0) held.set(`${declaring}.${constant[1]}`, constant[2]);
  }
}

function conditionOf(expression, where) {
  const named = /^([A-Z][A-Za-z]*)\.([A-Za-z]+)$/.exec(expression.trim());
  if (!named) throw new Error(`holes: ${where} names ${expression.trim()}, which is no constant this can follow`);

  const value = held.get(named[0]);
  if (value === undefined) throw new Error(`holes: nothing in the engine declares ${named[0]} for ${where} to name`);
  return value;
}

/** A quoted literal, or several concatenated across the lines a long sentence wraps over. */
function sentence(expression, where) {
  return splitTop(expression, "+")
    .map((part) => {
      const literal = /^"([\s\S]*)"$/.exec(part.trim());
      if (!literal) throw new Error(`holes: ${where} is not a sentence this can read: ${part.trim()}`);
      return literal[1].replace(/\\"/g, '"');
    })
    .join("");
}

const desk = constructions(
  bodyOf(code(read("Verdicts", "DeskFacts.cs")), "public static IReadOnlyList<DeskFact> Known", "[", "]", "DeskFacts.cs"),
).map((one) => {
  const args = splitTop(one, ",");
  if (args.length !== 2) {
    throw new Error(`holes: a DeskFact has ${args.length} arguments and it takes 2: ${one.trim().slice(0, 60)}`);
  }

  const named = conditionOf(args[0], "a DeskFact");
  return { named, because: sentence(args[1], `the reason for '${named}'`) };
});

if (desk.length === 0) throw new Error("holes: DeskFacts.Known calls nothing the desk's");

for (const one of desk) {
  // A desk fact naming a condition nothing declares excuses nothing, which is the finding WW183
  // is about. The suite says so too; saying it here keeps the page from publishing the orphan.
  if (!declared.includes(one.named)) {
    throw new Error(`holes: the desk fact '${one.named}' names a condition this engine does not declare`);
  }
}

const deskNames = new Set(desk.map((one) => one.named));
const underTest = declared.filter((one) => !deskNames.has(one));

// --- the precedence ---
// Not the enum's order, and the difference matters: taking the largest member value would rank a
// hole above a failure. Read off the fold a suite verdict makes rather than typed beside it.
const fold = bodyOf(
  code(readFileSync(join(repoDir, "src", "Winwright", "Scenarios", "Suite.cs"), "utf8")),
  "internal SuiteVerdict(",
  "{",
  "}",
  "Suite.cs",
);

const precedence = [...fold.matchAll(/readings\.Contains\(RunOutcome\.([A-Za-z]+)\)/g)].map((m) => m[1]);
if (precedence.length === 0) throw new Error("holes: the suite verdict no longer folds its readings by outcome");
if (!/readings\.Count > 0 \? RunOutcome\.([A-Za-z]+)/.test(fold)) {
  throw new Error("holes: the suite verdict no longer says what a run of no readings is");
}

precedence.push(/readings\.Count > 0 \? RunOutcome\.([A-Za-z]+)/.exec(fold)[1]);

const holes = {
  kinds,
  desk,
  underTest,
  precedence,
  // What a fold with nothing in it answers, which is the one a reader is most likely to disbelieve.
  empty: /readings\.Count > 0 \? RunOutcome\.[A-Za-z]+\s*:\s*RunOutcome\.([A-Za-z]+)/.exec(fold)?.[1] ?? "",
  counts: { conditions: declared.length, desk: desk.length, underTest: underTest.length },
};

if (!holes.empty) throw new Error("holes: nothing says what a run of no readings answers");

const out = join(siteDir, "docs", "src", "data", "holes.generated.json");
mkdirSync(dirname(out), { recursive: true });
writeFileSync(out, `${JSON.stringify(holes, null, 2)}\n`);

console.log(
  `holes: ${holes.counts.conditions} condition(s), ${holes.counts.desk} the desk's,`
    + ` ${holes.counts.underTest} this run's; ${precedence.join(" > ")}, and a run of none is ${holes.empty}`
    + " -> docs/src/data/holes.generated.json",
);
