import { act } from "../../lib/site-content";
import { Rich } from "../ui/Rich";

export function Act() {
  return (
    <section id="act">
      <div className="wrap">
        <div className="sec-head reveal">
          <div className="eyebrow">{act.eyebrow}</div>
          <h2>{act.heading}</h2>
          <p>
            <Rich runs={act.intro} />
          </p>
        </div>
        <div className="grid reveal">
          {act.cards.map((card) => (
            <div className="card" key={card.title}>
              <div className="ico">{card.icon}</div>
              <h3>{card.title}</h3>
              <p>
                <Rich runs={card.body} />
              </p>
            </div>
          ))}
        </div>
        <p className="verdict-note reveal">
          <Rich runs={act.destructive} />
        </p>
      </div>
    </section>
  );
}
