# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW536 A second window a fixture opens

Found by quickshell (QS292, 2026-10-14) adopting 1.1.0. A drop with toDesktop needs a
window to land in, and for a file that is an Explorer folder opened on a directory the
case can check afterwards. A fixture launches the application under test and nothing
else, so the folder window cannot be put on the desk by the case, and ExplorerDropRun
opens it from C# instead.

What to build: a fixture may declare a second window the engine opens before the first
step and closes after the last, by a shell verb on a path token ({files}/landing, opened
with explorer.exe), registered like any launch so a window left open is named as a
survivor. Its locator root is the desktop, as toDesktop's is.

Falsified when a drop case needs C# to put its target window on the desk.

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

### §WW535 A file a case can read

Found by quickshell (QS292, 2026-10-14) adopting 1.1.0. WW530 lets a case drop a file
onto an Explorer folder with toDesktop, and the README says whether the file arrived is
the next step's to check, which for Explorer is a file on disk and not an element. No
act or reading reaches a file, so the claim quickshell's ExplorerDropRun makes, that the
dropped file is there byte for byte, still cannot be written as a case.

What to build: a reading of a file the fixture named, by a path token the engine
resolves (such as {files}/landing/report.bin), which says whether it exists and whether
its bytes equal another named file's, waited for as any read-back is. Its absence after
the wait is a red naming the path; a different length or content says where the first
byte differs.

Falsified when a case cannot say that a file dropped onto Explorer landed with the bytes
it had.

## Block G — The scenario — a case is a data file

### §WW537 A claim that is the name again

Raised in the second dev.to comment on the 374-to-352 article. The commenter ran four
synthetic runs over three cases and diffed them three ways: by name, by `catches` as
authored, and by `catches` filled from the case's own name. The authored column did what
WW533 meant: quiet on a rename, red on a lost claim, and red on a case that kept its
name and changed its claim. The derived column matched the name diff line for line,
flagging the rename and missing the replacement. So what makes the claim diff worth
having is that `catches` says something the name does not. Being filled is not enough.

Today the loader takes any non-empty `catches`, so a case whose claim is its name pasted
again loads, counts as justified, and quietly turns WW533 back into WW532.

What to build: refuse, where it is written, a `catches` equal to the case's `name` once
both are folded (case, whitespace and punctuation ignored), addressed as
`cases[i].catches` and naming the name it repeats. Equality and never containment: a
short name can stand inside a real claim, and a refusal that turns away an honest
sentence teaches people to leave the field out, which is worse than the paste.
Containment waits for a pasted title that gets past equality.

Falsified when a case file whose `catches` repeats its own name loads without a refusal.

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing
