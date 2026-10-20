import { SiteButton, SiteFeatureIndex, SiteHero } from "@viglet/viglet-design-system/site";
import { Footer, Nav } from "../components/Chrome";
import { HeroRun } from "../components/HeroRun";
import { Lattice } from "../components/ui/Lattice";
import { Verdict } from "../components/sections/Verdict";
import { Laws } from "../components/sections/Laws";
import { Locate } from "../components/sections/Locate";
import { Act } from "../components/sections/Act";
import { Assert } from "../components/sections/Assert";
import { Capture } from "../components/sections/Capture";
import { Scenario } from "../components/sections/Scenario";
import { Project } from "../components/sections/Project";
import { Agent } from "../components/sections/Agent";
import { InApp } from "../components/sections/InApp";
import { Halves } from "../components/sections/Halves";
import { Refusals } from "../components/sections/Refusals";
import { NonGoals } from "../components/sections/NonGoals";
import { Install } from "../components/sections/Install";
import { Release } from "../components/sections/Release";
import { features } from "../lib/features";
import { useVersionText } from "../lib/published-version";
import { Rich } from "../lib/rich";
import { featureIndex, hero, heroRun, install, repoUrl } from "../lib/site-content";

// The landing page. The section order is the argument, not a feature list: the verdict
// first, because it is the reason the rest exists, then how to take it — this is a shipped
// package and a reader who is already convinced should not have to scroll past the whole
// case to find the reference line. Then the guarantees, then a run in the order a run meets
// them — locate, act, assert, capture — then the file a case lives in, the project it is
// declared against, the agent that drives it, the optional half, what it refuses, what it
// is not, and last the release: the version, the licence and where to say it is wrong.
export function Landing() {
  return (
    <>
      <Nav />
      <Hero />
      <Verdict />
      <Install />
      <Laws />
      <Locate />
      <Act />
      <Assert />
      <Capture />
      <Scenario />
      <Project />
      <Agent />
      <InApp />
      <Halves />
      <SiteFeatureIndex
        id="features"
        items={features.map((f) => ({
          href: `/winwright/features/${f.slug}/`,
          heading: f.heading,
          description: f.description,
        }))}
        eyebrow={featureIndex.eyebrow}
        title={featureIndex.heading}
        description={<Rich runs={featureIndex.intro} />}
        goLabel={featureIndex.go}
      />
      <Refusals />
      <NonGoals />
      <Release />
      <Footer />
    </>
  );
}

// The hero, drawn by the design system's SiteHero (WW553). The run a reader sees first goes in
// as its children, below the meta and the pills: SiteHero has no slot between the actions and
// the meta, where it used to sit, and the move was chosen over a local copy of the hero.
function Hero() {
  const withVersion = useVersionText();
  return (
    <SiteHero
      icon={{ src: "/winwright/logo.svg", alt: "winwright logo" }}
      badge={withVersion(hero.badge)}
      title={hero.titleLead}
      titleAccent={hero.titleAccent}
      sub={<Rich runs={hero.sub} />}
      actions={
        <>
          <SiteButton href="#install">{install.cta}</SiteButton>
          <SiteButton variant="ghost" href={repoUrl}>
            ★ View on GitHub
          </SiteButton>
        </>
      }
      meta={hero.meta}
      pills={hero.pills.map((runs, i) => (
        <Rich runs={runs} key={i} />
      ))}
    >
      <div className="vg-site-wrap">
        <div className="run-eyebrow">{heroRun.eyebrow}</div>
        <HeroRun />
      </div>
      <Lattice />
    </SiteHero>
  );
}
