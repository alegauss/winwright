using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Projects;
using Winwright.Tracing;
using Winwright.Verdicts;

namespace Winwright.Asserting;

/// <summary>What asking a window whether it is still computing turned out to say.</summary>
public sealed record LoadingCheck
{
    internal LoadingCheck(IReadOnlyList<Label> watched, IReadOnlyList<Label> showing, string absence)
    {
        Watched = watched;
        Showing = showing;
        Absence = absence;
    }

    /// <summary>What this reading is called wherever it is reported.</summary>
    public const string PreconditionName = "the page has finished computing";

    /// <summary>The loading strings this project declared, resolved for the language it is in.</summary>
    public IReadOnlyList<Label> Watched { get; }

    /// <summary>The ones the window is showing right now.</summary>
    public IReadOnlyList<Label> Showing { get; }

    /// <summary>Why nothing was read, where nothing was. Empty where it was.</summary>
    public string Absence { get; }

    /// <summary>Whether the reading was taken at all.</summary>
    public bool Was => Absence.Length == 0;

    /// <summary>Whether the page has finished. False on a reading that was never taken.</summary>
    public bool Settled => Was && Showing.Count == 0;

    /// <summary>What was read, said either way.</summary>
    public string Sentence()
    {
        if (!Was)
            return $"whether the page is still computing could not be read: {Absence}.";

        if (Showing.Count == 0)
            return $"the page is showing none of the {Watched.Count} loading string(s) this project declares.";

        var named = string.Join(", ", Showing.Select(one => $"'{one.Text}' ({one.Key})"));
        return $"the page is still computing: it is showing {named}, which this project declares as loading text.";
    }

    /// <inheritdoc cref="Sentence" />
    public override string ToString() => Sentence();

    /// <summary>
    /// The result a verdict counts. A page still computing is a failure a scenario asked about —
    /// it waited and the application did not finish — while a reading that could not be taken is a
    /// hole, because nothing was observed either way.
    /// </summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult AsAssertion(string named = "the page has finished computing")
    {
        if (!Was)
            return AssertionResult.Unchecked(named, Precondition.Absent(PreconditionName, Absence));

        return Settled ? AssertionResult.Pass(named, Sentence()) : AssertionResult.Fail(named, Sentence());
    }

    /// <summary>The step a trace records.</summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public TraceStep AsTraceStep(string named = "the page has finished computing") => new()
    {
        Verb = "read whether the page is still computing",
        Locator = named,
        Pattern = "the loading strings the project declares",
        ReadBack = Was ? $"{Showing.Count} of {Watched.Count} showing" : null,
        Verdict = Verdict(),
        Detail = Settled ? null : Sentence(),
    };

    private StepVerdict Verdict()
    {
        if (!Was)
            return StepVerdict.Unchecked;

        return Settled ? StepVerdict.Ok : StepVerdict.Failed;
    }
}

/// <summary>
/// Whether a page is still saying it is loading.
/// <para>
/// WW43. Measured in claude-tray: a report on a machine with 213 recent transcript files took about
/// 25 seconds to build, and at the default wait the copy came back as a heading, a subtitle and the
/// words <em>computing your consumption pace</em>. Two variants captured that way are near-identical
/// for the same reason, so comparing them proves nothing — and it was caught only because somebody
/// looked.
/// </para>
/// <para>
/// A longer wait is the wrong answer twice: it slows every capture and still passes the page that
/// needed longer still. So the loading strings are read from the project's own language files and
/// asked of the tree instead, which is this project's rule about expectations applied to the one
/// question a capture cannot answer for itself.
/// </para>
/// <para>
/// A key none of those files carries refuses the run. <see cref="Labels.For(string, ProjectDeclaration)" />
/// already does that, and it is why the resolution goes through it rather than reading the files
/// here: a check that silently matches nothing is the shape of defect this whole path exists to
/// stop.
/// </para>
/// </summary>
public static class Loading
{
    /// <summary>
    /// How deep this walks by default. Measured against the proving ground: its loading note is
    /// eight levels down, which is what an ordinary tab strip over a stack panel costs, and
    /// <see cref="Inspect.DefaultDepth" /> would not have reached it.
    /// </summary>
    public const int Deep = 12;

    /// <summary>
    /// Ask a window whether it is showing any of the loading strings the project declares.
    /// </summary>
    /// <param name="root">The window, or the pane, to read.</param>
    /// <param name="declaration">The project, which is where the keys and the strings both live.</param>
    /// <param name="depth">
    /// How deep to walk. Deeper than <see cref="Inspect.DefaultDepth" /> on purpose: a loading note
    /// sits inside the page that is loading, and the page sits inside whatever the application nests
    /// it in — five levels is a claim about somebody else's tree shape, and a walk that stopped
    /// short would report a page as finished because it never reached the note saying otherwise.
    /// </param>
    /// <exception cref="UnusableLabelException">
    /// Where a declared key is in none of the project's language files, or reads as a placeholder.
    /// Both are checks that could never match, and a check that could never match is one that will
    /// report a page as finished forever.
    /// </exception>
    public static LoadingCheck In(AutomationElement root, ProjectDeclaration declaration, int depth = Deep)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(declaration);

        if (!declaration.Declares("loading"))
        {
            return new LoadingCheck(
                [], [], $"{declaration.Path} declares no loading strings, so nothing here knows what "
                    + "this application says while it is still computing");
        }

        // Resolved before the tree is read, and outside the try: a key the project cannot resolve
        // is a scenario that is wrong, not a page that is loading, and swallowing it here would
        // turn an unanswerable check into a page that has finished.
        var watched = declaration.Loading.Select(key => Labels.For(key, declaration)).ToList();

        List<string> texts;
        try
        {
            var tree = Inspect.Under(root, depth);
            if (tree is null)
            {
                return new LoadingCheck(
                    watched, [], "the window has no control view to read, so nothing it is showing can be seen");
            }

            texts = tree
                .Walk()
                .Select(one => one.Facts.Name)
                .Where(one => one.Length > 0)
                .ToList();
        }
        catch (Exception gone)
            when (gone is ElementNotAvailableException or InvalidOperationException)
        {
            return new LoadingCheck(watched, [], $"the window went away while it was being read: {gone.Message}");
        }

        var showing = watched
            .Where(one => texts.Any(text => text.Contains(one.Text, StringComparison.Ordinal)))
            .ToList();

        return new LoadingCheck(watched, showing, "");
    }
}
