// The locator grammar, read out of the parser that defines it. WW487.
//
// Every case in every adopting repository is written in this grammar, and until now the only
// place it was written down was a section of a README a thousand lines long. The page that
// replaces that section is `site/docs/src/content/docs/locators.mdx`, and none of what it
// states is typed there: a table of keys beside `Locator.Parse` is wrong at the first
// predicate added, and wrong in the direction nothing reports.
//
// Four sources, and each answers a different half of the page:
//   src/Winwright/Locating/Locator.cs                  the forms, the keys, and which
//                                                      refusals the parser actually throws
//   src/Winwright/Locating/LocatorStep.cs              what each key matches, and the orders
//   src/Winwright/Locating/LocatorSyntaxException.cs   every way a locator can fail to parse
//
// What is NOT derivable here, and is said in prose on the page instead: the control types and
// the patterns. `UiaVocabulary` reads both off UI Automation itself at run time — deliberately,
// so a list cannot drift from the thing it describes — and no parse of a source file can reach
// them. The page says where a refusal offers the nearest spelling instead of listing hundreds.
//
// The examples are proven rather than published on trust. `LocatorTests` parses every row of
// the block this reads, and fails on one that stopped parsing — so a form on the page is one
// the suite ran.
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const siteDir = join(here, "..");
const repoDir = join(siteDir, "..");

const read = (...parts) => readFileSync(join(repoDir, "src", "Winwright", "Locating", ...parts), "utf8");

/** The four entities a doc comment escapes, back to the characters they stand for. */
function unescaped(text) {
  return text
    .replace(/&lt;/g, "<")
    .replace(/&gt;/g, ">")
    .replace(/&quot;/g, '"')
    .replace(/&amp;/g, "&");
}

/** A doc comment's prose as a page can print it: the inline tags carry nothing a reader of
 *  HTML needs, and `<see cref="Index"/>` is the word `Index` once the link is gone. */
function plain(xml) {
  return unescaped(
    xml
      .replace(/<see\s+cref="(?:[A-Za-z]+\.)*([A-Za-z]+)"\s*\/>/g, "$1")
      .replace(/<\/?(?:c|em|b|i)>/g, ""),
  )
    .replace(/\s+/g, " ")
    .trim();
}

/** The first sentence of a `<summary>`, which is the one line a table cell is.
 *
 *  The rest of the summary is a paragraph of reasoning and stays in the source, where it is
 *  read by whoever changes the thing it reasons about. Same decision as `product.mjs`, for the
 *  same reason. */
function firstSentence(summary) {
  return plain(summary.split("<para>")[0]).split(/(?<=\.)\s/)[0];
}

/** Every `<summary>`-carrying member of a declaration, by the line that declares it.
 *
 *  Doc comments accumulate and are dropped by any other non-blank line, so a member with none
 *  of its own never inherits the one above it. `declares` says which lines are members and
 *  what each is called; anything it does not recognise clears the comment. */
function documented(source, declares, where) {
  const found = [];
  let doc = [];
  for (const raw of source.split(/\r?\n/)) {
    const line = raw.trim();
    if (line.startsWith("///")) {
      doc.push(line.replace(/^\/\/\/\s?/, ""));
      continue;
    }
    const name = declares(line);
    if (name) {
      const summary = /<summary>([\s\S]*?)<\/summary>/.exec(doc.join(" "));
      if (!summary) throw new Error(`grammar: ${where} declares ${name} with no <summary> to read`);
      found.push({ name, meaning: firstSentence(summary[1]) });
      doc = [];
      continue;
    }
    if (line.length > 0) doc = [];
  }
  if (found.length === 0) throw new Error(`grammar: ${where} declares nothing this can read`);
  return found;
}

