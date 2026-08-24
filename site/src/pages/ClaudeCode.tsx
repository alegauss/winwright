import { Nav } from "../components/Nav";
import { Footer } from "../components/Footer";
import { Rich } from "../components/ui/Rich";
import { CopyButton } from "../components/ui/CopyButton";
import { claudeCode, friction } from "../lib/site-content";

const toolCount = claudeCode.read.length + claudeCode.do.length;

export function ClaudeCode() {
  return (
    <>
      <Nav />
      <header className="hero page-hero" id="top">
        <div className="wrap">
          <div className="eyebrow">{claudeCode.eyebrow}</div>
          <h1>{claudeCode.heading}</h1>
          <p className="sub">
            <Rich runs={claudeCode.intro} />
          </p>
        </div>
      </header>

      {/* the friction first: the read/do split answers one of these, and a page that
          opened on the answer told a reader who had never felt the others that the
          surface existed for keystrokes */}
      <section id="friction">
        <div className="wrap">
          <div className="sec-head reveal">
            <div className="eyebrow">{friction.eyebrow}</div>
            <h2>{friction.heading}</h2>
            <p>
              <Rich runs={friction.intro} />
            </p>
          </div>
          <div className="frictions">
            {friction.items.map((item) => (
              <div className="fr reveal" key={item.t}>
                <div className="fr-t">{item.t}</div>
                <div className="fr-pair">
                  <div className="fr-side fr-today">
                    <div className="fr-label">{friction.todayLabel}</div>
                    <div className="fr-cmd">
                      <code>{item.today.cmd}</code>
                    </div>
                    <div className="fr-body">
                      <Rich runs={item.today.body} />
                    </div>
                  </div>
                  <div className="fr-arrow" aria-hidden="true">
                    →
                  </div>
                  <div className="fr-side fr-here">
                    <div className="fr-label">{friction.hereLabel}</div>
                    <div className="fr-cmd">
                      <code>{item.here.cmd}</code>
                    </div>
                    <div className="fr-body">
                      <Rich runs={item.here.body} />
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
          <p className="fr-footer reveal">
            <Rich runs={friction.footer} />
          </p>
        </div>
      </section>

      <section id="tools">
        <div className="wrap">
          <div className="sec-head reveal">
            <h2>
              {toolCount} {claudeCode.statusLead}
            </h2>
            <p>
              <Rich runs={claudeCode.status} />
            </p>
          </div>

          <div className="verbs-split reveal">
            <div>
              <div className="verbs-head">{claudeCode.readHeading}</div>
              <div className="verbs">
                {claudeCode.read.map((v) => (
                  <div className="verb" key={v.k}>
                    <div className="verb-head">
                      <span className="verb-name">{v.k}</span>
                      <span className="verb-mark reads">reads</span>
                    </div>
                    <div className="verb-desc">{v.d}</div>
                  </div>
                ))}
              </div>
            </div>
            <div>
              <div className="verbs-head">{claudeCode.doHeading}</div>
              <div className="verbs">
                {claudeCode.do.map((v) => (
                  <div className="verb" key={v.k}>
                    <div className="verb-head">
                      <span className="verb-name">{v.k}</span>
                      <span className="verb-mark writes">drives</span>
                    </div>
                    <div className="verb-desc">{v.d}</div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </section>

      <section id="allowlist">
        <div className="wrap narrow">
          <div className="sec-head reveal">
            <h2>{claudeCode.allowlistHeading}</h2>
          </div>
          <div className="reveal">
            <p className="allowlist-lead">{claudeCode.allowlistLead}</p>
            <div className="codeblock copy">
              <code>{claudeCode.allowlistLine}</code>
              <CopyButton text={claudeCode.allowlistLine} label="Copy the allowlist entry" />
            </div>
            <p className="allowlist-note">
              <Rich runs={claudeCode.allowlistNote} />
            </p>
          </div>
        </div>
      </section>

      <section id="hook">
        <div className="wrap narrow">
          <div className="feature-section reveal">
            <h2>{claudeCode.hookHeading}</h2>
            <p>
              <Rich runs={claudeCode.hookBody} />
            </p>
          </div>
          <div className="feature-section reveal">
            <h2>{claudeCode.skillHeading}</h2>
            <p>
              <Rich runs={claudeCode.skillBody} />
            </p>
          </div>
        </div>
      </section>

      <section id="refuses">
        <div className="wrap">
          <div className="sec-head reveal">
            <h2>{claudeCode.refusesHeading}</h2>
            <p>
              <Rich runs={claudeCode.refusesLead} />
            </p>
          </div>
          <div className="refuses reveal">
            {claudeCode.refuses.map((r) => (
              <div className="refuse" key={r.t}>
                <h4>
                  <em>✗</em>
                  {r.t}
                </h4>
                <p>{r.b}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <Footer />
    </>
  );
}
