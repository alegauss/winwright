// The links the area does not own the other end of. WW497.
//
// The area is deliberately thin about the long form: it links the README rather than holding a
// second copy, and several of those links carry an anchor. An anchor is a heading spelled as a
// slug, and this repository renames headings whenever the argument under one changes.
//
// Nothing notices. The link still resolves, GitHub lands the reader at the top of a
// thousand-line file, and the page that sent them there reads exactly as it did when it worked.
// It is the quietest kind of rot, and it lands on the reader who was already being sent
// somewhere else for the detail.
//
// Both ends are in this repository, so the gate is small: read every anchor the built area
// emits at this repository's own README, and hold it against the headings the README declares.
//
// Out of scope on purpose. An external link is not this project's to keep — nuget.org and
// github.com's own pages answer to nobody here — and neither is the reverse direction: a
// heading no page points at is a heading, not a defect.
import { test, before } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync, readdirSync } from "node:fs";
import { dirname, join, relative } from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const repoDir = join(siteDir, "..");
const areaDir = join(siteDir, "dist", "docs");

/** The repository this area links into, as its pages spell it. */
const repository = "https://github.com/alegauss/winwright";

/**
 * A heading as GitHub spells it in an anchor: lowercased, punctuation dropped, runs of
 * whitespace hyphenated.
 *
 * Written here rather than imported, because what is being checked is agreement with a rule
 * this project does not own — and a shared helper would make the page and the check wrong in
 * the same direction, which is the one way a gate like this fails silently.
 */
function slug(heading) {
  return heading
    .trim()
    .toLowerCase()
    .replace(/[^\p{L}\p{N}\s-]/gu, "")
    .replace(/\s+/g, "-");
}

/** Every page of the built area, by the file its HTML is in. */
function pages(from) {
  const found = [];
  for (const one of readdirSync(from, { withFileTypes: true })) {
    const at = join(from, one.name);
    if (one.isDirectory()) {
      if (one.name.startsWith("_") || one.name === "pagefind") continue;
      found.push(...pages(at));
    } else if (one.name.endsWith(".html") || one.name.endsWith(".md")) {
      found.push(at);
    }
  }
  return found;
}

let anchors;
let headings;

before(() => {
  assert.ok(existsSync(areaDir), "dist/docs is missing — run `npm run build` first");

  const readme = readFileSync(join(repoDir, "README.md"), "utf8");
  headings = new Set(
    [...readme.matchAll(/^#{1,6}\s+(.+?)\s*$/gm)].map((one) => slug(one[1].replace(/`/g, ""))),
  );
  assert.ok(headings.size > 5, `the README declares ${headings.size} heading(s)`);

  // Every page and its twin, because a twin carries the same links and is the half an agent
  // reads — a rotten anchor there is one a model repeats.
  anchors = [];
  for (const file of pages(areaDir)) {
    const text = readFileSync(file, "utf8");
    for (const [, fragment] of text.matchAll(new RegExp(`${repository}[^"'\\s)]*#([\\w-]+)`, "g"))) {
      anchors.push({ fragment, where: relative(areaDir, file) });
    }
  }
});

test("every README anchor the area links is a heading the README declares", () => {
  assert.ok(anchors.length > 0, "the area links no README anchors, and several of its pages should");

  const broken = anchors.filter(
    // `#readme` is GitHub's own, and names the rendered file rather than a heading in it.
    (one) => one.fragment !== "readme" && !headings.has(one.fragment),
  );

  assert.deepEqual(
    broken.map((one) => `${one.where} -> #${one.fragment}`),
    [],
    "the area links a README anchor that names no heading, so the reader lands at the top of the file",
  );
});

test("the slug rule this holds the links to is the one the README's own headings produce", () => {
  // A check whose rule is wrong passes everything, which is worse than no check at all. So the
  // rule is exercised against headings this repository really carries, including the ones with
  // the punctuation that makes slugging interesting.
  assert.equal(slug("## Writing a case".replace(/^#+\s*/, "")), "writing-a-case");
  assert.equal(slug("What needs the application to cooperate"), "what-needs-the-application-to-cooperate");
  assert.equal(slug("One line if your application's project is at the repository root"),
    "one-line-if-your-applications-project-is-at-the-repository-root");
  assert.equal(slug("The verdict, and the exit code"), "the-verdict-and-the-exit-code");

  // And the headings really are in the set the check reads, so a rule that agreed with nothing
  // would be caught here rather than by everything passing.
  for (const one of ["writing-a-case", "addressing-an-element", "the-verbs"]) {
    assert.ok(headings.has(one), `the README no longer declares a heading slugging to #${one}`);
  }
});
