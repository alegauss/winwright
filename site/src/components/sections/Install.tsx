import { halves, install, nugetUrl, releasesUrl } from "../../lib/site-content";
import { harnessPackage } from "../../lib/product";
import { CopyButton } from "../ui/CopyButton";
import { Rich } from "../ui/Rich";

// No version number is typed in this section's copy, and that is the same rule as
// everywhere else rather than laziness: the reference lines below carry the one the tree
// declares, read out of Directory.Build.props by the generator, and `releases/latest`
// resolves to whatever actually shipped.
//
// Two acts, in the order an adopter performs them: the package references a csproj takes,
// then the two commands that wire the repository for whoever drives the application from a
// session. The second is optional and reads as optional.
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

          <p className="allowlist-note" style={{ marginBottom: "26px" }}>
            <Rich runs={install.note} />
          </p>

          <p className="allowlist-lead" style={{ marginBottom: "8px" }}>
            {install.agentLead}
          </p>
          <div className="install-commands">
            {install.agentCommands.map((command) => (
              <div className="codeblock copy" key={command}>
                <code>{command}</code>
                <CopyButton text={command} label="Copy the command" />
              </div>
            ))}
          </div>
          <p className="allowlist-note">
            <Rich runs={install.agentNote} />
          </p>

          <div className="hero-meta" style={{ marginTop: "30px" }}>
            {install.facts.map((fact) => (
              <span key={fact}>{fact}</span>
            ))}
          </div>

          <div className="hero-cta" data-twin="omit" style={{ marginTop: "26px" }}>
            <a className="btn btn-primary" href={nugetUrl(harnessPackage().id)}>
              {install.cta}
            </a>
            <a className="btn btn-ghost" href={releasesUrl}>
              {install.secondary}
            </a>
          </div>
        </div>
      </div>
    </section>
  );
}
