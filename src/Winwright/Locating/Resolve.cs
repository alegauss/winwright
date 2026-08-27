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

    /// <summary>
    /// The element, or null where nothing matched. Not public: an act reaches its element only
    /// through <see cref="Admitted"/>, which has already judged actionability, the way a launch
    /// reaches a process only through the register. Reading is what this stays open for inside the
    /// engine — a read needs no actionability, and an act cannot be spelled from out here.
    /// </summary>
    internal AutomationElement? Element { get; }

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

        return Ordered(found, step.Order);
    }

    /// <summary>
    /// Put matches in the order a step asked for. The tree's own order is whatever the application
    /// happened to create things in; a rectangle is a real property of the window, which is what
    /// makes the choice reviewable in the file rather than buried in a sort at one call site.
    /// </summary>
    private static IReadOnlyList<AutomationElement> Ordered(
        List<AutomationElement> found, MatchOrder? order)
    {
        if (order is null or MatchOrder.Tree || found.Count < 2)
            return found;

        var bounds = found.ToDictionary(
            element => element, element => ElementFacts.Of(element)?.Bounds ?? default);

        found.Sort((left, right) => order switch
        {
            MatchOrder.Left => Compare(bounds[left].Left, bounds[right].Left, bounds[left].Top, bounds[right].Top),
            MatchOrder.Right => Compare(bounds[right].Left, bounds[left].Left, bounds[left].Top, bounds[right].Top),
            MatchOrder.Top => Compare(bounds[left].Top, bounds[right].Top, bounds[left].Left, bounds[right].Left),
            _ => Compare(bounds[right].Top, bounds[left].Top, bounds[left].Left, bounds[right].Left),
        });

        return found;
    }

    private static int Compare(int first, int second, int tieFirst, int tieSecond)
    {
        var by = first.CompareTo(second);
        return by != 0 ? by : tieFirst.CompareTo(tieSecond);
    }

    private static AutomationElement? Walk(AutomationElement root, Locator locator)
    {
        var here = root;
        foreach (var step in locator.Steps)
        {
            var matches = Matching(here, step);

            // Strict, deliberately: a step matching several elements and saying nothing about
            // which is a scenario that will one day run against the other one, and be green.
            if (matches.Count > 1 && !step.Disambiguated)
                throw new AmbiguousLocatorException(step, matches.Select(Named).ToList());

            var wanted = (step.Index ?? 1) - 1;
            if (wanted >= matches.Count)
                return null;

            here = matches[wanted];
        }

        return here;
    }

    private static string Named(AutomationElement element)
    {
        var facts = ElementFacts.Of(element);
        return facts is null ? "(gone)" : $"{facts.AsLocatorStep()}  {facts.Bounds}";
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

        // WW252. Every step matched, which a diagnosis of a failed resolve cannot do unless the
        // element arrived between the two. Answered rather than indexed past the end: a diagnosis is a
        // page about a failure and never a second thing that can fail.
        if (reached >= locator.Steps.Count)
            return new LocatorMiss(locator, reached, deepest, MissKind.ArrivedLate, null, 0, []);

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

        // WW274. An Or where the step names several types, which is the whole of what a union costs
        // here: UI Automation takes the tree walk either way, so a family of controls is one search
        // rather than one search per type with the answers stitched together afterwards.
        var types = step.ControlTypes
            .Select(one => (Condition)new PropertyCondition(
                AutomationElement.ControlTypeProperty, UiaVocabulary.ControlTypeFor(one)))
            .ToList();

        if (types.Count == 1)
            conditions.Add(types[0]);
        else if (types.Count > 1)
            conditions.Add(new OrCondition([.. types]));

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
