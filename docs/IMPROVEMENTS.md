# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

### §WW552 Capturing a Mica window with Direct3D content

Project: PhotoBoost, tasks PB27 and PB40: a WPF window with ThemeMode Fluent (so Mica)
whose image area is Direct3D 11 through Vortice, either a child HWND with a DXGI flip
swapchain (HwndHost) or a D3DImage. The screen copy is refused because Glass.Of reads
mica, which is right. The off-screen render from Winwright.InApp draws the WPF visual
tree with RenderTargetBitmap, so a child HWND comes out empty and a D3DImage may come
out blank or stale, and the receipt cannot tell. The case wanted a capture of the
comparator after a zoom, to prove it drew something and that two renders match when
nothing changed. The claim settled for is automation readings (zoom, divider position,
image size) and the exported file. What winwright could offer: the render route
compositing child HWND swapchain content (DXGI or Windows.Graphics.Capture of the child,
which carries no backdrop), or a refusal that names the GPU region it could not draw
instead of a blank picture.

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

### §WW554 Marketplace name collision

Found adopting winwright in photo-boost (D:\Git\japode\photo-boost, PB38). The README's
project-scope install is `claude plugin marketplace add alegauss/winwright --scope
project` then `claude plugin install winwright@alegauss --scope project`, which writes
`extraKnownMarketplaces.alegauss` and `enabledPlugins["winwright@alegauss"]` into the
project's .claude/settings.json.

photo-boost already has `extraKnownMarketplaces.alegauss` pointing at github
alegauss/roadkeep, because roadkeep's .claude-plugin/marketplace.json is also named
"alegauss" (pportal has the same entry). Settings key marketplaces by name, so one
project cannot register both repositories: adding winwright's replaces roadkeep's source
and `roadkeep@alegauss` stops resolving, or the reverse. The plugin was therefore not
installed in photo-boost; its cases run through the Winwright package alone, which needs
no plugin.

What winwright should offer: a marketplace name of its own (e.g. "winwright"), so the
documented commands become `winwright@winwright` and sit beside roadkeep's; or one
shared "alegauss" marketplace in a single repository listing both plugins, with both
READMEs pointing at it. Either way the README's install lines should be tested against a
settings.json that already carries roadkeep's marketplace.

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

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
