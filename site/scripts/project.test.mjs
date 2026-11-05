// The published project declaration, held against the catalogue it came out of. WW490.
//
// The suite already holds `ProjectDeclaration.Keys` against the deserialiser's own shape in both
// directions, so what is left to establish here is the hop from that catalogue to the page: a
// key dropped by the argument parse, a sentence read short, a default counted a different way.
//
// Read by a second, blunter parse — `DeclaredKey` entries are counted by the `new(` that opens
// each one — and the three defaults are read again out of the types that seed them.
import { test } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const repoDir = join(siteDir, "..");

const source = (...parts) => readFileSync(join(repoDir, "src", "Winwright", ...parts), "utf8");

const payload = join(siteDir, "docs", "src", "data", "project.generated.json");
if (!existsSync(payload)) {
  throw new Error(`${payload} is missing — run \`npm run generate\` first`);
}

const project = JSON.parse(readFileSync(payload, "utf8"));
const declaration = source("Projects", "ProjectDeclaration.cs");

/** The `Keys` catalogue's body, by bracket balance. */
function catalogued() {
  const at = declaration.indexOf("public static IReadOnlyList<DeclaredKey> Keys");
  assert.ok(at >= 0, "ProjectDeclaration no longer declares Keys");

  const from = declaration.indexOf("[", at);
  let depth = 0;
  for (let i = from; i < declaration.length; i++) {
    if (declaration[i] === "[") depth++;
    else if (declaration[i] === "]" && --depth === 0) return declaration.slice(from + 1, i);
  }
  assert.fail("ProjectDeclaration.Keys is never closed");
}

test("every key the catalogue holds is published, in its order", () => {
  // The name is the first argument of each entry, so it is the first string literal after the
  // `new(` that opens one — a landmark the argument parse does not share.
  const entries = [...catalogued().matchAll(/new\(\s*"([A-Za-z]+)"/g)].map((m) => m[1]);
  assert.equal(
    entries.length,
    [...catalogued().matchAll(/\bnew\(/g)].length,
    "an entry of ProjectDeclaration.Keys does not open with its name",
  );
  assert.ok(entries.length > 10, `only ${entries.length} keys could be read out of the catalogue`);

  assert.deepEqual(project.keys.map((one) => one.name), entries, "the page's keys are not the catalogue's");
});

test("a key under an object is addressed by both halves", () => {
  for (const key of project.keys) {
    assert.equal(
      key.addressed,
      key.under.length > 0 ? `${key.under}.${key.name}` : key.name,
      `${key.name} is addressed as ${key.addressed}`,
    );
  }

  const addressed = project.keys.map((one) => one.addressed);
  assert.equal(new Set(addressed).size, addressed.length, "a key is published twice");
  assert.ok(
    project.keys.some((one) => one.under.length > 0),
    "no key is published under an object, and `language` has three",
  );
});

test("no key is published without saying what leaving it out does", () => {
  // Absence is never "nothing happens" here — it is a default that stands, a reading recorded as
  // not taken, or a refusal at the moment something asks. An empty cell reads as neither.
  for (const key of project.keys) {
    // `destructive` holds `{"id"}` or `{"key"}` entries, so a quote is prose here. What says a
    // value was not read whole is an escape that never decoded or a concatenation that never
    // joined — both of which would reach the page as source code.
    for (const [field, value] of [["holds", key.holds], ["means", key.means], ["absent", key.absent]]) {
      assert.ok(value.length > 0, `'${key.addressed}' publishes an empty ${field}`);
      assert.ok(!value.includes('\\"'), `'${key.addressed}'s ${field} still carries an undecoded escape`);
      assert.ok(!value.includes('" + "'), `'${key.addressed}'s ${field} was not joined across its lines`);
    }
  }

  assert.ok(
    project.keys.filter((one) => one.refuses.length > 0).length >= 2,
    "fewer than two keys publish a refusal, and loading and destructive both have one",
  );
});

test("the defaults are the ones the engine seeds", () => {
  // Read again out of the types that declare them, because these are the figures a reader copies
  // rather than reads — a page offering a stale timeout is a page that costs a debugging session.
  const seeded = [...source("Projects", "Timeouts.cs").matchAll(/\["([a-zA-Z]+)"\]\s*=\s*(\d+)/g)]
    .map((m) => ({ name: m[1], milliseconds: Number(m[2]) }));
  assert.ok(seeded.length > 0, "Timeouts.cs seeds nothing");
  assert.deepEqual(project.defaults.timeouts, seeded, "the seeded timeouts are not the ones Timeouts declares");

  const cap = /public const int DefaultCap = (\d+)/.exec(source("Acting", "Retry.cs"));
  assert.ok(cap, "Retry no longer declares a DefaultCap");
  assert.equal(project.defaults.attempts, Number(cap[1]), "the default attempts is not Retry's cap");

  const ignored = /DefaultSourceIgnore \{ get; \} =\s*new ReadOnlyCollection<string>\(\[([^\]]+)\]/.exec(declaration);
  assert.ok(ignored, "ProjectDeclaration no longer declares DefaultSourceIgnore as a list of names");
  assert.deepEqual(
    project.defaults.sourceIgnore,
    [...ignored[1].matchAll(/"([^"]+)"/g)].map((m) => m[1]),
    "the ignored directories are not the ones the engine seeds",
  );
});

test("the file the page names is the file the engine looks for", () => {
  const named = /public const string FileName = "([^"]+)"/.exec(declaration);
  assert.ok(named, "ProjectDeclaration no longer declares a FileName");
  assert.equal(project.file, named[1], "the page names a declaration file the engine does not look for");
});
