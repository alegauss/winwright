# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

### §WW555 Act resolves before an expanded popup is realised

Found in photo-boost (D:\Git\japode\photo-boost, PB41's run), case "choosing JPEG for a
transparent photo warns and offers a background" in cases/window.cases.json, on
Winwright 1.1.3.

The steps: `{"locator": "ComboBox#ExportFormat", "act": "expand"}` then `{"locator":
"ComboBox#ExportFormat > ListItem[name=\"JPEG\"]", "act": "select", "reads": "selected",
"expect": "selected"}` against a plain WPF ComboBox with three ComboBoxItems. It passed
three runs in four; the fourth broke at the select with NotActionableException "cannot
take this act: nothing matched, or what matched has gone since". A WPF ComboBox realises
its items' containers and automation peers when the popup opens, a moment after Expand
returns, so the select resolved its locator before the item existed. `select` is marked
repeatable, but the retries did not bridge it, and the verdict calls a race "nothing
matched", reading like a wrong locator.

Workaround in photo-boost: a `read` step on the same ListItem between the two, since an
expectation polls until the item appears; commented with this id.

What winwright should offer: an act resolves its locator against the act timeout the way
an expectation does, or `expand` returns only once the expanded element's children are
realised; and a NotActionable verdict says whether anything matched at any poll.

### §WW556 {files} is not filled into a fixture's arguments

Found in photo-boost (D:\Git\japode\photo-boost, PB42's run), fixture "a window on the
tiny models, exporting into its own folder" in cases/window.cases.json, on Winwright
1.1.3.

The fixture staged a photo and declared `"arguments": ["--open",
"{files}/rotated-exif6.jpg", ..., "--export", "{files}/out.png"]`, so the app opens the
staged copy and exports beside it, where a file step reads it back with `sameBytesAs`.
The declaration was refused: "it stages 1 file(s) and nothing names '{files}'".
FixtureDeclaration checks the working directory, the opened folder and the variables for
the token, never the arguments, and `Offered` fills only `{reachN}` into an argument, so
`{files}` would have reached the app literally anyway. The schema's own text for `files`
says a variable or the working directory names the directory, which is consistent, but
an app whose paths come in as flags (PhotoBoost's --open, --models, --export) has no
variable to take them.

Workaround in photo-boost: `"workingDirectory": "{files}"`, relative arguments, and the
six model files staged one by one with `from`/`to` into a `models/` subfolder, because
the project's own paths no longer resolve once the app starts in the staged folder.
Commented with this id.

What winwright should offer: `{files}` filled into arguments like `{reachN}` is, and
counted by the "nothing names it" check.

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
