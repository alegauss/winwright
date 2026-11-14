// A BreadcrumbList per page, so a search result shows where the page sits.
//
// Without one Google builds the trail out of the URL by itself, and for this area it chose to
// drop the middle: `/winwright/docs/verdicts/` came out as "alegauss.com › verdicts", which reads
// as a page of the hub rather than of winwright's documentation. Structured data is the one
// input Google takes for that trail, so every page declares it: the hub, the product, the
// area, and the page itself (the area's own index stops at the area).
import { defineRouteMiddleware } from "@astrojs/starlight/route-data";

const ORIGIN = "https://alegauss.com";

const trail = [
  { name: "alegauss.com", item: `${ORIGIN}/` },
  { name: "winwright", item: `${ORIGIN}/winwright/` },
  { name: "Docs", item: `${ORIGIN}/winwright/docs/` },
];

export const onRequest = defineRouteMiddleware((context) => {
  const route = context.locals.starlightRoute;
  const path = context.url.pathname.replace(/\/?$/, "/");
  const items = [...trail];
  if (path !== "/winwright/docs/") {
    items.push({ name: route.entry.data.title, item: `${ORIGIN}${path}` });
  }
  route.head.push({
    tag: "script",
    attrs: { type: "application/ld+json" },
    content: JSON.stringify({
      "@context": "https://schema.org",
      "@type": "BreadcrumbList",
      itemListElement: items.map((one, index) => ({
        "@type": "ListItem",
        position: index + 1,
        name: one.name,
        item: one.item,
      })),
    }),
  });
});
