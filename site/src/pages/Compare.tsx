import { Fragment } from "react";
import { Nav } from "../components/Nav";
import { Footer } from "../components/Footer";
import { Rich } from "../components/ui/Rich";
import { compare } from "../lib/site-content";

// The three symbols the legend declares, and nothing else reaches a cell: a matrix whose
// cells can say anything is a matrix nobody can total.
const CELL_CLASS: Record<string, string> = {
  "✓": "cmp-cell yes",
  "~": "cmp-cell partial",
  "✗": "cmp-cell no",
};

const cellClass = (sym: string) => CELL_CLASS[sym] ?? "cmp-cell no";

export function Compare() {
  return (
    <>
      <Nav />
      <header className="hero page-hero" id="top">
        <div className="wrap">
          <div className="eyebrow">{compare.eyebrow}</div>
          <h1>{compare.heading}</h1>
          <p className="sub">
            <Rich runs={compare.intro} />
          </p>
        </div>
      </header>

      <section>
        <div className="wrap">
          <div className="cmp-legend reveal">
            {compare.legend.map((l) => (
              <span key={l.sym}>
                <span className={cellClass(l.sym)}>{l.sym}</span>
                {l.label}
              </span>
            ))}
          </div>

          <div className="cmp-scroll reveal">
            <table className="cmp-table">
              <thead>
                <tr>
                  <th className="cmp-cap">Capability</th>
                  {compare.columns.map((c, i) => (
                    <th className={i === 0 ? "cmp-col own" : "cmp-col"} key={c}>
                      {c}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {compare.groups.map((group) => (
                  <Fragment key={group.law}>
                    <tr className="cmp-group">
                      <th colSpan={compare.columns.length + 1}>{group.law}</th>
                    </tr>
                    {group.rows.map((row) => (
                      <tr key={row.cap}>
                        <td className="cmp-cap">{row.cap}</td>
                        {row.cells.map((cell, i) => (
                          <td
                            className={i === 0 ? "cmp-td own" : "cmp-td"}
                            key={compare.columns[i]}
                          >
                            <span className={cellClass(cell)}>{cell}</span>
                          </td>
                        ))}
                      </tr>
                    ))}
                  </Fragment>
                ))}
              </tbody>
            </table>
          </div>
          <p className="cmp-note reveal">
            Read a row as a claim about this tool, not a score against that one — the last
            group is the one where the column beside it wins.
          </p>
        </div>
      </section>

      <section>
        <div className="wrap">
          <div className="sec-head reveal">
            <h2>{compare.winsHeading}</h2>
          </div>
          <div className="grid reveal">
            {compare.wins.map((w) => (
              <div className="card" key={w.name}>
                <h3>{w.name}</h3>
                <p>{w.body}</p>
              </div>
            ))}
          </div>
          <p className="cmp-wins-footer reveal">
            <Rich runs={compare.winsFooter} />
          </p>
        </div>
      </section>

      <Footer />
    </>
  );
}
