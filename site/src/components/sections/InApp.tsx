import { inApp } from "../../lib/site-content";
import { Rich } from "../ui/Rich";

export function InApp() {
  return (
    <section id="in-app">
      <div className="wrap">
        <div className="sec-head reveal">
          <div className="eyebrow">{inApp.eyebrow}</div>
          <h2>{inApp.heading}</h2>
          <p>
            <Rich runs={inApp.intro} />
          </p>
        </div>
        <ul className="feat-list two reveal">
          {inApp.list.map((runs, i) => (
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
