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

/** The four entities a doc comment escapes, back to the characters they stand for.
 *
 *  Its own function because a locator wants only this: `Window#main &gt; Pane` is text to
 *  publish, not prose to clean up, and running it through `plain` would collapse the spacing a
 *  reader is meant to see. */
export function unescaped(text) {
  return text
    .replace(/&lt;/g, "<")
    .replace(/&gt;/g, ">")
    .replace(/&quot;/g, '"')
    .replace(/&amp;/g, "&");
}

/** A doc comment's prose as a page can print it: the inline tags carry nothing a reader of HTML
 *  needs, and `<see cref="Index"/>` is the word `Index` once the link is gone. */
export function plain(xml) {
  return unescaped(
    xml
      .replace(/<see\s+cref="(?:[A-Za-z]+\.)*([A-Za-z]+)"\s*\/>/g, "$1")
      .replace(/<\/?(?:c|em|b|i|para)>/g, ""),
  )
    .replace(/\s+/g, " ")
    .trim();
}

/** Every member a declaration's body declares, by name. WW511.
 *
 *  A different question from `documented`'s, deliberately. That one asks which lines look like
 *  a member, which is a matcher and can be wrong in one direction without saying so: a line it
 *  does not recognise is a line nothing reports, so the member leaves the page in silence. This
 *  asks what the body separates, which no name-shape decides — `splitTop` is already here and
 *  already skips a comma inside a string, a bracket or a call.
 *
 *  The name is then read off a line this has already decided is a member, which is the safe
 *  half of the same reading: being wrong about the spelling of a member it found is a red that
 *  says so, where being wrong about whether a line is a member at all is silence.
 *
 *  So a pairing holds a generator's published list to this rather than to a second copy of the
 *  generator's own regex, which is the state WW511 found: `verbs.test.mjs` read `Cooperation`'s
 *  members with the very regex `verbs.mjs` reads them with, and checked one direction. A member
 *  both halves missed was a member neither reported.
 *
 *  Comment-only chunks are not members. A trailing comma leaves one, and so does a `//` note
 *  after the last member; neither declares anything. */
export function members(body) {
  const found = [];

  // Through `code` first, because a doc comment's prose has commas in it and `splitTop` only
  // knows about the ones inside a string, a bracket or a call. Stripping them is right here
  // and nowhere near `documented`: there the comment is the subject, and here the separators
  // are, so a sentence reading "the engine cannot call it at all — the assembly carries no
  // reference to it" was four members until this line existed.
  for (const chunk of splitTop(code(body), ",")) {
    const lines = chunk
      .split(/\r?\n/)
      .map((line) => line.trim())
      .filter((line) => line.length > 0 && !line.startsWith("//"));

    if (lines.length === 0) continue;

    // The member's own line is the last: a doc comment comes above it, and `splitTop` cut
    // after the comma that ended the member before.
    const named = /^([A-Za-z_]\w*)/.exec(lines.at(-1));
    if (!named) throw new Error(`csharp: a member declared as '${lines.at(-1)}' has no name this can read`);

    found.push({ name: named[1], declares: lines.at(-1) });
  }

  if (found.length === 0) throw new Error("csharp: that body declares no members");
  return found;
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
    // A matcher may answer with the name alone, or with an object carrying whatever else it
    // read off the line — `RunOutcome`'s members declare the process exit code, and nothing
    // else that comes through here wants it. WW507: the walk carries what it was handed and
    // never asks what it means, which is the same split one language over.
    const declared = declares(line);
    if (declared) {
      const carried = typeof declared === "string" ? { name: declared } : declared;
      const summary = /<summary>([\s\S]*?)<\/summary>/.exec(doc.join(" "));
      if (!summary) throw new Error(`csharp: ${where} declares ${carried.name} with no <summary> to read`);

      found.push({ ...carried, means: plain(paragraph ? summary[1].split("<para>")[0] : summary[1]) });
      doc = [];
      continue;
    }
    if (line.length > 0) doc = [];
  }
  if (found.length === 0) throw new Error(`csharp: ${where} declares nothing this can read`);
  return found;
}
