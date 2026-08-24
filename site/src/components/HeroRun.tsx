import { useEffect, useRef } from "react";
import { heroRun } from "../lib/site-content";
import { Rich } from "./ui/Rich";

// The hero is a run: an autoplaying transcript of one case, because what is sold here is
// the shape of the answer rather than a screenshot of a window. All steps render on the
// server and with no JS (so the twin and a crawler read the whole thing); the autoplay only
// reveals them one at a time after mount, which keeps SSR and the first client render
// identical and never trips hydration.
//
// Only the reader moves the window. As each step lands the panel scrolls ITS OWN element
// (scrollTop), never scrollIntoView, which would drag a reader who has scrolled past the
// hero back to it every step.
export function HeroRun() {
  const panelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const panel = panelRef.current;
    if (!panel) return;
    if (matchMedia("(prefers-reduced-motion: reduce)").matches) return;

    const steps = Array.from(panel.querySelectorAll<HTMLElement>(".run-step"));
    if (steps.length === 0) return;

    panel.classList.add("run--playing"); // CSS hides the steps until each gets .in
    let i = 0;
    let timer = 0;
    const tick = () => {
      if (i >= steps.length) return;
      steps[i].classList.add("in");
      panel.scrollTop = panel.scrollHeight; // own element only
      i += 1;
      timer = window.setTimeout(tick, 1250);
    };
    timer = window.setTimeout(tick, 450);
    return () => window.clearTimeout(timer);
  }, []);

  return (
    <div className="run reveal">
      <div className="run-ask">
        <span className="run-ask-tag">Case</span>
        <span className="run-ask-text">{heroRun.question}</span>
      </div>
      <div className="run-scroll" ref={panelRef}>
        <div className="run-step">
          <div className="run-cmd">
            <span className="run-prompt">›</span>
            <code>{heroRun.command}</code>
          </div>
          <pre className="run-out run-pack">{heroRun.preamble.join("\n")}</pre>
        </div>
        {heroRun.steps.map((step) => (
          <div className="run-step" key={step.cmd}>
            <div className="run-cmd">
              <span className="run-prompt">·</span>
              <code>{step.cmd}</code>
              <span className={`run-mark ${step.mark}`}>
                {step.mark === "ok" ? "observed" : "not observed"}
              </span>
            </div>
            <div className="run-out">{step.out}</div>
          </div>
        ))}
      </div>
      <div className="run-foot">
        <span className="run-cmp before">{heroRun.before}</span>
        <span className="run-cmp after">{heroRun.after}</span>
      </div>
      <p className="run-note">
        <Rich runs={heroRun.note} />
      </p>
    </div>
  );
}
