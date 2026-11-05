@echo off
rem WW384. The cases that take the desktop away from whatever is holding it, run alone.
rem
rem `run-tests.cmd` leaves these out, and the reason is the desk rather than the case: the clearer
rem ends in Win+D, whose foreground lock then refuses this process the desktop for minutes. Measured
rem twice while WW384 was written, at five reds in two other classes each time - so a run that takes
rem the desk is a run of its own, and this is it.
rem
rem It is a desk run, which means a guest and not the machine somebody is working at:
rem
rem   run-tests-vm.cmd -Run "run-desk.cmd Debug"
rem
rem and the guest's shell is worth restarting afterwards, because the lock outlives the run.
rem
rem No roll call: the roll compares what a run discovered against the runs before it, and a run that
rem asks for one trait would tell it the suite had shrunk to a case.
setlocal

set CONFIG=%1
if "%CONFIG%"=="" set CONFIG=Debug

dotnet build "%~dp0Winwright.slnx" --configuration %CONFIG% --nologo || exit /b 1

rem `-v normal`, because a run of one case that excused itself reads exactly like a run of one case
rem that proved something - and what the case read is written to the console for that reason.
dotnet test "%~dp0tests\Winwright.Tests\Winwright.Tests.csproj" ^
  --configuration %CONFIG% --no-build --nologo -v normal ^
  --filter "desk=alone"

exit /b %ERRORLEVEL%
