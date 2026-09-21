// The refusals a reader arrives with, read out of the places that say them. WW494.
//
// Documentation is organised by cause and readers arrive by symptom. Somebody stuck has a string
// on their screen and nothing else, and every explanation this project has written is filed
// under the thing they do not yet know is wrong.
//
// So the page is a row per message, and the value of it is entirely in whether the words on it
// are the words a reader pastes into a search box. Which means they cannot be paraphrased, and
// they cannot be typed: a message retyped onto a page is one that stops matching the day
// somebody improves the sentence, and it stops matching silently.
//
// Which rows to publish is a judgement — these are the ones that stop an adoption, and no sweep
// can decide that. Where each row's text comes from is not: every one names the file that says
// it and the words that must still be in it, and a sentence that has moved stops the build. The
// prose beside a row is the page's own; the message never is.
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const siteDir = join(here, "..");
const repoDir = join(siteDir, "..");

// The rows, in the order a reader meets them: what stops an adoption before anything runs,
// then what a first case is refused on.
//
// `at` is where the message lives and `finds` is the run of words that must still be there,
// published exactly as written. `means` and `clears` are the page's own sentences — what a
// reader cannot get from the message is exactly why they came looking.
const rows = [
  {
    id: "duplicate-attribute",
    when: "you build, before anything runs",
    at: "site/docs/src/data/adoption.captured.json",
    finds: "error CS0579: Duplicate",
    means:
      "a project underneath your application's own was compiled into it. Every default glob the "
      + "SDK applies reaches the whole tree below a project file, so the driving project's "
      + "generated assembly attributes were compiled a second time.",
    clears:
      "add DefaultItemExcludes to the application's project file, naming the folder the driving "
      + "project is in. With UseWPF on, the project named in the brackets can be a _wpftmp "
      + "project you never wrote, which is why this reads like a XAML problem and is not.",
    page: "/winwright/docs/adoption/",
  },
  {
    id: "server-not-built",
    when: "the tools are missing from a Claude Code session",
    at: "tools/winwright-mcp.cmd",
    finds: "winwright: the MCP server is not built, so this session has no winwright tools.",
    means:
      "the plugin is installed and the server it launches has never been compiled. A stdio "
      + "server cannot say so over the protocol — it either speaks it or exits — so this goes to "
      + "stderr, where the harness shows it beside the failed server.",
    clears: "build the solution once in Release. The two install commands do not do it for you.",
    page: "/winwright/docs/installing/",
  },
  {
    id: "no-foreground",
    when: "a run answers 2 and names holes",
    at: "src/Winwright/Windowing/Foreground.cs",
    finds: "the foreground belongs to the window under test",
    means:
      "the assertions never ran, because an act that synthesises input needs the foreground and "
      + "Windows did not grant it. It is not a flaky suite and it is not your application: it is "
      + "a desk somebody else is using, which includes you.",
    clears:
      "give the run a desk of its own — a VM with an interactive session, or any machine nobody "
      + "is sitting at. Re-running on the same busy desk is the one response that changes nothing.",
    page: "/winwright/docs/verdicts/",
  },
  {
    id: "no-in-app-half",
    when: "a capture asks the application to render itself",
    at: "src/Winwright/Capturing/OwnRender.cs",
    finds: "an application that renders its own tree when asked",
    means:
      "the application under test does not reference Winwright.InApp, or the run that started it "
      + "did not arm the surface. Nothing quietly took a copy of the screen instead — that is the "
      + "substitution this refuses.",
    clears:
      "reference Winwright.InApp in the application, arm it where the application starts, and "
      + "launch it from a run that names somewhere to write.",
    page: "/winwright/docs/in-app/",
  },
  {
    id: "selector-matches-nothing",
    when: "you run one case by name",
    at: "src/Winwright/Scenarios/Selection.cs",
    finds: "no case is called that; there is",
    means:
      "the selector matched no case, and the run was refused rather than run. A run of no cases "
      + "has no failure and no hole in it, so it would otherwise read as a pass about nothing.",
    clears:
      "copy a name out of the list the refusal prints. Names are exact, and they are the case's "
      + "own `name` field rather than its file.",
    page: "/winwright/docs/case-format/",
  },
  {
    id: "name-declared-twice",
    when: "the cases load",
    at: "src/Winwright/Scenarios/ScenarioFile.cs",
    finds: "is declared twice, so a case naming it names two",
    means:
      "two cases across the suite carry the same name, so selecting one by name would select "
      + "two. It is refused at load, before anything is launched.",
    clears: "rename one of them. A name has to be unique across the whole suite, not just its file.",
    page: "/winwright/docs/case-format/",
  },
  {
    id: "environment-reaches-nowhere",
    when: "a fixture loads",
    at: "src/Winwright/Scenarios/FixtureDeclaration.cs",
    finds: "and nothing carries it to the launch",
    means:
      "the fixture names an environment and nothing passes it to the application, so the "
      + "expectations would be read against a setting the launch never applied.",
    clears:
      "add the `flag` the environment reaches the application through, or carry it in "
      + "`variables`. A value carrying the environment counts as carrying it.",
    page: "/winwright/docs/case-format/",
  },
];

/** Every string a parsed payload holds, joined, so a message inside one is searched for as it
 *  reads rather than as JSON spells it — a capture written by PowerShell escapes an apostrophe
 *  as a unicode point, and a sentence written the way a reader sees it would match nothing. */
function strings(value) {
  if (typeof value === "string") return [value];
  if (Array.isArray(value)) return value.flatMap(strings);
  if (value && typeof value === "object") return Object.values(value).flatMap(strings);
  return [];
}

/** The message a row names, checked where it is said.
 *
 *  An exact substring rather than a pattern, and published exactly as it is found. What a reader
 *  pastes into a search box is a run of words off their screen, so the row has to hold a run of
 *  words that is still in the source — and a regex over an interpolated C# string publishes
 *  whatever the regex happened to reach, which is how a page comes to show half a sentence.
 *
 *  Where the message interpolates a value, the row carries the fixed part. That is the half a
 *  search matches on anyway: nobody searches for their own case name. */
function said(row) {
  const raw = readFileSync(join(repoDir, row.at), "utf8");
  const text = row.at.endsWith(".json") ? strings(JSON.parse(raw)).join("\n") : raw;

  if (!text.includes(row.finds)) {
    throw new Error(
      `messages: '${row.id}' is no longer in ${row.at} — the sentence moved or was reworded, and a `
        + "page publishing the old one matches nothing a reader searches for",
    );
  }

  return row.finds;
}

const messages = rows.map((row) => ({
  id: row.id,
  when: row.when,
  says: said(row),
  means: row.means,
  clears: row.clears,
  page: row.page,
  at: row.at,
}));

const seen = new Set();
for (const one of messages) {
  if (seen.has(one.id)) throw new Error(`messages: '${one.id}' is published twice`);
  seen.add(one.id);

  if (one.says.length === 0) throw new Error(`messages: '${one.id}' publishes an empty message`);
}

const out = join(siteDir, "docs", "src", "data", "messages.generated.json");
mkdirSync(dirname(out), { recursive: true });
writeFileSync(out, `${JSON.stringify({ messages }, null, 2)}\n`);

console.log(`messages: ${messages.length} refusal(s), each read where it is said -> docs/src/data/messages.generated.json`);
