// The captured run, held against the sample it ran. WW493.
//
// Like the build capture beside it, this one is committed: it needs a desk and a packed feed, so
// nothing on a CI runner can retake it. That makes a stale one worse than a written paragraph,
// because it reads like evidence — and this page's whole claim is that it is evidence.
//
// So the two files the page shows are compared with the files the sample really carries, the
// verdict is held to what a pass says, and a capture taken against a sample that has since
// changed is a red.
import { test } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const repoDir = join(siteDir, "..");
const sample = join(repoDir, "samples", "Walkthrough");

const payload = join(siteDir, "docs", "src", "data", "walkthrough.captured.json");
if (!existsSync(payload)) {
  throw new Error(`${payload} is missing — take it with \`capture-walkthrough.cmd\` in a guest`);
}

const walkthrough = JSON.parse(readFileSync(payload, "utf8"));

const carried = (...parts) => readFileSync(join(sample, ...parts), "utf8").replace(/\r\n/g, "\n").trimEnd();

test("the files the page shows are the files the sample carries", () => {
  // Read at capture time rather than retyped, so this is the check that the capture has not gone
  // stale against a sample somebody edited afterwards.
  assert.equal(
    walkthrough.declaration.replace(/\r\n/g, "\n").trimEnd(),
    carried("winwright.json"),
    "the declaration on the page is not the one samples/Walkthrough carries",
  );
  assert.equal(
    walkthrough.case.replace(/\r\n/g, "\n").trimEnd(),
    carried("cases", "walkthrough.cases.json"),
    "the case on the page is not the one samples/Walkthrough carries",
  );
});

test("the captured run passed, and says what it passed on", () => {
  assert.equal(walkthrough.steps.length, 3, `the capture holds ${walkthrough.steps.length} step(s) and the page reads three`);

  for (const step of walkthrough.steps) {
    assert.equal(step.code, 0, `'${step.what}' exited ${step.code} in the capture the page publishes as a working adoption`);
    assert.ok(step.said.length > 0, `'${step.what}' was captured with no output`);
    assert.doesNotMatch(step.said, /[A-Z]:\\Users\\/, `'${step.what}' carries a path off the capturing machine`);
  }

  const ran = walkthrough.steps.at(-1);
  assert.match(ran.said, /^Passed:/, "the captured run does not open with a pass");
  assert.match(ran.said, /2 assertions/, "the captured run does not report the assertions the case makes");

  // A pass that covered a hole is the one thing this whole project refuses, so the page may not
  // publish one: the detail line says how many were unchecked, and it has to say none.
  assert.match(ran.said, /0 unchecked/, "the captured run publishes a pass with something unchecked in it");
});

test("the case the page shows is one the loader would accept", () => {
  // Not a load — Node cannot call the loader — but the two fields a reader copies wrongest: the
  // reading, and the acts. A case published with a reading the vocabulary does not have is a
  // case somebody pastes and is refused on.
  const declared = JSON.parse(walkthrough.case);
  assert.ok(Array.isArray(declared.cases) && declared.cases.length > 0, "the published case file declares no cases");

  const readings = [...readFileSync(join(repoDir, "src", "Winwright", "Scenarios", "ReadBack.cs"), "utf8")
    .matchAll(/^ {8}new\("([^"]+)"/gm)].map((m) => m[1]);
  const verbs = [...readFileSync(join(repoDir, "src", "Winwright", "Scenarios", "ActVerb.cs"), "utf8")
    .matchAll(/^ {8}new\(\s*"([^"]+)"/gm)].map((m) => m[1]);
  assert.ok(readings.length > 0 && verbs.length > 0, "neither vocabulary could be read out of the engine");

  for (const one of declared.cases) {
    assert.ok(one.name?.length > 0, "a published case has no name");
    for (const step of one.steps) {
      assert.ok(verbs.includes(step.act), `the published case names the act '${step.act}', which the engine does not have`);
      if (step.reads) {
        assert.ok(readings.includes(step.reads), `the published case reads '${step.reads}', which the engine does not have`);
      }
    }
  }
});

test("the capture was taken on an SDK this tree still pins", () => {
  assert.match(walkthrough.sdk, /^\d+\.\d+\.\d+/, `the capture names '${walkthrough.sdk}' as an SDK`);

  const pinned = /"version"\s*:\s*"([^"]+)"/.exec(readFileSync(join(repoDir, "global.json"), "utf8"));
  assert.ok(pinned, "global.json no longer pins an SDK version");

  const [major, minor] = pinned[1].split(".");
  assert.ok(
    walkthrough.sdk.startsWith(`${major}.${minor}.`),
    `the run was captured on ${walkthrough.sdk} and this tree pins ${pinned[1]} — retake it with \`capture-walkthrough.cmd\``,
  );
});
