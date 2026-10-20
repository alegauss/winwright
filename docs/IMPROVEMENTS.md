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

### §WW557 An element replaced between looks

Found shipping WW555. photo-boost's select after expand was refused as "nothing matched"
one run in four on 1.1.3. The reading behind the fix: a look matched a ListItem whose
peer was gone before its facts were read, `Resolve` returned it as found with no facts,
and `Admitted` refused at once instead of polling on. WW555 counts such a look as no
look, and the refusal says "it matched on N of M looks and was gone each time" where
that happened.

Neither half is provoked here. `ShutPickerTests` drives photo-boost's two steps against
the fixture's WPF picker, and a control tree without the fix passed 32 rounds of 32 in
the guest on 2026-10-20: that picker realises its items before the first look.

What to build: a fixture shape that replaces its element between looks on purpose. A WPF
pane swapping a named Button for a new one of the same name on a short dispatcher timer
is the likely spelling, since each swap disconnects the old peer. Two cases: an act
lands once the swapping stops, and an act against one that never stops is refused with
the vanished sentence. Pair the flag where the other shapes are paired.

Then photo-boost's workaround (a `read` step between expand and select, commented WW555)
can go once a release carries the fix.

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system

### §WW553 The site on the shared design system

`site/src` carries `components/Nav.tsx`, `components/Footer.tsx` with the sponsor block,
`components/sections/Hero.tsx`, `FeatureIndex.tsx` and `NonGoals.tsx`,
`pages/Feature.tsx` and `components/ui/CopyButton.tsx`, `ThemeToggle.tsx`, `Rich.tsx`
and `RawSvg.tsx`, plus `scripts/prerender.mjs` and `scripts/og-image.mjs`: the template
seven other alegauss product sites copy under the same names. `index.css` declares its
own `--bg*`, `--panel*`, `--iris*` and `--accent*` tokens for both colour modes.

The move, after the pilot, claude-tray T389:

- `data-tone="cool"`: the ground is a violet-tinted white (`#f7f6fd`) and near-black
  (`#0c0918`), not clay; the warm tone would change the page.
- The accent from the hub's `projects.json` (`111,92,240`, bright `#a396ff`), the iris
  the page already uses.
- The shared components come from `./site`; `HeroRun`, `Lattice`, the product sections
  and pages and `lib/` stay in `site/src`.
- The build calls the `./vite` helpers instead of the two scripts.
- Google Analytics loads only after consent, the same flow as the hub (AG1).

The documentation area is in scope: `site/docs`, an Astro Starlight build that the
site's build runs as `build:docs`, then `docs-twins.mjs` and `docs-sitemap.mjs`. Its
`src/styles/area.css` copies the pitch page's palette and its `astro.config.mjs` loads
the gtag on every page. It takes the tone, the accent and the consent gate from the
package, keeping Starlight's layout.

Done when `site/src` has none of the files `./site` provides; the prerendered HTML of
every route, the twelve docs pages and their twins included, holds the same text as
before; light and dark screenshots of each route are compared before and after with the
differences listed in the commit; and the site appears in the design system's
`consumers.json`.
