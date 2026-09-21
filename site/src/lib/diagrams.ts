// The illustrative figures. These are drawings and terminal output — a dark island either
// way — so they keep their own fixed palette rather than following the page theme; the
// themed .shot-frame around them is what places them on a light or dark page. Kept as
// verbatim markup (not converted to JSX) so the drawing stays pixel-identical to the
// hand-written original, and rendered with dangerouslySetInnerHTML because it is static,
// author-controlled content with no interpolation.

export const treeDiagram = `
<svg viewBox="0 0 900 320" role="img" aria-label="A window and the control tree beneath it: the locator Window#main &gt; Pane &gt; Button#save matches one Button nested two panes deep, and the pane in between is skipped because the combinator means a descendant of, at any depth">
  <rect width="900" height="320" rx="12" fill="#0a0616"/>

  <text x="30" y="34" fill="#8a81ab" font-family="Inter,sans-serif" font-size="12" font-weight="700" letter-spacing="1.4">THE WINDOW</text>
  <line x1="430" y1="16" x2="430" y2="304" stroke="#322a5c" stroke-width="1" stroke-dasharray="5 5"/>
  <text x="460" y="34" fill="#8a81ab" font-family="Inter,sans-serif" font-size="12" font-weight="700" letter-spacing="1.4">THE CONTROL VIEW</text>

  <!-- the window, as a person sees it -->
  <rect x="30" y="54" width="368" height="220" rx="10" fill="#120d24" stroke="#322a5c"/>
  <rect x="30" y="54" width="368" height="30" rx="10" fill="#181231"/>
  <rect x="30" y="74" width="368" height="10" fill="#181231"/>
  <text x="46" y="74" fill="#ece7fb" font-family="Segoe UI Variable Text,Segoe UI,Inter,sans-serif" font-size="12">App — Relatórios</text>
  <rect x="50" y="104" width="328" height="102" rx="6" fill="#150f2c" stroke="#241d47"/>
  <text x="64" y="126" fill="#ada4cd" font-family="Segoe UI Variable Text,Segoe UI,Inter,sans-serif" font-size="11.5">Pane</text>
  <rect x="66" y="136" width="296" height="58" rx="5" fill="#191333" stroke="#241d47"/>
  <text x="80" y="156" fill="#8a81ab" font-family="Segoe UI Variable Text,Segoe UI,Inter,sans-serif" font-size="11">Pane (framework wrapper)</text>
  <rect x="248" y="160" width="100" height="26" rx="5" fill="#2a2158" stroke="#6f5cf0" stroke-width="1.6"/>
  <text x="298" y="178" text-anchor="middle" fill="#ece7fb" font-family="Segoe UI Variable Text,Segoe UI,Inter,sans-serif" font-size="12">Save</text>
  <rect x="50" y="220" width="328" height="34" rx="6" fill="#150f2c" stroke="#241d47"/>
  <text x="64" y="242" fill="#8a81ab" font-family="Segoe UI Variable Text,Segoe UI,Inter,sans-serif" font-size="11.5">StatusBar</text>

  <!-- the same thing as a tree -->
  <text x="460" y="76" fill="#ada4cd" font-family="JetBrains Mono,monospace" font-size="12.5">Window#main</text>
  <path d="M470 84 V108 H492" stroke="#3d3272" stroke-width="1.4" fill="none"/>
  <text x="500" y="112" fill="#ada4cd" font-family="JetBrains Mono,monospace" font-size="12.5">Pane#reportHost</text>
  <path d="M510 120 V144 H532" stroke="#3d3272" stroke-width="1.4" fill="none"/>
  <text x="540" y="148" fill="#8a81ab" font-family="JetBrains Mono,monospace" font-size="12.5">Pane            &lt;- the wrapper nobody wrote</text>
  <path d="M550 156 V180 H572" stroke="#6f5cf0" stroke-width="1.6" fill="none"/>
  <rect x="576" y="164" width="152" height="26" rx="5" fill="#2a2158" stroke="#6f5cf0" stroke-width="1.6"/>
  <text x="588" y="182" fill="#a396ff" font-family="JetBrains Mono,monospace" font-size="12.5">Button#save</text>
  <path d="M470 84 V216 H492" stroke="#3d3272" stroke-width="1.4" fill="none"/>
  <text x="500" y="220" fill="#8a81ab" font-family="JetBrains Mono,monospace" font-size="12.5">StatusBar</text>

  <line x1="30" y1="278" x2="870" y2="278" stroke="#241d47"/>
  <text x="46" y="300" fill="#a396ff" font-family="JetBrains Mono,monospace" font-size="12.5">Window#main &gt; Pane &gt; Button#save</text>
  <text x="330" y="300" fill="#8a81ab" font-family="Inter,sans-serif" font-size="11.5">— matches, because &gt; is a descendant of, at any depth. A direct-child locator is the one that breaks on somebody else's machine.</text>
</svg>`;

