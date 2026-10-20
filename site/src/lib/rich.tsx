import { SiteRich } from "@viglet/viglet-design-system/site";
import type { Rich as RichRuns } from "./site-content";
import { useVersionText } from "./published-version";

// The tagged run list from the content module, drawn by the design system's SiteRich (WW553).
// What stays here is this site's half: WW500's version substitution, so a sentence carrying the
// token states the published number wherever it appears and no section has to remember it.
export function Rich({ runs }: { runs: RichRuns }) {
  return <SiteRich runs={runs} transform={useVersionText()} />;
}
