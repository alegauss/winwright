import { halves, install, nugetUrl, releasesUrl } from "../../lib/site-content";
import { harnessPackage } from "../../lib/product";
import { useVersionText } from "../../lib/published-version";
import { SiteButton, SiteCopyButton } from "@viglet/viglet-design-system/site";
import { Rich } from "../../lib/rich";

// No version number is typed in this section's copy, and that is the same rule as
// everywhere else rather than laziness: the reference lines below carry the one the tree
// declares, read out of Directory.Build.props by the generator, and the nuget.org page
// resolves to whatever actually shipped.
//
// Two acts, in the order an adopter performs them: the package references a csproj takes,
// then the two commands that wire the repository for whoever drives the application from a
// session. The second is optional and reads as optional.
export function Install() {
  // WW500. The reference lines are the one thing on this page that leaves it — they are
  // copied into a csproj — so they carry the published version rather than the built one,
  // and the button copies the same string the block displays.
  const withVersion = useVersionText();
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
                <code>{withVersion(actor.iface)}</code>
                <SiteCopyButton
                  text={withVersion(actor.iface)}
                  label={`Copy the ${actor.who} reference`}
                  copyLabel="Copy"
                  copiedLabel="Copied!"
                />
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
                <SiteCopyButton text={command} label="Copy the command" copyLabel="Copy" copiedLabel="Copied!" />
              </div>
            ))}
          </div>
          <p className="allowlist-note">
            <Rich runs={install.agentNote} />
          </p>

          <div className="vg-site-hero-meta" style={{ marginTop: "30px" }}>
            {install.facts.map((fact) => (
              <span key={fact}>{fact}</span>
            ))}
          </div>

          <div className="vg-site-hero-actions" data-twin="omit" style={{ marginTop: "26px" }}>
            <SiteButton href={nugetUrl(harnessPackage().id)}>{install.cta}</SiteButton>
            <SiteButton variant="ghost" href={releasesUrl}>
              {install.secondary}
            </SiteButton>
          </div>
        </div>
      </div>
    </section>
  );
}
