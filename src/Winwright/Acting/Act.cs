using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Verdicts;
using Winwright.Windowing;

namespace Winwright.Acting;

/// <summary>
/// The acts that ask a control directly.
/// <para>
/// pportal's harness states the rule and the reason in its own header: a synthesised mouse click
/// lands on whatever is drawn at a point, so it needs the window in the foreground, and Windows
/// refuses the foreground to a process that does not already own it — which means a run started
/// from an editor or an agent drives somebody else's window. A pattern asks the control: no
/// pointer, no foreground, nothing on top to be confused with. That is what lets these run
/// unattended, which is the whole point of them.
/// </para>
/// <para>
/// Every one of them re-resolves its subject, requires actionability before touching anything, and
/// reads the control's values either side of the act, so what a report says moved is two values
/// and not one live view asked twice.
/// </para>
/// </summary>
public static class Act
{
    /// <summary>Press it, the way its own accessibility peer would.</summary>
    public static ActResult Invoke(Subject subject) => Through(
        subject, "invoke", "Invoke",
        element => ((InvokePattern)element.GetCurrentPattern(InvokePattern.Pattern)).Invoke());

    /// <summary>Flip it — on to off, off to on, and whatever a third state does next.</summary>
    public static ActResult Toggle(Subject subject) => Through(
        subject, "toggle", "Toggle",
        element => ((TogglePattern)element.GetCurrentPattern(TogglePattern.Pattern)).Toggle());

    /// <summary>Put text into it through the control's own value, rather than through the keyboard.</summary>
    public static ActResult SetValue(Subject subject, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Through(
            subject, "set value", "Value",
            element => ((ValuePattern)element.GetCurrentPattern(ValuePattern.Pattern)).SetValue(value));
    }

    /// <summary>Move it to a number, refusing one the control says it does not accept.</summary>
    public static ActResult SetRange(Subject subject, double value) => Through(
        subject, "set range", "RangeValue",
        element => ((RangeValuePattern)element.GetCurrentPattern(RangeValuePattern.Pattern)).SetValue(value));

    /// <summary>Select it, replacing whatever the container had selected.</summary>
    public static ActResult Select(Subject subject) => Through(
        subject, "select", "SelectionItem",
        element => ((SelectionItemPattern)element.GetCurrentPattern(SelectionItemPattern.Pattern)).Select());

    /// <summary>
    /// Add it to what its container has selected, leaving the rest selected. WW540.
    /// <para>
    /// Found by quickshell (QS314), whose sidebar takes several rows at once: <see cref="Select"/>
    /// replaces the selection, so a case could choose one row and never two. This is what UI
    /// Automation offers a multiple-selection container, and like every pattern act it needs no
    /// foreground. Refused where the container takes one row at a time, because there an add is a
    /// replace that reads as an add.
    /// </para>
    /// </summary>
    public static ActResult AddToSelection(Subject subject) => Through(
        subject, "add to selection", "SelectionItem",
        element =>
        {
            Several(subject, element);
            ((SelectionItemPattern)element.GetCurrentPattern(SelectionItemPattern.Pattern)).AddToSelection();
        });

    /// <summary>Take it out of what its container has selected, leaving the rest. WW540.</summary>
    public static ActResult RemoveFromSelection(Subject subject) => Through(
        subject, "remove from selection", "SelectionItem",
        element =>
        {
            Several(subject, element);
            ((SelectionItemPattern)element.GetCurrentPattern(SelectionItemPattern.Pattern)).RemoveFromSelection();
        });

    /// <summary>
    /// Refuse an element whose container takes one selection at a time, naming the container. WW540.
    /// </summary>
    /// <exception cref="NotActionableException">Where the container is not a multiple selection.</exception>
    private static void Several(Subject subject, AutomationElement element)
    {
        var container = ((SelectionItemPattern)element.GetCurrentPattern(SelectionItemPattern.Pattern)).Current.SelectionContainer;
        if (container is null)
        {
            throw new NotActionableException(
                subject.Locator.Text,
                Actionable.PatternMissing,
                "it is in no selection container, so there is no selection to add it to or take it out of");
        }

        var several = container.TryGetCurrentPattern(SelectionPattern.Pattern, out var pattern)
            && ((SelectionPattern)pattern).Current.CanSelectMultiple;
        if (!several)
        {
            var named = container.Current.AutomationId is { Length: > 0 } id ? $"#{id}" : $"'{container.Current.Name}'";
            throw new NotActionableException(
                subject.Locator.Text,
                Actionable.PatternMissing,
                $"its container {named} takes one selection at a time, so adding to it would replace what it "
                    + "held; 'select' is the act for a list like that");
        }
    }

