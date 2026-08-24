import { halves, install, releasesUrl } from "../../lib/site-content";
import { CopyButton } from "../ui/CopyButton";
import { Rich } from "../ui/Rich";

// No version number is typed in this section's copy, and that is the same rule as
// everywhere else rather than laziness: the reference lines below carry the one the tree
// declares, read out of Directory.Build.props by the generator, and `releases/latest`
// resolves to whatever actually shipped.
export function Install() {
  return (
    <section id="install">
      <div className="wrap">
        <div className="sec-head reveal">
          <div className="eyebrow">{install.eyebrow}</div>
          <h2>{install.heading}</h2>
          <p>
            <Rich runs={install.intro} />
          </p>
        </div>

        <div className="reveal" style={{ maxWidth: "720px", margin: "0 auto" }}>
          {halves.actors.map((actor) => (
            <div key={actor.who} style={{ marginBottom: "14px" }}>
              <p className="allowlist-lead" style={{ marginBottom: "8px" }}>
                {actor.sub}
              </p>
              <div className="codeblock copy">
                <code>{actor.iface}</code>
                <CopyButton text={actor.iface} label={`Copy the ${actor.who} reference`} />
              </div>
            </div>
          ))}

          <div className="hero-meta" style={{ marginTop: "26px" }}>
            {install.facts.map((fact) => (
              <span key={fact}>{fact}</span>
            ))}
          </div>

          <div className="hero-cta" data-twin="omit" style={{ marginTop: "26px" }}>
            <a className="btn btn-primary" href={releasesUrl}>
              {install.cta}
            </a>
            <a className="btn btn-ghost" href={releasesUrl}>
              {install.secondary}
            </a>
          </div>

          <p className="allowlist-note">
            <Rich runs={install.note} />
          </p>
        </div>
      </div>
    </section>
  );
}
