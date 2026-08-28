@echo off
rem WW117, WW138: build and run the suite, roll call included - the one command a person types
rem without thinking about either.
rem
rem A test host that dies mid-run takes its remaining tests with it and the runner still prints a
rem pass: measured here at 352 of 374, a green covering twenty-two tests that never ran.
rem
rem WW297: `dotnet msbuild -t:TestAndRoll` and no longer `dotnet test`, because the roll used to be
rem hung on AfterTargets and MSBuild skips those where the target failed - so a red run said nothing
rem about what it had excused. TestAndRoll reaches it on both verdicts and changes neither. This is
rem now a thing the plain `dotnet test` does not do: that still rolls on a green run and still goes
rem quiet on a red one, which is why this file is the command and not a convenience over it.
setlocal

set CONFIG=%1
if "%CONFIG%"=="" set CONFIG=Debug

dotnet build "%~dp0Winwright.slnx" --configuration %CONFIG% --nologo || exit /b 1
dotnet msbuild "%~dp0tests\Winwright.Tests\Winwright.Tests.csproj" -t:TestAndRoll -p:Configuration=%CONFIG% -p:VSTestNoBuild=true -nologo
exit /b %ERRORLEVEL%
