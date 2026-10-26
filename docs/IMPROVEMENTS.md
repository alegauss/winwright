# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

### §WW574 Carry build outputs to the guest

Met in Modelado (MOD28). The task's evidence is its own installer, installed, upgraded
and uninstalled on the guest. `build\build-installer.cmd` writes it to
`artifacts\installer\Modelado-Setup-<version>.exe`, and `.gitignore` keeps `artifacts\`
out of the tree, as build outputs should be. `tools\run-tests-vm.ps1` carries `git
ls-files -c -o --exclude-standard` of `-Tree`, so the installer never reaches the guest.
Building it there instead needs Inno Setup on the guest, which the runner rightly does
not provision.

The workaround is `build\check-installer-vm.ps1`, about 15 lines. It copies the two
installers, the check script and a fixture into `%TEMP%\modelado-installer-check`, runs
`git init` there so the runner can list it, and passes that folder as `-Tree` with
`-NoGate`. The run then also prints "no winwright.trx came back from the guest" and the
desk-claim narrative, which describe a case run this one is not.

What winwright should offer: a way to carry named files beside the tree, ignored or
outside it, e.g. `-Carry artifacts\installer\*.exe` (or a `carry` list in
winwright.json), landing at the same relative path in the guest. A run whose `-Run` is
not a case run should also be able to say so, so it does not report a missing trx.

### §WW575 Installed apps and relaunches

Met in Modelado (MOD31). The update's evidence is a click in the installed app: About's
"Update and restart" downloads the newer installer, starts it silently and exits, and
the installer starts Modelado again at the new version. A case wanted three things:

- a fixture whose application is the copy an installer put in
  `%LOCALAPPDATA%\Programs\Modelado\Modelado.exe`, not the build tree's that
  `winwright.json` names;
- steps that go on after the application exits by its own choice, without that reading as
  a crash;
- a claim that a new process of the same executable came up afterwards, and a locator
  that reaches its window.

None of these can be written, so `build\check-update.ps1` drives the windows itself with
System.Windows.Automation from Windows PowerShell. It finds elements by AutomationId
under the process's window, polls until they are enabled, invokes them, reads a
TextBlock's Name and polls `Get-Process` for the relaunched copy, about 40 lines that
duplicate what winwright's locators and waits already do. It runs on the guest through
`-Run` with the staged tree WW574 describes.

What winwright should offer: a fixture `application` path (or a variable) overriding the
project's exe, an act or expectation that the application exited, and an `await
relaunch` that rebinds the case to a new process of the same executable, with a
deadline.

### §WW576 A chained -Run

`run-tests-vm.ps1` writes the guest script line `call $script:Run >
"$GuestSync\vm-run.log" 2>&1`. With `-Run 'dotnet build X &
blender\tests\run-blender-tests.cmd'`, cmd binds the redirection to the last command
only: the build's output went to no log, and one run came back as "the guest wrote no
exit code". A `for /d %B` inside `-Run` also broke, because the line runs from a batch
file where `%B` must be `%%B`, and cmd refused the whole line before running any of it.

Modelado's workaround was `-Run 'cmd /c "a && b"'`. The runner could wrap `-Run` itself
(`call cmd /c "<Run>"`, or write `-Run` to its own .cmd and call that), so what a
developer types at a prompt is what the guest runs, as the parameter's own comment
promises.

### §WW578 -Bring through the .cmd wrapper

Modelado's MOD33 made the site's demo model on the guest (modelado-cli object, then edit
--simplify --preview) and needed two files back: robot.glb and robot-poster.png. It ran

    run-tests-vm.cmd -Tree D:\Git\alegauss\modelado -Run mod33-demo.cmd -NoGate -ResultsIn demo-out -Bring robot.glb,robot-poster.png

and the runner answered `no robot.glb,robot-poster.png came back from the guest`. The
wrapper calls `powershell -File tools\run-tests-vm.ps1 %*`, and under -File a comma list
is not an array: `[string[]] $Bring` binds one element holding the whole string, so the
first (and only) name is a file that does not exist. The script's own comment says the
first is fetched always and the rest on a green run, which no caller of the .cmd can
reach.

The workaround in Modelado: the guest script tars both files into demo-out\demo.tar and
-Bring names that one archive.

