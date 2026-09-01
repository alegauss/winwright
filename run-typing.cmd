@echo off
rem WW249: the measurement the suite deliberately does not carry. It types at a real window for
rem minutes, and that is a cost every guest run should not pay for a question asked once.
rem
rem Not `run-tests.cmd`, and that is the point of it being here: this takes the desk for as long as
rem it runs, so it is a thing a person starts on purpose. Run it in the guest the way the suite runs
rem there - `tools\run-tests-vm.ps1 -Run "run-typing.cmd 400"` - because a run on the host steals the
rem foreground from whoever is using it.
rem
rem The first argument is how many rounds it types; the tool's own default stands without one. The
rem second is which experiment to run, and the third is the build configuration.
rem
rem Bare, this reports how often the engine's repair fired and whether anything outlived it, against
rem the send the engine actually makes - one SendInput for the whole string.
rem
rem `sweep` is WW312's, and it drives a send the engine does not have: one call per code unit, 32,
rem 64 and 96ms apart, which is the shape WW310's band was measured in. It reads what was injected
rem beside what arrived at each spacing, so a fault inside the band can be attributed to the send or
rem to what happens after it. The configuration moved to the third argument when this gained the
rem second; `run-typing.cmd 400` and `run-typing.cmd 150 sweep` are the two a person types.
setlocal

set CONFIG=Debug
if not "%3"=="" set CONFIG=%3

dotnet build "%~dp0Winwright.slnx" --configuration %CONFIG% --nologo || exit /b 1
dotnet "%~dp0tools\Winwright.Typing\bin\%CONFIG%\net10.0-windows\Winwright.Typing.dll" %1 %2
exit /b %ERRORLEVEL%
