@echo off
rem WW157: the same suite as run-tests.cmd, on a desk that is not the operator's.
rem
rem A full run creates real windows, takes the foreground and synthesises input for two and a half
rem minutes, which is two and a half minutes the machine belongs to the suite. This hands that to a
rem VMware guest so the host stays usable, and is the one command a person types to get it.
rem
rem It arranges nothing. Credentials come from a file outside this tree - tools\run-tests-vm.ps1
rem says which, and says why the out-of-tree spelling is the default.
rem WW293. Everything is forwarded, and that is the whole of the fix. This read
rem `-Configuration %CONFIG% %2 %3 %4` - three tokens after the configuration - and the runner has
rem since grown `-Tree`, `-Name`, `-Run` and `-ResultsIn` for the adopting project whose migrated
rem cases had nowhere to run but the desk somebody is working at. Four flags with four values is
rem eight tokens, so the parameters that exist for adopters could not be given to the command
rem adopters are told to run: the invocation truncated in silence and PowerShell then refused with
rem `Falta um argumento para o parametro 'Name'`, naming the parameter that lost its value rather
rem than the wrapper that dropped it.
rem
rem `%*` and not a larger number, because raising three to eight is the same defect with more room in
rem it and goes stale the next time the script grows a flag. The configuration goes with it: it is
rem the script's first parameter, so `run-tests-vm.cmd Release` still binds positionally, and a bare
rem `run-tests-vm.cmd` gets the default the script itself declares rather than one spelled twice.
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\run-tests-vm.ps1" %*
exit /b %ERRORLEVEL%
