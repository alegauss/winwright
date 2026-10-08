# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW515 The order nothing enforces

WW509 split a launch into two calls. `Stage(root)` empties the directory a fixture's
files go in and copies them there; `Starting(executable, root)` builds the start info,
resolving `{files}` to that same directory. Both derive the path from the root and the
fixture's name, so they cannot disagree about *where* — and nothing says one must happen
before the other.

`Suite.Opened` stages first, which is why this ships correct. A caller that does not
gets a start info whose `APPDATA` points at a directory that may not exist or holds what
an earlier run left; the application reports a store it cannot read, and the red is
about the application. `SuiteLaunchTests` already launches through a door of its own, so
the second caller is not hypothetical — it stays correct only because its fixture stages
nothing.

WW508 made this argument one field over and took the other answer: `Starting` requires
the root rather than defaulting it, because a default there would be the defect that
task removed, spelled as a choice nobody made. The ordering here is that argument
unfinished.

What finishes it is a shape where the order cannot be got wrong rather than a comment
asking for it. `Starting` taking the staged directory is the blunt version and puts a
third parameter on a launch builder. A type standing for a fixture staged for one launch
— the fixture, the root, the directory, with `Starting(executable)` on it — reads right,
and is a refactor rather than a parameter.

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

### §WW516 A press that checks the foreground whatever it located

Found in quickshell's guest run on 2026-10-08 (cases\palette.cases.json and
cases\tabs.cases.json, run by Quickshell.Cases through Suite.Launch). The guest's
foreground belonged to explorer's 'Program Manager', not to the client under test.

The two cases met it differently. In tabs, step 1 is `press Ctrl+Shift+T` on
`Document[class="Terminal"]`; winwright checked that the foreground was the window under
test, found it was not for five seconds, and reported the step Unchecked with that
reason. Good. In palette, step 1 is `press Ctrl+Shift+P` on `TabItem[order=right]`; the
trace reads `Pattern = synthesised keyboard, ReadBack = selected, Verdict = Ok`, so the
press was called successful, though the chord went nowhere the client could hear and no
palette opened. Step 2, `set value` on `Edit`, then broke with NotActionableException
"nothing matched, or what matched has gone since", which names the wrong thing: nothing
was ever there to match.

What winwright should do: a press delivered as synthesised keyboard input checks the
same foreground precondition whatever its locator resolved to, a TabItem selected
through its pattern included, and reports Unchecked with the foreground's owner when it
fails, as the Document case did. A pattern act that succeeded (the tab selected) should
not stand in for the keyboard half that was never delivered.

Falsified when a case whose press lands while another window holds the foreground
reports that step Ok.

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

### §WW514 The hole nothing refuses

`format.mjs` publishes the schema's own sentences, read out of `ScenarioSchema.cs` as
source rather than from a running build. An interpolated description is a hole somebody
has to fill, and `stringly` fills it: a `$"..."` string's `{Name}` is looked up among
the constants that source declares, and one it cannot find is refused.

That refusal is right and its pattern is narrower than C#. It matches
`/\{([A-Za-z]+)\}/`, so a hole naming another type's constant —
`{FixtureDeclaration.Staged}` — is not recognised as a hole at all. Not resolved, not
refused, and the sentence reaches the page carrying the expression an author wrote: a
reader is told to spell a field with `'{FixtureDeclaration.Staged}'`.

Found writing WW509, which added the first such description and tripped exactly this.
The repair there was to spell the token out and pair the spelling to the constant, which
is right for that field and does nothing for the next one.

The fix is the pattern: anything brace-delimited that could be a name, dotted ones among
them, is a hole, and one this cannot resolve stops the build. The resolver has no
business following a qualified name into another file — refusing is the whole job,
because an author who is told has two correct answers, to spell it out or to move the
constant, and silence leaves them neither.

Not the fix: the refusal fires only for `$`-prefixed strings, which is right. A plain
string's braces are a reader's, and that is how WW509's spelling survives it.
