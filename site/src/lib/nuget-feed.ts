// WW500. What is actually published, asked of the feed rather than read off the build.
//
// `scripts/product.mjs` takes the version from `Directory.Build.props`, which is the right
// source at the moment the site is built and the wrong one an hour later: the site deploys on
// `workflow_dispatch` and publishing a release does not trigger it, so the two drift by
// construction. nuget.org is the authority on what an adopter can restore, and it answers a
// browser directly — the flat-container index sends `Access-Control-Allow-Origin: *`.
//
// Nothing here throws and nothing here returns a version it invented. The caller keeps the
// generated number until this answers with a better one, so the failure mode is the page
// exactly as correct as it is today.

/** nuget.org's flat-container index for one package: every version ever pushed, as strings.
 *  The id is lowercased because that endpoint is the one part of the API that requires it. */
export function feedUrl(packageId: string): string {
  return `https://api.nuget.org/v3-flatcontainer/${packageId.toLowerCase()}/index.json`;
}

interface Parsed {
  readonly release: readonly number[];
  readonly pre: readonly string[];
}

function parse(version: string): Parsed {
  // Build metadata is not part of precedence, so it goes before anything is compared.
  const [core, ...tail] = version.split("+")[0].split("-");
  const pre = tail.join("-");
  return {
    release: core.split(".").map((part) => Number.parseInt(part, 10) || 0),
    pre: pre ? pre.split(".") : [],
  };
}

/** SemVer precedence, numerically and never as text. `0.1.0-alpha.9` precedes
 *  `0.1.0-alpha.18`, which a string comparison gets backwards — and this package spent
 *  eighteen prereleases in exactly the range where it would have. */
function compare(a: string, b: string): number {
  const left = parse(a);
  const right = parse(b);

  for (let i = 0; i < Math.max(left.release.length, right.release.length); i++) {
    const difference = (left.release[i] ?? 0) - (right.release[i] ?? 0);
    if (difference !== 0) return difference;
  }

  // A release outranks any prerelease of the same core version — SemVer's own rule 11.3,
  // and the reason 1.0.0 must win over 1.0.0-rc.1 rather than losing on field count.
  if (left.pre.length === 0 && right.pre.length > 0) return 1;
  if (left.pre.length > 0 && right.pre.length === 0) return -1;

  for (let i = 0; i < Math.max(left.pre.length, right.pre.length); i++) {
    const one = left.pre[i];
    const other = right.pre[i];
    // A shorter set of prerelease fields is the lower one where all its fields matched.
    if (one === undefined) return -1;
    if (other === undefined) return 1;

    const oneIsNumeric = /^\d+$/.test(one);
    const otherIsNumeric = /^\d+$/.test(other);
    if (oneIsNumeric && otherIsNumeric) {
      const difference = Number(one) - Number(other);
      if (difference !== 0) return difference;
    } else if (oneIsNumeric !== otherIsNumeric) {
      // Numeric fields have lower precedence than alphanumeric ones.
      return oneIsNumeric ? -1 : 1;
    } else if (one !== other) {
      return one < other ? -1 : 1;
    }
  }

  return 0;
}

/** The version an adopter should be handed: the highest release where one has ever shipped,
 *  and the highest prerelease only where none has. A feed carrying nothing answers null,
 *  because a page with no version to state keeps the one it was built with. */
export function latestPublished(versions: readonly string[]): string | null {
  const usable = versions.filter((version) => version.trim() !== "");
  if (usable.length === 0) return null;

  const released = usable.filter((version) => !version.includes("-"));
  const pool = released.length > 0 ? released : usable;
  return pool.reduce((best, version) => (compare(version, best) > 0 ? version : best));
}

/** Ask the feed, and answer null on anything at all — an unreachable index, a shape that is
 *  not the one documented, a reader offline behind a proxy. Every one of those means the
 *  same thing to the caller, which is that the generated number stands. */
export async function fetchLatestPublished(
  packageId: string,
  signal?: AbortSignal,
): Promise<string | null> {
  try {
    const answer = await fetch(feedUrl(packageId), { signal });
    if (!answer.ok) return null;

    const body: unknown = await answer.json();
    const versions = (body as { versions?: unknown })?.versions;
    if (!Array.isArray(versions)) return null;

    return latestPublished(versions.filter((v): v is string => typeof v === "string"));
  } catch {
    return null;
  }
}
