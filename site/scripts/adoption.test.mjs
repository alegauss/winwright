// The captured adoption, held against the tree it claims to describe. WW493.
//
// This capture is committed rather than generated at build time: the site builds on a runner
// with no .NET, so a capture produced there is a capture that cannot exist. That makes it the
// one payload in the area nothing regenerates on its own — and a stale one is worse than a
// written paragraph, because it reads like evidence.
//
// So what is checked is everything that can be checked without running the builds again: that
// the repair it teaches is the line this repository actually depends on, that the refusal it
// shows is the one it says it is, and that a capture nobody has retaken since the SDK moved is
// a red rather than a page somebody trusts.
import { test } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const repoDir = join(siteDir, "..");

const payload = join(siteDir, "docs", "src", "data", "adoption.captured.json");
if (!existsSync(payload)) {
  throw new Error(`${payload} is missing — run \`capture-adoption.cmd\` first`);
}

const adoption = JSON.parse(readFileSync(payload, "utf8"));

test("the line it teaches is the one this repository depends on", () => {
  // `samples/Adopter` carries it because removing it is what breaks the sample. A page teaching
  // a different spelling would be teaching one nothing here is held to.
  const adopter = readFileSync(join(repoDir, "samples", "Adopter", "Adopter.csproj"), "utf8");
  const carried = /^\s*(<DefaultItemExcludes>.*<\/DefaultItemExcludes>)\s*$/m.exec(adopter);
  assert.ok(carried, "samples/Adopter/Adopter.csproj no longer carries a DefaultItemExcludes line");

  assert.equal(
    adoption.excludes,
    carried[1],
    "the capture teaches a DefaultItemExcludes line the adopter sample does not carry",
  );
  assert.match(adoption.excludes, /driving/, "the line the capture teaches excludes something other than the driving project");
});

test("the capture shows a refusal, a repair, and the order that produces them", () => {
  assert.ok(adoption.steps.length >= 3, `the capture holds ${adoption.steps.length} step(s) and the story takes three`);

  const [driving, refused, repaired] = adoption.steps;

  // The driving project alone is fine, which is the whole reason the failure arrives late and
  // about the wrong project. A capture where this one failed is a capture of something else.
  assert.equal(driving.code, 0, "the driving project's own build did not succeed in the capture");

  assert.notEqual(refused.code, 0, "the build with nothing stopping the globs succeeded, so the capture shows no refusal");
  assert.match(refused.said, /CS0579/, "the refusal the capture shows is not the duplicate-attribute one the page reads");
  assert.ok(
    [...refused.said.matchAll(/CS0579/g)].length > 1,
    "the capture shows one CS0579 and the page says there are nine",
  );

  assert.equal(repaired.code, 0, "the build with the line in it did not succeed, so the repair is not shown to work");
  assert.doesNotMatch(repaired.said, /CS0579/, "the repaired build still shows the error it repairs");
});

test("nothing in the capture is a path off the machine that took it", () => {
  // An absolute temp path is the one detail on the page that could not possibly be a reader's,
  // and it is also the one that leaks whose machine this was.
  for (const step of adoption.steps) {
    assert.ok(step.command.length > 0, `a step was captured with no command`);
    assert.ok(step.said.length > 0, `'${step.what}' was captured with no output`);
    assert.doesNotMatch(step.said, /[A-Z]:\\Users\\/, `'${step.what}' carries a path off the capturing machine`);
    assert.doesNotMatch(step.said, /AppData\\Local\\Temp/, `'${step.what}' carries a temp path`);
  }
});

test("the capture is in the language the page is written in", () => {
  // Taken on a desk set to another language, every message here comes back correct, reproducible
  // and unreadable to most of the people the page is for. It happened on the first capture.
  const [, refused, repaired] = adoption.steps;
  assert.match(refused.said, /error CS0579: Duplicate/, "the refusal was captured in another language");
  assert.match(repaired.said, /restore|Restored|up-to-date/, "the repaired build was captured in another language");
});

test("the capture names the SDK it was taken on, and it is one this tree pins", () => {
  // A capture nobody has retaken since the pin moved is a page describing a build nobody runs.
  assert.match(adoption.sdk, /^\d+\.\d+\.\d+/, `the capture names '${adoption.sdk}' as an SDK`);

  const pinned = /"version"\s*:\s*"([^"]+)"/.exec(readFileSync(join(repoDir, "global.json"), "utf8"));
  assert.ok(pinned, "global.json no longer pins an SDK version");

  const [major, minor] = pinned[1].split(".");
  assert.ok(
    adoption.sdk.startsWith(`${major}.${minor}.`),
    `the capture was taken on ${adoption.sdk} and this tree pins ${pinned[1]} — retake it with \`capture-adoption.cmd\``,
  );
});
