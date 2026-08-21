---
name: roadmap-docs
description: winwright's own shipping discipline — the one-task-one-commit rule and the `run-commit.cmd` that closes every task, design-before-ship, the blocks A–K that are reused rather than opened, the `## Done when` criteria a block is read against before it is called finished, and the adopter-facing surface a shipped feature must reach. The roadmap/changelog/rationale write path itself is NOT here: roadkeep owns it, and its own skill says which command to call. Use whenever a task is finished, before committing, when picking the next task, or when a governed file needs to change.
---

# Shipping discipline

**The write path is not in this file.** [`docs/ROADMAP.md`](../../../docs/ROADMAP.md),
[`docs/CHANGELOG.md`](../../../docs/CHANGELOG.md) and
[`docs/IMPROVEMENTS.md`](../../../docs/IMPROVEMENTS.md) are written by **roadkeep**, configured in
[`roadkeep.toml`](../../../roadkeep.toml): the fields are refused at insertion, the id and the
`(deps: … ✅)` annotation are derived, and a hand-edit is denied by the hook. Which command to call —
`add`, `ship`, `amend`, `status`, `pick`, `brief`, `section`, `non-goal`, `criterion` — is the
**`roadkeep` skill** at [`.claude/skills/roadkeep/SKILL.md`](../roadkeep/SKILL.md), which is the same
text in every project that adopted the tool. A rule stated in two files is a rule two files can
disagree about, so nothing here repeats it.

**How roadkeep is wired here, because it is not the plugin.** There is no plugin install in this
repo; `roadkeep install` wrote the three surfaces from the sibling checkout at `..\roadkeep` —
[`.mcp.json`](../../../.mcp.json) (the `roadkeep` MCP server), the guard on `SessionStart`,
`PreToolUse` and `Stop` in [`.claude/settings.json`](../../settings.json), and the copied
`roadkeep` skill. Consequences worth knowing before you are surprised by one: the tools arrive as
`mcp__roadkeep__*` **only after the session picks up `.mcp.json`**, so in a session started before
the install, use the CLI — `roadkeep <command>` on PATH, same engine, same refusals. The guard is
live either way: it denies a hand-edit to a governed file and runs `lint` as the turn ends. And
because the surfaces are copies, `roadkeep install --check` is what keeps them in step with the
checkout — it exits 1 on anything that drifted, and `roadkeep install` closes it.

What this file holds is what roadkeep has no opinion about: **when** a commit happens, what a task
owes before it may ship, and what a shipped feature owes an adopter.

## ⛔ One task, one commit (non-negotiable)

**You may NOT do more than one task before committing.**

- **One task → one `run-commit.cmd`.** The moment a task is complete and validated, run
  `roadkeep ship <id>` and commit — code and docs in that one commit — **before touching the next
  task.** Finishing a task means *the commit landed*.
- **A multi-task request** (a whole block, "execute Block C", a list of `WW<n>`s) **is not permission
  to batch.** It is a request to run tasks one at a time, committing after each. A single giant diff
  spanning many tasks is the failure this rule exists to prevent.
- **For any batch of ≥2 tasks, drive it with `/loop`** (self-paced): exactly one task per iteration,
  `run-commit.cmd` at the end of the iteration, then let the loop advance. Do not hand-roll a loop
  that defers commits.
- **Self-check before starting task N+1:** run `git status` / `git log -1`. If the previous task's
  work is not committed, stop and commit it first.
- `run-commit.cmd -m "<ascii conventional-commits title>"` from the repo root. **`-m` always**, and
  ASCII. It **stages everything**, so check `git status` first — a stray scratch file rides along.
  It is a **global command on the Windows PATH** (`D:\Dev\bin`), not a file in this repo;
  `where run-commit.cmd` confirms it, and not finding it in the tree is never a reason to fall back
  to raw `git commit`.
- **`roadkeep lint` must be clean for what you touched** before that commit. The `Stop` guard runs it
  for you, but the commit is yours to hold: see the standing findings below, and never let a task add
  a new one.

## Design before ship

`IMPROVEMENTS.md` is not documentation written afterwards — it is the rationale the roadmap line
*points at*, and `ship` deletes it once the reasoning has done its job. Two things follow.

**The backlog is currently ahead of its design, and `lint` says so.** Sections exist through `§WW23`;
every line from `WW24` on points at a section that does not, which is 84 standing
`ref.unresolved` findings on a clean tree. That is a real state of this repository, not noise to
step past: **a task's design section is written before the task ships**, with
`roadkeep section add <id> --title "…"` and the prose on stdin. The number goes down one task at a
time and never up — a line added with no section behind it is the drift this file exists to stop.

