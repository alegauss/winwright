import { featureIndex } from "../../lib/site-content";
import { features } from "../../lib/features";
import { Rich } from "../ui/Rich";

export function FeatureIndex() {
  return (
    <section id="features">
      <div className="wrap">
        <div className="sec-head reveal">
          <div className="eyebrow">{featureIndex.eyebrow}</div>
          <h2>{featureIndex.heading}</h2>
          <p>
            <Rich runs={featureIndex.intro} />
          </p>
        </div>
        <div className="feature-index reveal">
          {features.map((f) => (
            <a className="feature-card" href={`/winwright/features/${f.slug}/`} key={f.slug}>
              <h3>{f.heading}</h3>
              <p>{f.description}</p>
              <span className="feature-card-go">{featureIndex.go}</span>
            </a>
          ))}
        </div>
      </div>
    </section>
  );
}
