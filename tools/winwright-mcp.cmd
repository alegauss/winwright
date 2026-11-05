@echo off
rem WW221. The launcher between the harness and the MCP server, for the reason the guard has one.
rem
rem A stdio server has no way to say "I am not built": it either speaks the protocol on the stdin it
rem was handed or it exits, and an exit is how a server reports a crash. So a fresh clone showed the
rem server as failed, with a .NET assembly error as the whole explanation, in exactly the projects
rem where the two install commands had done everything they promised.
rem
rem The refusal goes to stderr, which the harness shows beside the failed server. That is the one
rem place a reader is already looking when the tools are missing.
rem
rem WW587. And where nothing was built, the release's own binaries are fetched rather than asked
rem for: the server and the guard, published for the version plugin.json names, into a folder
rem outside the plugin's clone that an update replaces. The guard's launcher reads the same folder
rem and never downloads, so this is the one place a fetch happens.
setlocal

set "ROOT=%~dp0.."
set "TFM=net10.0-windows"
set "DLL=%ROOT%\tools\Winwright.Mcp\bin\Release\%TFM%\Winwright.Mcp.dll"

rem A local build wins, Release first, so working on winwright never runs a downloaded copy.
if not exist "%DLL%" set "DLL=%ROOT%\tools\Winwright.Mcp\bin\Debug\%TFM%\Winwright.Mcp.dll"
if exist "%DLL%" goto runtime

rem The version is the one line of plugin.json that names it; the folder is per version, so an
rem update finds nothing and fetches its own.
set "VERSION="
if exist "%ROOT%\.claude-plugin\plugin.json" (
  for /f "tokens=2 delims=:," %%v in ('findstr /c:"\"version\"" "%ROOT%\.claude-plugin\plugin.json"') do set "VERSION=%%~v"
)
if defined VERSION set "VERSION=%VERSION: =%"
if defined VERSION set "VERSION=%VERSION:"=%"

if not defined VERSION (
  echo winwright: the MCP server is not built, so this session has no winwright tools.>&2
  echo winwright: no plugin.json beside this launcher names a version to fetch, so build it once - dotnet build -c Release "%ROOT%\Winwright.slnx">&2
  exit /b 1
)

set "FETCHED=%LOCALAPPDATA%\winwright\plugin\%VERSION%"
set "DLL=%FETCHED%\mcp\Winwright.Mcp.dll"
if exist "%DLL%" goto runtime

rem Everything the fetch says is moved to stderr, because stdout from here on is the protocol.
powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0winwright-fetch.ps1" -Version "%VERSION%" -Into "%FETCHED%" 1>&2

if not exist "%DLL%" (
  echo winwright: the MCP server is not built, so this session has no winwright tools.>&2
  echo winwright: the release binaries for %VERSION% could not be fetched into %FETCHED%.>&2
  echo winwright: build it instead - dotnet build -c Release "%ROOT%\Winwright.slnx">&2
  exit /b 1
)

:runtime
rem The binaries are framework-dependent and the engine is WPF, so the base runtime is not enough:
rem without the Desktop Runtime the server would exit with .NET's own message about a framework.
dotnet --list-runtimes 2>nul | findstr /b /c:"Microsoft.WindowsDesktop.App 10." >nul
if errorlevel 1 (
  echo winwright: the MCP server needs the Windows Desktop Runtime 10, and this machine has none, so this session has no winwright tools.>&2
  echo winwright: install it from https://dotnet.microsoft.com/download/dotnet/10.0 - or the .NET 10 SDK, which carries it.>&2
  exit /b 1
)

dotnet exec "%DLL%"
exit /b %ERRORLEVEL%
