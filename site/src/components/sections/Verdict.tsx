import { verdictSection } from "../../lib/site-content";
import { verdicts } from "../../lib/product";
import { verdictTerminal } from "../../lib/diagrams";
import { Rich } from "../ui/Rich";

// One card per member of RunOutcome, in the order the enum declares them — which is also
// exit-code order. The name, the number and the first line of each card are generated from
// the enum itself, so a fifth outcome would appear here and a renamed one would rename
// itself; what is typed in the content module is only the gloss underneath.
export function Verdict() {
  return (
    <section id="verdict">
      <div className="wrap">
        <div className="sec-head reveal">
          <div className="eyebrow">{verdictSection.eyebrow}</div>
          <h2>{verdictSection.heading}</h2>
          <p>
            <Rich runs={verdictSection.intro} />
          </p>
        </div>

        <div className="verdicts reveal">
          {verdicts.map((v) => (
            <div className={`vd vd-${v.name.toLowerCase()}`} key={v.name}>
              <div className="vd-code">{v.code}</div>
              <h3>{v.name}</h3>
              <p>{v.meaning}</p>
              {verdictSection.glosses[v.name] && (
                <p>
                  <Rich runs={verdictSection.glosses[v.name]} />
                </p>
              )}
            </div>
          ))}
        </div>

        <div className="reveal" style={{ marginTop: "34px" }}>
          <div className="term">
            <div className="bar">
              <i />
              <i />
              <i />
              <span>winwright run cases/report.wwx</span>
            </div>
            <pre
              // eslint-disable-next-line react/no-danger
              dangerouslySetInnerHTML={{ __html: verdictTerminal }}
            />
          </div>
        </div>

        <p className="verdict-note reveal">
          <Rich runs={verdictSection.note} />
        </p>
      </div>
    </section>
  );
}
