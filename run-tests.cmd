@echo off
rem WW117, WW138: build and run the suite, roll call included - the one command a person types
rem without thinking about either.
rem
rem A test host that dies mid-run takes its remaining tests with it and the runner still prints a
rem pass: measured here at 352 of 374, a green covering twenty-two tests that never ran.
rem
rem WW373: a single case is bounded now, so a deadlock ends the run rather than sitting in it. The
rem bound is declared in the test project rather than typed here - it is a property of this suite and
rem not of the command that starts it, and the guest runner types this same line. Ten minutes against
rem a slowest honest case of 158 seconds, and what comes back is a red naming the case with a
rem sequence file beside it instead of a run somebody has to go and notice.
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
