@echo off
rem WW493: capture one adoption's build from a run, rather than remembering it.
rem
rem The documentation area shows what this prints. It builds a throwaway repository laid out the way
rem an adopting one is - the application's project at the root, the driving project underneath - and
rem builds it twice: once with nothing stopping the application's globs reaching down, which is how
rem an adoption fails for most people the first time, and once with the one line that stops them.
rem
rem It is a command a person types rather than a build step, and that is not a shortcut. The site
rem builds on a runner with no .NET, so the capture is committed and this is what regenerates it -
rem `site\scripts\adoption.test.mjs` is what refuses a capture that has stopped describing this tree.
rem
rem Seconds, no desk, no packed feed. The halves of the adoption that need either are what WW493
rem still owes.
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\capture-adoption.ps1" %*
exit /b %ERRORLEVEL%
