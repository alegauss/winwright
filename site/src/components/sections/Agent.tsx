import { agent, claudeCode } from "../../lib/site-content";
import { Rich } from "../ui/Rich";

export function Agent() {
  return (
    <section id="agent">
      <div className="wrap">
        <div className="sec-head reveal">
          <div className="eyebrow">{agent.eyebrow}</div>
          <h2>{agent.heading}</h2>
          <p>
            <Rich runs={agent.intro} />
          </p>
        </div>

        <div className="reveal" style={{ maxWidth: "620px", margin: "0 auto" }}>
          {/* The four tools, read off the same records the depth page lists in full, so a
              tool cannot appear here and not there. The mark is the distinction that
              matters before anything else: three of them launch nothing. */}
          <div className="pills" style={{ marginTop: 0 }}>
            {claudeCode.read.map((tool) => (
              <span className="pill" key={tool.k}>
                <code>{tool.k}</code>
              </span>
            ))}
            {claudeCode.do.map((tool) => (
              <span className="pill" key={tool.k}>
                <code>{tool.k}</code>
              </span>
            ))}
          </div>
          <p className="allowlist-note">
            <Rich runs={agent.note} />
          </p>
          <p style={{ textAlign: "center", marginTop: "22px" }}>
            <a className="feature-link" href="/winwright/claude-code/">
              {agent.cta}
            </a>
          </p>
        </div>
      </div>
    </section>
  );
}
