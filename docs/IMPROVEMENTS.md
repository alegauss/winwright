# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

### §WW559 The desktop root a file step never reads

Found gating WW557 on 2026-10-20, in the guest, on a run whose desk read 'shell':
`FileSubjectTests.A_file_that_never_came_fails_saying_what_the_folder_holds` went red
1.3 s in with `COMException: Unexpected HRESULT` out of `UiaCoreApi.UiaGetRootNode`,
through `AutomationElement.RootElement` in the class's own `Desktop()` helper. The same
tree had passed the full guest run three times that day.

`Desktop()` already knows this failure. It was measured on the host gate at about one
run in four and is answered by asking again at once, up to ten times, on the reading
that the call collides with another and a desk that is missing fails all ten. This run
failed all ten, and the desk was there: the next steps of the same run drove windows on
it.

The case is about a file step, which never reads the root; `Suite.Run` asks for one and
the helper hands it the desktop. So the red is about the test's plumbing, which is what
the helper was written to stop.

What to build: either the helper waits between asks (a short sleep, bounded, so a
collision has time to clear), or a file-only suite run takes no root at all, which
removes the call rather than retrying it. The second is the better answer if `Suite.Run`
can say "no window" without a second spelling of it; read `Suite.Run`'s callers before
choosing.

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