**`💭` is in the open set for exactly this.** Mark a line whose design is unwritten with
`roadkeep status <id> 💭`, so what is planned is told apart from what is merely listed. (`pick
--designed` cannot filter on it until `roadkeep.toml` declares `undesigned = ["💭"]` under
`[markers]`; declare it when the filtering is wanted, and not before it is.)

## A block is a theme, and a theme is reused

**Reuse a block. Do not open one per batch of work.** A block names a **capability of this tool**,
and every task about that capability files under it, whenever it is found. Before `roadkeep add` the
question is *which theme is this*, never *which letter is next*.

| Block | Theme | What files under it |
|---|---|---|
| **A** | The verdict — a run is data, and "not observed" is an answer | exit codes, the unchecked list, the trace, the summary that may not say *every* |
| **B** | Attach, launch, and leave nothing behind | launching, attaching by pid or window, staleness of the binary, foreground ownership, leftover processes |
| **C** | Locate — the locator grammar and the tree an agent reads | the grammar, resolution and its deadline, actionability, `inspect`, staleness of an element |
| **D** | Act — patterns before pointers | the UIA patterns, declared synthesized input, read-back, focus, menus, the notification-area icon |
| **E** | Capture — the picture that proves what it photographed | off-screen render, the frame and its trim, DPI, occlusion, backdrop, blank and flat-colour refusals |
| **F** | Assert — the expectation is derived, never typed | what an assertion may claim and where the expected value comes from |
| **G** | The scenario — a case is a data file | the file format, loading, refusals at load, the per-project declaration |
| **H** | The Claude Code surface — plugin, tools, skill, hook | the MCP tools, the skill text, the hook, what an agent sees |
| **I** | The in-app half — the app cooperates with the harness | what an app under test declares so it can be driven honestly |
| **J** | Adoption — the proof is the deletion | the scripts this replaces in the projects that adopt it |
| **K** | The proving ground — a fixture app built to be hard to test | the fixture, its flags, the defects it reproduces |

All eleven are already declared in all three governed files, so `add` and `ship` find their heading.
**A block empties; it does not close** — `pick --block E` answering *"nothing is open in Block E"*
means that theme is quiet today, not finished.

**A new letter is only for a theme the table has no row for**, and then it is named for the
**capability**, never for the batch that found it: *"Verification — the checks that prove a change"*,
not *"what Block C turned up"*. `roadkeep block add L --title "…"` writes the heading into every
governed file at once — it is never hand-typed, and `ship` refuses with *"no heading declares Block
L"* until it exists. **Add the row to the table above in the same commit**, or the next task has
nothing to reuse and the drift starts again.

## The other two lists are governed too

`## Done when — Block X` and `## Non-goals` in the roadmap are not prose you may edit. They are
`roadkeep criterion` and `roadkeep non-goal`, declared in `roadkeep.toml` by `[criteria]` and
`[non_goals]` being present at all — both are declared here — and the hook denies a hand-edit to
either.

**When a task is decided against**, the conclusion lands as a non-goal (`roadkeep non-goal add`), so
the same idea is not re-filed by the next person who has it. `non-goal list` is a read to run
*before* proposing work, not after: the list binds what may be proposed, and nothing checks a
proposal against it for you.

### Criteria — the newest of roadkeep's lists, and the one this backlog leans on

`criterion` is recent, so it is easy to work here for a week without noticing it, and this project
has **32 criteria across all eleven blocks** — three per block, two under Block I. They are the
answer to a failure roadkeep measured: a non-goal says what is *not* built, nothing said what would
make a block **done**, so the only test left was *a line count reaching zero* — and a block declared
closed that way was reopened six times. A definition of done written into a rationale section is one
`ship` correctly deletes; this list is where it survives.

- **Two units, and the difference matters.** `--block X` is a claim about the capability and
  **outlives every line under it**. `--task <id>` is a claim about one line, lives under its own
  `## Done when — <id>` heading, and **leaves with the line**: `ship` and `retire` take the whole
  region in the same transaction. So a condition that stops mattering once the task lands goes on
  the task; one that still has to be true in a year goes on the block. Naming both addresses at once
  is refused.
- **Write the task's criteria at `add` time**, whenever "done" is anything more than the line's own
  sentence. `roadkeep brief <id>` prints the task's list *and* its block's, each with its address —
  which is the whole point: an agent that started the task through `brief` never has to ask what
  finishing means, and never invents its own answer.
