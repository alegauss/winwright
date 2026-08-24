import { refusals } from "../../lib/site-content";
import { Rich } from "../ui/Rich";

export function Refusals() {
  return (
    <section id="refusals">
      <div className="wrap">
        <div className="banner reveal">
          <div className="lock">{refusals.icon}</div>
          <h2>{refusals.heading}</h2>
          {refusals.body.map((runs, i) => (
            <p key={i}>
              <Rich runs={runs} />
            </p>
          ))}
        </div>
      </div>
    </section>
  );
}
