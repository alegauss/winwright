// The scenario format, read out of the loader's own schema. WW488.
//
// `winwright_format` already answers every field of a file, a case, a step and a fixture,
// whether it is required and the closed list of what it accepts — to an agent, in a repository
// that has installed the plugin. Somebody reading the site to decide whether to adopt this has
// neither, so the most precise description of the format is the one they cannot reach.
//
// `ScenarioSchema` is that description, and it is the same declaration the loader enforces: a
// page retyping a field name is wrong at the first rename and reports nothing when it happens.
// So this parses the declaration rather than the prose, and refuses to build where it cannot.
//
// Three sources, because two fields' closed lists are not literals:
//   src/Winwright/Scenarios/ScenarioSchema.cs   the four shapes, and what a value may be
//   src/Winwright/Scenarios/ActVerb.cs          everything `act` accepts
//   src/Winwright/Scenarios/ReadBack.cs         everything `reads` accepts
//
// What is deliberately NOT here: what each verb needs of the desk. That is the catalogue the
// suite checks in both directions, and publishing it is its own page.
import { mkdirSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

import { bodyOf, code, constants, constructions, splitTop } from "./csharp.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const siteDir = join(here, "..");
const repoDir = join(siteDir, "..");

const read = (name) =>
  readFileSync(join(repoDir, "src", "Winwright", "Scenarios", name), "utf8");

/** A C# string expression as its value: a literal, a constant, an interpolation of constants, or
 *  any of those concatenated. Throws on anything else, because a description this cannot read is
 *  one the page would publish as source code. */
function stringly(expression, consts, where) {
  return splitTop(expression, "+")
    .map((part) => {
      const piece = part.trim();
      if (consts.has(piece)) return consts.get(piece);

      const literal = /^\$?"([\s\S]*)"$/.exec(piece);
      if (!literal) throw new Error(`format: ${where} is not a string this can read: ${piece}`);

      const text = literal[1]
        .replace(/\\"/g, '"')
        .replace(/\\\\/g, "\\")
        .replace(/\\n/g, "\n")
        .replace(/\\t/g, "\t");

      if (!piece.startsWith("$")) return text;

      return text.replace(/\{([A-Za-z]+)\}/g, (whole, name) => {
        if (!consts.has(name)) throw new Error(`format: ${where} interpolates ${name}, which is no constant here`);
        return consts.get(name);
      });
    })
    .join("");
}

/** Any source under the engine, by the type it declares. A verb named by a constant of another
 *  type is still the word a case writes, so the page has to follow the reference rather than
 *  print the constant. */
const engine = join(repoDir, "src", "Winwright");
const sources = new Map(
  readdirSync(engine, { recursive: true, withFileTypes: true })
    .filter((one) => one.isFile() && one.name.endsWith(".cs"))
    .map((one) => [one.name.slice(0, -".cs".length), join(one.parentPath ?? one.path, one.name)]),
);

/** The constants one type declares, read once. */
const declared = new Map();
function constantsOf(type, where) {
  if (!declared.has(type)) {
    const file = sources.get(type);
    if (!file) throw new Error(`format: ${where} names ${type}, and the engine has no ${type}.cs`);
    declared.set(type, constants(readFileSync(file, "utf8")));
  }
  return declared.get(type);
}

/** A name a vocabulary entry opens with: written, or referred to as a constant of this type or
 *  of another. Anything else is refused, because a page publishing `OpensTrayMenu` as the word a
 *  case writes is a page that would have an author write it. */
function nameOf(expression, own, where) {
  const piece = expression.trim();
  const literal = /^"([^"]*)"/.exec(piece);
  if (literal) return literal[1];

  const qualified = /^([A-Z][A-Za-z]*)\.([A-Za-z]+)/.exec(piece);
  if (qualified) {
    const found = constantsOf(qualified[1], where).get(qualified[2]);
    if (found === undefined) throw new Error(`format: ${where} names ${piece}, which declares no such string`);
    return found;
  }

  const bare = /^([A-Za-z][A-Za-z0-9]*)/.exec(piece);
  const mine = bare ? own.get(bare[1]) : undefined;
  if (mine === undefined) throw new Error(`format: ${where} opens with ${piece.slice(0, 40)}, which is no name this can read`);
  return mine;
}

