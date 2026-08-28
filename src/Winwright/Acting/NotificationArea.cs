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

/// <summary>
/// What asking a tray icon for its menu did.
/// <para>
/// WW174. This used to answer no verdict at all and a step that read <c>Opened ? Ok : Failed</c>, so
/// a shell that would not open the flyout, an icon that vanished mid-act and a desk that refused the
/// focus were all recorded as the application failing to show a menu. WW168 closed that collapse one
/// call further down and this is where it was still live — in the verb an adopter reaches for more
/// often than the search underneath it.
/// </para>
/// </summary>
public sealed record TrayMenu
{
    internal TrayMenu(TrayIcon icon, bool opened, string? highlighted, string? because, Precondition? missing = null)
    {
        Icon = icon;
        Opened = opened;
        Highlighted = highlighted;
        Because = because;
        Missing = missing;
    }

    /// <summary>What this condition is called wherever it is reported.</summary>
    public const string PreconditionName = "the tray icon can be reached and asked";

    /// <summary>
    /// The desk fact that stopped this, where one did. Null where the menu opened, and null where it
    /// did not open for a reason that is about the application — which is the distinction the whole
    /// type was missing.
    /// </summary>
    public Precondition? Missing { get; }

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

    /// <summary>
    /// The result a verdict counts. A menu the application never showed is a failure a scenario
    /// asked about; a desk that would not let this run ask is a <em>hole</em>, and this block's
    /// neighbour criterion says nothing about the desk is reported as a defect in the code.
    /// </summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult AsAssertion(string named)
    {
        if (Opened)
            return AssertionResult.Pass(named, ToString());

        return Missing is not null
            ? AssertionResult.Unchecked(named, Missing)
            : AssertionResult.Fail(named, ToString());
    }

    /// <summary>
    /// The verdict this carries, spelled once so the assertion and the step beside it cannot drift
    /// apart — which is what happened when the step decided for itself out of <see cref="Opened" />.
    /// </summary>
    private StepVerdict Verdict()
    {
        if (Opened)
            return StepVerdict.Ok;

        return Missing is not null ? StepVerdict.Unchecked : StepVerdict.Failed;
    }

