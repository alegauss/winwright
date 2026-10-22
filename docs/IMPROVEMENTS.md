# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

### §WW569 A derived set outside a run speaks the machine's culture

Found in claude-tray: its `interaction` CI job was red on every push from cdb6e66 (13
Oct) to 9eb8136 (22 Oct). `tests/ClaudeTray.Cases/CasesLoad.cs` calls
`DerivedSet.Reported(..., project, "profiles")` and `DerivedSet.ReportedValue(...,
"iconFollows")` outside a run. The declaration spells the read-out `["--profile-names",
"--observing", "--lang", "{language}"]` and declares `"language": { "fallback": "en" }`.

With no `speaking`, `{language}` comes from the machine's UI culture: `pt-BR` on the
author's desk, which the application ships, and `en-US` on `windows-latest`, which it
refuses by design (exit 1). WW468 settled the language inside a run; through the API a
test calls itself it is still "whatever this machine is".

Two gaps:
1. The default. Without `speaking`, take the language from the declaration (`language.fallback`), never from `CurrentUICulture`, or refuse a culture with no language file in a sentence naming `speaking`.
2. The exception. It says only "exited 1". The application printed `unknown language 'en-US'` on stdout, which was the whole diagnosis, and nine days of red runs went undiagnosed because the log could not show it. Carry the arguments run and the first lines of stdout/stderr.

Workaround: `CasesLoad.cs` passes `speaking: CultureInfo.GetCultureInfo("en")` on all
five calls.

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system
