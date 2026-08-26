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
setlocal

set "ROOT=%~dp0.."
set "TFM=net10.0-windows"
set "DLL=%ROOT%\tools\Winwright.Mcp\bin\Release\%TFM%\Winwright.Mcp.dll"

if not exist "%DLL%" set "DLL=%ROOT%\tools\Winwright.Mcp\bin\Debug\%TFM%\Winwright.Mcp.dll"

if not exist "%DLL%" (
  echo winwright: the MCP server is not built, so this session has no winwright tools.>&2
  echo winwright: build it once - dotnet build -c Release "%ROOT%\Winwright.slnx">&2
  exit /b 1
)

dotnet exec "%DLL%"
exit /b %ERRORLEVEL%
