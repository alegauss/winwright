// The published in-app half, held against the package an adopter would ship. WW492.
//
// The whole page turns on one claim: every surface is off unless the process that started the
// application armed it. That claim is a property of the code — a `PathVariable` read at the
// moment somebody asks — so what this checks is that the page shows every guard there is, and
// that none of them reached the page as something other than the variable's own name.
//
// A surface added without one would be a surface this page says nothing about, and the sentence
// "it does nothing in a release" would quietly stop being true of the package as a whole.
import { test } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readdirSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const repoDir = join(siteDir, "..");
const half = join(repoDir, "src", "Winwright.InApp");

const payload = join(siteDir, "docs", "src", "data", "inapp.generated.json");
if (!existsSync(payload)) {
  throw new Error(`${payload} is missing — run \`npm run generate\` first`);
}

const inapp = JSON.parse(readFileSync(payload, "utf8"));

const sources = readdirSync(half, { withFileTypes: true })
  .filter((one) => one.isFile() && one.name.endsWith(".cs"))
  .map((one) => readFileSync(join(half, one.name), "utf8"));

test("every surface the package guards is on the page", () => {
  // Counted by the guard rather than by the type, because the guard is what the page is about:
  // a surface with no `PathVariable` would be one that answers whether or not anybody armed it,
  // and that is the claim this page makes on an adopter's behalf.
  const guarded = sources
    .flatMap((one) => [...one.matchAll(/public const string PathVariable = "([^"]+)"/g)])
    .map((m) => m[1])
    .sort();
  assert.ok(guarded.length > 0, "the in-app half declares no PathVariable");

  assert.deepEqual(
    inapp.surfaces.map((one) => one.variable).sort(),
    guarded,
    "the variables the page shows are not the ones the package reads",
  );

  for (const one of inapp.surfaces) {
    assert.match(one.variable, /^WINWRIGHT_[A-Z]+$/, `${one.surface} is armed by ${one.variable}`);
    assert.ok(one.answers.length > 0, `${one.surface} says nothing about what it answers`);
    assert.ok(one.unset.length > 0, `${one.surface} says nothing about what leaving it unset does`);
  }
});

test("no sentence on the page cites a task nobody outside this repository can resolve", () => {
  // The summaries here cite the task the reasoning started in, which is right for a reader of
  // the source and useless to an adopter: `WW349` resolves to nothing they can open.
  for (const one of inapp.surfaces) {
    for (const [field, value] of [["answers", one.answers], ["unset", one.unset]]) {
      assert.doesNotMatch(value, /WW\d+/, `${one.surface}'s ${field} carries a task id`);
      assert.ok(!value.includes("<"), `${one.surface}'s ${field} still carries a doc-comment tag`);
    }
  }
});

test("both halves agree on the messages they find each other by", () => {
  const registered = sources
    .flatMap((one) => [...one.matchAll(/public const string Registered[A-Za-z]* = "([^"]+)"/g)])
    .map((m) => m[1]);
  assert.ok(registered.length > 0, "the in-app half registers no message");

  assert.deepEqual(
    inapp.registered.map((one) => one.named).sort(),
    [...registered].sort(),
    "the messages the page names are not the ones the package registers",
  );

  // The engine sends what this answers. A page naming a message only one half knows would be
  // describing a protocol that cannot connect.
  const engine = readFileSync(join(repoDir, "src", "Winwright", "Capturing", "OwnRender.cs"), "utf8");
  for (const one of inapp.registered) {
    assert.ok(
      engine.includes(one.named) || engine.includes(`Renders.${one.ask}`),
      `nothing in OwnRender sends '${one.named}'`,
    );
  }
});

test("the framework the page asks for is the one the project file declares", () => {
  const project = readFileSync(join(half, "Winwright.InApp.csproj"), "utf8");
  assert.equal(
    inapp.wpf,
    /<UseWPF>true<\/UseWPF>/.test(project),
    "the page and the project file disagree about whether WPF is needed",
  );
});
