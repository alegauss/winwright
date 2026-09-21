import { release } from "../../lib/site-content";
import { useVersionText } from "../../lib/published-version";
import { Rich } from "../ui/Rich";

// The band a shipped product owes a reader before they decide: which version is published,
// what it is licensed under, where the notes are, and where to say something is wrong.
//
// Every figure here comes from the generated module, so this section cannot state a version
// the tree does not declare — which is the same rule the rest of the copy follows, applied
// to the one claim a reader is most likely to act on.
export function Release() {
  const withVersion = useVersionText();
  return (
    <section id="release">
      <div className="wrap narrow">
        <div className="sec-head reveal">
          <div className="eyebrow">{release.eyebrow}</div>
          <h2>{withVersion(release.heading)}</h2>
          <p>
            <Rich runs={release.intro} />
          </p>
        </div>

        <div className="reveal">
          <dl className="release-facts">
            {release.facts.map((fact) => (
              <div className="release-fact" key={fact.k}>
                <dt>{fact.k}</dt>
                <dd>
                  {"href" in fact && fact.href ? (
                    <a href={fact.href}>{withVersion(fact.v)}</a>
                  ) : (
                    withVersion(fact.v)
                  )}
                </dd>
              </div>
            ))}
          </dl>

          <div className="release-links" data-twin="omit">
            {release.links.map((link) => (
              <a key={link.href} href={link.href}>
                {link.label}
              </a>
            ))}
          </div>

          <p className="allowlist-note">
            <Rich runs={release.note} />
          </p>
        </div>
      </div>
    </section>
  );
}
