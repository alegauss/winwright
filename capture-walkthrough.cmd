@echo off
rem WW493: capture one adoption's run from a desk, rather than remembering it.
rem
rem `capture-adoption.cmd` captures the half that fails before anything runs. This is the other
rem half: it builds samples\Walkthrough - an application with a window, a case written against it,
rem and the four calls an adopting project writes - and keeps what the run said.
rem
rem It needs a desk, because the case creates a real window, takes it and reads it back. Run it the
rem way the suite is run and the capture comes back in the working tree:
rem
rem     run-tests-vm.cmd -NoGate -Run "capture-walkthrough.cmd" ^
rem         -ResultsIn "site\docs\src\data" -Bring "walkthrough.captured.json"
rem
rem What comes back lands in TestResults\vm\, which is where the runner stages everything it
rem fetches - copy it over site\docs\src\data\walkthrough.captured.json, which is the file the page
rem reads and `site\scripts\walkthrough.test.mjs` holds to this tree.
rem
rem It packs first, because the sample restores the engine from packages\ the way an adopting
rem repository restores it from a feed - a capture taken against a project reference would prove
rem this checkout, and what an adopter has is the package. `-NoPack` skips that for a second run.
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\capture-walkthrough.ps1" %*
exit /b %ERRORLEVEL%
