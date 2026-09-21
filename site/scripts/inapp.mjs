// The in-app half, read out of the package an adopter would ship. WW492.
//
// `Winwright.InApp` is the only thing this project asks anybody to put inside an application
// their users run, and that is a different decision from taking a test dependency. The README
// answers it with a list of types. The question actually being asked is what this does in a
// release nobody is testing, and the answer is a property of the code rather than a claim:
// every surface is armed by an environment variable, and unset means answer nothing.
//
// So what this reads is the guards. One source per surface, each a type declaring a
// `PathVariable` and saying in its own summary what it answers and why it is off by default.
//
//   src/Winwright.InApp/*.cs   every surface, and the variable that arms it
//
// The verbs that need this half and the condition a run without it answers are generated
// already, by `verbs.mjs` and `holes.mjs` — the page reads those rather than a third copy.
import { mkdirSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

import { plain } from "./csharp.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const siteDir = join(here, "..");
const repoDir = join(siteDir, "..");
const half = join(repoDir, "src", "Winwright.InApp");

const sources = readdirSync(half, { withFileTypes: true })
  .filter((one) => one.isFile() && one.name.endsWith(".cs"))
  .map((one) => join(half, one.name))
  .sort();

if (sources.length === 0) throw new Error("inapp: the in-app half has no sources to read");

/** The first sentence of the `<summary>` of a type.
 *
 *  A sentence and not the paragraph, which is `product.mjs`'s rule for the same reason: a table
 *  cell is one line. It also keeps the task id off the page — this repository's summaries cite
 *  one where the reasoning starts, and a reader of the site cannot resolve `WW349`. */
function declaring(text, type) {
  const at = text.indexOf(`public static class ${type}`);
  if (at < 0) return "";

  // Backwards to the doc comment that opens above it, which is the one the type carries.
  const opened = text.lastIndexOf("/// <summary>", at);
  if (opened < 0) return "";

  const closed = text.indexOf("</summary>", opened);
  if (closed < 0 || closed > at) return "";

  const paragraph = plain(
    text
      .slice(opened + "/// <summary>".length, closed)
      .replace(/^[ \t]*\/\/\/[ \t]?/gm, "")
      .split("<para>")[0],
  );

  return paragraph.split(/(?<=\.)\s/)[0];
}

const surfaces = [];
for (const path of sources) {
  const text = readFileSync(path, "utf8");
  for (const found of text.matchAll(/public static class ([A-Z][A-Za-z]*)[\s\S]{0,4000}?public const string PathVariable = "([^"]+)"/g)) {
    const [, type, variable] = found;

    // The variable's own summary says what unset means, which is the sentence the whole page
    // turns on. A surface armed by a variable nobody documented would publish as a blank.
    const guard = /\/\/\/ <summary>([\s\S]*?)<\/summary>\s*public const string PathVariable/.exec(
      text.slice(text.indexOf(`public static class ${type}`)),
    );
    if (!guard) throw new Error(`inapp: ${type}.PathVariable carries no <summary> saying what unset means`);

    surfaces.push({
      surface: type,
      variable,
      answers: declaring(text, type),
      unset: plain(guard[1].replace(/^[ \t]*\/\/\/[ \t]?/gm, "").split("<para>")[0]),
    });
  }
}

if (surfaces.length === 0) throw new Error("inapp: no surface of the in-app half declares a PathVariable");

for (const one of surfaces) {
  if (one.answers.length === 0) throw new Error(`inapp: ${one.surface} says nothing about what it answers`);
  if (one.unset.length === 0) throw new Error(`inapp: ${one.surface} says nothing about what unset means`);
}

// The message names both halves register. A harness and an application that disagree about this
// are two packages that cannot reference each other and now cannot find each other either.
const registered = [
  ...readFileSync(join(half, "Renders.cs"), "utf8")
    .matchAll(/public const string (Registered[A-Za-z]*) = "([^"]+)"/g),
].map((m) => ({ ask: m[1], named: m[2] }));

if (registered.length === 0) throw new Error("inapp: Renders registers no message for a harness to send");

const inapp = {
  surfaces: surfaces.sort((a, b) => (a.surface < b.surface ? -1 : 1)),
  registered,
  // Which framework the package needs, off the project file rather than off a sentence.
  wpf: /<UseWPF>true<\/UseWPF>/.test(readFileSync(join(half, "Winwright.InApp.csproj"), "utf8")),
};

const out = join(siteDir, "docs", "src", "data", "inapp.generated.json");
mkdirSync(dirname(out), { recursive: true });
writeFileSync(out, `${JSON.stringify(inapp, null, 2)}\n`);

console.log(
  `inapp: ${surfaces.length} surface(s), each off by default; ${registered.length} registered ask(s)`
    + ` -> docs/src/data/inapp.generated.json`,
);
