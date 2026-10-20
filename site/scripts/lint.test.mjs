// Source lints — the rules that are cheaper to hold in a test than to remember.
//
// Only the reader moves the window: a panel that keeps its own content in view scrolls its
// own element, never scrollIntoView, which scrolls every scrollable ancestor including the
// document. That is what would otherwise let the autoplaying run drag a reader back to the
// hero once per step.
//
// Also here: the site fetches no third-party font at page load, and the lattice's loop is
// arithmetic rather than luck.
import { test } from "node:test";
import assert from "node:assert/strict";
import { readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, extname } from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");

function walk(dir, out = []) {
  for (const name of readdirSync(dir)) {
    if (name === "node_modules" || name === "dist" || name === "dist-server") continue;
    const full = join(dir, name);
    if (statSync(full).isDirectory()) walk(full, out);
    else out.push(full);
  }
  return out;
}

const sourceFiles = walk(join(siteDir, "src")).filter((f) =>
  [".ts", ".tsx", ".js", ".jsx"].includes(extname(f)),
);

const rel = (f) => f.slice(siteDir.length + 1).replace(/\\/g, "/");

test("no source calls scrollIntoView", () => {
  // the call, not the word — a comment explaining why we avoid it is fine
  const offenders = sourceFiles.filter((f) => readFileSync(f, "utf8").includes("scrollIntoView("));
  assert.deepEqual(
    offenders.map(rel),
    [],
    "a panel must scroll its own element (scrollTop), never scrollIntoView",
  );
});

test("no source fetches a third-party font at page load", () => {
  const all = [...sourceFiles, join(siteDir, "index.html")];
  const offenders = all.filter((f) => readFileSync(f, "utf8").includes("fonts.googleapis.com"));
  assert.deepEqual(offenders.map(rel), []);
});

// WW553. Analytics waits for consent: the tag is not in the page, and the banner is the one
// source that may name the host it loads from — one for the pitch page and one for the docs
// area, which is the area's own build and inlines its banner script into every page.
test("analytics loads only from the consent banner", () => {
  assert.ok(
    !readFileSync(join(siteDir, "index.html"), "utf8").includes("googletagmanager.com"),
    "index.html loads Google Analytics before anyone was asked",
  );
  const naming = sourceFiles.filter((f) => readFileSync(f, "utf8").includes("googletagmanager.com"));
  assert.deepEqual(naming.map(rel), ["src/components/Consent.tsx"]);

  const docs = walk(join(siteDir, "docs"))
    .filter((f) => [".mjs", ".js", ".ts", ".astro", ".mdx", ".md"].includes(extname(f)))
    .filter((f) => readFileSync(f, "utf8").includes("googletagmanager.com"));
  assert.deepEqual(docs.map(rel), ["docs/src/consent.js"]);
});

test("a built docs page names Google Analytics only inside its consent script", () => {
  const page = join(siteDir, "dist", "docs", "index.html");
  const html = readFileSync(page, "utf8");
  assert.doesNotMatch(html, /<script[^>]*src="https:\/\/www\.googletagmanager\.com/, "the area loads the tag unasked");
  assert.match(html, /alegauss-consent/, "the area's pages carry no consent script");
});

// WW553. The chrome is the design system's, so a copy of one of its components coming back
// into src is the drift the move was made to end — named by the file it would be, since that
// is the name the eight product sites copied it under.
test("src carries none of the files the design system's site layer provides", () => {
  const provided = [
    "src/components/Nav.tsx",
    "src/components/Footer.tsx",
    "src/components/sections/Hero.tsx",
    "src/components/sections/FeatureIndex.tsx",
    "src/components/ui/CopyButton.tsx",
    "src/components/ui/ThemeToggle.tsx",
    "src/components/ui/Rich.tsx",
    "src/lib/theme.ts",
    "scripts/og-image.mjs",
  ];
  const present = walk(siteDir).map(rel).filter((f) => provided.includes(f));
  assert.deepEqual(present, []);
});

// The lattice loops by translating one whole repeat of its rows, so a period that does not
// divide that repeat puts a visible seam through the band once per cycle — every few
// seconds, forever. The arithmetic is the whole of the illusion, so it is asserted rather
// than trusted to whoever next retunes a layer.
test("every lattice period closes on the repeat the drift translates by", () => {
  const src = readFileSync(join(siteDir, "src", "components", "ui", "Lattice.tsx"), "utf8");
  const span = Number(/const SPAN = (\d+)/.exec(src)?.[1]);
  assert.ok(span > 0, "Lattice.tsx no longer declares SPAN");
  // the drift animation moves by 50% of a band drawn at 200% — one repeat is half the span
  const repeat = span / 2;
  const periods = [...src.matchAll(/period: (\d+)/g)].map((m) => Number(m[1]));
  assert.ok(periods.length >= 3, "expected a period per lattice layer");
  assert.deepEqual(
    periods.filter((p) => repeat % p !== 0),
    [],
    `every period must divide ${repeat}`,
  );
});

// A glyph placed at a fraction outside [0, 1) either overlaps the previous repeat or leaves
// a gap the eye reads as a stutter, and neither shows up until the band is on a page.
test("every lattice glyph sits inside its own period", () => {
  const src = readFileSync(join(siteDir, "src", "components", "ui", "Lattice.tsx"), "utf8");
  const glyphs = [...src.matchAll(/\{ at: ([\d.]+), wide: ([\d.]+)/g)].map((m) => ({
    at: Number(m[1]),
    wide: Number(m[2]),
  }));
  assert.ok(glyphs.length >= 3, "expected glyphs to check");
  const escaping = glyphs.filter((g) => g.at < 0 || g.at + g.wide > 1);
  assert.deepEqual(escaping, [], "a glyph must start and end inside one period");
});
