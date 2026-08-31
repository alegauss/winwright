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
rem second is the build configuration.
rem
rem The spacing sweep that used to be the third argument is gone with the experiment it served. WW304
rem swept it and WW310 read the result: the rate is a band and not a slope, so there was never a
rem number to adopt, and the engine repairs the fault by its signature instead. What this reports now
rem is how often that repair fired and whether anything outlived it.
setlocal

set CONFIG=Debug
if not "%2"=="" set CONFIG=%2

dotnet build "%~dp0Winwright.slnx" --configuration %CONFIG% --nologo || exit /b 1
dotnet "%~dp0tools\Winwright.Typing\bin\%CONFIG%\net10.0-windows\Winwright.Typing.dll" %1
exit /b %ERRORLEVEL%
