using System.Windows.Automation;

using Winwright.Projects;

namespace Winwright.Locating;

/// <summary>What resolving a locator found, or what it did not and why.</summary>
public sealed record Resolution
{
    internal Resolution(AutomationElement? element, ElementFacts? facts, LocatorMiss? miss, int waitedMs, int polls)
    {
        Element = element;
        Facts = facts;
        Miss = miss;
        WaitedMs = waitedMs;
        Polls = polls;
    }

    /// <summary>The element, or null where nothing matched.</summary>
    public AutomationElement? Element { get; }

    /// <summary>What it says about itself, read once at the moment it was found.</summary>
    public ElementFacts? Facts { get; }

    /// <summary>Why nothing matched, diagnosed. Null where something did.</summary>
    public LocatorMiss? Miss { get; }

    /// <summary>How long resolving took, which is what a trace step records.</summary>
    public int WaitedMs { get; }

    /// <summary>How many looks it took.</summary>
    public int Polls { get; }

    /// <summary>Whether anything matched.</summary>
    public bool Found => Element is not null;
}

/// <summary>
/// Turning a locator into an element, and turning a miss into a route.
/// <para>
/// Each step is searched for among the descendants of the one before it, which is what
/// <c>&gt;</c> means. When a step finds nothing the search stops there rather than guessing, and
/// what it stopped at is diagnosed: the contents of a collapsed combo box or an unselected page
/// are absent from the tree by design, and that reads exactly like a control that was renamed.
/// </para>
/// </summary>
public static class Resolve
{
    /// <summary>Resolve with a single look, which is what asking whether something is gone needs.</summary>
    public static Resolution Once(AutomationElement root, Locator locator)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(locator);

        var sighting = Attempt.Once(() => Walk(root, locator));
        return Answered(root, locator, sighting.Value, sighting.WaitedMs, sighting.Polls);
    }

    /// <summary>Resolve, polling until the deadline. The first look is taken before any waiting.</summary>
    public static Resolution Until(AutomationElement root, Locator locator, int deadlineMs, int pollMs = 25)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(locator);

        var sighting = Attempt.Until(() => Walk(root, locator), deadlineMs, pollMs);
        return Answered(root, locator, sighting.Value, sighting.WaitedMs, sighting.Polls);
    }

    /// <summary>The same, with the deadline read from what the project declared.</summary>
    public static Resolution Until(AutomationElement root, Locator locator, Timeouts timeouts, string named = "resolve")
    {
        ArgumentNullException.ThrowIfNull(timeouts);
        return Until(root, locator, timeouts.For(named), timeouts.For("poll"));
    }

    /// <summary>
    /// Every element under <paramref name="root"/> that one step matches, in tree order. Public
    /// because the same read answers the ordering question and the ambiguity one.
    /// </summary>
    public static IReadOnlyList<AutomationElement> Matching(AutomationElement root, LocatorStep step)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(step);

        var found = new List<AutomationElement>();
        try
        {
            foreach (AutomationElement candidate in root.FindAll(TreeScope.Descendants, ConditionFor(step)))
            {
                if (step.Pattern is null || (ElementFacts.Of(candidate)?.Supports(step.Pattern) ?? false))
                    found.Add(candidate);
            }
        }
        catch (ElementNotAvailableException)
        {
            // The subtree went while it was being searched; what was found stands.
        }

        return found;
    }

    private static AutomationElement? Walk(AutomationElement root, Locator locator)
    {
        var here = root;
        foreach (var step in locator.Steps)
        {
            var matches = Matching(here, step);
            var wanted = (step.Index ?? 1) - 1;
            if (wanted >= matches.Count)
                return null;

            here = matches[wanted];
        }

        return here;
    }

    private static Resolution Answered(
        AutomationElement root, Locator locator, AutomationElement? found, int waitedMs, int polls)
    {
        if (found is not null)
            return new Resolution(found, ElementFacts.Of(found), null, waitedMs, polls);

        return new Resolution(null, null, Diagnose(root, locator), waitedMs, polls);
    }

    private static LocatorMiss Diagnose(AutomationElement root, Locator locator)
    {
        var here = root;
        ElementFacts? deepest = null;
        var reached = 0;

        while (reached < locator.Steps.Count)
        {
            var step = locator.Steps[reached];
            var matches = Matching(here, step);
            var wanted = (step.Index ?? 1) - 1;
            if (wanted >= matches.Count)
                break;

            here = matches[wanted];
            deepest = ElementFacts.Of(here);
            reached++;
        }

        var stopped = locator.Steps[reached];

        // Elsewhere first, and deliberately: if the thing is in this window under some other
        // parent, the chain is wrong, and no amount of expanding what the chain named will help.
        var elsewhere = reached == 0 ? 0 : Matching(root, stopped).Count;
        if (elsewhere > 0)
            return new LocatorMiss(locator, reached, deepest, MissKind.ElsewhereInTheWindow, null, elsewhere, []);

        var route = LocatorMiss.RouteFrom(reached == 0 ? null : here, deepest);
        if (route is not null)
            return new LocatorMiss(locator, reached, deepest, MissKind.NavigationNeeded, route, 0, []);

        return new LocatorMiss(locator, reached, deepest, MissKind.Absent, null, 0, ClosedDoors(root));
    }

    /// <summary>
    /// What is shut in this window right now, at most a handful. Leads rather than an answer, and
    /// the count is capped because a lead list nobody reads to the end is a lead list nobody reads.
    /// </summary>
    private static IReadOnlyList<ClosedDoor> ClosedDoors(AutomationElement root, int most = 6)
    {
        var doors = new List<ClosedDoor>();
        try
        {
            foreach (AutomationElement candidate in root.FindAll(TreeScope.Descendants, Condition.TrueCondition))
            {
                if (doors.Count >= most)
                    break;

                var facts = ElementFacts.Of(candidate);
                if (facts is null)
                    continue;

                var how = LocatorMiss.Shut(candidate, facts);
                if (how is not null)
                    doors.Add(new ClosedDoor(facts.ToString(), how));
            }
        }
        catch (ElementNotAvailableException)
        {
            // The window went while it was being searched; what was found stands.
        }

        return doors;
    }

    private static Condition ConditionFor(LocatorStep step)
    {
        var conditions = new List<Condition>();
        if (step.ControlType is not null)
            conditions.Add(new PropertyCondition(
                AutomationElement.ControlTypeProperty, UiaVocabulary.ControlTypeFor(step.ControlType)));
        if (step.AutomationId is not null)
            conditions.Add(new PropertyCondition(AutomationElement.AutomationIdProperty, step.AutomationId));
        if (step.Name is not null)
            conditions.Add(new PropertyCondition(AutomationElement.NameProperty, step.Name));
        if (step.ClassName is not null)
            conditions.Add(new PropertyCondition(AutomationElement.ClassNameProperty, step.ClassName));

        return conditions.Count switch
        {
            0 => Condition.TrueCondition,
            1 => conditions[0],
            _ => new AndCondition([.. conditions]),
        };
    }
}
