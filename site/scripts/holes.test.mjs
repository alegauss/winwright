// The published holes, held against the engine that classifies them. WW491.
//
// The suite already holds `DeskFacts.Known` to the conditions the assembly declares, and holds a
// source sweep of those conditions to the same set — which is what makes this generator's own
// sweep trustworthy at all, since Node has no assembly to reflect over.
//
// What is left is the hop to the page: a bucket lost, a condition landing on the wrong side of
// the subtraction, a precedence read out of the fold in the enum's order instead of the fold's.
import { test } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readdirSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

import { code } from "./csharp.mjs";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const repoDir = join(siteDir, "..");
const engine = join(repoDir, "src", "Winwright");

const payload = join(siteDir, "docs", "src", "data", "holes.generated.json");
if (!existsSync(payload)) {
  throw new Error(`${payload} is missing — run \`npm run generate\` first`);
}

const holes = JSON.parse(readFileSync(payload, "utf8"));

/** Every engine source, output trees left out. */
const sources = readdirSync(engine, { recursive: true, withFileTypes: true })
  .filter((one) => one.isFile() && one.name.endsWith(".cs"))
  .filter((one) => !/[\\/](bin|obj)[\\/]/.test(one.parentPath ?? one.path))
  .map((one) => join(one.parentPath ?? one.path, one.name));

test("every condition the engine declares is on one side or the other", () => {
  // The two spellings `Holes.Declared` selects on, swept again. Nothing is allowed to be in
  // neither list: a condition the page does not show is one a reader who met it cannot look up,
  // which is the whole symptom this page was built for.
  const declared = new Set(
    sources.flatMap((one) =>
      [...code(readFileSync(one, "utf8")).matchAll(/public const string (?:[A-Za-z]*PreconditionName|Named) = "([^"]+)";/g)]
        .map((m) => m[1]),
    ),
  );
  assert.ok(declared.size > 10, `only ${declared.size} conditions could be swept out of the engine`);

  const published = [...holes.desk.map((one) => one.named), ...holes.underTest];
  assert.deepEqual(
    [...published].sort(),
    [...declared].sort(),
    "the conditions the page shows are not the ones the engine declares",
  );
  assert.equal(published.length, new Set(published).size, "a condition is published on both sides");
  assert.equal(holes.counts.conditions, declared.size, "the count is not the number of conditions");
});

test("the desk's are the ones DeskFacts calls the desk's, each with its reason", () => {
  // Counted by the `new(` that opens each entry, which is a landmark the generator's argument
  // parse does not share. A desk fact lost here is an assertion the page blames on somebody's
  // code when the machine is what refused it.
  const catalogue = readFileSync(join(engine, "Verdicts", "DeskFacts.cs"), "utf8");
  const at = catalogue.indexOf("public static IReadOnlyList<DeskFact> Known");
  assert.ok(at >= 0, "DeskFacts no longer declares Known");

  const entries = [...catalogue.slice(at).matchAll(/\bnew\(/g)].length;
  assert.ok(entries > 0, "DeskFacts.Known calls nothing the desk's");
  assert.equal(holes.desk.length, entries, `the page shows ${holes.desk.length} of ${entries} desk facts`);
  assert.equal(holes.counts.desk, holes.desk.length, "the desk count is not the rows shown");
  assert.equal(holes.counts.underTest, holes.underTest.length, "the under-test count is not the rows shown");

  for (const one of holes.desk) {
    assert.ok(one.named.length > 0, "a desk fact is published with no condition");
    assert.ok(one.because.length > 0, `'${one.named}' is published with no reason it is the desk's`);
    // A constant reference that was printed rather than followed: `Foreground.PreconditionName`
    // is not a sentence anybody meets in a summary.
    assert.ok(!/^[A-Z][A-Za-z]*\.[A-Za-z]+$/.test(one.named), `'${one.named}' is a constant, not a condition`);
    assert.ok(!one.because.includes('" + "'), `'${one.named}'s reason was not joined across its lines`);
  }
});

test("the three buckets are the enum's, and each says what to do about it", () => {
  const source = readFileSync(join(engine, "Verdicts", "Holes.cs"), "utf8");
  const members = [...source.matchAll(/^ {4}([A-Z][A-Za-z]*),$/gm)].map((m) => m[1]);
  assert.ok(members.length > 0, "Whose declares nothing this can read");

  assert.deepEqual(holes.kinds.map((one) => one.kind), members, "the buckets are not the ones Whose declares");
  for (const { kind, means } of holes.kinds) {
    assert.ok(means.length > 20, `${kind} is published with nothing useful said about it`);
    assert.ok(!means.includes("<"), `${kind}'s sentence still carries a doc-comment tag`);
  }
});

test("the precedence is the fold's order and never the enum's", () => {
  // The difference is the point: taking the largest member value would rank a hole above a
  // failure, which is the one comparison the numbers get backwards. A page publishing the enum's
  // order would be telling a reader the opposite of what a suite verdict does.
  assert.deepEqual(
    holes.precedence,
    ["Broken", "Failed", "Degraded", "Passed"],
    "the precedence the page states is not the one a suite verdict folds by",
  );

  const outcomes = readFileSync(join(engine, "Verdicts", "RunOutcome.cs"), "utf8");
  const byValue = [...outcomes.matchAll(/^\s*([A-Z][A-Za-z]*)\s*=\s*(\d+)/gm)]
    .sort((a, b) => Number(b[2]) - Number(a[2]))
    .map((m) => m[1]);
  assert.notDeepEqual(holes.precedence, byValue, "the fold's order and the enum's have stopped differing");

  assert.ok(holes.empty.length > 0, "nothing says what a run of no readings answers");
  assert.ok(
    holes.precedence.includes(holes.empty),
    `a run of no readings answers ${holes.empty}, which is no outcome the fold ranks`,
  );
});
