# Improvements

## Block A — The verdict (a run is data, and "not observed" is an answer)

## Block B — Attach, launch, and leave nothing behind

## Block C — Locate — the locator grammar and the tree an agent reads

## Block D — Act — patterns before pointers

### §WW550 Dropping a file from Explorer onto the application

Project: PhotoBoost (D:\Git\japode\photo-boost, WPF on .NET 10), task PB23, an
open-image screen whose drop zone accepts image files dragged from Explorer. The case it
wants: a fixture stages photo.png into {files}, a step drops {files}\photo.png onto
Pane#DropZone, and the next step reads Text#ImageSize. WW530 covers the opposite
direction (app onto Explorer) and WW520 element onto element in one window; nothing
starts the drag outside the application. The workaround planned is an --open <path> flag
(PB39) that reaches the same state, so the drop handler itself is never exercised. What
winwright could offer: a verb such as drop {files}\x onto <locator>, performed either by
a real drag from an Explorer window the harness opens and closes, or by an OLE
DoDragDrop with a CF_HDROP data object from a helper process, reported as Degraded where
the target refuses it.

### §WW551 The common Open and Save dialogs

Project: PhotoBoost, tasks PB23 (open) and PB25 (export as PNG, JPG or WebP). The cases
want to invoke Button#OpenButton, pick {files}\photo.png in the Open dialog and confirm;
then invoke Button#ExportButton, type {files}\out.png, choose the WebP filter and
confirm, and check the file step. The dialog is a separate top-level window owned by the
app, with a shell-hosted tree, and neither the README nor the verbs say how a case
reaches it, whether the file-name box and the type filter are addressable, or how a run
proves it closed. The workaround planned is --open and --export flags (PB39), which
leave the dialog code unproven. What winwright could offer: a locator root for the owned
dialog and a verb that sets the path, picks the filter and confirms, with a reading of
what was chosen.

## Block E — Capture — the picture that proves what it photographed

### §WW552 Capturing a Mica window with Direct3D content

Project: PhotoBoost, tasks PB27 and PB40: a WPF window with ThemeMode Fluent (so Mica)
whose image area is Direct3D 11 through Vortice, either a child HWND with a DXGI flip
swapchain (HwndHost) or a D3DImage. The screen copy is refused because Glass.Of reads
mica, which is right. The off-screen render from Winwright.InApp draws the WPF visual
tree with RenderTargetBitmap, so a child HWND comes out empty and a D3DImage may come
out blank or stale, and the receipt cannot tell. The case wanted a capture of the
comparator after a zoom, to prove it drew something and that two renders match when
nothing changed. The claim settled for is automation readings (zoom, divider position,
image size) and the exported file. What winwright could offer: the render route
compositing child HWND swapchain content (DXGI or Windows.Graphics.Capture of the child,
which carries no backdrop), or a refusal that names the GPU region it could not draw
instead of a blank picture.

## Block F — Assert — the expectation is derived, never typed

## Block G — The scenario — a case is a data file

## Block H — The Claude Code surface — plugin, tools, skill, hook

## Block I — The in-app half — the app cooperates with the harness

## Block J — Adoption — the proof is the deletion

## Block K — The proving ground — a fixture app built to be hard to test

## Block L — The documentation area — written for a reader who has installed nothing
