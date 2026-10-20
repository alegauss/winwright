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

### §WW560 The site in the design system's consumers.json

Left by WW553, which moved the pitch site and the documentation area onto
`@viglet/viglet-design-system` 2026.3.21 on 2026-10-20: `site/src` renders its chrome
from `./site` and builds through `./vite`, and `site/docs` reads the package's
`tokens.css`.

The design system keeps `consumers.json` (exported as `./consumers.json`), the list of
the products that take the package, with an entry shape of `id`, `name`, `package`,
`checkout`, `framework`, `chrome`, `accent`, `tone`, `sourceRoots` and `entries`. It
lists nine consumers and neither this site nor claude-tray: the `product-site` chrome is
reserved for them, and the entries are VDS220's to land in that repository, which is
paused until a site takes the release. Two have now.

The entry, measured from this checkout: `package` `site/package.json` (and `site/docs`
for the area), `framework` Vite + React 19 with an Astro Starlight area, `chrome`
`product-site`, `tone` `cool`, `accent` the hub's iris `111,92,240`, `sourceRoots`
`site/src` and `site/docs/src`, and `entries` `./site`, `./site.css`, `./tokens.css` and
`./vite`.

Nothing in this repository changes for it, so this line waits on VDS220 and closes when
that entry is in the design system's `consumers.json`. Resume VDS220 there (`roadkeep
resume VDS220`) once a release carrying both sites' entries is wanted.
