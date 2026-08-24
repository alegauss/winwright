import { Nav } from "../components/Nav";
import { Footer } from "../components/Footer";
import { Hero } from "../components/sections/Hero";
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
import { FeatureIndex } from "../components/sections/FeatureIndex";
import { Refusals } from "../components/sections/Refusals";
import { NonGoals } from "../components/sections/NonGoals";
import { Install } from "../components/sections/Install";

// The landing page. The section order is the argument, not a feature list: the verdict
// first, because it is the reason the rest exists, then the laws it follows from, then a
// run in the order a run meets them — locate, act, assert, capture — then the file a case
// lives in, the project it is declared against, the agent that drives it, the optional
// half, what it refuses, what it is not, and only then how to take it.
export function Landing() {
  return (
    <>
      <Nav />
      <Hero />
      <Verdict />
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
      <FeatureIndex />
      <Refusals />
      <NonGoals />
      <Install />
      <Footer />
    </>
  );
}