export const captureDiagram = `
<svg viewBox="0 0 900 340" role="img" aria-label="A capture refused: a toast belonging to the application overlaps the copy rectangle, and the refusal names the intruder, its process and the rectangle it covers, rather than cropping around it">
  <rect width="900" height="340" rx="12" fill="#0a0616"/>

  <rect x="40" y="40" width="420" height="240" rx="10" fill="#120d24" stroke="#322a5c"/>
  <rect x="40" y="40" width="420" height="28" rx="10" fill="#181231"/>
  <rect x="40" y="58" width="420" height="10" fill="#181231"/>
  <text x="56" y="59" fill="#ece7fb" font-family="Segoe UI Variable Text,Segoe UI,Inter,sans-serif" font-size="12">App — Relatórios</text>

  <rect x="62" y="90" width="376" height="166" rx="6" fill="none" stroke="#6f5cf0" stroke-width="1.6" stroke-dasharray="6 4"/>
  <text x="70" y="84" fill="#a396ff" font-family="JetBrains Mono,monospace" font-size="11">the copy rectangle</text>

  <rect x="86" y="112" width="240" height="12" rx="3" fill="#241d47"/>
  <rect x="86" y="136" width="300" height="12" rx="3" fill="#241d47"/>
  <rect x="86" y="160" width="180" height="12" rx="3" fill="#241d47"/>

  <rect x="256" y="182" width="240" height="64" rx="8" fill="#3a2352" stroke="#b070e0" stroke-width="1.6"/>
  <text x="276" y="208" fill="#f0dcff" font-family="Segoe UI Variable Text,Segoe UI,Inter,sans-serif" font-size="12.5">Update available</text>
  <text x="276" y="228" fill="#c9a6e0" font-family="Segoe UI Variable Text,Segoe UI,Inter,sans-serif" font-size="11">a toast — pid 14820</text>

  <path d="M508 160 H556" stroke="#e5484d" stroke-width="1.8" fill="none"/>
  <path d="M550 154 L558 160 L550 166 Z" fill="#e5484d"/>

  <rect x="572" y="92" width="292" height="156" rx="10" fill="#150f2c" stroke="#e5484d" stroke-width="1.4"/>
  <text x="592" y="120" fill="#ff6b6f" font-family="JetBrains Mono,monospace" font-size="12.5" font-weight="600">nothing stands over it: absent</text>
  <text x="592" y="146" fill="#cdc5e6" font-family="JetBrains Mono,monospace" font-size="11.5">1 window(s) stand over</text>
  <text x="592" y="164" fill="#cdc5e6" font-family="JetBrains Mono,monospace" font-size="11.5">376x166 at 62,90, taking</text>
  <text x="592" y="182" fill="#cdc5e6" font-family="JetBrains Mono,monospace" font-size="11.5">15360 of its 62416 pixel(s):</text>
  <text x="592" y="200" fill="#cdc5e6" font-family="JetBrains Mono,monospace" font-size="11.5">'Update available' (pid 14820)</text>
  <text x="592" y="218" fill="#cdc5e6" font-family="JetBrains Mono,monospace" font-size="11.5">over 240x64 at 194,92</text>
  <text x="592" y="240" fill="#8a81ab" font-family="Inter,sans-serif" font-size="11">A copy trimmed around it is a picture of something else.</text>

  <line x1="40" y1="300" x2="860" y2="300" stroke="#241d47"/>
  <text x="450" y="324" text-anchor="middle" fill="#8a81ab" font-family="Inter,sans-serif" font-size="11.5">The z order above the window is enumerated once and intersected with the rectangle — sampled points cannot answer for an area.</text>
</svg>`;

