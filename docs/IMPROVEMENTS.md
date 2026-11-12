# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW619 Launching through a file association

From Modelado's MOD75 and MOD78 (11/11/2026). The Store package declares a
`uap:FileTypeAssociation` for .glb, .obj, .ply and .stl, and the claim to prove is that
Open with in Explorer lists Modelado and that activating a file through the association
opens it in the window. A fixture can name an `application` and its `arguments`, and
`opens` a folder in Explorer, but nothing starts the application the way the shell does
for a file: through its association (ShellExecute with the file, or
IApplicationActivationManager.ActivateForFile for a packaged app).

What the case settled for (`cases-packaged\packaged.cases.json`, fixture "the packaged
window opened on a model the way Open with does"): the execution alias started with the
bare path, which is the command line the association runs, plus a registry check in
`build\check-msix.ps1` that the package's ProgID is under `.glb\OpenWithProgids`. The
activation itself, and the Open with list, stay unread.

What winwright should offer: a fixture field such as `"activates": "<file>"` that
launches through the file's association: ShellExecuteEx with `SEE_MASK_CLASSNAME` and a
ProgID, or `ActivateForFile` with an AUMID for a packaged app, since the default handler
is the desk's choice. The broker's process is no child of the run, so it is taken on as
an adopted process, outside the WW586 job, and the report says what it starts is
untracked. Cases: an extension with no association, and a ProgID or AUMID not claiming
the type, are refused with the reason; an activation landing in an instance already
running is said as that, never attached to silently.

### §WW620 Following the window a launcher's child draws

From Modelado's MOD79 (11/11/2026). The Microsoft Store refused a package with a second
application that has no Start entry (a headless app, HeadlessAppBypass), and an
application may declare one execution alias, so Modelado's package aliases
`modelado-cli.exe` alone. From outside the package the window is reached as
`modelado-cli window [arguments]`: the CLI starts `Modelado.exe` beside it, with package
identity, and waits until it closes (the Blender add-on follows that process to know
when its session ends).

`cases-packaged\packaged.cases.json` names that as the fixture's `application`
(`%LOCALAPPDATA%\Microsoft\WindowsApps\modelado-cli.exe`, arguments `window --provider
cpu`). winwright waits for the launched process to draw a window, and refuses: "drew no
window in 10000ms, so nothing about this case was observed". The window is there, in the
launched process's child.

What the project settled for: `build\check-msix.ps1` starts the alias itself and reads
the child's window (its process in the package folder, its title), and the three
packaged cases stay declared but unrun.

Validated 12/11/2026: WW586 already puts each launch in a job that the stop ends, so the
child needs no new registration; only the window wait (`Suite.cs:570`) asks the launched
pid alone. Offer an opt-in `"windowOf": "<image>"` that makes the wait ask the job's
members of that image too, never a default, or a splash draws first. Cases: a packaged
child outside the job is found by its parent pid or refused by name; a launcher exiting
while its child lives is not WW606's refusal; no match names what the job held.

### §WW621 WW621 - A leftover app holding the guest tree

From modelado, MOD81 (11/11/2026). `build\store-shots-vm.ps1` runs `run-tests-vm.ps1
-NoGate -Run 'powershell ... store-shots.ps1'`, a script that starts Modelado.exe from
the carried tree, drives it and closes it. A first attempt threw half-way (a UI
Automation lookup), so the script never closed the window. The run after it was refused
before carrying anything:

    run-tests-vm: the guest tree is held open by Modelado (11176) runs from C:\src\modelado-store-shots\app\Modelado.exe
      -> Windows said: access to 'clrjit.dll' was denied.. End them at the guest console, or revert the VM to a clean snapshot.

The runner already knows the holder's pid and that it runs from the very tree it is
about to replace, yet the only remedies it offers are a person at the guest console or a
snapshot revert. The workaround was by hand on the host: read `d:\tmp\winwright-vm.env`,
then `vmrun listProcessesInGuest` and `vmrun killProcessInGuest <vmx> 11176` (a
`taskkill /IM` through `runProgramInGuest` exited 1 without ending it). The script now
closes its window in a `finally`, but any adopter whose `-Run` dies leaves the same
lock.

Validated 12/11/2026, and wider: winwright's own suite meets it after any `-Bound` or
killed run. The desk claim is taken before the sync, so no live run of this host owns
the tree then. What the runner should do: end through `vmrun killProcessInGuest` each
holder running from the tree, say which pids, walk again and delete. One that only has a
tree file loaded is ended where it is a dotnet host whose command line names the tree;
explorer never, and the rest stay a refusal. No switch. The refusal's doubled period
(`denied.. End`) goes, and `runner-arms.ps1`'s two held-tree cases change with it.

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

### §WW622 The README fence the -Carry example never closes

Found while adding `-Needs` to the README's "Running an adopting project's cases off the
desk" section (WW618). The `-Carry` example closes its code block with a fence that has
prose after it on the same line: the closing three backticks are followed by "**That the
runner can carry two trees is why it prints which one it took** ...". A fence with text
after it does not close a block in CommonMark, so on GitHub everything after the example
renders as code until the next fence, which is the `-Needs` example below it.

What to build: split the line, so the fence stands alone and the sentence about which
tree the runner took is its own paragraph (it reads best after the `-Name` paragraph it
explains). Then a desk-free case over README.md that every fenced block's closing line
is the fence alone, so the next example added in this section cannot do it again. Check
whether an existing README sweep (the WW503 and WW499 gates read the README) is the
place for that rule rather than a new class.

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system

## Block N — The name — winwright becomes paneproof
