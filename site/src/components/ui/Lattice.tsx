// The lattice the mark sits in. The product's whole subject is the control tree under a
// window — rows of elements, one of which a locator matched — so the page settles out of a
// drifting tree at the foot of the hero and opens back into it above the footer. Each layer
// is a depth: the pale slow one behind is the tree further from the reader, the near one is
// where the reticle sits.
//
// Seamlessness here is arithmetic, not luck. Every glyph is drawn twice across a 2880-unit
// viewBox laid out at 200% of the band, so 1440 units is exactly one band width; the drift
// translates by 50% — one whole repeat — which means the frame after the last is the first.
// Each period below divides 1440 for the same reason: a layer that does not close on 1440
// shows a seam once per cycle, and once per cycle is every few seconds.
//
// The drift and the settle sit on two elements on purpose. Both are transforms, and two
// animations on one element are one property overwriting the other — so the outer div rises
// and falls, and the inner svg travels sideways.
//
// Decorative only: it carries no copy, so it is hidden from the accessibility tree and
// dropped from the Markdown twin, and it stops moving under prefers-reduced-motion.

const SPAN = 2880; // two identical repeats of 1440
const FLOOR = 200; // the viewBox the band is laid out in

/** One element outline, placed as a fraction of its layer's period so a layer is retuned by
 *  one number rather than by every x in it. */
interface Glyph {
  /** where the element starts, as a fraction of the period */
  at: number;
  /** how wide it is, as a fraction of the period */
  wide: number;
  /** drawn as the matched element: an outline with a reticle rather than a filled row */
  matched?: boolean;
}

interface Layer {
  key: "far" | "mid" | "near";
  /** the row's top edge in the viewBox, and how tall the elements on it are */
  y: number;
  height: number;
  /** the repeat length — must divide SPAN / 2, or the loop shows a seam */
  period: number;
  glyphs: Glyph[];
}

// Back to front. The further row is thinner, quieter and moving the other way; the nearer
// one is taller, closer together and quicker. The opposing directions are what stops three
// bands of rectangles reading as one.
const LAYERS: Layer[] = [
  {
    key: "far",
    y: 38,
    height: 13,
    period: 720,
    glyphs: [
      { at: 0.02, wide: 0.17 },
      { at: 0.24, wide: 0.09 },
      { at: 0.38, wide: 0.24 },
      { at: 0.68, wide: 0.13 },
      { at: 0.86, wide: 0.1 },
    ],
  },
  {
    key: "mid",
    y: 84,
    height: 15,
    period: 480,
    glyphs: [
      { at: 0.03, wide: 0.21 },
      { at: 0.29, wide: 0.11 },
      { at: 0.46, wide: 0.3 },
      { at: 0.82, wide: 0.14 },
    ],
  },
  {
    key: "near",
    y: 132,
    height: 18,
    period: 360,
    glyphs: [
      { at: 0.04, wide: 0.26 },
      { at: 0.36, wide: 0.16, matched: true },
      { at: 0.6, wide: 0.32 },
    ],
  },
];

/** Every repeat of one glyph across the span, so the row is authored once per period. */
function places(period: number, glyph: Glyph): { x: number; w: number }[] {
  const out = [];
  for (let base = 0; base < SPAN; base += period) {
    out.push({ x: base + glyph.at * period, w: glyph.wide * period });
  }
  return out;
}

export function Lattice({ className }: { className?: string }) {
  return (
    <div
      className={className ? `lattice ${className}` : "lattice"}
      aria-hidden="true"
      data-twin="omit"
    >
      {LAYERS.map((layer) => (
        <div className={`lat-settle lat-${layer.key}`} key={layer.key}>
          <svg
            className="lat-drift"
            viewBox={`0 0 ${SPAN} ${FLOOR}`}
            preserveAspectRatio="none"
            focusable="false"
          >
            {layer.glyphs.flatMap((glyph, gi) =>
              places(layer.period, glyph).map(({ x, w }, i) => (
                <rect
                  key={`${gi}-${i}`}
                  className={glyph.matched ? "lat-el lat-el--matched" : "lat-el"}
                  x={x}
                  y={layer.y}
                  width={w}
                  height={layer.height}
                  rx={4}
                />
              )),
            )}
          </svg>
        </div>
      ))}
    </div>
  );
}
