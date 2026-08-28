@echo off
rem WW302: the experiment WW249 needs, which the suite deliberately does not carry. It types at a
rem real window for minutes, and that is a cost every guest run should not pay for a question asked
rem once.
rem
rem Not `run-tests.cmd`, and that is the point of it being here: this takes the desk for as long as
rem it runs, so it is a thing a person starts on purpose. Run it in the guest the way the suite runs
rem there - `tools\run-tests-vm.ps1 -Run "run-typing.cmd 400"` - because a run on the host steals the
rem foreground from whoever is using it.
rem
rem The first argument is how many rounds each arm types; the tool's own default stands without one.
setlocal

set CONFIG=Debug
if not "%2"=="" set CONFIG=%2

dotnet build "%~dp0Winwright.slnx" --configuration %CONFIG% --nologo || exit /b 1
dotnet "%~dp0tools\Winwright.Typing\bin\%CONFIG%\net10.0-windows\Winwright.Typing.dll" %1
exit /b %ERRORLEVEL%
