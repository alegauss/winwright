# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

### §WW561 choose file name box not held

Found by photo-boost (PB57, 2026-10-20), winwright 1.1.4, a Save as case on a WPF
Microsoft.Win32.SaveFileDialog (FileName "rotated-exif6.png", filter "Png image|*.png"):

    { "locator": "Button#SaveButton", "act": "invoke" },
    { "locator": "Window[class=\"#32770\"]", "act": "choose file", "with": "{files}/saved.png" },
    { "locator": "Window[class=\"#32770\"]", "act": "read", "absent": true },
    { "locator": "Text#ExportStatus", "act": "read", "reads": "name", "expect": "Saved saved.png" }

Host (Windows 11, pt-BR): all four pass. On the VMware guest (run-tests-vm.cmd -Tree
photo-boost), two runs of the same tree gave two different failures:

1. choose file and the absent read passed, and the app reported "Saved rotated-exif6.png" in
   48 of 48 polls. The dialog confirmed with its own default name, and the act reported done.
2. choose file broke with AmbiguousLocatorException: Window[class="#32770"] matched 2
   elements. The README's own example uses this locator for the dialog.

What winwright should offer: choose file reads the name box back before confirming, and
refuses (naming what it held) when it does not hold the path given. It should also find
out why the guest's dialog drops the value: a ValuePattern write that the dialog's
default-name restore or autocomplete overwrites, where typing or WM_SETTEXT may hold.
And it needs a locator for "the common dialog this window opened" that cannot match two
#32770s, or a README example that says how to tell them apart. photo-boost removed its
Save case and waits on this line.

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing

## Block M — The site on the shared design system
