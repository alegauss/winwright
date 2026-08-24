using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Tracing;
using Winwright.Verdicts;
using Winwright.Windowing;

namespace Winwright.Acting;

/// <summary>One icon in the notification area, and whether it was hiding in the overflow.</summary>
/// <param name="Facts">What UI Automation says about it, rectangle included.</param>
/// <param name="Hidden">Whether it was found in the overflow flyout rather than on the taskbar.</param>
public sealed record TrayIcon(ElementFacts Facts, bool Hidden)
{
    /// <summary>
    /// Where it is on screen. This is the only address it has: asking a taskbar button for a
    /// clickable point throws on this shell, every one of them, measured.
    /// </summary>
    public WindowBounds Rectangle => Facts.Bounds;

    /// <summary>What the shell calls it — a tooltip, so often several lines of it.</summary>
    public string Name => Facts.Name;

    /// <summary>The one line a report names it by.</summary>
    public override string ToString()
    {
        var first = Name.Split('\n')[0].Trim();
        return $"{(Hidden ? "hidden " : "")}tray icon '{first}' [{Rectangle}]";
    }
}

/// <summary>What asking a tray icon for its menu did.</summary>
public sealed record TrayMenu
{
    internal TrayMenu(TrayIcon icon, bool opened, string? highlighted, string? because)
    {
        Icon = icon;
        Opened = opened;
        Highlighted = highlighted;
        Because = because;
    }

    /// <summary>The icon it was asked of.</summary>
    public TrayIcon Icon { get; }

    /// <summary>Whether a menu actually came up.</summary>
    public bool Opened { get; }

    /// <summary>What the menu is highlighting, where one came up.</summary>
    public string? Highlighted { get; }

    /// <summary>Why nothing came up, where nothing did.</summary>
    public string? Because { get; }

    /// <summary>What happened, said either way.</summary>
    public override string ToString() => Opened
        ? $"{Icon} opened its menu on \"{Highlighted}\"."
        : $"{Icon} showed no menu: {Because}.";

    /// <summary>The step a trace records.</summary>
    public TraceStep AsTraceStep() => new()
    {
        Verb = "open the tray menu",
        Locator = Icon.Name.Split('\n')[0].Trim(),
        Resolved = Icon.ToString(),
        Pattern = "focus and the application key",
        ReadBack = Highlighted,
        Verdict = Opened ? StepVerdict.Ok : StepVerdict.Failed,
        Detail = Opened ? null : ToString(),
    };
}

/// <summary>
/// What asking the overflow flyout to open or shut turned out to do.
/// <para>
/// WW165. These two verbs answered a bare bool, so a run that could not work the flyout said only
/// <c>false</c> — which of the two calls failed, what the shell was doing, whether the chevron was
/// there and would not take the act or was never found, none of it survived the return type. Its
/// sibling one method over already answers a reading with a reason and a step on it.
/// </para>
/// </summary>
public sealed record OverflowState
{
    internal OverflowState(string what, bool held, bool already, string? because)
    {
        What = what;
        Held = held;
        Already = already;
        Because = because;
    }

    /// <summary>What was asked of it — opened, or shut.</summary>
    public string What { get; }

    /// <summary>Whether the flyout ended up the way it was asked to be.</summary>
    public bool Held { get; }

    /// <summary>
    /// Whether it was already that way, so nothing was pressed. A real answer and not a detail: a
    /// run that opened the flyout and one that found it open leave the taskbar differently.
    /// </summary>
    public bool Already { get; }

    /// <summary>Why it is not, where it is not. Null where it is.</summary>
    public string? Because { get; }

    /// <summary>What happened, said either way.</summary>
    public override string ToString()
    {
        if (!Held)
            return $"the overflow was not {What}: {Because}.";

        return Already
            ? $"the overflow was already {What}, so nothing was pressed."
            : $"the overflow was {What}.";
    }

