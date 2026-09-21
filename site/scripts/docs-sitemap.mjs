// One sitemap for one base. WW496.
//
// The prerender writes `dist/sitemap.xml` from the route table and `robots.txt` names it.
// Starlight wrote `dist/docs/sitemap-index.xml` from its own pages, and nothing pointed at it.
// So the site published two sitemaps under one base, and the one a crawler is told about listed
// the pitch routes alone — which is backwards for what the area is. The pitch page is one scroll
// designed to be arrived at; the area is where the reads that decide an adoption live, and they
// are the ones somebody arrives at from a search for a message, a field name or a locator form.
//
// Of the two ways to fix it — the site's sitemap gains the area's pages, or it points at the
// area's index — this takes the first, and the second is turned off rather than left writing a
// file nobody reads. One file, named by the one robots.txt, listing everything under this base.
//
// It runs after the area's build, for the same reason the twins do: Astro empties its output
// directory before it writes, and the pages to list are the ones it just wrote.
import { existsSync, readFileSync, readdirSync, rmSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const distDir = join(siteDir, "dist");
const areaDir = join(distDir, "docs");

const sitemapPath = join(distDir, "sitemap.xml");
if (!existsSync(sitemapPath)) {
  throw new Error("docs-sitemap: dist/sitemap.xml is not there — the prerender writes it, and this runs after it");
}

if (!existsSync(areaDir)) {
  throw new Error("docs-sitemap: dist/docs is not there — this runs after the area's build, never before it");
}

/** Every page the area built, as the path a crawler would fetch. */
function routes(from, under = "") {
  const found = [];
  for (const one of readdirSync(from, { withFileTypes: true })) {
    if (one.isDirectory()) {
      if (one.name.startsWith("_") || one.name === "pagefind") continue;
      found.push(...routes(join(from, one.name), `${under}${one.name}/`));
    } else if (one.name === "index.html") {
      found.push(`/winwright/docs/${under}`);
    }
  }
  return found;
}

const area = routes(areaDir).sort();
if (area.length === 0) throw new Error("docs-sitemap: the area built no pages to list");

const sitemap = readFileSync(sitemapPath, "utf8");

// The pitch page's own entries, kept exactly as they were written — including whatever lastmod
// the prerender decided, which is a claim about the authored tree that this has no better
// answer for. Anything already under the area is dropped first, so running twice lists nothing
// twice.
const existing = [...sitemap.matchAll(/ {2}<url>[\s\S]*?<\/url>/g)]
  .map((one) => one[0])
  .filter((one) => !one.includes("/winwright/docs/"));

if (existing.length === 0) throw new Error("docs-sitemap: dist/sitemap.xml lists no routes to extend");

// The same shape the prerender writes, and the same lastmod where it stated one: a second date
// format in one file is a file two readers parse differently.
const lastmod = /<lastmod>([^<]+)<\/lastmod>/.exec(sitemap)?.[1] ?? "";
const added = area.map((path) => {
  const loc = `    <loc>https://alegauss.github.io${path}</loc>`;
  return lastmod.length > 0
    ? `  <url>\n${loc}\n    <lastmod>${lastmod}</lastmod>\n  </url>`
    : `  <url>\n${loc}\n  </url>`;
});

writeFileSync(
  sitemapPath,
  `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n`
    + `${[...existing, ...added].join("\n")}\n</urlset>\n`,
);

// The other sitemap, turned off by not being published. Starlight adds its own integration and
// exposes no flag for it, so this is where it goes — and it goes rather than staying, because a
// second sitemap under one base that nothing points at is a file that will one day be found and
// believed.
let dropped = 0;
for (const one of readdirSync(areaDir)) {
  if (!one.startsWith("sitemap")) continue;
  rmSync(join(areaDir, one));
  dropped++;
}

console.log(
  `docs-sitemap: ${existing.length} pitch route(s) + ${added.length} area page(s) in one sitemap`
    + `, ${dropped} unreferenced sitemap file(s) dropped`,
);
