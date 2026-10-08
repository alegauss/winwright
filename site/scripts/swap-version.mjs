// The version swap, out of the page's inline script so a run can watch it happen. WW510.
//
// WW506 put the swap on the area and nothing exercised it. The parts were each asserted — the
// picker against versions a case makes up, the markup against the ids it carries — and every
// one of those could hold while a reader was handed the number the page was built with: a
// selector matching nothing, a text node the highlighter split differently, a promise nobody
// awaited.
//
// An inline script is the one thing on this site no run could reach, so the whole behaviour
// moved here and the page is the two lines that call it with the real feed. `swapEveryBlock`
// takes the document and the asking as arguments for that reason: a case hands it the page the
// build wrote and a feed that answers from a table, and what runs is this and not a second
// copy written to be testable.
//
// Why `jsdom` and not the parser already here: the version lives inside the highlighter's
// `<pre>`, and `node-html-parser` gives that back as one raw string or as nothing at all,
// depending on `blockTextElements`. Neither is the tree a browser walks, and a case over
// either would have been a case about the parser.

/** Every text node under `node`, in document order. `createTreeWalker` would do this and is
 *  the document's rather than the node's; this keeps the walk here, where it needs nothing but
 *  the element it was handed. */
function texts(node) {
  if (node.nodeType === 3) return [node];

  const found = [];
  for (const child of node.childNodes ?? []) found.push(...texts(child));
  return found;
}

/**
 * Rewrite `built` to `published` everywhere this block states the version, and say how much
 * moved.
 *
 * Two things and not one, which is the specific worry rather than a general one: the
 * highlighter puts the version in its own text node and the copy button carries the whole
 * line in an attribute, so a reader who copies and a reader who reads have to get the same
 * number. A swap that moved one of them would be worse than one that moved neither.
 *
 * The text node is matched whole, because that is how the highlighter emits it — a version
 * inside a longer run of text is a sentence about the version and not the number a reader
 * acts on. The attribute is matched by substring, because it carries the whole line.
 *
 * @param {object} block the element carrying `data-built`, as the page renders it
 * @param {string} built the version the page was built with
 * @param {string} published what the feed answered
 * @returns {{ texts: number, codes: number }} how many of each were rewritten
 */
export function swapVersion(block, built, published) {
  if (!built) throw new Error("swap-version: no built version to replace");
  if (!published) throw new Error("swap-version: no published version to replace it with");
  if (built === published) return { texts: 0, codes: 0 };

  let swappedTexts = 0;
  for (const node of texts(block)) {
    if (node.textContent !== built) continue;

    node.textContent = published;
    swappedTexts++;
  }

  let swappedCodes = 0;
  for (const button of block.querySelectorAll("[data-code]")) {
    const code = button.dataset.code ?? "";
    if (!code.includes(built)) continue;

    button.dataset.code = code.split(built).join(published);
    swappedCodes++;
  }

  return { texts: swappedTexts, codes: swappedCodes };
}

/**
 * Ask the feed for every block on this page and swap the ones it answers about.
 *
 * The asking is a parameter because this is the half that could not be run otherwise: a case
 * hands over the page the build wrote and a feed that answers from a table, so the selector,
 * the two data attributes, the request per id, the promise and the rewriting are all the
 * page's own and all watched.
 *
 * Every block's requests go out together, which is what the inline script did: one request per
 * id per block and never one per line, however many times a block states the version.
 *
 * @param {Document} document the page
 * @param {(id: string) => Promise<string | null>} askFeed what the feed answers for one id
 * @returns {Promise<Array<{ built: string, published: string, texts: number, codes: number }>>}
 *   one entry per block that moved, which is what lets a caller — or a case — see that any did
 */
export function swapEveryBlock(document, askFeed) {
  const blocks = [...document.querySelectorAll(".ww-packages")].map(async (block) => {
    const built = block.dataset.built;
    const asks = (block.dataset.asks ?? "").split(" ").filter((one) => one.length > 0);
    if (!built || asks.length === 0) return null;

    // The halves are published together, so the first id that answers settles the number.
    const answers = await Promise.all(asks.map((id) => askFeed(id)));
    const published = answers.find((one) => one && one !== built);
    if (!published) return null;

    return { built, published, ...swapVersion(block, built, published) };
  });

  return Promise.all(blocks).then((all) => all.filter((one) => one !== null));
}
