import { useEffect } from "react";
import { componentFor } from "./routes";
import { PublishedVersionProvider } from "./lib/published-version";

// The client shell. No router: the page is chosen from the route map by the current path,
// and every cross-route link is a plain full load, because each route is a static file the
// prerender already wrote. The same App is what entry-server renders on the build side, so
// client and static file agree by construction.
export function App({ path }: { path: string }) {
  useEffect(() => {
    const els = Array.from(document.querySelectorAll<HTMLElement>(".reveal"));
    if (!("IntersectionObserver" in window)) {
      els.forEach((el) => el.classList.add("in"));
      return;
    }
    // This only toggles an element's own opacity class; it never scrolls anything.
    const io = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add("in");
            io.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.12 },
    );
    els.forEach((el) => io.observe(el));
    return () => io.disconnect();
  }, [path]);

  // WW500. Wrapped here rather than per section so every version on a page is the same one,
  // and so the server render and the client's first render share it — entry-server renders
  // this same App, which is what keeps the prerendered number from tearing on hydration.
  const Page = componentFor(path);
  return (
    <PublishedVersionProvider>
      <Page />
    </PublishedVersionProvider>
  );
}
