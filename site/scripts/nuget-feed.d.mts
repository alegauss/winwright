// The types over `nuget-feed.mjs`, for the one consumer that is TypeScript. WW506.
//
// A declaration beside the module rather than a second implementation: the area's client script
// and this site's React both run the same code, and what differs is only whether a compiler is
// reading it. The signatures are the ones the module's own doc comments describe.

/** nuget.org's flat-container index for one package. */
export function feedUrl(packageId: string): string;

/** The version an adopter should be handed, or null where the feed carries none. */
export function latestPublished(versions: readonly string[]): string | null;

/** Ask the feed. Answers null on anything at all, so the caller keeps what it was built with. */
export function fetchLatestPublished(
  packageId: string,
  signal?: AbortSignal,
): Promise<string | null>;
