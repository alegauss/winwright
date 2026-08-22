using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Tracing;
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
    /// foreground. Answers whether the flyout is open, which it also is when it already was.
    /// </summary>
    public static bool OpenOverflow(int settleMs = 2000, int pollMs = 25)
    {
        if (Overflow() is not null)
            return true;

        var chevron = Chevron();
        if (chevron is null)
            return false;

        try
        {
            ((InvokePattern)chevron.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
        }
        catch (Exception refused)
            when (refused is InvalidOperationException or ElementNotAvailableException)
        {
            return false;
        }

        // Waiting for the flyout to exist is not waiting for it to be usable: measured, the
        // window arrives before its icons are laid out, and an icon read in that gap reports a
        // rectangle of nothing — which is the one address these icons have.
        return Attempt.UntilTrue(
            () => Overflow() is not null && Hidden().Any(icon => icon.Rectangle.Width > 0),
            settleMs,
            pollMs).Happened;
    }

    /// <summary>Shut it again, so a run leaves the taskbar the way it found it.</summary>
    public static bool CloseOverflow(int settleMs = 2000, int pollMs = 25)
    {
        if (Overflow() is null)
            return true;

        var chevron = Chevron();
        if (chevron is null)
            return false;

        try
        {
            ((InvokePattern)chevron.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
        }
        catch (Exception refused)
            when (refused is InvalidOperationException or ElementNotAvailableException)
        {
            return false;
        }

        return Attempt.UntilTrue(() => Overflow() is null, settleMs, pollMs).Happened;
    }

    /// <summary>
    /// Find an icon by what the shell calls it. The match is on the name containing
    /// <paramref name="named"/> rather than equalling it, because a tray name is a tooltip and a
    /// real one runs to several lines of status. The overflow is opened when the taskbar does not
    /// hold it, because an icon hiding there is not in the tree until then.
    /// </summary>
    public static TrayIcon? Find(string named, bool openingTheOverflow = true, int settleMs = 2000, int pollMs = 25)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(named);

        var onTheBar = Showing().FirstOrDefault(icon => Matches(icon, named));
        if (onTheBar is not null || !openingTheOverflow)
            return onTheBar;

        return OpenOverflow(settleMs, pollMs)
            ? Hidden().FirstOrDefault(icon => Matches(icon, named))
            : null;
    }

    /// <summary>
    /// Open an icon's context menu the way a keyboard user does: focus it through automation, then
    /// press the application key. A synthesised right-click is deliberately not tried — on this
    /// shell it opens nothing at all, so a fallback to it would be a fallback to silence.
    /// </summary>
    public static TrayMenu OpenMenu(string named, int settleMs = 2000, int pollMs = 25)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(named);

        var icon = Find(named, openingTheOverflow: true, settleMs, pollMs);
        if (icon is null)
        {
            return new TrayMenu(
                new TrayIcon(new ElementFacts(named, "", "Button", "", false, true, default, new HashSet<string>()), false),
                false,
                null,
                "no icon in the notification area is called that, on the taskbar or in the overflow");
        }

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

        var before = Menu.Highlighted();
        Keys.SendApplicationKey();
        var came = Attempt.UntilTrue(
            () => Menu.Highlighted() is { } now && now != before && now != icon.Name, settleMs, pollMs);

        return came.Happened
            ? new TrayMenu(icon, true, Menu.Highlighted(), null)
            : new TrayMenu(icon, false, null, $"nothing was highlighted within {came.WaitedMs} ms of the application key");
    }

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
