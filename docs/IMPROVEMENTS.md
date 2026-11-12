# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

### §WW623 A fixture fail-fast after the Open dialog

Seen 12/11/2026 in one full guest run while shipping WW619 (run vm-20261112-111345-387).
`DialogTests.A_file_chosen_in_the_open_dialog_is_the_one_the_application_opens` went
red: the `choose file` step typed the path into the common Open dialog and invoked it
(step 2 Ok), the dialog closed (step 3 Ok), and then the fixture exited with
-1073740791, 0xC0000409, a fail-fast, so `Text#dialogSaid` was never there to read (step
4 Failed). The case reported the exit code on its own line, so the verdict was honest;
what is unknown is why the fixture died.

Not reproduced since: DialogTests alone passed three rounds in the guest, the WW619
packaged tests followed directly by DialogTests passed two rounds (the suspicion was a
package with a file type association registered and removed just before the dialog read
associations), and the next full guest run passed 2615 of 2615.

What to do: keep the crash when it happens. The fixture is a WPF process and 0xC0000409
leaves a Windows Error Reporting record; have the runner's blame gather
(tools/blame-gather.ps1) also bring back the newest WER report or local dump for
Winwright.Fixture.exe written during the run, so the next occurrence arrives with a
faulting module and stack rather than an exit code. Then decide from that whether the
fault is the fixture's own handler, the shell's dialog, or the guest.

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system

## Block N — The name — winwright becomes paneproof
