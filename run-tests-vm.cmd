@echo off
rem WW157: the same suite as run-tests.cmd, on a desk that is not the operator's.
rem
rem A full run creates real windows, takes the foreground and synthesises input for two and a half
rem minutes, which is two and a half minutes the machine belongs to the suite. This hands that to a
rem VMware guest so the host stays usable, and is the one command a person types to get it.
rem
rem It arranges nothing. Credentials come from a file outside this tree - tools\run-tests-vm.ps1
rem says which, and says why the out-of-tree spelling is the default.
setlocal

set CONFIG=%1
if "%CONFIG%"=="" set CONFIG=Debug

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\run-tests-vm.ps1" -Configuration %CONFIG% %2 %3 %4
exit /b %ERRORLEVEL%
