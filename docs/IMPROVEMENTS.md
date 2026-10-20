# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system

### §WW553 The site on the shared design system

The pitch site moved first (the part shipped 2026-10-20): `site/src` renders its chrome
from `@viglet/viglet-design-system` 2026.3.21 (`./site`, `./site.css`, `./tokens.css`,
`./vite`), pinned exactly, and the copies it replaced are gone; `scripts/lint.test.mjs`
names them so one cannot come back. Two findings bind what is left:

- The package has no violet tone. `cool` is its neutral default and `warm` is clay, so the
  site takes `cool` and lays the hub's iris (`111,92,240`, bright `#a396ff`) over
  `--vg-brand*` and `--vg-gradient` in `src/index.css`; the ground is the package's, no longer
  `#f7f6fd`. A violet tone in the package would remove that override.
- `SiteFooter` takes flat links and `SiteHero` has no slot between actions and meta, so the
  footer's three headed groups became one row and the run moved below the pills.

Left:

- The documentation area, `site/docs` (Astro Starlight). `src/styles/area.css` still copies
  the old palette onto `--sl-color-*`, and `astro.config.mjs` loads the gtag on every page
  unasked, which is now the only analytics under `/winwright/` without consent. The package
  ships nothing for Starlight, so this is a local consent gate reading the hub's
  `alegauss-consent` key (the `Consent.tsx` flow) and the area's colours read off the same
  tokens, keeping Starlight's layout.
- The design system's `consumers.json` lists neither this site nor claude-tray; its
  `product-site` chrome waits on VDS220 in that repository, which is paused until a site
  takes the release. This site now has.

Done when the area loads no analytics before consent, its palette follows the package's
tokens, and the site is in `consumers.json`.
