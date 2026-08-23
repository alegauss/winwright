@echo off
rem WW117, WW138: build and run the suite. The roll call is part of the run rather than a step
rem beside it, so there is nothing here the plain `dotnet test` does not already do - this exists
rem to build first and to be the one command a person types without thinking about either.
rem
rem A test host that dies mid-run takes its remaining tests with it and the runner still prints a
rem pass: measured here at 352 of 374, a green covering twenty-two tests that never ran.
setlocal

set CONFIG=%1
if "%CONFIG%"=="" set CONFIG=Debug

dotnet build "%~dp0Winwright.slnx" --configuration %CONFIG% --nologo || exit /b 1
dotnet test "%~dp0tests\Winwright.Tests\Winwright.Tests.csproj" --configuration %CONFIG% --no-build
exit /b %ERRORLEVEL%