What winwright should offer: -Bring split on commas inside the script (a name holding a
comma is not a file anyone brings), or a -Bring that takes a folder relative to
-ResultsIn and brings its files. Either way the miss names each file that did not come
back.

### §WW579 Dismiss the shell's overlays before the desk check

On 2026-10-26 Modelado's guest run was refused before anything ran:

    run-tests-vm: the guest's desk is waiting for an answer: SearchHost (pid 7432, Windows.UI.Core.CoreWindow) 'Pesquisar' held the foreground for every look, and it is still there after the run tried to put it away - left 'Pesquisar' (Windows.UI.Core.CoreWindow) alone: it has no minimise button, which is what a question looks like

The Windows Search flyout (SearchHost.exe, class Windows.UI.Core.CoreWindow) and the
Start menu (StartMenuExperienceHost.exe, same class) are shell overlays, not questions:
they have no minimise button because they close on Escape or on losing focus, and
nothing in them waits for an answer. The runner's rule "no minimise button means a
question, leave it alone" reads them as dialogs, so the operator had to open the VM
console and close Search by hand.

What winwright should do: before the foreground check, recognise these overlays by
process and class (SearchHost, StartMenuExperienceHost, ShellExperienceHost's action
centre, the notification flyouts) and dismiss them (send Escape to the overlay, or ask
the shell to hide it), then read the desk again. Say in the run's first lines what was
dismissed. A window outside that list keeps today's rule, so a real question is still
never answered on the operator's behalf.

### §WW580 Restart the guest shell once before refusing

Right after Search was closed on Modelado's guest (2026-10-26), the next run was
refused:

    run-tests-vm: nothing may take the foreground on the guest desk: explorer (pid 6928, Shell_TrayWnd) '' is holding it, and a window put up on that desk was refused for the whole deadline

The refusal's remedy is "Restart the guest shell and run again", and WW472 measured that
this is what clears it. The runner already holds the guest's credentials and runs
programs there through vmrun, so the remedy is one it can apply itself; instead every
adopter opens the console, or writes their own vmrun call, each time it happens.

