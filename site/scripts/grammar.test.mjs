// The generated grammar, held against the parser it was read out of. WW487.
//
// `scripts/grammar.mjs` throws on a source it cannot read, so the build already fails on a
// rename. What it cannot catch is the half that still parses and now says something else: a
// predicate arm added to `Locator.Parse` and never reaching the page, a refusal the enum
// declares that the parser stopped throwing, a form whose gloss went missing.
//
// So this reads the same four C# files again, by a different parse, and compares. Both
// directions every time — a page that omits a key and a page that invents one are different
// defects, and only one of them is visible to a reader.
import { test } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const repoDir = join(siteDir, "..");

const source = (name) =>
  readFileSync(join(repoDir, "src", "Winwright", "Locating", name), "utf8");

const payload = join(siteDir, "docs", "src", "data", "grammar.generated.json");
if (!existsSync(payload)) {
  // Not an assertion: the file is what every test below reads, so its absence is this suite
  // being unrunnable rather than one claim being false.
  throw new Error(`${payload} is missing — run \`npm run generate\` first`);
}

const grammar = JSON.parse(readFileSync(payload, "utf8"));

const locator = source("Locator.cs");
const step = source("LocatorStep.cs");
const fault = source("LocatorSyntaxException.cs");

test("every key the parser accepts is on the page, and no other", () => {
  // The switch in `Step` is what parses. A key in one of these lists and not the other is
  // either a predicate nobody is told about or one the page invented.
  const accepted = [...locator.matchAll(/case "([A-Za-z]+)":/g)].map((m) => m[1]);
  assert.ok(accepted.length > 0, "Locator.cs no longer switches on a predicate key");

  assert.deepEqual(
    grammar.predicates.map((one) => one.key),
    accepted,
    "the generated predicates are not the arms Locator.Parse switches on",
  );
});

test("the refusal over an unknown key offers exactly the keys the page publishes", () => {
  // `Keys` is the sentence a reader meets when they misspell one, and it is the second place
  // the set is written down. A page agreeing with the parser and disagreeing with the refusal
  // would send somebody looking for a key the refusal says does not exist.
  const listed = /private const string Keys = "([^"]+)"/.exec(locator);
  assert.ok(listed, "Locator.cs no longer declares Keys");

  assert.deepEqual(
    listed[1].split(",").map((one) => one.trim()),
    grammar.predicates.map((one) => one.key),
    "the keys the refusal offers are not the keys the page publishes",
  );
});

test("each key says what it matches, in the words of the field it fills", () => {
  // Derived rather than written: the sentence on the page is the summary of the `LocatorStep`
  // field the arm assigns to. A key whose cell is prose somebody typed is one that stops being
  // true when the field's meaning changes.
  const summaries = [...step.matchAll(/<summary>([\s\S]*?)<\/summary>/g)].map((m) =>
    m[1]
      .replace(/^[ \t]*\/\/\/[ \t]?/gm, "")
      .replace(/<para>[\s\S]*/, "")
      // A cref is the word it links, not nothing: `<see cref="Index"/>` reads as `Index` on
      // the page, and a parse that dropped it would disagree with the generator about a
      // sentence they are both right about.
      .replace(/<see cref="(?:[A-Za-z]+\.)*([A-Za-z]+)"\s*\/>/g, "$1")
      .replace(/<[^>]+>/g, "")
      .replace(/\s+/g, " ")
      .trim(),
  );

  for (const { key, matches } of grammar.predicates) {
    assert.ok(matches.length > 0, `'${key}' says nothing about what it matches`);
    assert.ok(
      summaries.some((one) => one.startsWith(matches)),
      `'${key}' claims to match "${matches}", which begins no summary LocatorStep declares`,
    );
  }
});

test("every form the page shows is a locator and a gloss, and covers every key", () => {
  // The forms come off the <code> block in `Locator`'s own remarks, which is the summary a
  // reviewer of a new predicate has to change. A key with no form is a word in a table nobody
  // can see the spelling of.
  assert.ok(grammar.forms.length > 0, "the page shows no forms");

  for (const form of grammar.forms) {
    assert.ok(form.locator.length > 0, "a form has no locator");
    assert.ok(form.addresses.length > 0, `'${form.locator}' says nothing about what it addresses`);
    assert.ok(!form.locator.includes("&"), `'${form.locator}' still carries a doc-comment entity`);
  }

  for (const { key } of grammar.predicates) {
    assert.ok(
      grammar.forms.some((form) => form.locator.includes(`[${key}=`)),
      `no form shows [${key}=...]`,
    );
  }
});

test("every refusal the page describes is one the parser throws", () => {
  // The other direction of the same check the generator makes. An arm declared and never
  // thrown is a refusal a reader is told to expect and cannot reach.
  const thrown = new Set([...locator.matchAll(/LocatorFault\.([A-Za-z]+)/g)].map((m) => m[1]));
  assert.ok(thrown.size > 0, "Locator.cs throws no LocatorFault");

  for (const { arm, meaning } of grammar.refusals) {
    assert.ok(thrown.has(arm), `the page describes ${arm}, which Locator.Parse never throws`);
    assert.ok(meaning.length > 0, `${arm} is named with nothing said about it`);
  }
  assert.equal(
    grammar.refusals.length,
    thrown.size,
    "the parser throws an arm the page does not describe",
  );
});

test("the refusals are the enum's members, in the order it declares them", () => {
  // Minus the default, which is what a throw that did not say which leaves behind. Order
  // matters because the enum's own reasoning is written against it.
  const declared = [...fault.matchAll(/^\s{4}([A-Z][A-Za-z]*),$/gm)].map((m) => m[1]);
  assert.ok(declared.length > 1, "LocatorFault declares nothing this can read");

  assert.deepEqual(
    grammar.refusals.map((one) => one.arm),
    declared.slice(1),
    "the page's refusals are not LocatorFault's members after the default one",
  );
});

test("the orders are the ones the grammar takes, and never the tree's own", () => {
  // `MatchOrder` has five members and `[order=…]` takes four: the tree's own order is what a
  // step saying nothing already gets, so publishing it would be a predicate that narrows
  // nothing. Which one is refused is read off the refusal rather than assumed.
  const refused = [...locator.matchAll(/sorted == MatchOrder\.([A-Za-z]+)/g)].map((m) => m[1]);
  assert.ok(refused.length > 0, "Locator.cs refuses no MatchOrder");

  const declared = [...step.matchAll(/^\s{4}([A-Z][A-Za-z]*),$/gm)].map((m) => m[1]);
  const taken = declared.filter((one) => !refused.includes(one)).map((one) => one.toLowerCase());

  assert.deepEqual(
    grammar.orders.map((one) => one.order),
    taken,
    "the orders the page lists are not the ones Locator.Parse accepts",
  );

  // The refusal names them in prose too, and a reader meets that sentence rather than this
  // table. Four words written twice is four words that can disagree.
  const said = /is no order here; they are ([^"]+)"/.exec(locator);
  assert.ok(said, "Locator.cs no longer names the orders in its refusal");
  assert.equal(
    said[1].replace(/ and /g, ", ").split(",").map((one) => one.trim()).join("|"),
    taken.join("|"),
    "the orders the refusal names are not the ones the page lists",
  );
});