// A run's own summary, in the shape the engine prints it: the reading of the machine first,
// then the headline, then one line per assertion that did not pass. Kept verbatim so the
// column alignment and the per-line colours render exactly as `VerdictSummary.Render`
// produces them — a figure of the output rather than a redrawing of it.
export const verdictTerminal = `this run measured 5 conditions, 1 not read: an application that renders its own tree when asked.
  ok      the running instance is the binary this run named: bin/Debug/App.exe, launched by this run
  ok      a binary built from this source: sources unchanged since the build
  <span class="pass">agrees</span>  the application is in the language this scenario is written for: pt-BR, from settings.json
  ok      the foreground belongs to the window under test: App — Relatorios
  <span class="c">not read an application that renders its own tree when asked: it does not take Winwright.InApp</span>
<span class="sum">DEGRADED (exit 2) - 4 assertions: 3 passed, 0 failed, 1 unchecked (all the desk's)</span>
  <span class="deg">unchecked</span> step 4  report.pt-BR - <span class="rem">'an overflow flyout this run can work' absent
                          (the desk's): the shell would not open the flyout, so
                          the pane could not be brought to the front</span>`;

// The case file, in the format the loader actually reads: an object with `cases`, and
// optionally the `fixtures` they are launched against. A figure rather than copy, because
// the alignment is the point and a text node whose column is hand-placed cannot take a
// string of unknown width.
export const scenarioFile = `{
  <span class="g">"fixtures"</span>: [
    { "name": "report", "environment": "dark", "flag": "--theme",
      "language": "pt-BR", "shareable": true }
  ],
  <span class="g">"cases"</span>: [
    {
      <span class="g">"name"</span>:     "the report pane comes back in the resolved language",
      <span class="g">"catches"</span>:  "a translated menu entry that leaves its labels in English",
      <span class="g">"filed"</span>:    "WW63",
      <span class="g">"tags"</span>:     ["smoke", "i18n"],
      <span class="g">"needs"</span>:    ["the foreground belongs to the window under test"],
      <span class="g">"fixture"</span>:  "report",
      <span class="g">"steps"</span>: [
        { "locator": "MenuItem#languagePtBR", "act": "invoke",
          "named": "switch to pt-BR" },
        { "locator": "Pane#reportHost &gt; Text", "act": "read",
          "covers": "report.labels" }, <span class="c">// every string the key declares</span>
        { "locator": "Text#total", "act": "read",
          "expectReported": "monthlyTotal" }, <span class="c">// the app's own read-out</span>
        { "locator": "Pane#reportHost", "act": "capture",
          "with": "report.pt-BR" }
      ]
    }
  ]
}`;

export const projectJson = `{
  <span class="g">"executable"</span>: "bin/Debug/net10.0-windows/YourApp.exe",
  <span class="g">"sourceRoot"</span>: "src/YourApp",
  <span class="g">"sourceIgnore"</span>: ["bin", "obj"],
  <span class="g">"fingerprintStore"</span>: "%APPDATA%/YourApp",
  <span class="g">"languageFiles"</span>: ["strings.en.json", "strings.pt-BR.json"],
  <span class="g">"language"</span>: { "preferenceFile": "settings.json", "preferenceKey": "ui.language", "fallback": "en" },
  <span class="g">"timeouts"</span>: { "resolve": 5000, "stop": 5000 },
  <span class="g">"attempts"</span>: 3,
  <span class="g">"destructive"</span>: [{ "id": "quitCommand" }, { "key": "menu.exit" }]
}`;
