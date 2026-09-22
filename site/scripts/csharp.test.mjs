// The shared C# reader, and the rule that keeps it the only one. WW502.
//
// Four generators read this repository's C# to build a page, and three of them read it the same
// way. WW489 extracted that walk into `csharp.mjs` rather than write a third copy; `grammar.mjs`
// was left out of the move and kept its own `body`, `documented` and `plain` — which had already
// drifted from the shared pair about what a doc comment's first paragraph is.
//
// Nothing paired them, which is the whole defect: both were right for what they were written
// for, neither said so, and the next person to fix a rendering in one would have fixed it in the
// wrong half. This is the pairing.
import { test } from "node:test";
import assert from "node:assert/strict";
import { readFileSync, readdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const scriptsDir = dirname(fileURLToPath(import.meta.url));

/** Every generator and case in this directory, except the shared reader itself. */
const others = readdirSync(scriptsDir)
  .filter((one) => one.endsWith(".mjs") && one !== "csharp.mjs" && one !== "csharp.test.mjs")
  .map((one) => ({ name: one, text: readFileSync(join(scriptsDir, one), "utf8") }));

test("the shared reader is the only one that declares the walk", () => {
  // By name, because a second walk arrives spelled the same way the first is — somebody copies
  // the function that already works. What each generator then asks of it is its own business
  // and is never checked here, which is the split WW193 made one language over.
  const walk = ["code", "splitTop", "constructions", "bodyOf", "constants", "plain", "unescaped", "documented"];

  for (const { name, text } of others) {
    for (const one of walk) {
      assert.doesNotMatch(
        text,
        new RegExp(`(?:^|\\n)\\s*(?:export\\s+)?function\\s+${one}\\s*\\(`),
        `${name} declares its own ${one}, and csharp.mjs already has one`,
      );
    }
  }
});

test("a generator that reads the engine's C# reads it through the shared one", () => {
  // Reading a source and never importing the reader is the state grammar.mjs was in: its own
  // brace balance, its own doc comment, its own entity table, all of it a copy.
  //
  // `messages.mjs` is the exception and says so: it checks that an exact run of words is still
  // in a file, which needs no parse at all — a reader would be the wrong tool, not a shared one.
  const exceptions = new Set(["messages.mjs", "product.mjs"]);

  for (const { name, text } of others) {
    if (exceptions.has(name) || name.endsWith(".test.mjs")) continue;
    if (!text.includes('"src", "Winwright"') && !text.includes("src/Winwright")) continue;

    assert.match(
      text,
      /from "\.\/csharp\.mjs"/,
      `${name} reads the engine's C# and does not go through the shared reader`,
    );
  }
});

test("the shared reader still offers the whole walk", () => {
  // The seven a generator would otherwise copy. One of them going is how the copying starts
  // again: the caller that needed it writes its own, and nothing says the two exist.
  //
  // Whether it has started answering somebody's question instead of walking is a judgement
  // about meaning, and this makes no claim about it — a list of forbidden words would pass a
  // reader that had gone wrong in any other way, which is a check that reads like one and is
  // not. It is a thing for whoever reviews an export being added.
  const shared = readFileSync(join(scriptsDir, "csharp.mjs"), "utf8");
  const exported = [...shared.matchAll(/export function ([A-Za-z]+)/g)].map((one) => one[1]);

  assert.deepEqual(
    exported.sort(),
    ["bodyOf", "code", "constants", "constructions", "documented", "plain", "splitTop", "unescaped"],
    "the shared reader's surface changed, and every generator reads it",
  );
});
