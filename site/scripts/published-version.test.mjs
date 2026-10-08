// The landing page's version swap, rendered. WW510's remainder.
//
// The area's half ships a function a case runs over the built page; this half is React state
// behind an effect, and there is nothing to extract that would make it reachable — the
// behaviour IS the wiring. A fetch resolves, the provider sets state, the context carries it,
// and every component stating the version restates it. Three lines of React, and the one
// thing on this half that could silently never happen.
//
// So this renders the real component. The feed is answered by stubbing `fetch` rather than by
// handing the provider a fetcher, which is deliberate: what runs is then the whole chain the
// browser runs — the effect, `fetchLatestPublished`, `feedUrl`, the picker's comparison, the
// state, the context and the substitution — rather than the half below an injection point.
//
// `esbuild` is here because the component is TSX and this runner executes `.mjs`. It bundles
// the one module with React left external, which is 20 lines and no change to the build. The
// alternative was testing a copy of the logic written to be testable, which is the thing this
// project keeps filing tasks about.
import { test, before } from "node:test";
import assert from "node:assert/strict";
import { mkdirSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { JSDOM } from "jsdom";
import { build } from "esbuild";

const siteDir = join(dirname(fileURLToPath(import.meta.url)), "..");
const bundled = join(siteDir, "dist-test", "published-version.mjs");

/** The token the copy carries, read from the source rather than retyped. */
const VERSION = /VERSION = "([^"]+)"/.exec(
  readFileSync(join(siteDir, "src", "lib", "site-content.ts"), "utf8"),
)?.[1];

/** The version this tree was built at. Off the generator's own JSON payload rather than out
 *  of the TypeScript beside it: `product.mjs` writes both from the same read of the csproj
 *  files, and a regex over a module is the retyping this site exists not to do. */
const generated = JSON.parse(
  readFileSync(join(siteDir, "docs", "src", "data", "product.generated.json"), "utf8"),
).version;

let React;
let createRoot;
let provider;

before(async () => {
  assert.ok(VERSION, "site-content.ts declares no VERSION token");
  assert.ok(generated, "product.generated.ts declares no version");

  mkdirSync(dirname(bundled), { recursive: true });
  await build({
    entryPoints: [join(siteDir, "src", "lib", "published-version.tsx")],
    outfile: bundled,
    bundle: true,
    format: "esm",
    platform: "neutral",
    jsx: "automatic",
    external: ["react", "react-dom", "react/jsx-runtime"],
    logLevel: "silent",
  });

  // Before React is imported, because `react-dom/client` reads the document at module scope.
  const dom = new JSDOM("<!doctype html><html><body><div id='root'></div></body></html>", {
    url: "https://alegauss.github.io/winwright/",
  });
  globalThis.window = dom.window;
  globalThis.document = dom.window.document;
  globalThis.HTMLElement = dom.window.HTMLElement;
  globalThis.Element = dom.window.Element;
  globalThis.Node = dom.window.Node;
  globalThis.IS_REACT_ACT_ENVIRONMENT = true;

  // Node declares its own `navigator` as a getter, so it is defined over rather than assigned.
  Object.defineProperty(globalThis, "navigator", {
    value: dom.window.navigator,
    configurable: true,
    writable: true,
  });

  React = await import("react");
  ({ createRoot } = await import("react-dom/client"));
  provider = await import(`file://${bundled.split("\\").join("/")}`);
});

/** Render the provider with a feed that answers `versions`, and hand back what the page says. */
async function stated(versions, { ok = true } = {}) {
  const asked = [];
  globalThis.fetch = (url) => {
    asked.push(String(url));
    return Promise.resolve({
      ok,
      json: () => Promise.resolve({ versions }),
    });
  };

  const { PublishedVersionProvider, usePublishedVersion, useVersionText } = provider;

  // Two readers, because the page has both: a component stating the version on its own, and
  // copy carrying the token inside a longer sentence.
  function Shows() {
    const bare = usePublishedVersion();
    const substituted = useVersionText()(`install ${VERSION} today`);
    return React.createElement("div", null, `${bare}|${substituted}`);
  }

  const host = document.createElement("div");
  document.body.append(host);
  const root = createRoot(host);

  await React.act(async () => {
    root.render(React.createElement(PublishedVersionProvider, null, React.createElement(Shows)));
  });

  const [bare, substituted] = host.textContent.split("|");
  await React.act(async () => root.unmount());
  host.remove();

  return { bare, substituted, asked };
}

test("the feed's published version is what the page ends up stating", async () => {
  const { bare, substituted, asked } = await stated(["0.9.0", "2.5.0", "2.6.0-beta.2"]);

  assert.equal(bare, "2.5.0", "the page does not state the latest published release");
  assert.equal(substituted, "install 2.5.0 today", "the copy's token was not substituted with it");
  assert.equal(asked.length, 1, "the provider asked the feed more than once for one page load");
  assert.match(asked[0], /^https:\/\/api\.nuget\.org\/v3-flatcontainer\/winwright\/index\.json$/);
});

test("a feed that declines leaves the reader the version this tree was built at", async () => {
  // Null is the feed not knowing — offline, behind a proxy, a package never published — and
  // the generated number is what the page ships for exactly that reason.
  for (const [because, versions, options] of [
    ["it answered nothing", [], {}],
    ["it was not ok", ["2.5.0"], { ok: false }],
    ["it answered no versions array", undefined, {}],
  ]) {
    const { bare, substituted } = await stated(versions, options);

    assert.equal(bare, generated, `the page stopped stating ${generated} although ${because}`);
    assert.equal(substituted, `install ${generated} today`, `the copy went wrong although ${because}`);
  }
});

test("the token never survives to a reader", async () => {
  // WW500's own rule, read back through a render rather than off the built output: a component
  // that forgets to substitute renders the token, and a stale number would have rendered as an
  // ordinary version and said nothing.
  for (const versions of [["2.5.0"], []]) {
    const { bare, substituted } = await stated(versions);

    assert.doesNotMatch(bare, /\{\{|\}\}/, "the page states the token instead of a version");
    assert.ok(!substituted.includes(VERSION), `the copy still carries ${VERSION}`);
  }
});

test("a release outranks a prerelease of the same core, through the whole chain", async () => {
  // The comparison is where the real failure lived, and `nuget-feed.test.mjs` holds it against
  // versions it makes up. This is the same claim arriving the way a reader gets it: through the
  // effect, the fetch and the context.
  const { bare } = await stated(["3.0.0-rc.2", "3.0.0", "3.0.0-rc.10"]);

  assert.equal(bare, "3.0.0");
});