/** One brace-delimited body out of a source, from the line that opens it. */
function body(source, opens, where) {
  const at = source.indexOf(opens);
  if (at < 0) throw new Error(`grammar: ${where} no longer declares ${opens}`);
  const from = source.indexOf("{", at);
  let depth = 0;
  for (let i = from; i < source.length; i++) {
    if (source[i] === "{") depth++;
    else if (source[i] === "}" && --depth === 0) return source.slice(from + 1, i);
  }
  throw new Error(`grammar: ${where} never closes ${opens}`);
}

const locatorSource = read("Locator.cs");
const stepSource = read("LocatorStep.cs");
const faultSource = read("LocatorSyntaxException.cs");

// --- the forms ---
// The shape of the grammar, as the type that parses it states it: one row per form, the
// locator and what it addresses. It sits in `Locator`'s own <remarks>, which is the summary a
// reader of the source meets and the one a reviewer of a new predicate has to change.
function forms(source) {
  const blocks = [...source.matchAll(/<code>([\s\S]*?)<\/code>/g)];
  if (blocks.length !== 1) {
    throw new Error(
      `grammar: Locator.cs carries ${blocks.length} <code> blocks and this reads the one that`
        + " states the grammar — say which",
    );
  }

  const rows = [];
  for (const raw of blocks[0][1].split(/\r?\n/)) {
    const line = raw.trim().replace(/^\/\/\/\s?/, "").trim();
    if (line.length === 0) continue;

    // Two or more spaces: the block is two aligned columns, and a locator never carries a run
    // of spaces outside a quoted value — one that did would show up here as a split row rather
    // than as a wrong page.
    const split = line.split(/\s{2,}/);
    if (split.length !== 2) {
      throw new Error(`grammar: '${line}' is not a locator and a gloss in two aligned columns`);
    }
    rows.push({ locator: unescaped(split[0]), addresses: unescaped(split[1]) });
  }
  if (rows.length === 0) throw new Error("grammar: the <code> block in Locator.cs states no forms");
  return rows;
}

