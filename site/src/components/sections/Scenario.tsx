import { scenario } from "../../lib/site-content";
import { scenarioFile } from "../../lib/diagrams";
import { Rich } from "../ui/Rich";

export function Scenario() {
  return (
    <section id="scenario">
      <div className="wrap">
        <div className="sec-head reveal">
          <div className="eyebrow">{scenario.eyebrow}</div>
          <h2>{scenario.heading}</h2>
          <p>
            <Rich runs={scenario.intro} />
          </p>
        </div>

        <div className="split">
          <div className="reveal">
            <div className="term">
              <div className="bar">
                <i />
                <i />
                <i />
                <span>{scenario.fileTitle}</span>
              </div>
              <pre
                // eslint-disable-next-line react/no-danger
                dangerouslySetInnerHTML={{ __html: scenarioFile }}
              />
            </div>
          </div>
          <div className="split-txt reveal">
            <ul className="feat-list">
              {scenario.list.map((runs, i) => (
                <li key={i}>
                  <span className="chk">✓</span>
                  <span>
                    <Rich runs={runs} />
                  </span>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </div>
    </section>
  );
}
