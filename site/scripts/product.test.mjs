// The site's own claims, held against the product they are about.
//
// Every figure on the page is generated, which stops it from being typed wrong — it does
// not stop the *generator* from being wired to the wrong thing, or the copy around a figure
// from being rewritten until it no longer agrees with it. So these read the built output
// and compare it to this repository's source, by a route that does not go through the
// generated module at all: the regexes below are a second, independent parse on purpose.
import { test, before } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const repoDir = join(siteDir, "..");
const distDir = join(siteDir, "dist");

let landing;
before(() => {
  const md = join(distDir, "index.md");
  assert.ok(existsSync(md), "dist/index.md is missing — run `npm run build` first");
  landing = readFileSync(md, "utf8");
});

/** The enum members and their values, read straight out of the C#. */
function outcomes() {
  const src = readFileSync(
    join(repoDir, "src", "Winwright", "Verdicts", "RunOutcome.cs"),
    "utf8",
  );
  const body = /enum\s+RunOutcome\s*\{([\s\S]*)\}/.exec(src);
  assert.ok(body, "RunOutcome.cs no longer declares an enum RunOutcome");
  const found = [...body[1].matchAll(/^\s*([A-Z][A-Za-z]*)\s*=\s*(\d+)\s*,?$/gm)].map((m) => ({
    name: m[1],
    code: Number(m[2]),
  }));
  assert.ok(found.length > 0, "RunOutcome.cs declares no members");
  return found;
}

test("the landing page names every outcome the enum declares, with its code", () => {
  for (const { name, code } of outcomes()) {
    assert.ok(landing.includes(name), `the page never names ${name}`);
    // the card renders the code as its own paragraph, so the number is on the page as a
    // number rather than only inside a sentence about it
    assert.match(
      landing,
      new RegExp(`(^|\\n)${code}(\\n|$)`),
      `the page does not show ${code} as ${name}'s exit code`,
    );
  }
});

test("the landing page claims no outcome the enum does not declare", () => {
  const declared = new Set(outcomes().map((o) => o.name));
  // The four words a reader would take as a verdict. One of them appearing on the page
  // without being in the enum is the drift this test exists for — a fifth outcome named in
  // prose and shipped nowhere.
  for (const word of ["Passed", "Failed", "Degraded", "Broken", "Skipped", "Inconclusive"]) {
    if (declared.has(word)) continue;
    assert.ok(
      !new RegExp(`\\b${word}\\b`).test(landing),
      `the page names ${word} as an outcome and RunOutcome does not declare it`,
    );
  }
});

test("the package references on the page carry the version the tree declares", () => {
  const props = readFileSync(join(repoDir, "Directory.Build.props"), "utf8");
  const version = /<Version>([^<]+)<\/Version>/.exec(props)?.[1];
  assert.ok(version, "Directory.Build.props no longer declares a Version");

  for (const csproj of ["Winwright/Winwright.csproj", "Winwright.InApp/Winwright.InApp.csproj"]) {
    const xml = readFileSync(join(repoDir, "src", ...csproj.split("/")), "utf8");
    const id = /<PackageId>([^<]+)<\/PackageId>/.exec(xml)?.[1];
    assert.ok(id, `${csproj} no longer declares a PackageId`);
    assert.ok(
      landing.includes(`Include="${id}" Version="${version}"`),
      `the page does not offer ${id} at ${version}`,
    );
  }
});

test("the page states the count of verdicts as a word, and the word is right", () => {
  const words = ["zero", "one", "two", "three", "four", "five", "six", "seven", "eight"];
  const expected = words[outcomes().length];
  assert.ok(expected, "more outcomes than this test has words for");
  assert.ok(
    landing.includes(`${expected} verdicts`),
    `the page does not say "${expected} verdicts"`,
  );
});
