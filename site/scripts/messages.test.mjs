// The published refusals, held against the places that say them. WW494.
//
// The generator already refuses to build where a sentence has moved, so what is left to
// establish is what it cannot see: that every row still points somewhere real, that no row
// publishes a paraphrase, and that the prose beside a message is not the message again.
//
// The last one is the whole risk of a page like this. A row whose "what it means" repeats the
// words on the reader's screen has told them nothing, and it reads exactly like a row that
// helped.
import { test } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const repoDir = join(siteDir, "..");

const payload = join(siteDir, "docs", "src", "data", "messages.generated.json");
if (!existsSync(payload)) {
  throw new Error(`${payload} is missing — run \`npm run generate\` first`);
}

const { messages } = JSON.parse(readFileSync(payload, "utf8"));

test("every message is still in the file the row names", () => {
  // The generator's own check, made again here against the file rather than the payload: the
  // two agreeing is what says the row is a quotation rather than a memory of one.
  assert.ok(messages.length > 4, `only ${messages.length} refusal(s) are published`);

  for (const one of messages) {
    const at = join(repoDir, one.at);
    assert.ok(existsSync(at), `'${one.id}' names ${one.at}, and there is no such file`);

    const raw = readFileSync(at, "utf8");
    const text = one.at.endsWith(".json") ? JSON.stringify(JSON.parse(raw)) : raw;
    const found = one.at.endsWith(".json")
      ? JSON.parse(`"${one.says.replace(/"/g, '\\"')}"`)
      : one.says;

    assert.ok(
      text.includes(found) || text.includes(one.says),
      `'${one.id}' publishes a message ${one.at} does not say`,
    );
  }
});

test("no row explains a message by repeating it", () => {
  // A row whose explanation is the message again has told a stuck reader nothing, and it reads
  // exactly like a row that helped.
  for (const one of messages) {
    assert.ok(one.means.length > 40, `'${one.id}' explains itself in ${one.means.length} characters`);
    assert.ok(one.clears.length > 20, `'${one.id}' says nothing useful about what clears it`);
    assert.ok(
      !one.means.includes(one.says),
      `'${one.id}' explains the message by quoting it back`,
    );
  }
});

test("every row sends the reader somewhere that exists", () => {
  const pages = messages.map((one) => one.page);
  assert.ok(pages.every((one) => one.startsWith("/winwright/docs/")), "a row links outside the area");

  for (const one of messages) {
    // The area's routes are its content files, so a link with no page behind it is a 404 the
    // build is perfectly happy with.
    const slug = one.page.replace("/winwright/docs/", "").replace(/\/$/, "");
    const page = slug.length === 0 ? "index" : slug;
    assert.ok(
      existsSync(join(siteDir, "docs", "src", "content", "docs", `${page}.mdx`)),
      `'${one.id}' links ${one.page} and there is no ${page}.mdx to answer it`,
    );
  }
});

test("the three that stop an adoption outright are published", () => {
  // Named rather than counted. These are the measurement the page was written from: a build
  // that fails before anything runs, a plugin whose server was never compiled, and a desk that
  // grants no foreground. A page missing one of them is a page that does not answer the
  // question it was built for.
  for (const id of ["duplicate-attribute", "server-not-built", "no-foreground"]) {
    assert.ok(messages.some((one) => one.id === id), `the page no longer publishes '${id}'`);
  }
});