    /// <summary>The step a trace records.</summary>
    public TraceStep AsTraceStep() => new()
    {
        Verb = "open the tray menu",
        Locator = Icon.Name.Split('\n')[0].Trim(),
        Resolved = Icon.ToString(),
        Pattern = "focus and the application key",
        ReadBack = Highlighted,
        Verdict = Verdict(),
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
    /// The condition a hole here is about.
    /// <para>
    /// WW190. This was composed at the call — <c>"an overflow this run can " + What</c> — so the
    /// name changed with the verb and matched nothing. A condition spelled differently on every
    /// path is one no catalogue can hold and no caller can match, which meant the hole this type
    /// answers could never be recognised as a fact about the desk: <c>DeskFacts</c> names the
    /// conditions that are the desk's, and a name invented at the throw site is in no list at all.
    /// </para>
    /// </summary>
    public const string PreconditionName = "an overflow flyout this run can work";

    /// <summary>
    /// The result a verdict counts. A shell that would not work the flyout is a fact about the
    /// desk and never a defect in the code under test, so it is a <em>hole</em> — this block's
    /// neighbour criterion says nothing about the desk is reported as a defect in the code.
    /// </summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult AsAssertion(string named) => Held
        ? AssertionResult.Pass(named, ToString())
        : AssertionResult.Unchecked(named, Precondition.Absent(PreconditionName, Because ?? ToString()));

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
    /// <summary>
    /// What the condition is called wherever it is reported. A constant and not a sentence built
    /// round the icon's name: a precondition is a thing a reader recognises across runs, and one
    /// spelled differently for every icon is one nothing can be matched against. What was looked
    /// for is in <see cref="Because" />, where it belongs.
    /// </summary>
    public const string PreconditionName = "the notification area can be searched";

    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult AsAssertion(string named)
    {
        if (Found)
            return AssertionResult.Pass(named, Sentence());

        return Everywhere
            ? AssertionResult.Fail(named, Sentence())
            : AssertionResult.Unchecked(named, Precondition.Absent(PreconditionName, Because));
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
    /// Whether there is a notification area on this desk to look at at all.
    /// <para>
    /// WW190. Every other reading here answers a question about an icon, and a run whose taskbar
    /// was covered had no icon to ask about — so a check on the shell went red about the shell,
    /// which is a fact about the desk and never a defect in the code under test. Measured: holding
    /// the guest's desk turned this into six reds naming an empty taskbar, an absent chevron and a
    /// flyout that would not open.
    /// </para>
    /// <para>
    /// Named as the search's own condition rather than a new one, because that is what it is: a
    /// desk with no taskbar, no chevron or no icon at all is a desk where the notification area
    /// cannot be searched. What it is not is a bool — the absence says which of the three it found,
    /// and a reader handed "the tray is unreachable" has to go and look.
    /// </para>
    /// </summary>
    public static Precondition Reachable()
    {
        if (Tray() is null)
            return Precondition.Absent(
                TraySearch.PreconditionName, $"no window of class {TrayClassName} is on this desk");

        if (Chevron() is null)
            return Precondition.Absent(
                TraySearch.PreconditionName,
                "the taskbar is there and carries no chevron, so nothing hidden can be reached");

        return Showing().Count > 0
            ? Precondition.Met(TraySearch.PreconditionName)
            : Precondition.Absent(
                TraySearch.PreconditionName, "the taskbar is there and shows no icon at all");
    }

    /// <summary>
    /// Whether this desk is placing tray icons at all, wherever they land.
    /// <para>
    /// WW217. <see cref="Reachable"/> answers whether the area can be searched and stops at the bar,
    /// which is the wrong question for the case that has just failed to find its own icon: a search
    /// that opened the flyout, read it, and did not see one genuinely looked everywhere — so the
    /// verdict was a red about the code, and the sentence beside it said the shell had taken the
    /// icon and put it nowhere. On a guest under a full suite that is the shell being slow, and a
    /// red about it sends the reader to this repository.
    /// </para>
    /// <para>
    /// So the question is asked once, after the fact, about the desk rather than about the icon: is
    /// anything placed anywhere. A bar or a flyout holding icons is a shell that places them, and
    /// ours being absent from it is a finding. A bar with nothing on it and a flyout this run cannot
    /// read has placed nobody's icon, and that is not a finding about anything.
    /// </para>
    /// <para>
    /// It leaves the taskbar as it found it. Looking may have to open the flyout, and a flyout left
    /// standing is the thing the next case trips on.
    /// </para>
    /// </summary>
    /// <param name="settleMs">How long working the flyout may take.</param>
    /// <param name="pollMs">How often that wait looks again.</param>
    public static Precondition Placing(int settleMs = 2000, int pollMs = 25)
    {
        if (Tray() is null)
            return Precondition.Absent(TraySearch.PreconditionName, $"no window of class {TrayClassName} is on this desk");

        // The cheap half first, and it is also the common answer: a bar with anything on it is a
        // shell that places icons, and nothing has to be opened to find that out.
        if (Showing().Count > 0)
            return Precondition.Met(TraySearch.PreconditionName);

        if (Chevron() is null)
        {
            return Precondition.Absent(
                TraySearch.PreconditionName,
                "the taskbar shows no icon and carries no chevron, so nothing placed anywhere could be reached");
        }

        var already = Overflow() is not null;
        var flyout = OpenOverflow(settleMs, pollMs);
        try
        {
            if (!flyout.Held)
            {
                return Precondition.Absent(
                    TraySearch.PreconditionName,
                    $"the taskbar shows no icon and the overflow could not be looked in — {flyout}");
            }

            return Hidden().Count > 0
                ? Precondition.Met(TraySearch.PreconditionName)
                : Precondition.Absent(
                    TraySearch.PreconditionName,
                    "neither the taskbar nor the overflow holds a single icon, so this shell is placing none");
        }
        finally
        {
            // Only what this reading opened. A flyout that was already standing belongs to whoever
            // opened it, and shutting it here would answer their next look with a closed one.
            if (!already && flyout.Held)
                CloseOverflow(settleMs, pollMs);
        }
    }

    /// <summary>
    /// Open the overflow through the chevron's invoke pattern, which needs no pointer and no
    /// foreground.
    /// <para>
    /// WW165: answers a reading rather than a bool. A run that could not work the flyout said only
    /// <c>false</c>, and a red naming no cause sends a reader to a re-run rather than to the shell.
    /// </para>
    /// <para>
    /// WW288. A flyout that was already standing goes through the same gate as one this call opened,
    /// and it used to go through none: the window existing was enough. So the same desk got two
    /// different answers depending on who had opened it, and the permissive one was the answer a
    /// caller acted on — <see cref="Find" /> then polled a flyout nothing had established was usable,
    /// which is the poll WW223 traced its recurrence to.
    /// </para>
    /// </summary>
    public static OverflowState OpenOverflow(int settleMs = 2000, int pollMs = 25)
    {
        // WW288. Gated, and waited for rather than read once, because a flyout somebody else opened a
        // moment ago is in exactly the state the comment below describes: the window arrives before
        // its icons do. `already` still says who opened it, since that is what decides whether a
        // caller may shut it again.
        if (Overflow() is not null)
        {
            var standing = Attempt.UntilTrue(Usable, settleMs, pollMs);
            if (standing.Happened)
                return new OverflowState("opened", true, already: true, null);

            var went = Overflow() is null;
            var because = went
                ? $"the flyout was standing and had gone within {standing.WaitedMs}ms, so it was on its "
                    + "way out rather than open"
                : $"the flyout was standing and laid out no icon within {standing.WaitedMs}ms";

            return new OverflowState("opened", false, already: true, because);
        }

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
        //
        // WW288. The predicate is shared with the already-standing path above rather than written
        // twice. Two spellings of one gate is how they came to disagree, and the disagreement was
        // the whole defect: a flyout somebody else opened passed a test this one would have failed.
        var came = Attempt.UntilTrue(Usable, settleMs, pollMs);

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

    /// <summary>
    /// Whether the flyout is not merely there but can be read: open, with at least one icon that has
    /// laid out to a width.
    /// <para>
    /// WW288. One predicate, because this used to be two — the path that pressed the chevron waited
    /// for this, and the path that found a flyout already standing waited for nothing at all. The
    /// second is the one <see cref="Find" /> was polling behind when WW223 recurred: a window that
    /// exists is not a desk that can be looked at, and it is also what a flyout on its way out looks
    /// like.
    /// </para>
    /// </summary>
    private static bool Usable() =>
        Overflow() is not null && Hidden().Any(icon => icon.Rectangle.Width > 0);

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
    /// <param name="settleMs">
    /// How long each stage of working the flyout may take: opening it, and then WW220's wait for the
    /// icon this search was named for. Two stages and not one, because a flyout that has laid out
    /// somebody else's icon is open and is not yet an answer to this question.
    /// </param>
    /// <param name="pollMs">How often those waits look again.</param>
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

        // WW220. Polled for the name this search was given, and not read once. The gate the flyout
        // was settled against is any icon with a width — which a flyout holding four of the shell's
        // own satisfies on the first poll, while the one this run added is still arriving. Measured:
        // a case added an icon, watched a reading find it, shut the flyout, asked again through here,
        // and got told it was on neither the taskbar nor the overflow.
        //
        // The cost is on the negative, and that is the right way round: an icon genuinely absent now
        // pays the settle before saying so, and a fast wrong no is exactly what this was.
        TrayIcon? hidden = null;
        Attempt.UntilTrue(
            () => (hidden = Hidden().FirstOrDefault(icon => Matches(icon, named))) is not null,
            settleMs,
            pollMs);

        if (hidden is not null)
            return new TraySearch(named, hidden, everywhere: true, flyout, "");

        // WW223. The poll gave up, and until now that came out as one sentence whichever of two
        // things had happened. WW220 made the look a poll rather than a single read; what a poll
        // cannot fix is a flyout that shuts while it is running — Hidden() then answers empty for
        // the rest of the deadline, and "on neither" becomes a claim about the icon assembled out of
        // a desk that had stopped being lookable. That is the shape WW168, WW174 and WW179 each
        // caught once already: not found because nothing was there, against not found because
        // nothing could look.
        //
        // Read once, after the fact, for the reason the diagnosis is taken after the last attempt:
        // what a reader wants is the desk as it stood when the run gave up.
        var open = Overflow() is not null;
        var inside = open ? Hidden().Count : 0;
        var showing = Showing().Count;

        if (!open)
        {
            // Not everywhere, so it is a hole under this search's own condition rather than an
            // absence. A caller told the icon is missing restarts nothing; a caller told the flyout
            // shut mid-look knows to ask again.
            return new TraySearch(
                named, null, everywhere: false, flyout,
                $"the overflow shut while this search was looking in it, so the flyout was not read to "
                    + $"the end — the taskbar's own {showing} icon(s) are all this saw");
        }

        // The counts are the half that used to be missing. An empty flyout and a flyout holding four
        // of the shell's own end this sentence the same way, and the difference is the one that says
        // whether the shell is placing anything at all.
        return new TraySearch(
            named, null, everywhere: true, flyout,
            $"it is on neither the taskbar nor the overflow ({showing} on the bar, {inside} in the flyout)");
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
            // WW174: and the search's verdict too, not only its sentence. A shell that would not
            // open the flyout never got asked whether the icon is there, so the menu it could not
            // ask for is a hole under the search's own condition rather than a menu that failed.
            return new TrayMenu(
                new TrayIcon(new ElementFacts(named, "", "Button", "", false, true, default, new HashSet<string>()), false),
                false,
                null,
                search.Because,
                search.Everywhere ? null : Precondition.Absent(TraySearch.PreconditionName, search.Because));
        }

        var icon = search.Icon!;

        var element = Live(icon);
        if (element is null)
        {
            // WW174. The icon was there and then was not, which is a fact about a shell rearranging
            // its own taskbar. Nothing was asked of the application, so nothing about it was
            // observed — and a red here sends a reader looking for a menu bug that never existed.
            const string vanished = "the icon went away between finding it and asking it";
            return new TrayMenu(icon, false, null, vanished, Precondition.Absent(TrayMenu.PreconditionName, vanished));
        }

        try
        {
            element.SetFocus();
        }
        catch (Exception refused)
            when (refused is InvalidOperationException or ElementNotAvailableException)
        {
            // WW174. The route to a tray menu is focus and then the application key, so a desk that
            // will not give the focus stops the act before it starts. That is the same class of
            // fact as a foreground Windows would not grant, and it is a hole for the same reason.
            var because = $"it would not take the focus: {refused.Message}";
            return new TrayMenu(icon, false, null, because, Precondition.Absent(TrayMenu.PreconditionName, because));
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