// --- the two vocabularies a closed list is read off ---
// Each entry opens with the name a case writes, which is the one thing about a verb or a reading
// this needs. Everything else in them is a lambda.
function vocabulary(source, type) {
  const own = constants(source);
  const found = constructions(
    bodyOf(code(source), `private static readonly ${type}[] Vocabulary`, "[", "]", `${type}.cs`),
  ).map((one) => nameOf(one, own, `an entry of ${type}'s vocabulary`));

  if (found.length === 0) throw new Error(`format: ${type} declares no vocabulary`);
  return found;
}

/** The same vocabulary with what each entry says about itself. WW501.
 *
 *  `reads` is the one closed list whose words are not guessable: `selected` and `picked` are the
 *  pair an author gets wrong, and choosing wrong is not a red that names the mistake — a reading
 *  the element does not offer answers null forever. So the entries carry a sentence, and one that
 *  stopped carrying it stops the build rather than reaching the page as a bare word again. */
function explained(source, type) {
  const own = constants(source);
  const found = constructions(
    bodyOf(code(source), `private static readonly ${type}[] Vocabulary`, "[", "]", `${type}.cs`),
  ).map((one) => {
    const args = splitTop(one, ",");
    const name = nameOf(args[0], own, `an entry of ${type}'s vocabulary`);
    const means = /^"([\s\S]*)"$/.exec((args[1] ?? "").trim());

    if (!means) {
      throw new Error(`format: ${type}'s '${name}' says nothing about what it reads, and the page publishes it`);
    }
    return { name, means: means[1].replace(/\\"/g, '"') };
  });

  if (found.length === 0) throw new Error(`format: ${type} declares no vocabulary`);
  return found;
}

const schemaSource = read("ScenarioSchema.cs");
const schema = code(schemaSource);
const consts = constants(schema);

const closed = new Map([
  ["ActVerb.All", vocabulary(read("ActVerb.cs"), "ActVerb")],
  ["ReadBack.All", vocabulary(read("ReadBack.cs"), "ReadBack")],
]);

/** Everything a field accepts: a list of literals, one of the vocabularies, or nothing. */
function accepts(expression, where) {
  const piece = expression.trim();
  if (piece === "[]") return [];

  for (const [named, words] of closed) {
    if (piece.startsWith(named)) return words;
  }

  if (piece.startsWith("[")) {
    return splitTop(piece.slice(1, -1), ",")
      .filter((one) => one.length > 0)
      .map((one) => stringly(one, consts, where));
  }

  throw new Error(`format: ${where} accepts something this cannot read: ${piece}`);
}

/** One `Field(...)`, positional arguments in the order the record declares them. */
function field(argv, where) {
  const args = splitTop(argv, ",");
  if (args.length < 5) throw new Error(`format: ${where} has ${args.length} arguments and a Field has at least 5`);

  const named = new Map();
  const positional = [];
  for (const one of args) {
    const labelled = /^([A-Za-z]+):\s*([\s\S]+)$/.exec(one);
    // `Taking.Text` carries a dot and never a colon, so a colon at the top level of an argument
    // is a named argument and nothing else.
    if (labelled && !one.startsWith('"')) named.set(labelled[1], labelled[2].trim());
    else positional.push(one);
  }

  const name = stringly(positional[0], consts, where);
  const holds = /^Taking\.([A-Za-z]+)$/.exec(positional[2]);
  if (!holds) throw new Error(`format: ${name} holds ${positional[2]}, which is no Taking`);

  const instead = positional[5] ?? named.get("Instead") ?? '""';
  return {
    name,
    required: positional[1] === "true",
    holds: holds[1],
    means: stringly(positional[3], consts, `${where}'s '${name}'`),
    oneOf: accepts(positional[4], `${where}'s '${name}'`),
    instead: instead === '""' ? "" : stringly(instead, consts, where),
    claims: (named.get("Claims") ?? positional[6]) === "true",
  };
}

/** One of the schema's four lists, as the page shows it. */
function shape(property, called) {
  const declared = bodyOf(
    schema,
    `public static IReadOnlyList<Field> ${property} { get; }`,
    "[",
    "]",
    "ScenarioSchema.cs",
  );

  const fields = constructions(declared).map((one) => field(one, `${called}`));
  if (fields.length === 0) throw new Error(`format: ScenarioSchema.${property} declares no fields`);
  return { shape: called, property, fields };
}

// What a value may be. The enum says it in one word per member and the summary says it in a
// sentence, and a page showing `Pairs` without the sentence is showing an implementation detail.
const kinds = (() => {
  // The source as written and not as `code` leaves it: a doc comment is what this reads, and
  // that pass takes every comment off so the structural parse cannot trip over one.
  const declared = bodyOf(schemaSource, "public enum Taking", "{", "}", "ScenarioSchema.cs");
  const found = [];
  let doc = [];
  for (const raw of declared.split(/\r?\n/)) {
    const line = raw.trim();
    if (line.startsWith("///")) {
      doc.push(line.replace(/^\/\/\/\s?/, ""));
      continue;
    }
    const member = /^([A-Z][A-Za-z]*),$/.exec(line);
    if (member) {
      const summary = /<summary>([\s\S]*?)<\/summary>/.exec(doc.join(" "));
      if (!summary) throw new Error(`format: Taking.${member[1]} carries no <summary> to read`);
      found.push({ kind: member[1], means: summary[1].replace(/\s+/g, " ").trim() });
      doc = [];
      continue;
    }
    if (line.length > 0) doc = [];
  }
  if (found.length === 0) throw new Error("format: Taking declares no members");
  return found;
})();

const format = {
  kinds,
  // WW501. The one closed list whose words a reader cannot guess, so it is published with the
  // sentence each entry carries rather than as `oneOf`'s bare names.
  readings: explained(read("ReadBack.cs"), "ReadBack"),
  shapes: [
    shape("File", "file"),
    shape("Case", "case"),
    shape("Step", "step"),
    shape("Fixture", "fixture"),
  ],
};

// The same twelve, said the same way. `reads` publishes the names and this publishes the
// sentences, and a page showing one list beside the other is a page where an author reads a
// sentence under the wrong word.
const named = format.shapes
  .find((one) => one.shape === "step")
  ?.fields.find((one) => one.name === "reads")
  ?.oneOf ?? [];

if (named.join("|") !== format.readings.map((one) => one.name).join("|")) {
  throw new Error(
    `format: 'reads' accepts ${named.join(", ")} and the sentences are for `
      + format.readings.map((one) => one.name).join(", "),
  );
}

// Every kind a field claims to hold is one the enum has. A page grouping by a kind that is not
// there would render an empty word, which reads as a field that says nothing about its value.
const known = new Set(kinds.map((one) => one.kind));
for (const { shape: called, fields } of format.shapes) {
  for (const one of fields) {
    if (!known.has(one.holds)) throw new Error(`format: the ${called}'s '${one.name}' holds ${one.holds}, which Taking does not declare`);
  }
}

const out = join(siteDir, "docs", "src", "data", "format.generated.json");
mkdirSync(dirname(out), { recursive: true });
writeFileSync(out, `${JSON.stringify(format, null, 2)}\n`);

console.log(
  `format: ${format.shapes.map((one) => `${one.fields.length} ${one.shape}`).join(", ")} field(s),`
    + ` ${kinds.length} kind(s) -> docs/src/data/format.generated.json`,
);
