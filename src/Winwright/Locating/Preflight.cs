using System.Windows.Automation;

using Winwright.Scenarios;

namespace Winwright.Locating;

/// <summary>One act a scenario declares: what it addresses, and the pattern it goes through.</summary>
/// <param name="Verb">What the act is, as the scenario names it — click, type, toggle.</param>
/// <param name="Locator">What it addresses.</param>
/// <param name="Pattern">The UI Automation pattern it needs, spelled as the grammar spells one.</param>
public sealed record ActRequirement(string Verb, Locator Locator, string Pattern)
{
    /// <summary>The one line a refusal or a report names it by.</summary>
    public override string ToString() => $"{Verb} {Locator} (needs {Pattern})";
}

/// <summary>An act this application cannot take, whatever the scenario says.</summary>
/// <param name="Act">The act that was checked.</param>
/// <param name="Element">What its locator resolved to.</param>
/// <param name="Offered">What that element does offer, in alphabetical order.</param>
public sealed record ActRefusal(ActRequirement Act, ElementFacts Element, IReadOnlyList<string> Offered)
{
    /// <summary>The sentence the author has to act on, with the element and the pattern both named.</summary>
    public string Because =>
        $"{Act.Verb} needs {Act.Pattern}, and {Element} offers "
        + (Offered.Count == 0 ? "no pattern at all" : string.Join(", ", Offered));
}

/// <summary>
/// What each act needs, checked against what the controls actually offer, before the run starts.
/// <para>
/// Reaching for a pattern a control does not carry is a run-time failure otherwise, discovered on
/// a red run and usually far from the line that caused it. Here it is a refusal at load with the
/// element and the pattern both named — which is only possible because inspect already reads what
/// each element offers.
/// </para>
/// <para>
/// An act whose locator does not resolve right now is <em>not</em> refused: the control may appear
/// after an earlier step, and refusing it would make every scenario that navigates unloadable. It
/// is counted and named as unchecked instead, because a preflight that silently skipped what it
/// could not see would report a clean check it did not make.
/// </para>
/// </summary>
public static class Preflight
{
    /// <summary>What one locator's element offers, or null where it does not resolve from here.</summary>
    public static IReadOnlyList<string>? Offers(AutomationElement root, Locator locator)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(locator);

        var facts = Resolve.Once(root, locator).Facts;
        return facts?.Patterns.OrderBy(name => name, StringComparer.Ordinal).ToList();
    }

    /// <summary>Check every act, and answer with all three outcomes rather than the first refusal.</summary>
    public static Preflighted Check(AutomationElement root, IEnumerable<ActRequirement> acts)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(acts);

        var refused = new List<ActRefusal>();
        var unchecked_ = new List<ActRequirement>();
        var cleared = new List<ActRequirement>();

        foreach (var act in acts)
        {
            var facts = Resolve.Once(root, act.Locator).Facts;
            if (facts is null)
            {
                unchecked_.Add(act);
                continue;
            }

            if (ActionabilityCheck.Of(facts, act.Pattern).Missing.Contains(Actionable.PatternMissing))
            {
                refused.Add(new ActRefusal(
                    act, facts, facts.Patterns.OrderBy(name => name, StringComparer.Ordinal).ToList()));
                continue;
            }

            cleared.Add(act);
        }

        return new Preflighted(refused, unchecked_, cleared);
    }

    /// <summary>Check, and stop where anything was refused.</summary>
    /// <exception cref="ScenarioRefusedException">Where an act needs a pattern its control does not carry.</exception>
    public static Preflighted Require(AutomationElement root, IEnumerable<ActRequirement> acts)
    {
        var checked_ = Check(root, acts);
        if (checked_.Refused.Count > 0)
        {
            throw new ScenarioRefusedException(
                checked_.Refused[0].Act.ToString(),
                string.Join("; ", checked_.Refused.Select(refusal => refusal.Because)));
        }

        return checked_;
    }
}

/// <summary>The whole reading of a preflight: what was refused, what was cleared, and what was not seen.</summary>
/// <param name="Refused">Acts whose control does not carry the pattern they need.</param>
/// <param name="Unchecked">Acts whose locator does not resolve yet, so nothing about them was checked.</param>
/// <param name="Cleared">Acts whose control carries what they need.</param>
public sealed record Preflighted(
    IReadOnlyList<ActRefusal> Refused,
    IReadOnlyList<ActRequirement> Unchecked,
    IReadOnlyList<ActRequirement> Cleared)
{
    /// <summary>Whether anything was refused.</summary>
    public bool Refuses => Refused.Count > 0;

    /// <summary>
    /// The reading in one sentence. It never says every act was checked while anything was not —
    /// the same rule the run's own summary is held to, one altitude earlier.
    /// </summary>
    public string Sentence()
    {
        var total = Refused.Count + Unchecked.Count + Cleared.Count;
        if (total == 0)
            return "no act declares a pattern to check.";

        var clauses = new List<string>();
        if (Refused.Count > 0)
            clauses.Add($"{Refused.Count} refused: {string.Join("; ", Refused.Select(one => one.Because))}");
        if (Unchecked.Count > 0)
            clauses.Add(
                $"{Unchecked.Count} not checked, their controls not being in the tree yet: "
                + string.Join(", ", Unchecked.Select(act => act.Locator.ToString())));

        return clauses.Count == 0
            ? $"every one of {total} acts can be taken by the control it addresses."
            : $"{Cleared.Count} of {total} acts cleared; " + string.Join("; ", clauses) + ".";
    }
}
