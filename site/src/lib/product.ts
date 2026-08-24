// The read side of the generator. `product.generated.ts` is written by scripts/product.mjs
// out of this repository's own source on every build; this module is what the copy imports,
// so a section never reaches into the generated file's shape and a change to that shape is
// one edit rather than twelve.
//
// Nothing here invents a value. Every accessor either returns what was generated or throws,
// because a page that renders "undefined" where a version was is a page that shipped a
// missing figure as a present one.

import { product, type Package, type Verdict } from "./product.generated";

export { product };
export type { Package, Verdict };

/** The one declared version of every assembly in the tree, from Directory.Build.props. */
export const version = product.version;

/** The package the project driving the application takes. */
export function harnessPackage(): Package {
  return byRole("harness");
}

/** The package the application under test takes — and only if it wants the readings that
 *  cannot be taken from outside its process. */
export function inAppPackage(): Package {
  return byRole("app");
}

function byRole(takenBy: Package["takenBy"]): Package {
  const found = product.packages.find((p) => p.takenBy === takenBy);
  if (!found) throw new Error(`product: no package is taken by the ${takenBy}`);
  return found;
}

/** The verdicts in the order the enum declares them, which is also exit-code order. */
export const verdicts: readonly Verdict[] = product.verdicts;

export function verdict(name: string): Verdict {
  const found = product.verdicts.find((v) => v.name === name);
  if (!found) throw new Error(`product: RunOutcome no longer declares ${name}`);
  return found;
}

/** How many outcomes a run can end in. Stated in prose as a word, so the page says "four
 *  verdicts rather than two" without anybody keeping the four current by hand. */
export const verdictCount = product.verdicts.length;

const WORDS = [
  "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
  "ten", "eleven", "twelve",
];

/** A small count as the word prose wants. Above the table it falls back to the digits,
 *  which is the only honest thing a list of twelve words can do. */
export function spelled(n: number): string {
  return WORDS[n] ?? String(n);
}

/** The same, capitalised — for a heading that opens on the count. */
export function Spelled(n: number): string {
  const w = spelled(n);
  return w.charAt(0).toUpperCase() + w.slice(1);
}
