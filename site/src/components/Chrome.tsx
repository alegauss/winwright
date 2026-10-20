import {
  SiteButton,
  SiteFooter,
  SiteNav,
  SiteSponsor,
  SiteThemeToggle,
} from "@viglet/viglet-design-system/site";
import {
  footer,
  install,
  navLinks,
  parentUrl,
  releasesUrl,
  repoUrl,
  sponsor,
  THEME_KEY,
} from "../lib/site-content";
import { ConsentSettings } from "./Consent";
import { Lattice } from "./ui/Lattice";

// The chrome every route shares, drawn by the design system's site layer (WW553, after
// claude-tray's T389). What is left here is this product's half of it: which links, which
// words, and the lattice the footer stands on.

const brand = { href: "/winwright/", name: "winwright", logo: "/winwright/logo.svg" };

const themeLabel = (next: string) => `Switch to ${next} theme`;

export function Nav() {
  return (
    <SiteNav
      brand={brand}
      parent={{ href: parentUrl, name: "alegauss", pre: "part of", title: "alegauss — small developer tools" }}
      links={[...navLinks]}
      actions={
        <>
          <SiteButton href={releasesUrl}>{install.ctaShort}</SiteButton>
          <SiteButton variant="ghost" href={repoUrl}>
            ★ View on GitHub
          </SiteButton>
          <SiteThemeToggle storageKey={THEME_KEY} label={themeLabel} />
        </>
      }
    />
  );
}

export function Footer() {
  return (
    <SiteFooter
      brand={brand}
      links={footer.links}
      // Rendered server-side with the rest of the page, so the sponsor is in the served HTML
      // rather than injected after load: content a crawler or an LLM never runs JavaScript to
      // find would not be worth declaring.
      sponsor={<SiteSponsor declaration={sponsor} moreLabel="More at" note="Both Apache 2.0 and self-hostable." />}
      disclaimer={
        <>
          {footer.disclaimer} <ConsentSettings />
        </>
      }
    >
      <Lattice className="lattice--footer" />
    </SiteFooter>
  );
}