// --- the predicates ---
// What parses is the switch in `Step`, and nothing else: `Keys` is the sentence a refusal
// prints and is held against the switch below rather than read as the truth.
//
// What each key *matches* is the summary of the field it fills. The arm names that field, so
// the join is derived too — `class` fills `className`, which is `ClassName`, and no table here
// says so.
function predicates() {
  const step = body(locatorSource, "private static LocatorStep Step(", "Locator.cs");
  const arms = [...body(step, "switch (key)", "Locator.cs's Step").matchAll(/case "([A-Za-z]+)":([\s\S]*?)(?=\n\s*(?:case "|default:))/g)];
  if (arms.length === 0) throw new Error("grammar: the switch in Locator.cs's Step has no arms to read");

  const fields = new Map(
    documented(
      stepSource,
      (line) => /^public [\w?<>, ]+ ([A-Z][A-Za-z]*) \{ get/.exec(line)?.[1],
      "LocatorStep.cs",
    ).map((one) => [one.name, one.meaning]),
  );

  const found = [];
  for (const [, key, arm] of arms) {
    const filled = [...arm.matchAll(/^[ \t]*([a-z][A-Za-z0-9]*) = /gm)].at(-1)?.[1];
    if (!filled) throw new Error(`grammar: the '${key}' arm fills no field this can name`);

    const property = filled[0].toUpperCase() + filled.slice(1);
    const meaning = fields.get(property);
    if (!meaning) {
      throw new Error(`grammar: '${key}' fills ${filled}, and LocatorStep declares no ${property}`);
    }
    found.push({ key, matches: meaning });
  }

  // `Keys` is what a refusal over an unknown key offers, so a key the switch accepts and that
  // sentence omits is a key nobody is told about — and the page would publish a set the
  // refusal contradicts.
  const listed = /private const string Keys = "([^"]+)"/.exec(locatorSource);
  if (!listed) throw new Error("grammar: Locator.cs no longer declares Keys");

  const said = listed[1].split(",").map((one) => one.trim());
  const accepted = found.map((one) => one.key);
  if (said.join("|") !== accepted.join("|")) {
    throw new Error(
      `grammar: the parser accepts ${accepted.join(", ")} and its refusal offers ${said.join(", ")}`,
    );
  }

  return { predicates: found, fields };
}

// --- the orders ---
// `MatchOrder` has five members and the grammar takes four: `Tree` is the tree's own order,
// which is what a step saying nothing about it already gets, so writing it would be a
// predicate that narrows nothing. Which one is refused is read off the refusal.
function orders() {
  const declared = documented(
    body(stepSource, "public enum MatchOrder", "LocatorStep.cs"),
    (line) => /^([A-Z][A-Za-z]*),$/.exec(line)?.[1],
    "LocatorStep.cs's MatchOrder",
  );

  const refused = [...locatorSource.matchAll(/sorted == MatchOrder\.([A-Za-z]+)/g)].map((m) => m[1]);
  if (refused.length === 0) throw new Error("grammar: Locator.cs refuses no MatchOrder, so every member parses");

  for (const one of refused) {
    if (!declared.some((member) => member.name === one)) {
      throw new Error(`grammar: Locator.cs refuses MatchOrder.${one}, which MatchOrder does not declare`);
    }
  }

  return declared
    .filter((one) => !refused.includes(one.name))
    .map((one) => ({ order: one.name.toLowerCase(), meaning: one.meaning }));
}

// --- the refusals ---
// Half of what a grammar is worth is in what it will not parse, and these are the thirteen
// ways it says so. Held both ways: an arm thrown that the enum does not declare would not
// compile, but an arm declared and never thrown is a refusal the page describes and the parser
// cannot produce — which is the direction that goes unnoticed.
function refusals() {
  const declared = documented(
    body(faultSource, "public enum LocatorFault", "LocatorSyntaxException.cs"),
    (line) => /^([A-Z][A-Za-z]*),$/.exec(line)?.[1],
    "LocatorSyntaxException.cs's LocatorFault",
  );

  const thrown = new Set([...locatorSource.matchAll(/LocatorFault\.([A-Za-z]+)/g)].map((m) => m[1]));
  for (const one of thrown) {
    if (!declared.some((member) => member.name === one)) {
      throw new Error(`grammar: Locator.cs throws LocatorFault.${one}, which the enum does not declare`);
    }
  }

  // The default member is the one a throw that did not say which leaves behind. It is the only
  // member the parser never names, and a second one appearing here is an arm that stopped
  // being reachable rather than a page that needs a shorter table.
  const unthrown = declared.filter((one) => !thrown.has(one.name));
  if (unthrown.length !== 1 || unthrown[0].name !== declared[0].name) {
    throw new Error(
      `grammar: ${unthrown.map((one) => one.name).join(", ") || "no member"} is never thrown, and the`
        + ` only one that should be is ${declared[0].name}`,
    );
  }

  return declared.slice(1).map((one) => ({ arm: one.name, meaning: one.meaning }));
}

const { predicates: keys } = predicates();
const grammar = {
  forms: forms(locatorSource),
  predicates: keys,
  orders: orders(),
  refusals: refusals(),
};

// Every key the parser accepts has a form on the page showing it. A predicate added with no
// example is one a reader meets as a word in a table and has to guess the spelling of.
for (const { key } of grammar.predicates) {
  if (!grammar.forms.some((form) => form.locator.includes(`[${key}=`))) {
    throw new Error(`grammar: no form in Locator.cs's <code> block shows [${key}=...]`);
  }
}

const out = join(siteDir, "docs", "src", "data", "grammar.generated.json");
mkdirSync(dirname(out), { recursive: true });
writeFileSync(out, `${JSON.stringify(grammar, null, 2)}\n`);

console.log(
  `grammar: ${grammar.forms.length} form(s), ${grammar.predicates.length} predicate(s),`
    + ` ${grammar.orders.length} order(s), ${grammar.refusals.length} refusal(s)`
    + " -> docs/src/data/grammar.generated.json",
);
