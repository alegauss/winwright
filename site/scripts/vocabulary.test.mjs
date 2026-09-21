// The words, held to the pages that use them. WW498.
//
// A glossary rots in two directions and neither is visible on the page. A word can be defined
// and then stop being used, which is a definition nobody meets; and a word can be leaned on
// everywhere and never defined, which is the state this page was written to end.
//
// So both are checked, and the arrow under each definition is held to a page that exists —
// because the whole shape of the page is "here is the word, and here is where it does its
// work", and an arrow into nothing is worse than no arrow.
import { test, before } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync, readdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const contentDir = join(siteDir, "docs", "src", "content", "docs");
const builtDir = join(siteDir, "dist", "docs");

let page;
let defined;

before(() => {
  const at = join(contentDir, "vocabulary.mdx");
  assert.ok(existsSync(at), "there is no vocabulary.mdx for the area's words");
  page = readFileSync(at, "utf8");

  // A definition is a heading, which is also what makes each one linkable from a page that
  // uses the word — the reason the design gives for writing it at all.
  defined = [...page.matchAll(/^##\s+(.+?)\s*$/gm)].map((one) => one[1].trim());
  assert.ok(defined.length >= 6, `the page defines ${defined.length} word(s) and the design names six`);
});

test("the six words the area leans on are the ones defined", () => {
  // Named rather than counted. These are the words a reader already owns, used here to mean
  // something narrower, which is worse than a term they have never seen.
  for (const word of ["A case", "A hole", "The desk", "A reading", "A fixture", "A project"]) {
    assert.ok(defined.includes(word), `the words page no longer defines '${word}'`);
  }
});

test("every definition sends the reader to a page that exists", () => {
  const arrows = [...page.matchAll(/^→\s+\[[^\]]+\]\((\/winwright\/docs\/[^)]*)\)/gm)].map((one) => one[1]);
  assert.ok(arrows.length >= 6, `${arrows.length} definition(s) point anywhere, and there are ${defined.length}`);

  for (const href of arrows) {
    const slug = href.replace("/winwright/docs/", "").replace(/\/$/, "");
    const file = slug.length === 0 ? "index" : slug;
    assert.ok(
      existsSync(join(contentDir, `${file}.mdx`)),
      `a definition points at ${href} and there is no ${file}.mdx to answer it`,
    );
  }
});

test("no definition runs longer than the page it is meant to save a reader from", () => {
  // One paragraph each. A glossary long enough to need its own navigation is one nobody
  // reaches the end of, which is the same reader this page exists for.
  const sections = page.split(/^##\s+/m).slice(1);
  for (const one of sections) {
    const body = one.split("\n").slice(1).join("\n").split("→")[0].trim();
    assert.ok(body.length > 100, `'${one.split("\n")[0]}' is defined in ${body.length} characters`);
    assert.ok(body.length < 900, `'${one.split("\n")[0]}' runs to ${body.length} characters, which is no longer a definition`);
  }
});

test("a word the page defines is a word the area actually uses", () => {
  // The other direction, against the built pages rather than the sources: a definition nobody
  // meets is one that will go on describing a word the area stopped using.
  assert.ok(existsSync(builtDir), "dist/docs is missing — run `npm run build` first");

  const elsewhere = readdirSync(builtDir, { recursive: true })
    .map((one) => String(one))
    .filter((one) => one.endsWith("index.md") && !one.includes("vocabulary"))
    .map((one) => readFileSync(join(builtDir, one), "utf8"))
    .join("\n")
    .toLowerCase();

  assert.ok(elsewhere.length > 1000, "the area's twins could not be read back");

  for (const word of defined) {
    const bare = word.replace(/^(A|An|The)\s+/i, "").toLowerCase();
    assert.ok(
      elsewhere.includes(bare),
      `the words page defines '${bare}' and no other page of the area uses it`,
    );
  }
});
