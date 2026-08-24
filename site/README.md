# winwright site

The public site — a self-contained Vite + React 19 + TypeScript + Tailwind v4 workspace, and
this repository's only Node workspace. It is standalone: `dotnet build` neither builds nor
needs it, and it never writes into `docs/`, which is roadkeep's.

## Commands

```
npm install        # once
npm run dev        # dev server at /winwright/
npm run build      # generate → tsc → client → og image → SSR → prerender
npm test           # the site's own claims, against what the build produced
npm run typecheck  # tsc -b, no emit
npm run preview    # serve the built dist/
```

`npm run build` is the gate, and it is one command on purpose: it regenerates the product
figures from this repository's own source, type-checks, builds the client, rasterises the
social card, builds the SSR bundle and prerenders every route with its Markdown twin, its
`manifest.json`, its `sitemap.xml` and its `robots.txt`. A drifted `<head>` template or a
route with no page fails it. `npm test` then asserts the built output, so it runs after the
build rather than instead of it.

GitHub Pages derives the base path from the repository name, so Vite's `base` is
`/winwright/` and every asset path carries that prefix. Renaming the repository moves every
published URL at once — the prefix is written once, in [vite.config.ts](vite.config.ts), and
once in [src/routes.tsx](src/routes.tsx) where the canonical URLs are built.

## Where things live

| Path | What |
|---|---|
| `src/lib/site-content.ts` | **All copy** — sections only render it, so a claim is one array element a reviewer can check |
| `src/lib/features.ts` | One record per depth page; its route, title and description are read off the same record |
| `src/lib/diagrams.ts` | The illustrative SVGs and the terminal figures, kept verbatim |
| `src/lib/product.ts` + `product.generated.ts` | **Every figure the copy states**, read out of the C# by `scripts/product.mjs` |
| `src/lib/theme.ts` + `index.html` pre-paint script + `src/index.css` tokens | **The theme follows the OS**, a stored choice overrides it, applied before first paint |
| `src/routes.tsx` | The route table and its metadata, asserted against each other at import time |
| `src/components/sections/` | One component per landing section; the composition (order, JSX) lives here |
| `src/pages/Landing.tsx` | The landing page, section order = the argument |
| `scripts/` | The generator, the prerender and the tests that read `dist/` |

## The generator, and why there is one

`scripts/product.mjs` reads three files this repository already has and writes
`src/lib/product.generated.ts`:

- `Directory.Build.props` — the one declared version of every assembly here.
- the two `.csproj` files — the package ids, and which half takes which.
- `src/Winwright/Verdicts/RunOutcome.cs` — the outcomes **and their exit codes**.

The last is the reason it exists at all. The enum's member values *are* the process exit
codes — that is the product's own decision — so a table of codes typed onto a web page would
be the same mapping written a third time. `scripts/product.test.mjs` then reads the built
page and compares it to the C# by an independent parse, which is what catches a generator
wired to the wrong thing rather than a number typed wrong.

## Deliberate non-goals here

No third-party fonts fetched at page load, no analytics, no cookie banner. Inter and
JetBrains Mono are named with system fallbacks rather than pulled from a CDN. Nothing on the
page scrolls the window except the reader.

## What is not written down here

A list of what does not exist yet. What is open lives in
[`docs/ROADMAP.md`](../docs/ROADMAP.md), where a tool keeps it honest and a shipped task
removes its own line — a second list of the future, kept by hand in a readme, is a list that
goes stale in silence.
