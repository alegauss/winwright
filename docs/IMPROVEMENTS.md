# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

### §WW538 Clicking with another button

Found by quickshell QS310, where a middle click on a tab closes it, as it does in every
browser and terminal on Windows. The case cannot be written: the `click` verb takes only
a reason as its argument and always presses the left button, although `Pointer.Click`
already takes a `MouseButton` and `Win32` already sends the middle button's down and up.
quickshell proves the gesture with a unit test that raises WPF's routed events on the
tab item instead, which is the substitute the evidence rule says not to need.

The engine half exists, so this is the grammar's: a step names the button, say
`"button": "middle"` (or `right`) beside `"act": "click"`, defaulting to left so no case
changes, and the step's rendered line and its refusal name the button. A right click
opens a context menu, which QS311 in quickshell will want next for a tab's Close others.

Done when a case middle-clicks an element of the fixture app and reads what the click
did, and a case naming an unknown button is refused with the buttons it takes.

## Block E — Capture — the picture that proves what it photographed

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing
