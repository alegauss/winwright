// The documentation area's joins, held against what declares them elsewhere.
//
// `site/docs/` is a second npm project with a second build, and every one of the four things
// below is a fact it shares with the pitch page next door. None of them fails loudly on its
// own: a wrong base 404s in production and nowhere else, a wrong output directory publishes a
// site with the area missing, and a build step in the wrong order deletes what it just wrote.
// So they are read out of both files and compared here.
import { test, before } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readdirSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

import { code } from "./csharp.mjs";

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

test("the built format page publishes every field of every shape the loader reads", () => {
  // WW488. The same pairing as the grammar page's, against the other generator: `format.mjs`
  // is held to the schema by `format.test.mjs`, and this says the page rendered what it read.
  // A shape whose table never reached the page is the failure that looks like nothing.
  const page = join(builtDir, "case-format", "index.html");
  assert.ok(existsSync(page), "dist/docs/case-format/index.html is missing — run `npm run build` first");
  const rendered = readFileSync(page, "utf8");

  const schema = read(repoDir, "src", "Winwright", "Scenarios", "ScenarioSchema.cs");
  const consts = new Map(
    [...schema.matchAll(/public const string ([A-Za-z]+) = "([^"]*)"/g)].map((m) => [m[1], m[2]]),
  );

  /** One `IReadOnlyList<Field>` property's body, by bracket balance. */
  const listed = (property) => {
    const at = schema.indexOf(`public static IReadOnlyList<Field> ${property} { get; }`);
    assert.ok(at >= 0, `ScenarioSchema no longer declares ${property}`);

    const from = schema.indexOf("[", at);
    let depth = 0;
    for (let i = from; i < schema.length; i++) {
      if (schema[i] === "[") depth++;
      else if (schema[i] === "]" && --depth === 0) return schema.slice(from + 1, i);
    }
    assert.fail(`ScenarioSchema.${property} is never closed`);
  };

  for (const [property, shape] of [["File", "file"], ["Case", "case"], ["Step", "step"], ["Fixture", "fixture"]]) {
    // The key each field opens with, written or addressed as a constant of this same file.
    const keys = [...listed(property).matchAll(/new\(\s*(?:"([^"]+)"|([A-Z][A-Za-z]*))\s*,\s*(?:true|false)\s*,\s*Taking\./g)]
      .map((m) => m[1] ?? consts.get(m[2]))
      .filter((one) => one !== undefined);

    // Every field and not merely the ones this regex happened to match: a field written across
    // several lines is the one it would miss, and a check that skips a field silently is the
    // shape of green this repository is about. `Taking.` is on every Field and on nothing else.
    assert.equal(
      keys.length,
      [...listed(property).matchAll(/Taking\./g)].length,
      `the ${shape}'s keys could not all be read back out of the schema`,
    );
    for (const key of keys) {
      assert.match(
        rendered,
        new RegExp(`id="${shape}-${key}"`),
        `the format page publishes no row for the ${shape}'s '${key}'`,
      );
    }
  }
});

test("the built verbs page carries a row per verb the catalogue enters, and its needs", () => {
  // WW489. `verbs.test.mjs` holds the payload to `Cooperating.Known`; this says the page
  // rendered it. The needs are checked on the page itself because that column is the reason
  // the page exists — a row published without it reads as a verb that needs nothing.
  const page = join(builtDir, "verbs", "index.html");
  assert.ok(existsSync(page), "dist/docs/verbs/index.html is missing — run `npm run build` first");
  const rendered = readFileSync(page, "utf8");

  const catalogue = read(repoDir, "tests", "Winwright.Tests", "Cooperating.cs");
  const entries = [...catalogue.matchAll(/new\("([A-Za-z]+)\.([A-Za-z]+)",\s*Cooperation\.([A-Za-z]+),\s*(true|false),/g)];
  assert.ok(entries.length > 20, `only ${entries.length} verbs could be read out of the catalogue`);

  let needing = 0;
  for (const [, family, member, needs, desk] of entries) {
    assert.match(
      rendered,
      new RegExp(`id="verb-${family}-${member}"`),
      `the verbs page publishes no row for ${family}.${member}`,
    );
    if (desk === "true" || needs !== "None") needing++;
  }

  // The page marks what a verb needs with a tag and nothing else, so the tags have to be there
  // at least as often as the catalogue says something is needed.
  const tagged = [...rendered.matchAll(/class="ww-tag ww-(?:claim|required)"/g)].length;
  assert.ok(
    tagged >= needing,
    `the catalogue needs something of ${needing} verbs and the page marks ${tagged}`,
  );
});

test("the built project page carries a row per key, and the defaults the engine seeds", () => {
  // WW490. `project.test.mjs` holds the payload to the catalogue and the suite holds the
  // catalogue to the deserialiser; this is the last hop. The defaults are checked on the page
  // itself because they are the figures a reader copies rather than reads.
  const page = join(builtDir, "project", "index.html");
  assert.ok(existsSync(page), "dist/docs/project/index.html is missing — run `npm run build` first");
  const rendered = readFileSync(page, "utf8");

  const declaration = read(repoDir, "src", "Winwright", "Projects", "ProjectDeclaration.cs");
  const at = declaration.indexOf("public static IReadOnlyList<DeclaredKey> Keys");
  assert.ok(at >= 0, "ProjectDeclaration no longer declares Keys");

  const entries = [...declaration.slice(at).matchAll(/new\(\s*"([A-Za-z]+)",\s*"([A-Za-z]*)",/g)];
  assert.ok(entries.length > 10, `only ${entries.length} keys could be read out of the catalogue`);

  for (const [, name, under] of entries) {
    const anchor = under.length > 0 ? `${under}-${name}` : name;
    assert.match(rendered, new RegExp(`id="key-${anchor}"`), `the project page publishes no row for '${name}'`);
  }

  // The seeded waits, as the page writes them. A figure typed onto a page is right on the day
  // it is typed, which is the hazard every generator here exists for.
  const seeded = [...read(repoDir, "src", "Winwright", "Projects", "Timeouts.cs").matchAll(/\["([a-zA-Z]+)"\]\s*=\s*(\d+)/g)];
  assert.ok(seeded.length > 0, "Timeouts.cs seeds nothing");
  for (const [, name, milliseconds] of seeded) {
    assert.ok(rendered.includes(`>${name}<`), `the project page never names the '${name}' timeout`);
    assert.ok(
      rendered.includes(Number(milliseconds).toLocaleString("en-GB")),
      `the project page does not show ${milliseconds} for '${name}'`,
    );
  }
});

test("the built verdict page shows every condition a hole can name", () => {
  // WW491. The page an adopter reaches for after their first `2`. A condition missing from it is
  // one they met and cannot look up, which is the symptom the page exists to close — and it is
  // invisible on a page that otherwise reads complete.
  const page = join(builtDir, "verdicts", "index.html");
  assert.ok(existsSync(page), "dist/docs/verdicts/index.html is missing — run `npm run build` first");
  const rendered = readFileSync(page, "utf8");

  const engine = join(repoDir, "src", "Winwright");
  const declared = new Set(
    readdirSync(engine, { recursive: true, withFileTypes: true })
      .filter((one) => one.isFile() && one.name.endsWith(".cs"))
      .filter((one) => !/[\\/](bin|obj)[\\/]/.test(one.parentPath ?? one.path))
      .flatMap((one) =>
        [...code(readFileSync(join(one.parentPath ?? one.path, one.name), "utf8"))
          .matchAll(/public const string (?:[A-Za-z]*PreconditionName|Named) = "([^"]+)";/g)]
          .map((m) => m[1]),
      ),
  );
  assert.ok(declared.size > 10, `only ${declared.size} conditions could be swept out of the engine`);

  for (const condition of declared) {
    // Rendered as prose, so an apostrophe in one arrives as its entity.
    assert.ok(
      rendered.includes(condition.replace(/'/g, "&#39;")) || rendered.includes(condition),
      `the verdict page never shows the condition "${condition}"`,
    );
  }
});

test("the built in-app page names every guard, and the condition a run without it answers", () => {
  // WW492. The page an adopter reads before putting this package in a product real people run.
  // A guard missing from it weakens the one claim the page makes — that the half does nothing
  // unless somebody armed it — and it weakens it invisibly.
  const page = join(builtDir, "in-app", "index.html");
  assert.ok(existsSync(page), "dist/docs/in-app/index.html is missing — run `npm run build` first");
  const rendered = readFileSync(page, "utf8");

  const half = join(repoDir, "src", "Winwright.InApp");
  const guarded = readdirSync(half, { withFileTypes: true })
    .filter((one) => one.isFile() && one.name.endsWith(".cs"))
    .flatMap((one) =>
      [...readFileSync(join(half, one.name), "utf8").matchAll(/public const string PathVariable = "([^"]+)"/g)]
        .map((m) => m[1]),
    );
  assert.ok(guarded.length > 0, "the in-app half declares no PathVariable");

  for (const variable of guarded) {
    assert.ok(rendered.includes(variable), `the in-app page never names ${variable}`);
  }

  // The message that brought a reader here, spelled as the run spells it.
  const condition = /public const string PreconditionName = "([^"]+)"/.exec(
    read(repoDir, "src", "Winwright", "Capturing", "OwnRender.cs"),
  );
  assert.ok(condition, "OwnRender no longer declares the condition a run without the half answers");
  assert.ok(
    rendered.includes(condition[1]),
    `the in-app page never shows "${condition[1]}", which is the hole that sends a reader to it`,
  );
});

test("the built adoption page shows the captured output rather than describing it", () => {
  // WW493. The claim the page makes is that these blocks were executed, so the check is that
  // what the capture holds is what the page renders — a page that summarised it would read the
  // same and be the paragraph this replaces.
  const page = join(builtDir, "adoption", "index.html");
  assert.ok(existsSync(page), "dist/docs/adoption/index.html is missing — run `npm run build` first");
  const rendered = readFileSync(page, "utf8");

  const adoption = JSON.parse(read(siteDir, "docs", "src", "data", "adoption.captured.json"));
  assert.ok(adoption.steps.length >= 3, "the capture holds fewer steps than the page reads");

  for (const step of adoption.steps) {
    assert.ok(rendered.includes(step.command), `the adoption page never shows \`${step.command}\``);
  }

  // The refusal's own first line, which is the thing a stuck reader will have on screen and
  // will search for.
  const refused = adoption.steps.find((one) => one.code !== 0);
  assert.ok(refused, "the capture shows no refusal, and the page is about one");
  assert.ok(
    rendered.includes("CS0579"),
    "the adoption page never shows the error code the capture is about",
  );
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
