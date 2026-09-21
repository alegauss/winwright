// A Markdown twin per page of the documentation area, and the area in llms.txt. WW495.
//
// The pitch page next door writes a twin per route and an `llms.txt` that lists them, because a
// model reading this project should not have to render a web page to learn what it is. The area
// published HTML and a search index and nothing else — the same defect one page at a time, and
// it landed on exactly the pages an agent most needs: the format, the grammar, the verbs.
//
// Converted from the built HTML, never authored twice. Most of these pages are generated — a
// verb table off the catalogue, a format table off the loader's schema, a captured run — so a
// twin written from the MDX source would carry the prose and none of it. One render, two
// outputs, so the two cannot disagree about anything.
//
// It runs AFTER the area's build, which is not a preference: Astro empties its output directory
// before it writes, so a twin written first is a twin deleted by the build that produced its
// HTML. `site/package.json` chains it last for that reason.
//
// The area's pages join the site's own `llms.txt` and `manifest.json` rather than starting a
// second pair: two indexes under one base is one answer a crawler takes and one nobody asked for.
import { existsSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import { dirname, join, relative } from "node:path";
import { fileURLToPath } from "node:url";

import { parse } from "node-html-parser";

import { htmlToMarkdown } from "./markdown.mjs";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const distDir = join(siteDir, "dist");
const areaDir = join(distDir, "docs");

if (!existsSync(areaDir)) {
  throw new Error("docs-twins: dist/docs is not there — this runs after the area's build, never before it");
}

/** Every page the area built, by the directory its index.html sits in. */
function pages(from) {
  const found = [];
  for (const one of readdirSync(from, { withFileTypes: true })) {
    const at = join(from, one.name);
    if (one.isDirectory()) {
      // `_astro` and `pagefind` hold the assets and the search index, which are not pages.
      if (one.name.startsWith("_") || one.name === "pagefind") continue;
      found.push(...pages(at));
    } else if (one.name === "index.html") {
      found.push(from);
    }
  }
  return found;
}


/** The page's own content, which is what a twin is of.
 *
 *  `<main>` and not the whole document: the area's chrome is a header, a sidebar and a table of
 *  contents, and a twin carrying three copies of the navigation is a twin an agent has to skim
 *  the same way it would have skimmed the HTML. */
function content(html, where) {
  const main = /<main[^>]*>([\s\S]*)<\/main>/.exec(html);
  if (!main) throw new Error(`docs-twins: ${where} has no <main>, so nothing says which half of it is the page`);

  const root = parse(main[1], { comment: false });

  for (const block of root.querySelectorAll("pre")) {
    // The syntax highlighter wraps every line of a code block in its own divs, and a `<pre>` is
    // raw text to the parser — so what is in it is a string rather than a tree, and the markup
    // reached the twin verbatim wherever the block was not rendered as a fence.
    //
    // Flattened to the code it highlights, which is all a fence ever says. The line break is
    // where the next line opens and there is nowhere else to get it: the markup carries none.
    const code = block.rawText
      .replace(/<div class="ec-line">/g, "\n")
      .replace(/<[^>]+>/g, "")
      .replace(/\n{3,}/g, "\n\n")
      .trim();

    // Entities are left as they are: the converter decodes the fence afterwards, and decoding
    // twice turns an escaped `&amp;lt;` in somebody's code sample into a tag.
    block.set_content(code);
  }

  return root.toString();
}

/** What the page calls itself, off the one place that is not the rendered body. */
function titled(html) {
  const found = /<title>([^<]*)<\/title>/.exec(html);
  return found ? found[1].replace(/\s*\|.*$/, "").trim() : "";
}

const base = "/winwright/docs";
const written = [];

for (const at of pages(areaDir).sort()) {
  const html = readFileSync(join(at, "index.html"), "utf8");
  const markdown = htmlToMarkdown(content(html, relative(distDir, at) || "docs"));

  if (markdown.trim().length === 0) {
    throw new Error(`docs-twins: ${relative(distDir, at)} converted to nothing, so the twin would be an empty page`);
  }

  writeFileSync(join(at, "index.md"), markdown);

  const rel = relative(areaDir, at).split(/[\\/]/).filter(Boolean).join("/");
  written.push({
    path: rel.length > 0 ? `${base}/${rel}/` : `${base}/`,
    title: titled(html),
    markdown: `docs/${rel.length > 0 ? `${rel}/` : ""}index.md`,
    markdownBytes: Buffer.byteLength(markdown, "utf8"),
  });
}

if (written.length === 0) throw new Error("docs-twins: the area built no pages to twin");

// --- the site's own manifest, extended rather than duplicated ---
const manifestPath = join(distDir, "manifest.json");
if (!existsSync(manifestPath)) {
  throw new Error("docs-twins: dist/manifest.json is not there — the prerender writes it, and this runs after it");
}

const manifest = JSON.parse(readFileSync(manifestPath, "utf8"));
if (!Array.isArray(manifest.routes)) throw new Error("docs-twins: the manifest no longer lists routes");

// Its own key rather than more `routes`. `routes` is the pitch page's contract — every entry
// there is a prerendered route with an HTML twin, a canonical and a sitemap line, and the cases
// that hold that contract read the whole list. The area's pages are none of those things: they
// are Astro's output, listed here so an agent can find the cheap half without crawling the
// expensive one. Assigned rather than appended, so running this twice lists nothing twice.
manifest.area = written.map((one) => ({
  path: one.path,
  url: `https://alegauss.github.io${one.path}`,
  title: one.title,
  markdown: one.markdown,
  markdownBytes: one.markdownBytes,
}));

writeFileSync(manifestPath, `${JSON.stringify(manifest, null, 2)}\n`);

// --- llms.txt, with the area listed where an agent is already looking ---
const llmsPath = join(distDir, "llms.txt");
if (!existsSync(llmsPath)) {
  throw new Error("docs-twins: dist/llms.txt is not there — the prerender writes it, and this runs after it");
}

const marker = "## The documentation area";
const llms = readFileSync(llmsPath, "utf8");
if (llms.includes("{{version}}")) {
  throw new Error("docs-twins: llms.txt still carries a placeholder, so the prerender has not run yet");
}

const listed = [
  marker,
  "",
  "Longer reads for somebody deciding whether to adopt this, each with a Markdown twin beside",
  "it. Read `<route>index.md` rather than the HTML.",
  "",
  ...written.map((one) => `- [${one.title}](https://alegauss.github.io${one.path}index.md)`),
  "",
].join("\n");

writeFileSync(
  llmsPath,
  llms.includes(marker) ? `${llms.slice(0, llms.indexOf(marker))}${listed}` : `${llms.trimEnd()}\n\n${listed}`,
);

console.log(
  `docs-twins: ${written.length} twin(s) written beside the area's HTML, listed in manifest.json and llms.txt`,
);