    /// <summary>
    /// The result a verdict counts. A shell that would not work the flyout is a fact about the
    /// desk and never a defect in the code under test, so it is a <em>hole</em> — this block's
    /// neighbour criterion says nothing about the desk is reported as a defect in the code.
    /// </summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult AsAssertion(string named) => Held
        ? AssertionResult.Pass(named, ToString())
        : AssertionResult.Unchecked(named, Precondition.Absent($"an overflow this run can {What}", Because ?? ToString()));

    /// <summary>The step a trace records.</summary>
    public TraceStep AsTraceStep(string named) => new()
    {
        Verb = $"{What} the overflow",
        Locator = named,
        Pattern = "Invoke on the chevron",
        ReadBack = Held ? (Already ? "it already was" : "it is") : null,
        Verdict = Held ? StepVerdict.Ok : StepVerdict.Unchecked,
        Detail = Held ? null : ToString(),
    };
}

/// <summary>
/// What looking for an icon by name turned out to find, and how far the looking got.
/// <para>
/// WW168. <c>Find</c> answered <c>TrayIcon?</c>, and null carried two facts that are not the same
/// one: the icon is not in the notification area, or the flyout would not open and only the taskbar
/// could be looked at. The first is a finding about the application. The second is a fact about the
/// desk, and this block's neighbour criterion says nothing about the desk is reported as a defect in
/// the code — so reporting them the same way is the confusion this whole project exists over,
/// reproduced one return type down.
/// </para>
/// <para>
/// Measured while shipping WW159: a case that added two icons of its own and asked for each by name
/// went red on one, and passed twice on the host straight afterwards. What had happened was the
/// second reading, reported as the first.
/// </para>
/// </summary>
public sealed record TraySearch
{
    internal TraySearch(string named, TrayIcon? icon, bool everywhere, OverflowState? overflow, string because)
    {
        Named = named;
        Icon = icon;
        Everywhere = everywhere;
        Overflow = overflow;
        Because = because;
    }

    /// <summary>What was asked for, as the caller spelled it.</summary>
    public string Named { get; }

    /// <summary>The icon, where one answered. Null where none did.</summary>
    public TrayIcon? Icon { get; }

    /// <summary>Whether one answered at all.</summary>
    public bool Found => Icon is not null;

    /// <summary>
    /// Whether every place the icon could have been was looked at. This is the field the whole
    /// reading exists for: not found and <c>true</c> is an answer about the application, and not
    /// found and <c>false</c> is an answer about the desk.
    /// </summary>
    public bool Everywhere { get; }

    /// <summary>
    /// What working the flyout did, where this search had to work it. Null where the icon was on the
    /// taskbar, so nothing was pressed, and null where the caller asked for the bar alone.
    /// </summary>
    public OverflowState? Overflow { get; }

    /// <summary>Why there is no icon, where there is none. Empty where there is one.</summary>
    public string Because { get; }

    /// <summary>What the search did, said either way.</summary>
    public string Sentence()
    {
        if (Found)
            return $"{Icon} answers to '{Named}'.";

        return $"nothing in the notification area answers to '{Named}': {Because}.";
    }

    /// <inheritdoc cref="Sentence" />
    public override string ToString() => Sentence();

    /// <summary>
    /// The result a verdict counts. An icon that is genuinely not there is a failure a scenario
    /// asked about; a search that could not reach the overflow never got to ask, so it is a
    /// <em>hole</em> and never a red about the code.
    /// </summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult AsAssertion(string named)
    {
        if (Found)
            return AssertionResult.Pass(named, Sentence());

        return Everywhere
            ? AssertionResult.Fail(named, Sentence())
            : AssertionResult.Unchecked(named, Precondition.Absent($"a notification area this run can search for '{Named}'", Because));
    }

    /// <summary>
    /// The verdict this search carries, which is the same three-way answer <see cref="AsAssertion" />
    /// gives and is spelled once so the two can never drift apart.
    /// </summary>
    private StepVerdict Verdict()
    {
        if (Found)
            return StepVerdict.Ok;

        return Everywhere ? StepVerdict.Failed : StepVerdict.Unchecked;
    }

