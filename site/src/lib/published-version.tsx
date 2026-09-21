// WW500. The version the page states, and the one place it is decided.
//
// The copy in `site-content.ts` carries a token rather than a number, and this substitutes
// into it. A token rather than the generated string on purpose: a component that forgets to
// substitute renders `{{version}}`, which is visible in the build's own output and asserted
// against in `scripts/nuget-feed.test.mjs`. A stale number would have rendered as an
// ordinary version and said nothing.
//
// The generated number is what the server renders and what the client starts from, so the
// prerendered markup states a real version and the first client render matches it rather
// than tearing it.
//
// The feed can move the number down as well as up, and that is the point rather than a flaw:
// the tree declares a version the moment somebody bumps it and publishes it some time after,
// so a site deployed in between states a version nobody can restore. What is published is
// what a reader can act on, which is the only claim this page is making.
import { createContext, useCallback, useContext, useEffect, useState } from "react";
import type { ReactNode } from "react";

import { harnessPackage, version as generated } from "./product";
import { fetchLatestPublished } from "./nuget-feed";
import { VERSION } from "./site-content";

const Published = createContext<string>(generated);

/** Wraps the page so every section states one version. One fetch per page load, aborted if
 *  the tree goes away under it — the site is a full load per route, so that is once. */
export function PublishedVersionProvider({ children }: { children: ReactNode }) {
  // Widened deliberately: the generated module declares its version `as const`, so an
  // inferred state here would be the literal this tree was built at and would refuse every
  // other version the feed can answer with — which is all of them.
  const [published, setPublished] = useState<string>(generated);

  useEffect(() => {
    const abort = new AbortController();

    void fetchLatestPublished(harnessPackage().id, abort.signal).then((found) => {
      // Null is the feed declining to answer, which leaves the generated number standing.
      if (found) setPublished(found);
    });

    return () => abort.abort();
  }, []);

  return <Published.Provider value={published}>{children}</Published.Provider>;
}

/** The published version as a string, for a component that states it on its own. */
export function usePublishedVersion(): string {
  return useContext(Published);
}

/** The substitution itself, for copy that carries the token inside a longer sentence.
 *  Returns the string it was given where there is no token, so an unrelated line is not
 *  copied on every render. */
export function useVersionText(): (text: string) => string {
  const published = usePublishedVersion();
  return useCallback(
    (text: string) => (text.includes(VERSION) ? text.replaceAll(VERSION, published) : text),
    [published],
  );
}
