# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW618 Developer Mode on the guest for packaged-app checks

From Modelado's MOD73 (11/11/2026). `build\check-msix-vm.ps1` stages the unsigned MSIX
layout `build-msix.cmd` makes and runs `tools\run-tests-vm.ps1 -Tree <stage> -NoGate
-Run "powershell ... check-msix.ps1"`, which calls `Add-AppxPackage -Register` on the
layout, then starts the app through `shell:AppsFolder\<family>!<app>` to prove the Start
entry.

The guest refused it: HRESULT 0x80073CFF, no developer licence or sideload policy. The
guest's Developer Mode is off, and the runner's session is not elevated, so the script's
attempt to set `HKLM\...\AppModelUnlock\AllowDevelopmentWithoutDevLicense` failed with
access denied. No `-Run` command can change that, so every packaged-app check an adopter
writes (Modelado's MOD73 and MOD78; claude-tray's packaged build) stops before its first
step, and the task is deferred until a person changes the VM by hand.

What winwright should offer: a desk requirement the run declares (say `-Needs
developer-mode`, or a field in the settings file) that the runner either establishes on
the guest through its own elevated tools channel before the run, or refuses up front
with the sentence and the setting to change, rather than leaving the adopter's script to
discover it as a deployment error halfway through. A guest-preparation step that turns
Developer Mode on once, recorded in the guest's settings, would serve every adopter
packaging an app.

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
launches the application through the file's association (by ProgID or AUMID where
several apps claim the type, since the default handler is the desk's choice), tracks the
process it lands in, and refuses with the reason when no association names the
application.

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

What winwright should offer: a fixture field such as `"window": "child"` (or by process
name, `"windowOf": "Modelado.exe"`) under which the case attaches to the first window a
descendant of the launched process draws, registers that process for stopping as it does
the launched one, and stops both at the end. The same serves any launcher, updater stub
or shell-out the adopter starts the application through.

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

What winwright should offer: when every holder runs from inside the guest tree it is
about to replace (so it can only be a leftover of an earlier run of this same tree), end
those processes itself and say which, or at least a `-EndHolders` switch that does it,
and a refusal naming that switch. Processes outside the tree stay a refusal, as now.

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system

## Block N — The name — winwright becomes paneproof
