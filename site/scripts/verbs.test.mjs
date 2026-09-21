// The published verbs, held against the catalogue they came out of. WW489.
//
// The catalogue is already checked against the engine in both directions by the suite, so what
// is left to establish here is the other hop: that the page says what the catalogue says. A
// generator that dropped a verb, mislaid a `true`, or counted the figures a different way would
// publish a page that reads exactly as confidently as a right one.
//
// Counted by a second, blunter parse — `Cooperation.` is on every entry and on nothing else in
// that list — and the three figures the page opens with are recomputed from the rows.
import { test } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const repoDir = join(siteDir, "..");

const payload = join(siteDir, "docs", "src", "data", "verbs.generated.json");
if (!existsSync(payload)) {
  throw new Error(`${payload} is missing — run \`npm run generate\` first`);
}

const published = JSON.parse(readFileSync(payload, "utf8"));
const catalogue = readFileSync(join(repoDir, "tests", "Winwright.Tests", "Cooperating.cs"), "utf8");

/** The `Known` list's body, by bracket balance. */
function known() {
  const at = catalogue.indexOf("public static IReadOnlyList<VerbNeeds> Known");
  assert.ok(at >= 0, "Cooperating no longer declares Known");

  const from = catalogue.indexOf("[", at);
  let depth = 0;
  for (let i = from; i < catalogue.length; i++) {
    if (catalogue[i] === "[") depth++;
    else if (catalogue[i] === "]" && --depth === 0) return catalogue.slice(from + 1, i);
  }
  assert.fail("Cooperating.Known is never closed");
}

test("every verb the catalogue entered is published, in its order", () => {
  // `Cooperation.` is on every entry and nowhere else in the list, so a verb this finds and the
  // page does not is a verb the argument parse lost.
  const entries = [...known().matchAll(/new\("([A-Za-z]+\.[A-Za-z]+)",\s*Cooperation\./g)].map((m) => m[1]);
  assert.equal(
    entries.length,
    [...known().matchAll(/Cooperation\./g)].length,
    "some entry of Cooperating.Known is not spelled new(\"Type.Method\", Cooperation....",
  );

  assert.deepEqual(
    published.verbs.map((one) => one.named),
    entries,
    "the verbs the page publishes are not the ones the catalogue enters",
  );
});

test("what each verb needs is what the catalogue entered", () => {
  // Read off the entry rather than off the payload's own fields, and both directions: a verb
  // published as needing nothing when it synthesises input is the one wrong row that sends
  // somebody's suite to a build agent it cannot run on.
  const entries = new Map(
    [...known().matchAll(/new\("([A-Za-z]+\.[A-Za-z]+)",\s*Cooperation\.([A-Za-z]+),\s*(true|false),/g)]
      .map((m) => [m[1], { needs: m[2], desk: m[3] === "true" }]),
  );
  assert.ok(entries.size > 0, "no entry of Cooperating.Known could be read back");

  for (const verb of published.verbs) {
    const entered = entries.get(verb.named);
    assert.ok(entered, `${verb.named} is published and the catalogue enters no such verb`);
    assert.equal(verb.needs, entered.needs, `${verb.named} is published needing ${verb.needs}`);
    assert.equal(verb.desk, entered.desk, `${verb.named} is published ${verb.desk ? "" : "not "}needing a desk`);
    assert.ok(verb.does.length > 0, `${verb.named} is published with nothing said about it`);
    assert.ok(!verb.does.includes('"'), `${verb.named}'s sentence was not read whole`);
  }
});

test("the figures the page opens with are the rows it goes on to show", () => {
  // The same arithmetic `Cooperating.Render` prints to a reader of the suite. A page whose
  // headline disagrees with its own table is worse than one with no headline.
  const { counts, verbs } = published;
  assert.equal(counts.verbs, verbs.length, "the count is not the number of rows");
  assert.equal(
    counts.anywhere,
    verbs.filter((one) => one.needs === "None" && !one.desk).length,
    "the against-anything count is not the rows that need nothing",
  );
  assert.equal(counts.desk, verbs.filter((one) => one.desk).length, "the desk count is not the rows that need one");
  assert.equal(
    counts.inApp,
    verbs.filter((one) => one.needs !== "None").length,
    "the in-app count is not the rows that need the half",
  );
  assert.equal(counts.families, new Set(verbs.map((one) => one.family)).size, "the family count is not the families shown");

  // Every figure is one somebody would act on, so none of them may be a zero this never read.
  for (const [named, figure] of Object.entries(counts)) {
    assert.ok(figure > 0, `the page opens with ${named} at ${figure}`);
  }
});

test("every kind of cooperation is one the enum declares, and says what it is", () => {
  const members = [...catalogue.matchAll(/^ {4}([A-Z][A-Za-z]*),$/gm)].map((m) => m[1]);
  const kinds = published.cooperation.map((one) => one.kind);
  assert.ok(kinds.length > 0, "the page explains no kind of cooperation");

  for (const { kind, means } of published.cooperation) {
    assert.ok(members.includes(kind), `the page explains ${kind}, which Cooperation does not declare`);
    assert.ok(means.length > 0, `${kind} is published with nothing said about it`);
    assert.ok(!means.includes("<"), `${kind}'s sentence still carries a doc-comment tag`);
  }
  for (const verb of published.verbs) {
    assert.ok(kinds.includes(verb.needs), `${verb.named} needs ${verb.needs}, which the page never explains`);
  }
});
