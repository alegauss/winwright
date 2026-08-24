import { agent, claudeCode } from "../../lib/site-content";
import { CopyButton } from "../ui/CopyButton";
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
          <p className="allowlist-lead">{claudeCode.allowlistLead}</p>
          <div className="codeblock copy">
            <code>{claudeCode.allowlistLine}</code>
            <CopyButton
              text={claudeCode.allowlistLine}
              label="Copy the allowlist entry"
            />
          </div>
          <p className="allowlist-note">
            <Rich runs={claudeCode.allowlistNote} />
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
