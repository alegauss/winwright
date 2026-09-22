// The generated scenario format, held against the schema it was read out of. WW488.
//
// `scripts/format.mjs` throws on a declaration it cannot read, so the build already fails on a
// rename it can see. What it cannot catch is a parse that still succeeds and now says something
// else: a field dropped because a `new(...)` moved onto one line, a closed list quietly read as
// empty, a description that came back as the constant's name rather than the key.
//
// So this counts and names the same things by a second, blunter parse — one that does not
// resolve anything — and compares. The two agreeing is the claim; either alone is a guess.
import { test } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const repoDir = join(siteDir, "..");

const source = (name) =>
  readFileSync(join(repoDir, "src", "Winwright", "Scenarios", name), "utf8");

const payload = join(siteDir, "docs", "src", "data", "format.generated.json");
if (!existsSync(payload)) {
  throw new Error(`${payload} is missing — run \`npm run generate\` first`);
}

const format = JSON.parse(readFileSync(payload, "utf8"));
const schema = source("ScenarioSchema.cs");

const shaped = (name) => {
  const found = format.shapes.find((one) => one.shape === name);
  assert.ok(found, `the payload declares no ${name}`);
  return found;
};

/** The body of one `IReadOnlyList<Field>` property, by bracket balance. */
function declared(property) {
  const at = schema.indexOf(`public static IReadOnlyList<Field> ${property} { get; }`);
  assert.ok(at >= 0, `ScenarioSchema no longer declares ${property}`);

  const from = schema.indexOf("[", at);
  let depth = 0;
  for (let i = from; i < schema.length; i++) {
    if (schema[i] === "[") depth++;
    else if (schema[i] === "]" && --depth === 0) return schema.slice(from + 1, i);
  }
  assert.fail(`ScenarioSchema.${property} is never closed`);
}

test("every shape holds the fields the schema declares, in its order", () => {
  // Counted by the `Taking.` each field names, which every Field carries and nothing else in
  // these lists does — a blunter landmark than the generator's argument parse, and one that
  // notices a field the parse silently dropped.
  for (const [property, shape] of [["File", "file"], ["Case", "case"], ["Step", "step"], ["Fixture", "fixture"]]) {
    const kinds = [...declared(property).matchAll(/Taking\.([A-Za-z]+)/g)].map((m) => m[1]);
    assert.ok(kinds.length > 0, `ScenarioSchema.${property} declares no fields`);

    assert.deepEqual(
      shaped(shape).fields.map((one) => one.holds),
      kinds,
      `the ${shape}'s fields are not the ones ScenarioSchema.${property} declares`,
    );
  }
});

test("no field is published under the name of the constant that addresses it", () => {
  // Five of the keys are written as constants — `Cases`, `Steps`, `Tags`, `Needs`, `Catches` —
  // and a parse that did not follow them would publish the constant. It looks like a key and is
  // not one, which is the failure a reader only finds on a refused run.
  const consts = new Map(
    [...schema.matchAll(/public const string ([A-Za-z]+) = "([^"]*)"/g)].map((m) => [m[1], m[2]]),
  );
  assert.ok(consts.size > 0, "ScenarioSchema declares no constants");

  for (const { shape, fields } of format.shapes) {
    for (const field of fields) {
      assert.ok(field.name.length > 0, `a ${shape} field has no name`);
      assert.ok(
        !consts.has(field.name) || consts.get(field.name) === field.name,
        `the ${shape}'s '${field.name}' is a constant's name rather than the key it holds`,
      );
      assert.ok(
        !/^[A-Z]/.test(field.name),
        `the ${shape}'s '${field.name}' is capitalised, so it is a symbol and not a key`,
      );
    }
  }
});

