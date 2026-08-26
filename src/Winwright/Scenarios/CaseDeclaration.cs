using System.Collections.ObjectModel;

namespace Winwright.Scenarios;

/// <summary>
/// A case: a name, the steps it is, what selects it, what it needs present, and the defect it exists
/// to catch.
/// <para>
/// This is the whole of what a case is. There is no place on it for a loop, a deadline, a retry
/// cap, a process to launch or a verdict to assemble, because every one of those belongs to
/// <see cref="CaseRun"/> — and the reason the framework exists rather than the library it would
/// otherwise have been is that eight cases writing those five things eight times is where the
/// duplication actually was.
/// </para>
/// <para>
/// Two refusals, both about a case that cannot fail. One with no steps drives nothing. One whose
/// steps all expect nothing acts and never looks, so it reads green on a build with the defect
/// still in it — the unearned green this project refuses everywhere else, arriving as a file rather
/// than as a verdict. Refusing them here is what makes <see cref="Verdicts.RunVerdict.Over(System.Collections.Generic.IEnumerable{Verdicts.AssertionResult})"/>
/// unable to be handed nothing by a run of a case that loaded.
/// </para>
/// <para>
/// <see cref="Catches"/> is not refused when it is absent, and that is deliberate. WW63 wants a case
/// nobody can justify to be <em>visible</em>, and a required field would instead make it invented:
/// asked for a sentence they do not have, an author writes one, and the field stops meaning anything.
/// So it is optional, and <see cref="Unjustified"/> counts what is missing.
/// </para>
/// </summary>
public sealed record CaseDeclaration
{
    private CaseDeclaration(
        string name,
        IReadOnlyList<StepDeclaration> steps,
        IReadOnlyList<string> tags,
        IReadOnlyList<string> needs,
        string catches,
        string filed,
        FixtureDeclaration fixture,
        bool onlyReads)
    {
        Name = name;
        Steps = steps;
        Tags = tags;
        Needs = needs;
        Catches = catches;
        Filed = filed;
        Fixture = fixture;
        OnlyReads = onlyReads;
    }

    /// <summary>What the case is called, and the name a run of it is reported under.</summary>
    public string Name { get; }

    /// <summary>Its steps, in the order they are performed.</summary>
    public IReadOnlyList<StepDeclaration> Steps { get; }

    /// <summary>
    /// What this case can be selected by besides its name, in declared order. Empty where it
    /// declares none, and then only its name and its file select it.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// What this machine has to have before there is anything for this case to observe, by name and
    /// in declared order.
    /// <para>
    /// WW61. pportal's interaction tests fail rather than skip when no controller is plugged in,
    /// because xUnit gives them no third outcome to use. This project has one, so the condition
    /// belongs in the case — this needs two profiles, this needs a pad, this needs a display that
    /// renders — and its absence is named and counted rather than argued about.
    /// </para>
    /// </summary>
    public IReadOnlyList<string> Needs { get; }

    /// <summary>
    /// The defect this case exists to catch — what went wrong without it. Empty where the case
    /// declares none, which is a case nobody can justify and is counted rather than refused.
    /// </summary>
    public string Catches { get; }

    /// <summary>The task this case was filed under, where it names one. Empty otherwise.</summary>
    public string Filed { get; }

    /// <summary>
    /// What this case is launched against. <see cref="FixtureDeclaration.Plain"/> where it names
    /// none, and then the application is launched as it comes.
    /// </summary>
    public FixtureDeclaration Fixture { get; }

    /// <summary>
    /// Whether this case leaves the window as it found it, and may therefore be lent one.
    /// <para>
    /// WW62. Declared by the case because only the case knows: an author who wrote four reads and
    /// no acts knows it, and nothing about the steps proves it — a read whose act is
    /// <c>expand</c> changes the tree, and one whose act is <c>select</c> changes what is
    /// selected. Whether a run actually lends anything is opted into per invocation.
    /// </para>
    /// </summary>
    public bool OnlyReads { get; }

    /// <summary>Whether this case says what it exists to catch.</summary>
    public bool Justified => Catches.Length > 0;

    /// <summary>Whether this case declares <paramref name="tag"/>, however it is cased.</summary>
    public bool Tagged(string tag) => Tags.Contains(tag?.Trim() ?? "", StringComparer.OrdinalIgnoreCase);

    /// <summary>How many of its steps say something a run could find false.</summary>
    public int Checks
    {
        get
        {
            var counted = 0;
            foreach (var step in Steps)
                if (step.Checkable)
                    counted++;

            return counted;
        }
    }

    /// <summary>
    /// Why this case is worth running, as a report prints it — or that nothing says.
    /// <para>
    /// Named <c>Sentence</c> like every other reader-facing line in this engine, and not because the
    /// name reads better: the suite's rendering catalogue pairs each of them with the case that
    /// asserts its text, and a rendering spelled anything else is a rendering the catalogue does not
    /// know to ask about.
    /// </para>
    /// </summary>
    public string Sentence()
    {
        if (!Justified)
            return $"{Name}: nothing says what this catches, so nobody can tell what deleting it would cost.";

        var filed = Filed.Length == 0 ? "" : $" [{Filed}]";
        return $"{Name}: {Catches}{filed}";
    }

