import { locate } from "../../lib/site-content";
import { treeDiagram } from "../../lib/diagrams";
import { Rich } from "../ui/Rich";
import { RawSvg } from "../ui/RawSvg";

export function Locate() {
  return (
    <section id="locate">
      <div className="wrap">
        <div className="sec-head reveal">
          <div className="eyebrow">{locate.eyebrow}</div>
          <h2>{locate.heading}</h2>
          <p>
            <Rich runs={locate.intro} />
          </p>
        </div>

        <div className="gram reveal">
          {locate.lines.map(([form, what]) => (
            <div className="gram-line" key={form}>
              <code className="gram-form">{form}</code>
              <span className="gram-what">{what}</span>
            </div>
          ))}
        </div>

        <div className="reveal" style={{ marginTop: "34px" }}>
          <RawSvg className="shot-frame" markup={treeDiagram} />
        </div>

        <ul className="feat-list two reveal" style={{ marginTop: "34px" }}>
          {locate.notes.map((runs, i) => (
            <li key={i}>
              <span className="chk">✓</span>
              <span>
                <Rich runs={runs} />
              </span>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