- **The lead is the address, not a field.** `criterion amend <lead> --why "…"` rewrites the reason
  where it sits, because `add` appends and the order of the list is the shape of what finishing
  means — a drop-and-re-add moves a line and shows a reviewer a deletion. A changed *lead* is the
  one case that is genuinely `drop` plus `add`. The address is the **pair**, so the same lead under
  two blocks is two claims and not a duplicate.
- **`add` opens the `## Done when — …` heading** where there is none — but never the block: a label
  the roadmap does not declare is refused, so a typo opens nothing. The heading also survives the
  last bullet, since a block whose criteria all went is one somebody asked the question about.
- **`criterion list [--block X|--task <id>]` is never refused**, and it says *which* empty it found —
  ungoverned, unasked, or all dropped. Read it **before a block's last open line ships**: "the block
  is finished" is that reading, never a line count reaching zero. And read the `--why` as written —
  each of the 32 here names a run or an observation that settles it, which is what makes the reading
  a check instead of a mood.
- **Presence, not enforcement.** roadkeep asserts the list exists and is well formed; whether the
  work *satisfies* a criterion is a judgement it has no model for (L4). Nothing goes red when a
  criterion is untrue, which is exactly why the read-back before shipping is a rule here rather than
  a suggestion.

## `ship --why`, or live with it

`ship` copies the roadmap line's `why` into the ledger by default — and that line states a
**problem**, because that is what a roadmap line is for. A ledger entry states an **outcome**: what
now works. `--why` is the only chance to say so. **`amend` refuses a shipped id** ("it is already in
the changelog"), so write it at ship time:

```
roadkeep ship WW1 --why "A run that could not evaluate an assertion exits 2 and names each unrun assertion, so CI tells a hole apart from a pass without reading prose."
```

**There is one door back, and it is not a second draft.** `record amend <id> --why "…"` rewrites an
entry's sentence where it stands — not `drop` plus `add`, which would move the line to the end of
its block and show a reviewer a deletion where a word changed. Use it for a sentence that is *wrong
about the repository*, not for one you would now phrase better.

## A task that leaves without shipping

**`retire` works in this project** — `[markers] retired = "🗑"` is declared and no `[ledger] marker =
false` suppresses it, so a 🗑 entry can be told from a ✅ one. (It is refused in claude-tray for
exactly that reason; do not import the workaround from there.) `roadkeep retire <id> --why "…"`, and
the sentence carries the whole burden of not lying:

- **Open with the decision, not with work.** *"Measured before deciding, and the premise did not
  survive: …"* — never a sentence that reads as something built.
- **Give the evidence that settled it**, in numbers where there are numbers. A decision with no
  measurement behind it is an opinion that has taken an id.
- **Say where the conclusion now lives** — usually the non-goal you filed with it.

## The adopter-facing surface gate

winwright is a tool other projects adopt, so its users are the scenarios and the agents driving
them. **Every time a task ships, run this decision:**

1. **Would an adopter do something differently because this shipped?** A new verb, a locator form, a
   refusal they can now hit, an exit code, a field in the scenario file, a change to what the MCP
   tools or the skill say. If **no** — internal engineering, a refactor, a dev-only flag — it gets
   **no** README or reference change. Say so in the commit message and stop. Don't invent thin docs
   for internal work.
2. **If yes, hit the surfaces that exist:** the README's feature list, the scenario-format reference,
   and — for anything an agent calls — the Block H surface itself: the tool descriptions and the
   skill text, which are what an agent reads instead of the README. A refusal that is not written
   down is a refusal an adopter meets for the first time on a red run.
3. **Write for the adopter, not the commit.** Never paste `IMPROVEMENTS.md` rationale verbatim: that
   file argues *why*, a reference says *what it does and how to use it*.
4. These surfaces are in the same repo, so they belong in the **same commit** as the task.

## Prove it by running

The whole point of this tool is that a green means something, so its own tasks are held to it:

- **A verdict claim is proven by a run**, against the Block K fixture once it exists, and against the
  real target where it does not. A capture task is not done without the picture; an act task is not
  done without the read-back that says the act landed.
- **Never report a pass that skipped assertions.** A summary saying every check passed while one of
  them never ran is `WW6`, which is to say the exact defect this project was started over. If
  something could not be evaluated here, that goes in the report — named — and not in an info line.

## Release notes

A release is not a task. Cutting one is a `chore: release vX.Y.Z` commit of its own, never bundled
with a task.
