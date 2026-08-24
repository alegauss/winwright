import { laws } from "../../lib/site-content";
import { Rich } from "../ui/Rich";

export function Laws() {
  return (
    <section id="laws">
      <div className="wrap">
        <div className="sec-head reveal">
          <div className="eyebrow">{laws.eyebrow}</div>
          <h2>{laws.heading}</h2>
          <p>
            <Rich runs={laws.intro} />
          </p>
        </div>
        <div className="laws reveal">
          {laws.items.map((law) => (
            <div className="law" key={law.id}>
              <span className="law-id">{law.id}</span>
              <div className="law-body">
                <h3>{law.title}</h3>
                <p>{law.body}</p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