    /// <summary>Open it, which is what puts its contents into the tree at all.</summary>
    public static ActResult Expand(Subject subject) => Through(
        subject, "expand", "ExpandCollapse",
        element => ((ExpandCollapsePattern)element.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Expand());

    /// <summary>Shut it again.</summary>
    public static ActResult Collapse(Subject subject) => Through(
        subject, "collapse", "ExpandCollapse",
        element => ((ExpandCollapsePattern)element.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Collapse());

    /// <summary>
    /// Size the top-level window the subject is in, in device-independent pixels, and read back the
    /// size it ended at. WW570.
    /// <para>
    /// Found by PhotoBoost (PB63): a toolbar whose groups move into its overflow at a narrow width had
    /// no case, because nothing set a window's size. The subject is any element of the window, the way
    /// a capture's is, since the window itself is the root a locator searches under and never one of
    /// its matches. A maximised window is restored first: it would otherwise keep its size and read as
    /// one that refused.
    /// </para>
    /// <para>
    /// <c>SetWindowPos</c> with no move, no restack and no activation, so it needs no foreground, like
    /// every act here. The size asked for is scaled by the display's own DPI and not the window's: a
    /// window unaware of scaling answers 96 for itself and is still drawn scaled. Whether the window
    /// took all of it is the read-back's to say: a minimum size refuses part of a request, and the
    /// result then reads the size asked against the size it ended at rather than refusing the step.
    /// </para>
    /// </summary>
    /// <param name="subject">An element of the window to size.</param>
    /// <param name="width">The outer width, in device-independent pixels.</param>
    /// <param name="height">The outer height, in device-independent pixels.</param>
    /// <exception cref="NotActionableException">Where the subject is in no window, or the window refused to be sized.</exception>
    public static ActResult Resize(Subject subject, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        var admitted = Admitted.To(subject);
        var window = admitted.Window;
        if (window == 0)
        {
            throw new NotActionableException(
                subject.Locator.Text, Actionable.NotInTree, "it is in no top-level window, so there is no window to size");
        }

        var scale = Win32.ScaleOf(window);
        var before = SizeOf(window, scale);

        if (Win32.IsZoomed(window))
            Win32.ShowWindow(window, Win32.ShowRestore);

        if (!Win32.SetWindowPos(
                window, 0, 0, 0, (int)Math.Round(width * scale), (int)Math.Round(height * scale), Win32.SizeOnly))
        {
            throw new NotActionableException(
                subject.Locator.Text,
                Actionable.Disabled,
                $"its window refused to be sized (error {System.Runtime.InteropServices.Marshal.GetLastPInvokeError()})");
        }

        return new ActResult(
            "resize",
            subject.Locator,
            "SetWindowPos",
            admitted.Facts,
            PatternValues.Sized(before),
            PatternValues.Sized(SizeOf(window, scale)),
            admitted.Reading.Resolution.WaitedMs,
            admitted.Reading.Resolution.Polls);
    }

    /// <summary>A window's outer size in device-independent pixels, as <c>640x720</c>. WW570.</summary>
    private static string SizeOf(nint window, double scale)
    {
        Win32.GetWindowRect(window, out var at);
        var wide = (int)Math.Round((at.Right - at.Left) / scale);
        var high = (int)Math.Round((at.Bottom - at.Top) / scale);
        return string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{wide}x{high}");
    }

    /// <summary>
    /// The boxes in the common Open and Save dialogs that take the file's name, by automation id.
    /// WW551: the ids are the same in every language, where the names are the translated label beside
    /// them — and the two dialogs differ, measured on the guest: Open holds <c>ComboBox#1148</c>, the
    /// template's own control id, and Save holds <c>ComboBox#FileNameControlHost</c> one pane down.
    /// The name is typed into the edit inside whichever is there.
    /// </summary>
    public static IReadOnlyList<string> FileNameBoxes { get; } = ["1148", "FileNameControlHost"];

    /// <summary>The common dialog's confirming button, IDOK, which is Open or Save depending on the dialog.</summary>
    public const string ConfirmButton = "1";

    /// <summary>How long one character sent to the name box may take to be answered, before the act gives up on the dialog.</summary>
    public const uint NameCharMs = 5000;

    /// <summary>
    /// Choose a file in the system's common Open or Save dialog: type the path into its name box and
    /// confirm through the button's own pattern. WW551.
    /// <para>
    /// Found by PhotoBoost (PB23, PB25): opening and exporting go through this dialog, and a case
    /// could not reach it, so the application grew flags only its tests used. Measured on the guest:
    /// the dialog the application owns is a child of its window in the tree, <c>#32770</c> by class,
    /// with a name box among <see cref="FileNameBoxes"/> and the confirm at <c>Button#1</c> whatever
    /// language the shell speaks — so the step's locator is the dialog, and this finds the two
    /// controls under it by those ids.
    /// </para>
    /// <para>
    /// No pointer and no synthesised key, so it needs no foreground, like every pattern act here.
    /// Whether the dialog took the path is not this act's claim: a read of the dialog after it, saying
    /// <c>"absent": true</c>, claims the dialog closed, and one that stays open — a file that is not
    /// there, a save that asks before replacing — is then a red. The type is chosen before, with
    /// <c>pick</c> on <c>ComboBox#1136</c> in Open and <c>ComboBox#FileTypeControlHost</c> in Save.
    /// </para>
    /// <para>
    /// WW561, found by PhotoBoost (PB57), is why the name is typed and not set. A Save dialog given a
    /// default name keeps that name as its own until the box is edited, and a value set through
    /// <c>ValuePattern</c> or <c>WM_SETTEXT</c> is not an edit to it: measured on the guest, the box
    /// read the path back for two seconds and the dialog still confirmed with the default — closing
    /// green over a file the case never named, or, where the default already existed, asking to
    /// replace it in a second <c>#32770</c> that made the dialog's own locator ambiguous. Each
    /// character sent as <c>WM_CHAR</c> to the box's own window is an edit, and the dialog saved the
    /// typed name every time. The box is then read back, and the act refuses without confirming where
    /// it does not hold the path, naming what it held.
    /// </para>
    /// </summary>
    /// <param name="subject">The dialog.</param>
    /// <param name="path">The file, as a full path.</param>
    /// <exception cref="NotActionableException">
    /// Where the subject is no common dialog — it holds no name box or no confirm — or where its name
    /// box would not take the path.
    /// </exception>
    public static ActResult ChooseFile(Subject subject, string path)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var admitted = Admitted.To(subject, "Window");
        var before = admitted.Reading;

        admitted.Do(dialog =>
        {
            var box = FileNameBoxes
                .Select(id => dialog.FindFirst(TreeScope.Descendants, Of(id, ControlType.ComboBox)))
                .FirstOrDefault(found => found is not null);
            var name = box?.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit))
                ?? throw Refused(subject, $"a file-name box ({string.Join(" or ", FileNameBoxes.Select(id => $"ComboBox#{id}"))} with an edit in it)");
            var confirm = dialog.FindFirst(TreeScope.Children, Of(ConfirmButton, ControlType.Button))
                ?? throw Refused(subject, "a confirming button (Button#1)");

            if (!Typed(name.Current.NativeWindowHandle, path))
                throw Refused(subject, "a name box that answers what is typed into it in time");

            // Sent and answered before this read, so one look is the whole of it and not a race.
            var held = ((ValuePattern)name.GetCurrentPattern(ValuePattern.Pattern)).Current.Value ?? string.Empty;
            if (held != path)
            {
                throw new NotActionableException(
                    subject.Locator.Text,
                    Actionable.Disabled,
                    $"its name box held '{held}' after '{path}' was typed into it, so confirming would choose a "
                        + "file the case did not name; nothing was confirmed");
            }

            ((InvokePattern)confirm.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
        });

        // One look and not a resolve: the dialog is meant to be gone, and a resolve would spend its
        // whole deadline confirming that. Whether it went is the step's claim, waited for there.
        var after = subject.ReadOnce();
        return new ActResult(
            "choose file",
            subject.Locator,
            "WM_CHAR, Invoke",
            admitted.Facts,
            before.Values,
            after.Values,
            before.Resolution.WaitedMs,
            before.Resolution.Polls);
    }

    /// <summary>
    /// Type <paramref name="text"/> over everything in an edit, by its own window: all of it selected,
    /// then one <c>WM_CHAR</c> per code unit, each sent and answered within <see cref="NameCharMs"/>.
    /// False where the edit has no window or a send was not answered.
    /// </summary>
    private static bool Typed(nint edit, string text)
    {
        if (edit == 0)
            return false;

        if (Win32.SendMessageTimeoutValue(edit, Win32.EmSetSel, 0, -1, Win32.AbortIfHung, NameCharMs, out _) == 0)
            return false;

        foreach (var unit in text)
        {
            if (Win32.SendMessageTimeoutValue(edit, Win32.WmChar, unit, 0, Win32.AbortIfHung, NameCharMs, out _) == 0)
                return false;
        }

        return true;
    }

    /// <summary>A control of the common dialog by its id and its type.</summary>
    private static AndCondition Of(string id, ControlType type) => new(
        new PropertyCondition(AutomationElement.AutomationIdProperty, id),
        new PropertyCondition(AutomationElement.ControlTypeProperty, type));

    /// <summary>The refusal for a window that is not the common dialog, naming what was looked for.</summary>
    private static NotActionableException Refused(Subject subject, string wanted) => new(
        subject.Locator.Text,
        Actionable.PatternMissing,
        $"it holds no {wanted}, so it is not the system's common Open or Save dialog");

    /// <summary>
    /// The shape every act has: resolve, judge, act, read back. The judgement comes before the
    /// act and not after it, so nothing here ever touches a control that could not take it.
    /// </summary>
    /// <exception cref="NotActionableException">Where the element cannot take the act.</exception>
    private static ActResult Through(Subject subject, string verb, string pattern, Action<AutomationElement> doing)
    {
        ArgumentNullException.ThrowIfNull(subject);

        // The element is reached only through the admission, which judged it on the way out.
        var admitted = Admitted.To(subject, pattern);
        var before = admitted.Reading;

        admitted.Do(doing);

        // Read again through the subject rather than through the element just used: the act may
        // have replaced what it addressed, and a handle held across it is a handle to what was.
        var after = subject.Read();
        return new ActResult(
            verb,
            subject.Locator,
            pattern,
            admitted.Facts,
            before.Values,
            after.Values,
            before.Resolution.WaitedMs,
            before.Resolution.Polls);
    }
}

