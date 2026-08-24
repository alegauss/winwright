import { hero, heroRun, install, repoUrl } from "../../lib/site-content";
import { Rich } from "../ui/Rich";
import { HeroRun } from "../HeroRun";
import { Lattice } from "../ui/Lattice";

export function Hero() {
  return (
    <header className="hero" id="top">
      <div className="wrap">
        <img className="hero-icon" src="/winwright/logo.svg" alt="winwright logo" />
        <div className="badge">
          <span className="dot" /> {hero.badge}
        </div>
        <h1>
          {hero.titleLead}
          <br />
          <span className="grad">{hero.titleAccent}</span>
        </h1>
        <p className="sub">
          <Rich runs={hero.sub} />
        </p>
        {/* The call to action is dropped from the Markdown twin by this attribute — it
            converts a reader and costs an agent the same forty words on every page. */}
        <div className="hero-cta" data-twin="omit">
          <a className="btn btn-primary" href="#install">
            {install.cta}
          </a>
          <a className="btn btn-ghost" href={repoUrl}>
            ★ View on GitHub
          </a>
        </div>

        <div className="run-eyebrow">{heroRun.eyebrow}</div>
        <HeroRun />
        <div className="hero-meta">
          {hero.meta.map((item) => (
            <span key={item}>{item}</span>
          ))}
        </div>
        <div className="pills">
          {hero.pills.map((runs, i) => (
            <span className="pill" key={i}>
              <Rich runs={runs} />
            </span>
          ))}
        </div>
      </div>
      <Lattice />
    </header>
  );
}
