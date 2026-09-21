import { renderToString, renderToStaticMarkup } from "react-dom/server";
import { App } from "./App";

// The build-side render. scripts/prerender.mjs imports this from the SSR bundle and calls
// render(path) once per route, then patches the head from the same route table. The route
// metadata is re-exported so the prerender reads exactly the table the assertion in
// routes.tsx has already checked against the component map.
export { ROUTE_META, canonicalUrl, outputDir, OG_IMAGE } from "./routes";

// The generated version, re-exported for the one file on this site that is authored rather
// than rendered: llms.txt is hand-written prose in public/, so the prerender substitutes
// the version into it instead of leaving two package references to go stale by hand.
export { version } from "./lib/product";

// WW500. The version picker, re-exported for `scripts/nuget-feed.test.mjs` for the same
// reason prerender.test.mjs reads dist/: the SSR bundle is the one form in which this
// site's TypeScript is something node's test runner can import. Ordering prereleases is
// where this can be wrong quietly — `0.1.0-alpha.9` against `0.1.0-alpha.18` — so it is
// asserted rather than reasoned about.
export { latestPublished, feedUrl } from "./lib/nuget-feed";

/** The hydratable render written into the HTML file. */
export function render(path: string): string {
  return renderToString(<App path={path} />);
}

/** The same component tree as clean static HTML — what the Markdown twin is converted from,
 *  so the twin cannot drift from the page because it is not authored a second time. */
export function renderStatic(path: string): string {
  return renderToStaticMarkup(<App path={path} />);
}
