// The swap, run over the page the build actually wrote. WW510.
//
// Both halves of the site state a version the browser asks nuget.org for, and until this
// nothing exercised the swap on either. The parts were asserted and the behaviour was not:
// `nuget-feed.test.mjs` runs the picker against versions it makes up, `docs-area.test.mjs`
// asserts the block carries the version it was built with and the ids to ask — and every one
// of those holds while a reader is handed the built number forever.
//
// So this takes the real built HTML into a real DOM, hands `swapEveryBlock` a feed that
// answers from a table, and watches what the reader would have been handed. The selectors, the
// two data attributes, the request per id, the promise and the rewriting are the page's own.
//
// What it does not take is a browser, so loading the module over HTTP is still unexercised —
// an `import` the bundler got wrong would pass here. That is the remainder recorded on the
// line, and it is a smaller thing than what this closes.
import { test } from "node:test";
import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { JSDOM } from "jsdom";

import { swapEveryBlock, swapVersion } from "./swap-version.mjs";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const built = join(siteDir, "dist", "docs", "installing", "index.html");

/** The installing page in a DOM, as the build wrote it. */
function page() {
  assert.ok(
    existsSync(built),
    "the area is not built, and this case is about the page the build produced — run `npm run build`",
  );

  return new JSDOM(readFileSync(built, "utf8")).window.document;
}

/** Every version the page was built stating, which is one per block and the same one. */
function versions(document) {
  const found = [...document.querySelectorAll(".ww-packages")].map((one) => one.dataset.built);
  assert.ok(found.length > 0, "the installing page states no package references");
  assert.ok(
    found.every((one) => one && one.length > 0),
    "a block carries no built version, so the page cannot know what to swap",
  );

  return found;
}

test("the number a reader sees becomes the one the feed published", async () => {
  const document = page();
  const was = versions(document)[0];

  const moved = await swapEveryBlock(document, () => Promise.resolve("9.9.9-test"));

  assert.equal(moved.length, document.querySelectorAll(".ww-packages").length);
  for (const block of document.querySelectorAll(".ww-packages")) {
    assert.ok(
      block.textContent.includes("9.9.9-test"),
      "a block does not state the published version after the swap",
    );
    assert.ok(
      !block.textContent.includes(`Version="${was}"`),
      `a block still states Version="${was}", which is the stale number a reader copies`,
    );
  }
});

test("a reader who copies gets the same number as a reader who reads", async () => {
  // The specific worry rather than a general one. The page rewrites two things for one number,
  // and the two disagreeing is worse than both being stale: one reader pastes a version the
  // other was never shown, and nothing on the page says which is right.
  const document = page();
  const was = versions(document)[0];

  const moved = await swapEveryBlock(document, () => Promise.resolve("9.9.9-test"));

  for (const one of moved) assert.ok(one.codes > 0, "a block swapped its text and not its copy button");

  for (const button of document.querySelectorAll(".ww-packages [data-code]")) {
    assert.ok(
      button.dataset.code.includes("9.9.9-test"),
      "the copy button still hands over the built version",
    );
    assert.ok(
      !button.dataset.code.includes(`Version="${was}"`),
      `the copy button still hands over Version="${was}"`,
    );
  }
});

test("the swap moves the version and nothing else on the line", async () => {
  const document = page();

  // The ids this block states, which is not what `data-asks` carries: that is every id to ask
  // the feed, and a block renders one half. They are the part of the line that must not move,
  // and are what a swap over the block as one string would have taken with it.
  const stated = [...document.querySelectorAll(".ww-packages")].map((block) =>
    (block.dataset.asks ?? "")
      .split(" ")
      .filter((id) => id.length > 0 && block.textContent.includes(`Include="${id}"`)),
  );

  assert.ok(stated.flat().length > 0, "no block states a package this run can watch survive");

  await swapEveryBlock(document, () => Promise.resolve("9.9.9-test"));

  const blocks = [...document.querySelectorAll(".ww-packages")];
  for (const [at, ids] of stated.entries()) {
    for (const id of ids) {
      assert.ok(
        blocks[at].textContent.includes(`Include="${id}"`),
        `${id} did not survive the swap`,
      );
    }
  }
});

test("one request per id per block, and never one per line", async () => {
  // What the inline script promised in a comment and nothing held it to. A block states the
  // version once or twice depending on the half, and a request per statement would be the feed
  // asked four times for a page that needs two answers.
  const document = page();
  const asked = [];

  await swapEveryBlock(document, (id) => {
    asked.push(id);
    return Promise.resolve("9.9.9-test");
  });

  const expected = [...document.querySelectorAll(".ww-packages")].flatMap((block) =>
    (block.dataset.asks ?? "").split(" ").filter((one) => one.length > 0),
  );

  assert.deepEqual(asked.sort(), expected.sort());
});

test("a feed that declines leaves the reader the number the page was built with", async () => {
  // Null is the feed not knowing — offline, behind a proxy, a package that has never been
  // published — and the generated number is what the page ships for exactly that reason. A
  // swap to `null` would have put the word on the one line somebody copies into a csproj.
  const document = page();
  const was = versions(document)[0];

  for (const answer of [null, undefined, ""]) {
    const same = page();
    assert.deepEqual(await swapEveryBlock(same, () => Promise.resolve(answer)), []);

    for (const block of same.querySelectorAll(".ww-packages"))
      assert.ok(block.textContent.includes(was), `the block stopped stating ${was}`);
  }

  // And the feed answering what the page already says is not a swap either.
  assert.deepEqual(await swapEveryBlock(document, () => Promise.resolve(was)), []);
});

test("the swap refuses a version it cannot act on", () => {
  // Rather than quietly doing nothing, which is how the defect WW510 is about looks from the
  // outside: a page that never swapped and a page handed nothing to swap to read alike.
  const block = page().querySelector(".ww-packages");

  assert.throws(() => swapVersion(block, "", "9.9.9"), /no built version/);
  assert.throws(() => swapVersion(block, "1.0.0", ""), /no published version/);
});

test("the page's script is the feed and nothing a run cannot reach", () => {
  // The pairing that keeps this case meaningful. The behaviour moved out of the inline script
  // so a run could watch it, and a copy going back in is the defect returning — the way these
  // things do, with somebody editing the page rather than the module.
  const script = readFileSync(join(siteDir, "docs", "src", "components", "Packages.astro"), "utf8");

  assert.match(script, /import \{ swapEveryBlock \} from "\.\.\/\.\.\/\.\.\/scripts\/swap-version\.mjs"/);

  for (const own of ["createTreeWalker", "NodeFilter", "querySelectorAll", "dataset.built", "Promise.all"]) {
    assert.ok(
      !script.includes(own),
      `Packages.astro does ${own} itself, and swap-version.mjs already does — so a run watches one and a reader gets the other`,
    );
  }
});
