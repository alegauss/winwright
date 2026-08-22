@echo off
rem WW117: run the suite and then check that everyone who was discovered answered.
rem
rem A test host that dies mid-run takes its remaining tests with it and the runner still prints a
rem pass: measured here at 352 of 374, a green covering twenty-two tests that never ran. The count
rem is the check, so the run is these three steps and not the middle one.
setlocal

set SUITE=%~dp0tests\Winwright.Tests\Winwright.Tests.csproj
set OUT=%~dp0TestResults
set CONFIG=%1
if "%CONFIG%"=="" set CONFIG=Debug

if not exist "%OUT%" mkdir "%OUT%"

dotnet build "%~dp0Winwright.slnx" --configuration %CONFIG% --nologo || exit /b 1

rem Discovery first, and from the built assembly, so the two lists are about the same binary.
dotnet test "%SUITE%" --configuration %CONFIG% --no-build --list-tests > "%OUT%\discovered.txt" || exit /b 1

rem The run itself. Its exit code is kept: a red suite is still red whatever the roll call says.
dotnet test "%SUITE%" --configuration %CONFIG% --no-build ^
  --logger "trx;LogFileName=winwright.trx" --results-directory "%OUT%"
set RAN=%ERRORLEVEL%

dotnet run --project "%~dp0tools\Winwright.RollCall\Winwright.RollCall.csproj" --configuration %CONFIG% --no-build -- ^
  --discovered "%OUT%\discovered.txt" --results "%OUT%\winwright.trx"
set ROLL=%ERRORLEVEL%

if not "%ROLL%"=="0" exit /b %ROLL%
exit /b %RAN%
