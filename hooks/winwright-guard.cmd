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
set "DLL=%ROOT%\tools\Paneproof.Guard\bin\Release\%TFM%\Paneproof.Guard.dll"

rem Release first, then Debug: an adopter builds Release and a developer here builds Debug, and
rem preferring Release means a stale Debug build never quietly answers for a fresh Release one.
if not exist "%DLL%" set "DLL=%ROOT%\tools\Paneproof.Guard\bin\Debug\%TFM%\Paneproof.Guard.dll"
if exist "%DLL%" goto run

rem WW587. Then the release's own copy, in the folder tools\winwright-mcp.cmd fetches into for the
rem version plugin.json names. Read and never fetched: this runs on every write under a ten-second
rem timeout, and the server's launcher fetches once per session for both of them.
set "VERSION="
if exist "%ROOT%\.claude-plugin\plugin.json" (
  for /f "tokens=2 delims=:," %%v in ('findstr /c:"\"version\"" "%ROOT%\.claude-plugin\plugin.json"') do set "VERSION=%%~v"
)
if defined VERSION set "VERSION=%VERSION: =%"
if defined VERSION set "VERSION=%VERSION:"=%"
if defined VERSION set "DLL=%LOCALAPPDATA%\winwright\plugin\%VERSION%\guard\Paneproof.Guard.dll"

if not exist "%DLL%" (
  echo winwright: the guard is not built, so nothing in this session is refusing a hand-written harness.>&2
  echo winwright: the MCP server fetches it with its own binaries when a session starts, or build it once - dotnet build -c Release "%ROOT%\Paneproof.slnx">&2
  exit /b 1
)

:run

dotnet exec "%DLL%"
exit /b %ERRORLEVEL%
