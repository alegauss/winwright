// The area's Markdown twins, held against the HTML they were converted from. WW495.
//
// The claim a twin makes is that it is the same page, cheaper to read. So what is checked is
// that there is one beside every page the area built, that each carries what that page is about
// rather than its chrome, and that the site's own index lists them — an agent that cannot find a
// twin reads the HTML, which is the state this replaced.
import { test, before } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync, readdirSync } from "node:fs";
import { dirname, join, relative } from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const distDir = join(siteDir, "dist");
const areaDir = join(distDir, "docs");

/** Every directory of the built area that holds a page. */
function pages(from) {
  const found = [];
  for (const one of readdirSync(from, { withFileTypes: true })) {
    const at = join(from, one.name);
    if (one.isDirectory()) {
      if (one.name.startsWith("_") || one.name === "pagefind") continue;
      found.push(...pages(at));
    } else if (one.name === "index.html") {
      found.push(from);
    }
  }
  return found;
}

let built;
before(() => {
  assert.ok(existsSync(areaDir), "dist/docs is missing — run `npm run build` first");
  built = pages(areaDir);
  assert.ok(built.length > 4, `the area built ${built.length} page(s)`);
});

test("every page the area built has a twin beside it", () => {
  for (const at of built) {
    const twin = join(at, "index.md");
    assert.ok(existsSync(twin), `${relative(distDir, at)} has no index.md beside its index.html`);

    const markdown = readFileSync(twin, "utf8");
    assert.ok(markdown.trim().length > 200, `${relative(distDir, at)}'s twin is ${markdown.length} characters`);
    assert.match(markdown, /^#\s+\S/m, `${relative(distDir, at)}'s twin opens with no heading`);
  }
});

test("a twin carries the page and not its chrome", () => {
  // The area's navigation is a sidebar, a header and a table of contents, and every page has the
  // same one. A twin carrying it is a twin an agent has to skim exactly as it would the HTML.
  for (const at of built) {
    const markdown = readFileSync(join(at, "index.md"), "utf8");

    // Starlight's own furniture, by the strings it renders rather than by class.
    for (const chrome of ["Skip to content", "Search", "On this page"]) {
      assert.ok(
        !markdown.includes(chrome),
        `${relative(distDir, at)}'s twin carries '${chrome}', so the conversion kept the chrome`,
      );
    }

    assert.ok(!markdown.includes("<div"), `${relative(distDir, at)}'s twin still carries markup`);
    assert.ok(!markdown.includes("&#"), `${relative(distDir, at)}'s twin still carries an entity`);
  }
});

test("a generated table survives into the twin", () => {
  // Most of these pages are generated, which is the whole reason a twin is converted from the
  // render rather than written from the MDX: a twin authored from the source would carry the
  // prose and none of the tables.
  const verbs = readFileSync(join(areaDir, "verbs", "index.md"), "utf8");
  const rows = verbs.split("\n").filter((one) => one.startsWith("|"));
  assert.ok(rows.length > 50, `the verbs twin holds ${rows.length} table row(s), and the page shows over a hundred`);
  assert.match(verbs, /\|\s*Family\s*\|/, "the verbs twin has lost the table's header");
});

test("the site's own index lists the area's twins, and lists them once", () => {
  const manifest = JSON.parse(readFileSync(join(distDir, "manifest.json"), "utf8"));

  // Its own key. `routes` is the pitch page's contract — every entry there is prerendered, with
  // an HTML twin, a canonical and a sitemap line — and the area's pages are none of those.
  assert.ok(Array.isArray(manifest.area), "the manifest does not list the area");
  assert.ok(
    !manifest.routes.some((one) => String(one.path).startsWith("/winwright/docs")),
    "an area page is listed among the prerendered routes, whose contract it does not meet",
  );

  const listed = manifest.area;
  assert.equal(listed.length, built.length, `the manifest lists ${listed.length} of the area's ${built.length} pages`);

  const paths = listed.map((one) => one.path);
  assert.equal(new Set(paths).size, paths.length, "the manifest lists one of the area's pages twice");

  for (const one of listed) {
    assert.ok(one.title?.length > 0, `${one.path} is listed with no title`);
    assert.ok(one.markdownBytes > 200, `${one.path} is listed at ${one.markdownBytes} bytes`);
    assert.ok(existsSync(join(distDir, one.markdown)), `${one.path} is listed as ${one.markdown}, which is not there`);
  }

  const llms = readFileSync(join(distDir, "llms.txt"), "utf8");
  assert.ok(llms.includes("## The documentation area"), "llms.txt does not name the area");
  assert.ok(!llms.includes("{{version}}"), "llms.txt still carries a placeholder");
  for (const one of listed) {
    assert.ok(llms.includes(`${one.path}index.md`), `llms.txt does not list ${one.path}`);
  }
});
