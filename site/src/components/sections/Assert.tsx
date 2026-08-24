import { assert } from "../../lib/site-content";
import { Rich } from "../ui/Rich";

export function Assert() {
  return (
    <section id="assert">
      <div className="wrap">
        <div className="sec-head reveal">
          <div className="eyebrow">{assert.eyebrow}</div>
          <h2>{assert.heading}</h2>
          <p>
            <Rich runs={assert.intro} />
          </p>
        </div>
        <ul className="feat-list two reveal">
          {assert.list.map((runs, i) => (
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