    /// <summary>The step a trace records.</summary>
    public TraceStep AsTraceStep(string named) => new()
    {
        Verb = "find a tray icon",
        Locator = Named,
        Resolved = Icon?.ToString(),
        Pattern = Everywhere ? "the taskbar and the overflow" : "the taskbar alone",
        ReadBack = Found ? Icon!.Name.Split('\n')[0].Trim() : null,
        Verdict = Verdict(),
        Detail = Found ? null : Sentence(),
    };
}

/// <summary>
/// The notification area, which is the hardest thing on this desktop to drive.
/// <para>
/// Its icons have no clickable point — asking for one throws, and on Windows 11 build 26200 every
/// taskbar button throws, not only the tray ones — so the bounding rectangle is what addresses
/// them. An icon may sit inside the overflow flyout, which has to be opened before the icon is in
/// the tree at all, and the chevron that opens it is found by its automation id rather than by its
/// position among the other tray buttons, which changes every time an application starts.
/// </para>
/// <para>
/// Worse: a synthesised right-click on an icon does not open its menu on this shell. The route
/// that works is focus through automation plus the application key — the path a keyboard user
/// already has.
/// </para>
/// </summary>
public static class NotificationArea
{
    /// <summary>The taskbar's own window class.</summary>
    public const string TrayClassName = "Shell_TrayWnd";

    /// <summary>The class of the flyout the hidden icons live in.</summary>
    public const string OverflowClassName = "TopLevelWindowForOverflowXamlIsland";

    /// <summary>What every notification-area icon is identified by, wherever it sits.</summary>
    public const string IconAutomationId = "NotifyItemIcon";

    /// <summary>What the chevron is identified by. Never its position: that moves.</summary>
    public const string ChevronAutomationId = "SystemTrayIcon";

    /// <summary>The taskbar, or null where this desktop has none.</summary>
    public static AutomationElement? Tray()
    {
        var handle = Win32.FindWindowW(TrayClassName, null);
        return handle == 0 ? null : AutomationElement.FromHandle(handle);
    }

