import { halves } from "../../lib/site-content";
import { Rich } from "../ui/Rich";

export function Halves() {
  return (
    <section id="halves">
      <div className="wrap">
        <div className="sec-head reveal">
          <div className="eyebrow">{halves.eyebrow}</div>
          <h2>{halves.heading}</h2>
          <p>
            <Rich runs={halves.intro} />
          </p>
        </div>
        <div className="actors reveal">
          {halves.actors.map((actor) => (
            <div
              className={actor.primary ? "actor actor-primary" : "actor"}
              key={actor.who}
            >
              <div className="actor-head">
                <span className="actor-who">{actor.who}</span>
                <span className="actor-sub">{actor.sub}</span>
              </div>
              <div className="actor-iface">{actor.iface}</div>
              <p className="actor-job">{actor.job}</p>
            </div>
          ))}
        </div>
        <p className="actors-note reveal">
          <Rich runs={halves.actorsNote} />
        </p>
      </div>
    </section>
  );
}
