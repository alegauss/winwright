import type { ReactNode } from "react";
import type { Rich as RichRuns, Run } from "../../lib/site-content";
import { useVersionText } from "../../lib/published-version";

// Renders the tagged run list from the content module — plain text, inline <code>, <b>
// and <i> — without any markup parsing, so the copy stays data and the Markdown twin
// generator has a structure to walk rather than HTML to re-parse.
function RunNode({ run }: { run: Run }): ReactNode {
  if (typeof run === "string") return run;
  if ("code" in run) return <code>{run.code}</code>;
  if ("b" in run) return <b>{run.b}</b>;
  return <i>{run.i}</i>;
}

// WW500. Every run passes the version substitution on its way out, so a sentence carrying
// the token states the published number wherever it appears — the copy declares the token
// once and no section has to remember this. A run without one comes back as the same
// object, so this costs an allocation only where it changed something.
function substituted(run: Run, withVersion: (text: string) => string): Run {
  if (typeof run === "string") {
    const said = withVersion(run);
    return said === run ? run : said;
  }
  if ("code" in run) {
    const said = withVersion(run.code);
    return said === run.code ? run : { code: said };
  }
  if ("b" in run) {
    const said = withVersion(run.b);
    return said === run.b ? run : { b: said };
  }
  const said = withVersion(run.i);
  return said === run.i ? run : { i: said };
}

export function Rich({ runs }: { runs: RichRuns }) {
  const withVersion = useVersionText();
  return (
    <>
      {runs.map((run, i) => (
        <RunNode key={i} run={substituted(run, withVersion)} />
      ))}
    </>
  );
}
