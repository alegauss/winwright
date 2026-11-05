@echo off
rem WW271: pack into the bootstrap feed, and evict what NuGet already extracted of that version.
rem
rem The two are one command because doing the first without the second is invisible. NuGet extracts a
rem package once per version and reads the extraction from then on, and the version in `packages/`
rem never changes while WW230's publish is outstanding - so a repack over the same number leaves the
rem adopter restoring exactly what it had, and nothing anywhere says so.
rem
rem Measured three times in one session, each after shipping a step field: the engine gained the
rem field, the package stayed where it was, and every case file in claude-tray refused to load with
rem `there is no such reading` - a sentence that is true of the engine that answered and sends the
rem reader to the case file to correct a field that was already right.
rem
rem This whole file goes when WW230's deletion lands. It exists for the two-clone bootstrap and for
rem nothing else, which is why it is named for what it is rather than for what it does.
setlocal

set CONFIG=%1
if "%CONFIG%"=="" set CONFIG=Release

dotnet pack "%~dp0Winwright.slnx" --configuration %CONFIG% --output "%~dp0packages" --nologo || exit /b 1

rem The declared version, read rather than typed: a copy of it here is the fifth place it lives, and
rem the copy list was already wrong once (WW239). Token 3 because the line splits as indent, tag
rem name, value - which the first version of this file got wrong, evicted a folder called `Version`
rem that does not exist, and printed success. Exactly the half-done sequence it was written to stop.
for /f "tokens=3 delims=<>" %%v in ('findstr /i "<Version>" "%~dp0Directory.Build.props"') do set DECLARED=%%v
if "%DECLARED%"=="" (
    echo pack-local: no ^<Version^> in Directory.Build.props, so nothing could be evicted 1>&2
    exit /b 1
)

rem What proves the parse: the pack above just wrote this file, so a name that does not match it is a
rem version this script read wrongly - and evicting the wrong folder is indistinguishable from
rem evicting nothing. Checked rather than assumed, because the failure is silent either way.
if not exist "%~dp0packages\Winwright.%DECLARED%.nupkg" (
    echo pack-local: read the version as '%DECLARED%' and packed no Winwright.%DECLARED%.nupkg, 1>&2
    echo             so nothing would have been evicted and the adopter would go on reading the old one 1>&2
    exit /b 1
)

rem Both packages, because an adopter driving a cooperating application restores the in-app half too.
for %%p in (winwright winwright.inapp) do (
    if exist "%USERPROFILE%\.nuget\packages\%%p\%DECLARED%" (
        rmdir /s /q "%USERPROFILE%\.nuget\packages\%%p\%DECLARED%"
        echo pack-local: evicted %%p %DECLARED% from the global packages folder
    )
)

echo pack-local: packed %DECLARED% into packages\ - the next restore in an adopting clone will read it
exit /b 0
