// The documentation area's joins, held against what declares them elsewhere.
//
// `site/docs/` is a second npm project with a second build, and every one of the four things
// below is a fact it shares with the pitch page next door. None of them fails loudly on its
// own: a wrong base 404s in production and nowhere else, a wrong output directory publishes a
// site with the area missing, and a build step in the wrong order deletes what it just wrote.
// So they are read out of both files and compared here.
import { test, before } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const repoDir = join(siteDir, "..");
const docsDir = join(siteDir, "docs");
const builtDir = join(siteDir, "dist", "docs");

const read = (...parts) => readFileSync(join(...parts), "utf8");

/** One `const NAME = "…"` out of a module, by a parse that does not import it. */
function declared(source, name, where) {
  const found = new RegExp(`const ${name} = "([^"]+)"`).exec(source);
  assert.ok(found, `${where} no longer declares ${name}`);
  return found[1];
}

const astroConfig = read(docsDir, "astro.config.mjs");
const viteConfig = read(siteDir, "vite.config.ts");
const sitePackage = JSON.parse(read(siteDir, "package.json"));

test("the area is served one segment under the base the pitch page is served at", () => {
  // Pages derives the base from the repository name, and Astro rewrites only the links it
  // generates — so an area declaring anything else links correctly everywhere but production.
  const base = declared(viteConfig, "BASE", "vite.config.ts");
  const areaBase = declared(astroConfig, "BASE", "docs/astro.config.mjs");
  assert.equal(
    areaBase,
    `${base}docs`.replace("//", "/"),
    "the documentation area's base is not the site's base plus one segment",
  );
});

test("the area builds into the directory the deploy uploads", () => {
  // site.yml uploads site/dist and nothing else. An area writing outside it is the half of
  // the site no publish carries — and the deploy still goes green.
  assert.equal(
    declared(astroConfig, "OUT_DIR", "docs/astro.config.mjs"),
    "../dist/docs",
    "the area no longer builds into the pitch page's dist/",
  );
});

test("the area's build runs last, after everything that empties dist/", () => {
  // `vite build` empties dist/ and Astro empties its own outDir before it writes. Reordered,
  // the area is deleted by the step after it.
  const build = sitePackage.scripts.build;
  assert.ok(build.includes("npm run build:docs"), "`build` no longer chains the area's build");
  assert.ok(
    build.trimEnd().endsWith("npm run build:docs"),
    "the area's build is not the last step of `build`, so a later step empties what it wrote",
  );
});

test("the site links to the area rather than to a file on GitHub", () => {
  const content = read(siteDir, "src", "lib", "site-content.ts");
  const docsHref = /export const docsUrl = "([^"]+)"/.exec(content);
  assert.ok(docsHref, "site-content.ts no longer declares docsUrl as a plain path");
  assert.equal(
    docsHref[1],
    `${declared(viteConfig, "BASE", "vite.config.ts")}docs/`,
    "the site's Docs link does not point at the documentation area",
  );
});

test("every page the sidebar lists is a page that exists", () => {
  // A link with no page behind it is a 404 the build is perfectly happy with.
  const links = [...astroConfig.matchAll(/link: "([^"]+)"/g)].map((m) => m[1]);
  assert.ok(links.length > 0, "the sidebar lists no pages");
  for (const link of links) {
    const slug = link.replace(/^\/|\/$/g, "");
    const page = slug === "" ? "index" : slug;
    assert.ok(
      existsSync(join(docsDir, "src", "content", "docs", `${page}.mdx`)),
      `the sidebar links ${link} and there is no ${page}.mdx to answer it`,
    );
  }
});

// --- what the build actually produced ---

let landing;
before(() => {
  const page = join(builtDir, "index.html");
  assert.ok(existsSync(page), "dist/docs/index.html is missing — run `npm run build` first");
  landing = readFileSync(page, "utf8");
});

test("the built area carries the version this tree declares", () => {
  // The figure is generated into docs/src/data/product.generated.json by the same script the
  // pitch page's is. This is the second, independent read of the property it came from.
  const version = /<Version>([^<]+)<\/Version>/.exec(read(repoDir, "Directory.Build.props"))?.[1];
  assert.ok(version, "Directory.Build.props no longer declares a Version");
  assert.ok(
    landing.includes(version),
    `the area's first page does not offer the packages at ${version}`,
  );
});

test("the built grammar page publishes every predicate and every refusal the parser has", () => {
  // WW487. `grammar.test.mjs` holds the generated payload against the C#; this is the other
  // end, and it is a different claim: a payload can be right while the page never renders it.
  // Both halves of the table, because a key with no sentence beside it is a key nobody can use.
  const page = join(builtDir, "locators", "index.html");
  assert.ok(existsSync(page), "dist/docs/locators/index.html is missing — run `npm run build` first");
  const rendered = readFileSync(page, "utf8");

  const locator = read(repoDir, "src", "Winwright", "Locating", "Locator.cs");
  const keys = /private const string Keys = "([^"]+)"/.exec(locator);
  assert.ok(keys, "Locator.cs no longer declares Keys");

  for (const key of keys[1].split(",").map((one) => one.trim())) {
    assert.match(
      rendered,
      new RegExp(`id="predicate-${key}"`),
      `the grammar page publishes no row for [${key}=...]`,
    );
  }

  const thrown = new Set([...locator.matchAll(/LocatorFault\.([A-Za-z]+)/g)].map((m) => m[1]));
  for (const arm of thrown) {
    assert.match(
      rendered,
      new RegExp(`id="refusal-${arm}"`),
      `the grammar page never names ${arm}, which Locator.Parse throws`,
    );
  }
});

test("the built area names every outcome the enum declares, with its code", () => {
  const src = read(repoDir, "src", "Winwright", "Verdicts", "RunOutcome.cs");
  const body = /enum\s+RunOutcome\s*\{([\s\S]*)\}/.exec(src);
  assert.ok(body, "RunOutcome.cs no longer declares an enum RunOutcome");
  const members = [...body[1].matchAll(/^\s*([A-Z][A-Za-z]*)\s*=\s*(\d+)\s*,?$/gm)];
  assert.ok(members.length > 0, "RunOutcome.cs declares no members");

  for (const [, name, code] of members) {
    assert.ok(landing.includes(name), `the area never names ${name}`);
    assert.match(
      landing,
      new RegExp(`ww-verdict-code">${code}<`),
      `the area does not show ${code} as ${name}'s exit code`,
    );
  }
});