    /// <summary>Every case among <paramref name="declared"/> that says nothing about why it exists.</summary>
    /// <param name="declared">The cases to count.</param>
    public static IReadOnlyList<CaseDeclaration> Unjustified(IEnumerable<CaseDeclaration> declared)
    {
        ArgumentNullException.ThrowIfNull(declared);
        return new ReadOnlyCollection<CaseDeclaration>(declared.Where(one => !one.Justified).ToList());
    }

    /// <summary>Declare one, refusing a case that could not fail.</summary>
    /// <param name="name">What to call it.</param>
    /// <param name="steps">Its steps, in order.</param>
    /// <exception cref="ScenarioRefusedException">Where it is unnamed, empty, or expects nothing.</exception>
    public static CaseDeclaration Of(string name, params StepDeclaration[] steps) =>
        Declared(name, steps);

    /// <summary>
    /// The whole door: every field a case has, judged before anything is built.
    /// </summary>
    /// <param name="name">What to call it.</param>
    /// <param name="steps">Its steps, in order.</param>
    /// <param name="tags">What selects it besides its name.</param>
    /// <param name="needs">What this machine has to have before it can observe anything.</param>
    /// <param name="catches">The defect it exists to catch.</param>
    /// <param name="filed">The task it was filed under.</param>
    /// <param name="fixture">What to launch it against. Null for the application as it comes.</param>
    /// <param name="onlyReads">That it leaves the window as it found it, so a window may be lent to it.</param>
    /// <exception cref="ScenarioRefusedException">Where any field could not run on any machine.</exception>
    public static CaseDeclaration Declared(
        string name,
        IEnumerable<StepDeclaration>? steps,
        IEnumerable<string>? tags = null,
        IEnumerable<string>? needs = null,
        string? catches = null,
        string? filed = null,
        FixtureDeclaration? fixture = null,
        bool onlyReads = false)
    {
        var called = string.IsNullOrWhiteSpace(name) ? "<unnamed case>" : name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ScenarioRefusedException(called, "a case is reported under a name, and this one has none");

        var collected = new List<StepDeclaration>();
        foreach (var step in steps ?? [])
            collected.Add(step ?? throw new ScenarioRefusedException(called, "one of its steps is nothing at all"));

        if (collected.Count == 0)
            throw new ScenarioRefusedException(called, "it has no steps, so it drives nothing and would read green forever");

        if (!collected.Exists(step => step.Checkable))
        {
            throw new ScenarioRefusedException(
                called,
                $"none of its {collected.Count} step{(collected.Count == 1 ? "" : "s")} expects anything, "
                + "so it acts and never looks and can only ever read green");
        }

        // A present-but-blank justification is worse than an absent one: it reads as answered.
        if (catches is not null && catches.Trim().Length == 0)
            throw new ScenarioRefusedException(called, "it says what it catches and then says nothing, which reads as answered");

        if (filed is not null && filed.Trim().Length == 0)
            throw new ScenarioRefusedException(called, "it names the task it was filed under and then names none");

        return new CaseDeclaration(
            called,
            new ReadOnlyCollection<StepDeclaration>(collected),
            Words(called, tags, "tag", "selects nothing"),
            Words(called, needs, "requirement", "is nothing this machine could be asked for"),
            catches?.Trim() ?? "",
            filed?.Trim() ?? "",
            fixture ?? FixtureDeclaration.Plain,
            onlyReads);
    }

    /// <summary>The same as <see cref="Declared"/>, kept for the two fields a selection reads.</summary>
    /// <param name="name">What to call it.</param>
    /// <param name="tags">What selects it besides its name.</param>
    /// <param name="steps">Its steps, in order.</param>
    /// <exception cref="ScenarioRefusedException">Where any field could not run, or a tag is declared twice.</exception>
    public static CaseDeclaration WithTags(string name, IEnumerable<string>? tags, params StepDeclaration[] steps) =>
        Declared(name, steps, tags);

    /// <summary>The one line a listing shows: the name, how much of it is a check, and what selects it.</summary>
    public override string ToString()
    {
        var tagged = Tags.Count == 0 ? "" : $" [{string.Join(" ", Tags)}]";
        var needed = Needs.Count == 0 ? "" : $" (needs {string.Join(", ", Needs)})";
        return $"{Name}: {Steps.Count} step{(Steps.Count == 1 ? "" : "s")}, {Checks} checked{tagged}{needed}";
    }

    private static IReadOnlyList<string> Words(string called, IEnumerable<string>? given, string what, string blankly)
    {
        var collected = new List<string>();
        foreach (var one in given ?? [])
        {
            if (string.IsNullOrWhiteSpace(one))
                throw new ScenarioRefusedException(called, $"one of its {what}s is blank, and a blank {what} {blankly}");

            var trimmed = one.Trim();
            if (collected.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                throw new ScenarioRefusedException(called, $"it declares the {what} '{trimmed}' twice");

            collected.Add(trimmed);
        }

        return new ReadOnlyCollection<string>(collected);
    }
}