test("a description is prose and never the expression that built it", () => {
  // `{}` is prose here — `forEach` says the member reaches a locator through it — so what is
  // looked for is an interpolation that was never filled in, which is a constant's name in
  // braces and nothing else.
  const consts = [...schema.matchAll(/public const string ([A-Za-z]+) = "/g)].map((m) => m[1]);
  assert.ok(consts.length > 0, "ScenarioSchema declares no constants");

  for (const { shape, fields } of format.shapes) {
    for (const { name, means } of fields) {
      assert.ok(means.length > 0, `the ${shape}'s '${name}' says nothing about what it is for`);
      for (const leak of ['"', " + ", "$"]) {
        assert.ok(
          !means.includes(leak),
          `the ${shape}'s '${name}' carries ${JSON.stringify(leak)}, so its description was not read whole`,
        );
      }
      for (const named of consts) {
        assert.ok(
          !means.includes(`{${named}}`),
          `the ${shape}'s '${name}' still carries {${named}}, so an interpolation was never filled in`,
        );
      }
    }
  }
});

test("the two computed lists are the vocabularies, not an empty set", () => {
  // `act` and `reads` are the only fields whose closed list is code rather than literals, and
  // an unresolved one reads as free text — which is a page saying any word will do.
  for (const [field, file, type] of [["act", "ActVerb.cs", "ActVerb"], ["reads", "ReadBack.cs", "ReadBack"]]) {
    const body = source(file);
    const at = body.indexOf(`private static readonly ${type}[] Vocabulary`);
    assert.ok(at >= 0, `${file} no longer declares a vocabulary`);

    // Every entry of the vocabulary, counted by the `new(` that opens it at one indentation.
    const entries = [...body.slice(at).matchAll(/^ {8}new\(/gm)].length;
    assert.ok(entries > 0, `${file} declares an empty vocabulary`);

    const published = shaped("step").fields.find((one) => one.name === field);
    assert.ok(published, `the step has no '${field}'`);
    assert.equal(
      published.oneOf.length,
      entries,
      `'${field}' publishes ${published.oneOf.length} of ${type}'s ${entries} entries`,
    );
    for (const one of published.oneOf) {
      assert.ok(one.length > 0 && !/^[A-Z]/.test(one), `'${field}' accepts ${JSON.stringify(one)}, which is no written word`);
    }
  }
});

test("every reading is published with the sentence its own entry carries", () => {
  // WW501. `reads` publishes its closed list in full, and the words are not guessable: choosing
  // wrong is not a red that names the mistake, because a reading the element does not offer
  // answers null forever. So the sentence has to reach the author, and it has to be the entry's
  // own rather than one the page invented.
  const vocabulary = source("ReadBack.cs");
  const entries = [...vocabulary.matchAll(/new\(\s*"([^"]+)",\s*"([^"]+)"/g)].map((m) => ({
    name: m[1],
    means: m[2],
  }));
  assert.ok(entries.length > 6, `only ${entries.length} reading(s) could be read out of ReadBack`);

  assert.deepEqual(
    format.readings.map((one) => one.name),
    entries.map((one) => one.name),
    "the readings the page explains are not the ones ReadBack declares",
  );

  for (const one of format.readings) {
    const entry = entries.find((each) => each.name === one.name);
    assert.equal(one.means, entry.means, `'${one.name}' is published with a sentence ReadBack does not carry`);
    assert.ok(one.means.endsWith("."), `'${one.name}' is published without a full stop`);
  }

  // The same twelve the field accepts. A page showing one list beside the other is a page where
  // an author reads a sentence under the wrong word.
  const reads = shaped("step").fields.find((one) => one.name === "reads");
  assert.deepEqual(reads.oneOf, format.readings.map((one) => one.name), "'reads' accepts other words than the page explains");

  // Distinct, because the pair this exists for is `selected` and `picked`: two readings sharing
  // a sentence are two an author still cannot tell apart.
  const said = format.readings.map((one) => one.means);
  assert.equal(new Set(said).size, said.length, "two readings are published with the same sentence");
});

test("exactly the two subjects are alternatives, and the claims are the ones marked", () => {
  const step = shaped("step").fields;

  const alternatives = step.filter((one) => one.instead).map((one) => one.name);
  assert.deepEqual(alternatives, ["locator", "tray"], "the step's one-of group is not the two subjects");

  // Counted off the declaration by the named argument that sets it, which the generator reads
  // positionally as often as not.
  const marked = [...declared("Step").matchAll(/Claims:\s*true/g)].length;
  assert.equal(
    step.filter((one) => one.claims).length,
    marked,
    "the claims the page marks are not the ones the schema marks",
  );
  assert.ok(marked > 1, "a step with one claim needs no rule about making exactly one");
});

test("every kind a field holds is one the enum explains", () => {
  const members = [...schema.matchAll(/^ {4}([A-Z][A-Za-z]*),$/gm)].map((m) => m[1]);
  const enumerated = format.kinds.map((one) => one.kind);
  assert.ok(enumerated.length > 0, "the payload publishes no kinds");

  for (const kind of enumerated) {
    assert.ok(members.includes(kind), `the page explains ${kind}, which Taking does not declare`);
  }
  for (const { shape, fields } of format.shapes) {
    for (const one of fields) {
      assert.ok(enumerated.includes(one.holds), `the ${shape}'s '${one.name}' holds ${one.holds}, which the page never explains`);
    }
  }
  for (const { kind, means } of format.kinds) {
    assert.ok(means.length > 0, `${kind} is published with nothing said about it`);
  }
});
