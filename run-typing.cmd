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
rem `sweep` is WW312's, and it drives a send the engine does not have: one call per code unit, at 0,
rem 32, 48, 64, 80 and 96ms apart, which are the spacings WW310's band was measured across. It reads
rem what was injected beside what arrived at each one, so a fault inside the band can be attributed
rem to the send or to what happens after it.
rem
rem Three arms at every spacing, so it is eighteen cells and long: at 150 rounds it takes the better
rem part of an hour. `quiet` and `watched` differ only in whether anything reads the box while the
rem queue drains; `whole` is the engine's own shape, erasing and sending in one act so the backspaces
rem are still draining when the text goes in.
rem
rem `delay` is WW329's, and it drives the send the engine does have with the pause the engine does
rem not take: erase and send in one act, then wait 0, 50 or 150ms before looking at the box at all.
rem What it prices is the alternative to the resend - a pause paid by every send against three
rem resends paid by the 1% that fault - so it reports the milliseconds a round beside the rate.
rem
rem The configuration moved to the third argument when this gained the second. `run-typing.cmd 400`,
rem `run-typing.cmd 150 sweep` and `run-typing.cmd 1200 delay` are the three a person types.
setlocal

set CONFIG=Debug
if not "%3"=="" set CONFIG=%3

dotnet build "%~dp0Winwright.slnx" --configuration %CONFIG% --nologo || exit /b 1
dotnet "%~dp0tools\Winwright.Typing\bin\%CONFIG%\net10.0-windows\Winwright.Typing.dll" %1 %2
exit /b %ERRORLEVEL%
