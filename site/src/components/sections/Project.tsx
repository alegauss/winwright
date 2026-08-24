import { project } from "../../lib/site-content";
import { projectJson } from "../../lib/diagrams";
import { Rich } from "../ui/Rich";

export function Project() {
  return (
    <section id="project">
      <div className="wrap">
        <div className="sec-head reveal">
          <div className="eyebrow">{project.eyebrow}</div>
          <h2>
            <Rich runs={project.heading} />
          </h2>
          <p>
            <Rich runs={project.intro} />
          </p>
        </div>
        <div className="reveal">
          <pre
            className="codeblock"
            // eslint-disable-next-line react/no-danger
            dangerouslySetInnerHTML={{ __html: projectJson }}
          />
        </div>
        <p className="verdict-note reveal">
          <Rich runs={project.note} />
        </p>
      </div>
    </section>
  );
}
