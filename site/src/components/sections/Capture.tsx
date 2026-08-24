import { capture } from "../../lib/site-content";
import { captureDiagram } from "../../lib/diagrams";
import { Rich } from "../ui/Rich";
import { RawSvg } from "../ui/RawSvg";

export function Capture() {
  return (
    <section id="capture">
      <div className="wrap">
        <div className="sec-head reveal">
          <div className="eyebrow">{capture.eyebrow}</div>
          <h2>{capture.heading}</h2>
          <p>
            <Rich runs={capture.intro} />
          </p>
        </div>

        <div className="reveal" style={{ marginBottom: "34px" }}>
          <RawSvg className="shot-frame" markup={captureDiagram} />
        </div>

        <div className="steps reveal">
          {capture.steps.map((step) => (
            <div className="step" key={step.title}>
              <div className="n">{step.n}</div>
              <h4>{step.title}</h4>
              <p>
                <Rich runs={step.body} />
              </p>
            </div>
          ))}
        </div>

        <p className="verdict-note reveal">
          <Rich runs={capture.note} />
        </p>
      </div>
    </section>
  );
}