    /// <summary>The overflow flyout, where it is open.</summary>
    public static AutomationElement? Overflow() => AutomationElement.RootElement
        .FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ClassNameProperty, OverflowClassName))
        .Cast<AutomationElement>()
        .FirstOrDefault();

    /// <summary>The chevron that opens the overflow, found by id.</summary>
    public static AutomationElement? Chevron() => Tray()?.FindFirst(
        TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, ChevronAutomationId));

    /// <summary>The icons on the taskbar itself, largest-first by nothing — in shell order.</summary>
    public static IReadOnlyList<TrayIcon> Showing() => Under(Tray(), hidden: false);

    /// <summary>The icons in the overflow. Empty until it has been opened.</summary>
    public static IReadOnlyList<TrayIcon> Hidden() => Under(Overflow(), hidden: true);

    /// <summary>
    /// Open the overflow through the chevron's invoke pattern, which needs no pointer and no
    /// foreground.
    /// <para>
    /// WW165: answers a reading rather than a bool. A run that could not work the flyout said only
    /// <c>false</c>, and a red naming no cause sends a reader to a re-run rather than to the shell.
    /// </para>
    /// </summary>
    public static OverflowState OpenOverflow(int settleMs = 2000, int pollMs = 25)
    {
        if (Overflow() is not null)
            return new OverflowState("opened", true, already: true, null);

        var chevron = Chevron();
        if (chevron is null)
        {
            return new OverflowState(
                "opened", false, false, "the taskbar shows no chevron, so there is no flyout to open");
        }

        try
        {
            ((InvokePattern)chevron.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
        }
        catch (Exception refused)
            when (refused is InvalidOperationException or ElementNotAvailableException)
        {
            return new OverflowState("opened", false, false, $"the chevron would not take the act: {refused.Message}");
        }

        // Waiting for the flyout to exist is not waiting for it to be usable: measured, the
        // window arrives before its icons are laid out, and an icon read in that gap reports a
        // rectangle of nothing — which is the one address these icons have.
        var came = Attempt.UntilTrue(
            () => Overflow() is not null && Hidden().Any(icon => icon.Rectangle.Width > 0),
            settleMs,
            pollMs);

        if (came.Happened)
            return new OverflowState("opened", true, false, null);

        // The two are told apart, because a reader's next move differs: a flyout that never came
        // is the shell refusing, and one that came with nothing laid out in it is the gap above.
        return new OverflowState(
            "opened",
            false,
            false,
            Overflow() is null
                ? $"the chevron was pressed and no flyout came within {came.WaitedMs}ms"
                : $"the flyout came and laid out no icon within {came.WaitedMs}ms");
    }

    /// <summary>Shut it again, so a run leaves the taskbar the way it found it.</summary>
    public static OverflowState CloseOverflow(int settleMs = 2000, int pollMs = 25)
    {
        if (Overflow() is null)
            return new OverflowState("shut", true, already: true, null);

        var chevron = Chevron();
        if (chevron is null)
        {
            return new OverflowState(
                "shut", false, false, "the flyout is open and the taskbar shows no chevron to shut it with");
        }

        try
        {
            ((InvokePattern)chevron.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
        }
        catch (Exception refused)
            when (refused is InvalidOperationException or ElementNotAvailableException)
        {
            return new OverflowState("shut", false, false, $"the chevron would not take the act: {refused.Message}");
        }

        var went = Attempt.UntilTrue(() => Overflow() is null, settleMs, pollMs);
        return went.Happened
            ? new OverflowState("shut", true, false, null)
            : new OverflowState("shut", false, false, $"the flyout was still there after {went.WaitedMs}ms");
    }

    /// <summary>
    /// Find an icon by what the shell calls it. The match is on the name containing
    /// <paramref name="named"/> rather than equalling it, because a tray name is a tooltip and a
    /// real one runs to several lines of status. The overflow is opened when the taskbar does not
    /// hold it, because an icon hiding there is not in the tree until then.
    /// <para>
    /// WW168: answers a reading rather than <c>TrayIcon?</c>. Null said both "it is not there" and
    /// "the flyout would not open, so only half the places were looked at", and a caller had no way
    /// to tell a finding about the application from a fact about the desk.
    /// </para>
    /// </summary>
    /// <param name="named">Part of what the shell calls the icon.</param>
    /// <param name="openingTheOverflow">
    /// Whether to open the flyout when the taskbar does not hold it. False looks at the bar alone,
    /// which is a smaller question and is reported as one rather than as an absent icon.
    /// </param>
    /// <param name="settleMs">How long working the flyout may take.</param>
    /// <param name="pollMs">How often that wait looks again.</param>
    public static TraySearch Find(string named, bool openingTheOverflow = true, int settleMs = 2000, int pollMs = 25)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(named);

        var onTheBar = Showing().FirstOrDefault(icon => Matches(icon, named));
        if (onTheBar is not null)
            return new TraySearch(named, onTheBar, everywhere: true, null, "");

        if (!openingTheOverflow)
        {
            // Not everywhere, and deliberately so. The caller narrowed the question, and an answer
            // narrower than the question is still not an answer to the wider one.
            return new TraySearch(
                named, null, everywhere: false, null,
                "it is not on the taskbar, and this search was told not to open the overflow");
        }

        var flyout = OpenOverflow(settleMs, pollMs);
        if (!flyout.Held)
        {
            // The reading WW165 started answering, handed back rather than dropped. This is the
            // whole of WW168: the shell would not let this run look, which is not the icon being
            // absent and must never be reported as though it were.
            return new TraySearch(
                named, null, everywhere: false, flyout,
                $"it is not on the taskbar, and the overflow could not be looked in — {flyout}");
        }

        var hidden = Hidden().FirstOrDefault(icon => Matches(icon, named));
        return hidden is not null
            ? new TraySearch(named, hidden, everywhere: true, flyout, "")
            : new TraySearch(named, null, everywhere: true, flyout, "it is on neither the taskbar nor the overflow");
    }

    /// <summary>
    /// Open an icon's context menu the way a keyboard user does: focus it through automation, then
    /// press the application key. A synthesised right-click is deliberately not tried — on this
    /// shell it opens nothing at all, so a fallback to it would be a fallback to silence.
    /// </summary>
    public static TrayMenu OpenMenu(string named, int settleMs = 2000, int pollMs = 25)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(named);

        var search = Find(named, openingTheOverflow: true, settleMs, pollMs);
        if (!search.Found)
        {
            // WW168: the search's own sentence rather than one typed here. This used to say the icon
            // was on neither the taskbar nor the overflow whatever had happened, which was a
            // statement about the application on the runs where the flyout had simply not opened.
            return new TrayMenu(
                new TrayIcon(new ElementFacts(named, "", "Button", "", false, true, default, new HashSet<string>()), false),
                false,
                null,
                search.Because);
        }

        var icon = search.Icon!;

        var element = Live(icon);
        if (element is null)
            return new TrayMenu(icon, false, null, "the icon went away between finding it and asking it");

        try
        {
            element.SetFocus();
        }
        catch (Exception refused)
            when (refused is InvalidOperationException or ElementNotAvailableException)
        {
            return new TrayMenu(icon, false, null, $"it would not take the focus: {refused.Message}");
        }

        var before = OnTheDesk();
        Keys.SendApplicationKey();
        var came = Attempt.UntilTrue(
            () => OnTheDesk() is { } now && now != before && now != icon.Name, settleMs, pollMs);

        return came.Happened
            ? new TrayMenu(icon, true, OnTheDesk(), null)
            : new TrayMenu(icon, false, null, $"nothing was highlighted within {came.WaitedMs} ms of the application key");
    }

    /// <summary>
    /// What the desk is highlighting, read desktop-wide on purpose.
    /// <para>
    /// WW155 scoped every menu reading to the application under test, and this is the one place
    /// that would be wrong. The notification area is the shell's, and the menu an icon opens is
    /// drawn by whichever process owns the icon — so a reading scoped to the application this run
    /// is driving would reject the very menu this verb exists to open. Named here rather than
    /// reached through the menu's own verb, so the call site says which reading it is taking.
    /// </para>
    /// </summary>
    private static string? OnTheDesk() =>
        Traversal.WhoHasFocus()?.Name is { Length: > 0 } name ? name : null;

    private static bool Matches(TrayIcon icon, string named) =>
        icon.Name.Contains(named, StringComparison.OrdinalIgnoreCase);

    /// <summary>The live element behind an icon, for a caller that needs one.</summary>
    public static AutomationElement? ElementFor(TrayIcon icon)
    {
        ArgumentNullException.ThrowIfNull(icon);
        return Live(icon);
    }

    private static AutomationElement? Live(TrayIcon icon)
    {
        var root = icon.Hidden ? Overflow() : Tray();
        if (root is null)
            return null;

        return root.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, IconAutomationId))
            .Cast<AutomationElement>()
            .FirstOrDefault(candidate => (candidate.Current.Name ?? "") == icon.Name);
    }

    private static IReadOnlyList<TrayIcon> Under(AutomationElement? root, bool hidden)
    {
        if (root is null)
            return [];

        var found = new List<TrayIcon>();
        try
        {
            foreach (AutomationElement icon in root.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, IconAutomationId)))
            {
                if (ElementFacts.Of(icon) is { } facts)
                    found.Add(new TrayIcon(facts, hidden));
            }
        }
        catch (ElementNotAvailableException)
        {
            // The shell rebuilt the tray while it was being read; what was found stands.
        }

        return found;
    }
}