What winwright should do: when the taskbar (explorer, Shell_TrayWnd) holds the
foreground for the whole deadline, restart explorer in the guest session once (taskkill
explorer, start explorer, wait for the session's shell), read the desk again, and go on
if it is now takeable. Say so in the run's lines. Refuse as today only when the restart
did not clear it, so the refusal means a desk that is really held. A switch to forbid
the restart keeps a run that must not touch the shell possible.

### §WW581 A desk-free run

Modelado runs CLI-only work on the guest through run-tests-vm.cmd -Run: model downloads,
modelado-cli reconstructions for the site's demo (MOD33), a CPU probe (MOD34). None of
them opens a window. On 2026-10-26 two such runs were refused before anything ran:

    run-tests-vm: the guest's desk is waiting for an answer: SearchHost ... 'Pesquisar' held the foreground
    run-tests-vm: nothing may take the foreground on the guest desk: explorer (pid 6928, Shell_TrayWnd) '' is holding it

The refusals are right for a suite of UI cases, and the reason a person reads them is to
restart the guest shell. For a -Run that drives no window they stop work that the desk
does not touch, and the operator had to close the Search window and restart explorer by
hand.

The runner offers no way to say so: the parameters are -Screenshot, -CommittedOnly,
-NoGate, -Vmx, -Tree, -Name, -Run, -ResultsIn, -Bring and -Bound, and none skips the
desk claim and the foreground check.

What winwright should offer: a -NoDesk (or -Headless) switch that skips the foreground
check and the desk claim, still holding the guest so two runs do not interleave, and
says in its first lines that the run takes no desk. A refusal over the desk could then
name that switch for a run whose command is not the suite.

### §WW582 Commands before the launch

Met in modelado, MOD65 (2026-10-26). modelado-cli gained `cloud image-to-3d`, which
sends a photo to Meshy AI and records the spend in
%LOCALAPPDATA%\Modelado\cloud-ledger.jsonl, the ledger the window's About reads. The
task's evidence is that a job sent from the CLI shows in About, so the case wanted to
be:

```json
{ "name": "a window whose LOCALAPPDATA had a Meshy job sent by modelado-cli",
  "variables": { "LOCALAPPDATA": "{files}" },
  "before": [{ "run": "tools/Modelado.Cli/bin/Debug/net10.0/modelado-cli.exe",
               "arguments": ["cloud", "image-to-3d", "cases/fixtures/photo.png", "-o", "{files}/m.glb",
                             "--cloud-api", "meshy=http://127.0.0.1:47631/meshy/"],
               "input": "msy_case_key", "expectExit": 0 }],
  "arguments": ["--cloud-api", "meshy=http://127.0.0.1:47631/meshy/"] }
```

then a step reading `Text#CloudUsage-meshy` for "1 job(s), estimated 30, charged 30". A
fixture can stage files, variables and arguments, but cannot run a program in its staged
folder, with the fixture's variables, before the application launches. Staging a
hand-written ledger instead would prove only that About reads a file, not that the CLI
writes what About reads.

What modelado settled for: a CLI test (tests/Modelado.Cli.Tests/CliJsonTests.cs) and the
Blender add-on test assert the ledger the CLI wrote, through CloudLedger.Drift, the call
About's line is made from. The window itself never opens on it.

winwright should offer: a fixture's `before` list of commands, each run with the
fixture's variables and working directory, its standard input given, its exit code
checked, and its output in the report, so a failing setup reads as a setup failure
rather than a red step.

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

### §WW572 Drag between two points of one element

Met in photo-boost on 2026-10-23, task PB67. The crop editor is drawn on a GPU surface
(a child HWND with a swapchain), so its eight handles and the rectangle's inside are
pixels, not elements. Resizing the crop means pressing on a handle and releasing
somewhere else on the same surface. The step the evidence wanted:

```json
{ "locator": "Slider#Surface", "act": "drag", "from": "12%,8%", "to": "40%,30%",
  "named": "the top-left handle is dragged inwards" }
```

`drag` (WW520) only goes from one element onto another, and `pick at` (WW267) is a
single press at a position, so no step can press at one point of an element and release
at another. The case in `cases\window.cases.json` sets the rectangle through the
surface's ValuePattern with `set value` instead, and the handle arithmetic is covered by
unit tests in `tests\PhotoBoost.Render.Tests`; the gesture itself, a mouse drag landing
on the handle the maths says it should, has no case.

What winwright should offer: `drag` with a `from` and a `to` position inside the subject
(the same position grammar `pick at` takes: fractions or pixels from the element's
top-left), synthesised as press, moves and release, and read back like any other act.
Any canvas, map, slider track or custom-drawn editor needs it.

### §WW577 A step's own named wait

`timeouts.resolve` in winwright.json is the one wait every step's read takes, and a step
cannot name a longer one. Modelado's real Meshy AI job takes minutes between Send and
the result landing in the history, and the step that reads that version could only wait
for it once the whole project's resolve was raised from 10 s to 10 min. That raise could
not be committed: every `absent` step waits the full resolve, so the gate would run for
hours.

A step that waits on a slow, external result (a cloud job, a long local operation) needs
its own named wait, declared once in `timeouts` (for example `"cloud": 600000`) and
chosen by the step (`"waits": "cloud"`). That keeps the rule that no number is typed
into a case. Modelado's MOD53 live case (a 50 s 3D Object on the guest) found a second
wait: a read whose element is there but whose value has not arrived gives up after
`timeouts.act` (one poll over 13 s), so `act` had to be raised as well as `resolve`. The
named wait has to cover both.

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

### §WW573 Claims on an image file

Met in Modelado (MOD19), case "Save view as image writes a 2048 px PNG of the view,
clear around the model" in `cases\window.cases.json`. The app renders the 3D view again
at 2048 px on the long side and writes a PNG with a transparent background. The case
wanted:

```
{ "file": "{files}/view.png", "act": "read", "image": { "longSide": 2048, "transparent": true } }
```

A file step takes `read` with `sameBytesAs` and nothing else (WW535). The saved bytes
depend on the viewport's proportions, which differ from one desk to the next, so there
is no fixture to compare them to. The case works around this by dropping the PNG back
into the app and reading the app's own source chip (`view.png · 2048 × N · cut out`).
That works only because this app happens to open PNGs and report their size and alpha.
An app that writes images it cannot open has no case at all.

What winwright should offer: a file step claim that decodes an image (PNG, JPEG, WebP)
and checks its width, height or long side, whether it has transparent pixels, and
perhaps the alpha at a named point. Each claim is derived from the decoded file and
reported with what was read, so a wrong size names the size found.

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system
