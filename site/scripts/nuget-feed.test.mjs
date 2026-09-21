// WW500. The version the page states is chosen rather than read, and this is where that
// choice is asserted.
//
// Read out of the built SSR bundle for the same reason prerender.test.mjs reads dist/: that
// bundle is the one form in which this site's TypeScript is something node can import. So
// these run after `npm run build`, which is what CI does.
//
// The second half asserts the other side of the token: that nothing shipped into dist/ still
// carries an unsubstituted `{{version}}`. A section that forgets to substitute renders the
// token literally, and this is what turns that into a red instead of a page saying
// "Winwright {{version}}" to whoever reads it next.
import { test, before } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const distDir = join(siteDir, "dist");

let latestPublished;
let feedUrl;

before(async () => {
  const bundle = join(siteDir, "dist-server", "entry-server.js");
  assert.ok(existsSync(bundle), "dist-server/entry-server.js is missing — run `npm run build` first");
  ({ latestPublished, feedUrl } = await import(`file://${bundle}`));
});

test("the flat-container url is the package id lowercased", () => {
  // That endpoint is the one part of the API that requires it, and the ids here are mixed
  // case — so a url built from the id as written 404s and the page silently stays behind.
  assert.equal(
    feedUrl("Winwright.InApp"),
    "https://api.nuget.org/v3-flatcontainer/winwright.inapp/index.json",
  );
});

test("a release outranks every prerelease of the same version", () => {
  // SemVer 11.3, and the case this package actually met: 1.0.0 shipped over eighteen
  // 0.1.0 prereleases, and a comparison that counted fields would have preferred one.
  assert.equal(latestPublished(["1.0.0-rc.1", "1.0.0", "1.0.0-alpha.2"]), "1.0.0");
  assert.equal(latestPublished(["0.1.0-alpha.18", "1.0.0"]), "1.0.0");
});

test("prerelease counters are compared as numbers, not as text", () => {
  // The defect this exists for: "0.1.0-alpha.9" sorts after "0.1.0-alpha.18" as text, so a
  // string comparison would have offered alpha.9 for ten releases and looked plausible.
  assert.equal(
    latestPublished(["0.1.0-alpha.9", "0.1.0-alpha.18", "0.1.0-alpha.10"]),
    "0.1.0-alpha.18",
  );
});

test("release numbers are compared as numbers too", () => {
  assert.equal(latestPublished(["1.9.0", "1.10.0", "1.2.0"]), "1.10.0");
  assert.equal(latestPublished(["1.0.0", "1.0.10", "1.0.9"]), "1.0.10");
});

test("a prerelease wins only where nothing has been released", () => {
  assert.equal(latestPublished(["0.1.0-alpha.1", "0.1.0-alpha.2"]), "0.1.0-alpha.2");
});

test("alphanumeric prerelease fields outrank numeric ones, and longer outranks shorter", () => {
  assert.equal(latestPublished(["1.0.0-alpha", "1.0.0-alpha.1"]), "1.0.0-alpha.1");
  assert.equal(latestPublished(["1.0.0-alpha.1", "1.0.0-beta"]), "1.0.0-beta");
  assert.equal(latestPublished(["1.0.0-1", "1.0.0-alpha"]), "1.0.0-alpha");
});

test("build metadata does not decide precedence", () => {
  assert.equal(latestPublished(["1.0.0+abc", "1.0.1"]), "1.0.1");
});

test("a feed with nothing in it answers null rather than inventing a version", () => {
  // Null is what leaves the generated number standing. A version invented here would be one
  // the page states and nobody can restore.
  assert.equal(latestPublished([]), null);
  assert.equal(latestPublished(["", "  "]), null);
});

/** Every file the build writes that a reader or an agent actually reads. */
function shipped(dir) {
  const found = [];
  for (const entry of readdirSync(dir)) {
    const path = join(dir, entry);
    if (statSync(path).isDirectory()) {
      found.push(...shipped(path));
    } else if (/\.(html|md|txt|xml)$/.test(entry)) {
      found.push(path);
    }
  }
  return found;
}

test("nothing shipped still carries an unsubstituted version token", () => {
  assert.ok(existsSync(distDir), "dist/ is missing — run `npm run build` first");

  const leaked = shipped(distDir).filter((path) =>
    readFileSync(path, "utf8").includes("{{version}}"),
  );

  assert.deepEqual(
    leaked.map((path) => path.slice(distDir.length + 1)),
    [],
    "a section renders the token instead of a version — it is missing the substitution",
  );
});
