import { Nav } from "../components/Nav";
import { Footer } from "../components/Footer";
import { Rich } from "../components/ui/Rich";
import { RawSvg } from "../components/ui/RawSvg";
import { features, type FeatureRecord } from "../lib/features";
import { scenario, verdictSection } from "../lib/site-content";
import { treeDiagram, captureDiagram, verdictTerminal, scenarioFile } from "../lib/diagrams";

// The title bars come from the content module rather than being typed here, so the depth
// page and the landing section cannot name the same figure two different things — which is
// how an invented file extension survived on one of them after the other was corrected.
function Figure({ kind }: { kind: FeatureRecord["figure"] }) {
  if (kind === "tree") return <RawSvg className="shot-frame reveal" markup={treeDiagram} />;
  if (kind === "capture") return <RawSvg className="shot-frame reveal" markup={captureDiagram} />;
  if (kind === "verdictTerminal") {
    return <Terminal title={verdictSection.terminalTitle} markup={verdictTerminal} />;
  }
  if (kind === "scenarioFile") {
    return <Terminal title={scenario.fileTitle} markup={scenarioFile} />;
  }
  return null;
}

function Terminal({ title, markup }: { title: string; markup: string }) {
  return (
    <div className="reveal">
      <div className="term">
        <div className="bar">
          <i />
          <i />
          <i />
          <span>{title}</span>
        </div>
        <pre
          // eslint-disable-next-line react/no-danger
          dangerouslySetInnerHTML={{ __html: markup }}
        />
      </div>
    </div>
  );
}

export function FeaturePage({ record }: { record: FeatureRecord }) {
  const idx = features.findIndex((f) => f.slug === record.slug);
  const prev = idx > 0 ? features[idx - 1] : null;
  const next = idx < features.length - 1 ? features[idx + 1] : null;

  return (
    <>
      <Nav />
      <header className="hero page-hero" id="top">
        <div className="wrap">
          <a className="feature-back" href="/winwright/#features">
            ← All pillars
          </a>
          <div className="eyebrow">{record.eyebrow}</div>
          <h1>{record.heading}</h1>
          <p className="sub">
            <Rich runs={record.lead} />
          </p>
        </div>
      </header>

      <section>
        <div className="wrap">
          {record.figure && <Figure kind={record.figure} />}
          <div className="feature-body">
            {record.sections.map((s) => (
              <div className="feature-section reveal" key={s.heading}>
                <h2>{s.heading}</h2>
                {s.body && (
                  <p>
                    <Rich runs={s.body} />
                  </p>
                )}
                {s.list && (
                  <ul className="feat-list">
                    {s.list.map((item, i) => (
                      <li key={i}>
                        <span className="chk">✓</span>
                        <span>
                          <Rich runs={item} />
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
                {s.onward && (
                  <p>
                    <a className="feature-link" href={s.onward.href}>
                      {s.onward.label}
                    </a>
                  </p>
                )}
              </div>
            ))}
          </div>

          <div className="feature-nav reveal">
            {prev ? (
              <a className="feature-nav-link" href={`/winwright/features/${prev.slug}/`}>
                ← {prev.heading}
              </a>
            ) : (
              <span />
            )}
            {next ? (
              <a className="feature-nav-link next" href={`/winwright/features/${next.slug}/`}>
                {next.heading} →
              </a>
            ) : (
              <span />
            )}
          </div>
        </div>
      </section>

      <Footer />
    </>
  );
}
