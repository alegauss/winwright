// Reading C# well enough to publish what it declares. WW488 wrote this to parse the scenario
// schema; WW489 needed the same five functions for the verb catalogue, and a second copy of a
// parser is two parsers that disagree about a file neither of them owns.
//
// It is deliberately not a C# parser. It knows four things: where a declaration's body ends,
// where a `new(...)` ends, where an argument ends, and that a doc comment is data. Everything
// past that is each generator's own question, which is the split WW193 made one level down —
// the walk is shared and the question never is.
//
// Every function here throws rather than guessing. A page built from a declaration this
// misread is a page that is confidently wrong, and the whole reason these figures are
// generated is that a wrong one reports nothing.

/** The source with what a person wrote about it taken off, so a comment cannot be read as a
 *  declaration and an apostrophe in one cannot unbalance a quote. A `//` inside a string is
 *  left alone, which is the odd-quote test `Checkout.Code` makes on the other side.
 *
 *  Doc comments go with the rest: a structural parse must not trip over one. Where the doc
 *  comment IS the subject, read the source as written and use `documented`. */
export function code(source) {
  return source
    .split(/\r?\n/)
    .map((line) => {
      if (line.trimStart().startsWith("//")) return "";
      let quoted = false;
      for (let i = 0; i < line.length; i++) {
        if (line[i] === "\\" && quoted) i++;
        else if (line[i] === '"') quoted = !quoted;
        else if (!quoted && line[i] === "/" && line[i + 1] === "/") return line.slice(0, i);
      }
      return line;
    })
    .join("\n");
}

/** Split an expression at a separator that is not inside a string, a bracket or a call. */
export function splitTop(text, separator) {
  const found = [];
  let depth = 0;
  let quoted = false;
  let at = 0;
  for (let i = 0; i < text.length; i++) {
    const letter = text[i];
    if (quoted) {
      if (letter === "\\") i++;
      else if (letter === '"') quoted = false;
      continue;
    }
    if (letter === '"') quoted = true;
    else if (letter === "(" || letter === "[" || letter === "{") depth++;
    else if (letter === ")" || letter === "]" || letter === "}") depth--;
    else if (letter === separator && depth === 0) {
      found.push(text.slice(at, i).trim());
      at = i + 1;
    }
  }
  found.push(text.slice(at).trim());
  return found;
}

/** Every `new(...)` argument list in a collection expression, in the order it declares them.
 *  Reading stops past each closing bracket, so a `new(...)` nested inside one is not a second
 *  entry. */
export function constructions(body) {
  const found = [];
  for (let i = 0; i < body.length; i++) {
    if (!body.startsWith("new(", i)) continue;

    const from = i + "new(".length;
    let depth = 1;
    let quoted = false;
    let at = from;
    for (; at < body.length && depth > 0; at++) {
      const letter = body[at];
      if (quoted) {
        if (letter === "\\") at++;
        else if (letter === '"') quoted = false;
        continue;
      }
      if (letter === '"') quoted = true;
      else if (letter === "(") depth++;
      else if (letter === ")") depth--;
    }
    if (depth !== 0) throw new Error(`csharp: a new(...) at ${i} is never closed`);

    found.push(body.slice(from, at - 1));
    i = at - 1;
  }
  return found;
}

/** One brace- or bracket-delimited body, from the declaration that opens it. */
export function bodyOf(source, opens, open, shut, where) {
  const at = source.indexOf(opens);
  if (at < 0) throw new Error(`csharp: ${where} no longer declares ${opens}`);

  // Past the declaration itself: `ActVerb[] Vocabulary` carries the opening bracket in its own
  // type, and a search from the start of the match finds that one and reads an empty list.
  const from = source.indexOf(open, at + opens.length);
  let depth = 0;
  for (let i = from; i < source.length; i++) {
    if (source[i] === open) depth++;
    else if (source[i] === shut && --depth === 0) return source.slice(from + 1, i);
  }
  throw new Error(`csharp: ${where} never closes ${opens}`);
}

/** Every `public const string NAME = "value";` a source declares, by name. A thing addressed by
 *  one of these is the same word the engine's own refusals print, and resolving it is what keeps
 *  a page from publishing the constant's name instead. */
export function constants(source) {
  return new Map(
    [...source.matchAll(/public const string ([A-Za-z]+) = "([^"]*)"/g)].map((m) => [m[1], m[2]]),
  );
}

/** A doc comment's prose as a page can print it: the inline tags carry nothing a reader of HTML
 *  needs, and `<see cref="Index"/>` is the word `Index` once the link is gone. */
export function plain(xml) {
  return xml
    .replace(/<see\s+cref="(?:[A-Za-z]+\.)*([A-Za-z]+)"\s*\/>/g, "$1")
    .replace(/<\/?(?:c|em|b|i|para)>/g, "")
    .replace(/&lt;/g, "<")
    .replace(/&gt;/g, ">")
    .replace(/&quot;/g, '"')
    .replace(/&amp;/g, "&")
    .replace(/\s+/g, " ")
    .trim();
}

/** Every `<summary>`-carrying member of a declaration, by the line that declares it.
 *
 *  Doc comments accumulate and are dropped by any other non-blank line, so a member with none
 *  of its own never inherits the one above it. `declares` says which lines are members and what
 *  each is called; anything it does not recognise clears the comment.
 *
 *  `paragraph` takes the first paragraph, which is the line a table cell is — the rest of a
 *  summary is reasoning and stays in the source, where whoever changes the thing reads it. */
export function documented(source, declares, where, paragraph = true) {
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
      if (!summary) throw new Error(`csharp: ${where} declares ${name} with no <summary> to read`);
      found.push({ name, means: plain(paragraph ? summary[1].split("<para>")[0] : summary[1]) });
      doc = [];
      continue;
    }
    if (line.length > 0) doc = [];
  }
  if (found.length === 0) throw new Error(`csharp: ${where} declares nothing this can read`);
  return found;
}
