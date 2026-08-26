@echo off
rem WW221. The launcher between the harness and the guard.
rem
rem The hook used to be `dotnet exec` on a path under bin\Release, which a fresh clone does not have
rem until somebody builds it. What an adopter got from the two install commands was a session where
rem every write ran a .NET error about a missing assembly - and a guard that is not there refuses
rem nothing, so the one surface whose whole job is to be in the way was the one that went quiet.
rem
rem Loud and not blocking. Exiting 2 would deny every write in the repository because a build is
rem missing, which is the guard standing in front of everything instead of in front of a harness
rem script. Exit 1 says so where somebody reads it and lets the write through.
rem
rem A .cmd because it has to run before anything is built, and cmd.exe is the one interpreter this
rem project already assumes: Windows is a non-goal to leave, and the two runners at the root are
rem .cmd already.
setlocal

set "ROOT=%~dp0.."
set "TFM=net10.0-windows"
set "DLL=%ROOT%\tools\Winwright.Guard\bin\Release\%TFM%\Winwright.Guard.dll"

rem Release first, then Debug: an adopter builds Release and a developer here builds Debug, and
rem preferring Release means a stale Debug build never quietly answers for a fresh Release one.
if not exist "%DLL%" set "DLL=%ROOT%\tools\Winwright.Guard\bin\Debug\%TFM%\Winwright.Guard.dll"

if not exist "%DLL%" (
  echo winwright: the guard is not built, so nothing in this session is refusing a hand-written harness.>&2
  echo winwright: build it once - dotnet build -c Release "%ROOT%\Winwright.slnx">&2
  exit /b 1
)

dotnet exec "%DLL%"
exit /b %ERRORLEVEL%
