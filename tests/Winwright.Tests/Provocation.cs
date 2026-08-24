using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

namespace Winwright.Tests;

/// <summary>Why a refusal needs no shape from the fixture, where it needs none.</summary>
internal enum Without
{
    /// <summary>
    /// It needs nothing the fixture can be: a string, a file, an argument, or a window a case
    /// builds for itself. WW146 widened this from <em>no window at all</em>, which several entries
    /// under it never claimed — two elements matching one step is a tree, and a hand-built window
    /// is a tree.
    /// </summary>
    NoShape,

    /// <summary>
    /// It needs a shape the fixture cannot take. The bucket that must not grow, and WW146 emptied
    /// it: three refusals got a shape apiece and the fourth turned out to belong above. It is kept
    /// rather than deleted because the reading it makes possible — <em>how many refusals can
    /// nothing reach</em> — is the one this block wanted, and it is only worth anything if the
    /// answer of nothing is a measurement rather than an absence.
    /// </summary>
    NotYet,
}

/// <summary>
/// One refusal, against the flag that provokes it or the reason none can.
/// </summary>
/// <param name="Refusal">The exception type, by name.</param>
/// <param name="Flag">The fixture flag that reaches it, or empty.</param>
/// <param name="Why">Where no flag reaches it, which of the two reasons that is.</param>
/// <param name="Because">The sentence a reader needs, in either case.</param>
/// <param name="Case">
/// The case that provokes it, as <c>Class.Method</c>, where no flag does. Empty on an entry a flag
/// reaches, which has its own case in <see cref="ProvokedByFlagTests" />.
/// <para>
/// WW160. Twelve entries said some version of <em>a case builds this</em> and nothing anywhere
/// asserted that such a case existed. The suite did contain most of them, which is exactly what
/// made the gap quiet: the entries were true when they were written, and an entry whose case
/// somebody deleted reads identically to one whose case still runs.
/// </para>
/// </param>
internal sealed record Provoked(string Refusal, string Flag, Without? Why, string Because, string Case = "")
{
    /// <summary>Whether driving the fixture can reach it at all.</summary>
    public bool ThroughTheFixture => Flag.Length > 0;

    /// <summary>
    /// The one line the pairing prints. WW160 put the case on it: naming what provokes a refusal
    /// and then not saying where it is leaves a reader to grep for the type, which is the reading
    /// they came here to be spared.
    /// </summary>
    public override string ToString() => ThroughTheFixture
        ? $"{Refusal,-28} --{Flag}: {Because}"
        : $"{Refusal,-28} (no flag, {Phrase(Why!.Value)}): {Because}"
            + (Case.Length == 0 ? "" : $" [{Case}]");

    /// <summary>
    /// The reason in words rather than as a member name. Written out because the two are read by a
    /// person: <em>nothingdrawn</em> was both ugly and, after WW146, untrue of half the list.
    /// </summary>
    private static string Phrase(Without why) => why switch
    {
        Without.NoShape => "nothing the fixture can be",
        _ => "a shape the fixture cannot take",
    };
}

/// <summary>
/// Every refusal this framework names, paired with what provokes it.
/// <para>
/// WW132. The framework names nineteen refusals and four of them were reached by driving the
/// fixture. The rest are asserted against hand-built windows, against arguments passed in a test,
/// or not at all — and the catalogue and the exception types were two lists nobody compared, so a
/// refusal added later started unprovokable and stayed that way.
/// </para>
/// <para>
/// Some need no shape and saying so is the point: a locator that does not parse is a string, and no
/// window has to exist for it. Four needed one the fixture could not take, and WW132 named them as
/// such rather than leaving them off — this framework's value is concentrated in its refusals, and
/// a refusal nobody can provoke is one that will quietly stop working.
/// </para>
/// <para>
/// WW146 closed that bucket. Three of the four got a shape apiece — a page that lays out to
/// nothing, a page that paints nothing, a render with no application above it — and the fourth was
/// moved up: a receipt about the wrong window is the harness handing over the wrong handle rather
/// than an application misbehaving, so a case builds it and no flag ever will.
/// </para>
/// <para>
/// The pairing is checked rather than kept: the assemblies are read for what they name, and an
/// entry here that no longer matches a type, or a type with no entry, is a red.
/// </para>
/// </summary>
internal static class Provocation
{
    /// <summary>
    /// The two halves whose refusals this pairing is about. The fixture is not among them and
    /// cannot be: the suite references it without its assembly on purpose, because an application
    /// under test is launched from its own output rather than read from beside the harness. Its own
    /// refusal about an unknown flag is provoked by running it, which is where it belongs.
    /// </summary>
    public static IReadOnlyList<Assembly> Assemblies { get; } = new ReadOnlyCollection<Assembly>(
    [
        typeof(Winwright.Locating.Subject).Assembly,
        typeof(Winwright.InApp.Coordinates).Assembly,
    ]);

    /// <summary>Every refusal, paired.</summary>
    public static IReadOnlyList<Provoked> Known { get; } = new ReadOnlyCollection<Provoked>(
    [
        new("AnotherInstanceException", "title", null,
            "a second window of the same application under another title is what the instance check counts"),
        new("UnusableLabelException", "language", null,
            "the fixture's own strings carry one key whose value is a placeholder no exact read can match"),
        new("StoreTouchedException", "mutate", null,
            "a settings file rewritten to the same length, which is the accident the fingerprint exists for"),
        new("ApartmentTimeoutException", "pump", null,
            "a window whose dispatcher never runs, so work handed to it is never taken up"),
        new("UnrenderableException", "sizeless", null,
            "a page whose every row is collapsed lays out to nothing, and an empty file is a successful render"),
        new("BlankPictureException", "blank", null,
            "a page the right size painting nothing, rendered on no background, which is a picture nothing drew"),
        new("NoBackgroundException", "unbacked", null,
            "a render before the application exists, so neither the theme nor a window says what to draw it on"),

        new("LocatorSyntaxException", "", Without.NoShape,
            "a locator that does not parse is a string, and no window has to exist for one",
            "LocatorTests.A_control_type_ui_automation_does_not_have_is_refused_with_the_nearest_words"),
        new("AmbiguousLocatorException", "", Without.NoShape,
            "two elements matching one step is a tree, and a hand-built window is a tree",
            "MatchOrderTests.Two_elements_with_the_same_name_are_refused_rather_than_guessed_between"),
        new("AttachFailedException", "", Without.NoShape,
            "attaching to a process that is not there needs no process, which is the whole point of it",
            "AppTargetTests.A_pid_nothing_is_running_as_is_refused_rather_than_run_against_something_else"),
        new("DeclarationMissingException", "", Without.NoShape,
            "a project that declares nothing is a file, and the refusal is about the file",
            "ProjectDeclarationTests.What_the_project_never_declared_is_refused_by_name"),
        new("UnreadableTraceException", "", Without.NoShape,
            "a trace that is not a trace is a file, and reading one back is what this is about",
            "TraceTests.A_blank_line_from_a_truncated_run_is_skipped_and_a_broken_one_is_not"),
        new("UnearnedGreenException", "", Without.NoShape,
            "a verdict assembled wrongly is arithmetic on results, with no window anywhere in it",
            "CoverageTests.One_assertion_that_never_ran_takes_the_word_away"),
        new("UnderivableSetException", "", Without.NoShape,
            "a strings file declaring nothing under a key is a file, and the refusal is read off it",
            "DerivedSetTests.A_key_that_declares_nothing_is_refused_rather_than_deriving_an_empty_set"),
        new("NotActionableException", "", Without.NoShape,
            "an element disabled, offscreen or without the pattern is a control, and a hand-built window has controls",
            "ActionabilityTests.The_refusal_carries_the_locator_and_which_of_the_four_it_was"),
        new("DestructiveEntryException", "", Without.NoShape,
            "a declared entry against a control by that name, both of which a test builds",
            "DestructiveEntryTests.Invoke_refuses_a_declared_entry_and_the_refusal_names_it"),
        new("ScenarioRefusedException", "", Without.NoShape,
            "a declared act whose control carries no such pattern, which is a preflight over a tree",
            "AssertionDeclarationTests.An_assertion_that_names_nothing_to_observe_is_refused_at_load"),
        new("ThreadBoundException", "", Without.NoShape,
            "a brush made on one thread and read from another, which needs two threads and no window",
            "FreezablesTests.A_brush_from_another_thread_is_refused_for_a_reason_about_threading"),

        // WW146 moved this one up rather than inventing a shape for it. A receipt about the wrong
        // window is the harness handing over the wrong handle, not an application misbehaving, and
        // a fixture that lied about which window it owns would be reproducing the harness's bug in
        // the thing the harness is pointed at.
        new("WrongCaptureException", "", Without.NoShape,
            "a receipt composed over a window and a target a case hands it, and it already builds both. "
                + "WW188: this type is five refusals, and an entry here holds a flag or a reason and never "
                + "both — so the arms are paired one by one in CaptureArms, where two of them name the "
                + "fixture shape that provokes them",
            "CaptureReceiptTests.A_picture_of_somebody_elses_window_is_refused_and_names_both_processes"),
    ]);

    /// <summary>One flag's name out of the line the catalogue prints for it.</summary>
    private static string FlagName(string line)
    {
        var body = line[2..];
        var ends = body.IndexOfAny([' ', '=']);
        return ends < 0 ? body : body[..ends];
    }

    /// <summary>The refusals the assemblies actually name, by type name.</summary>
    public static IReadOnlyList<string> Named() => new ReadOnlyCollection<string>(
        Assemblies
            .SelectMany(one => one.GetExportedTypes())
            .Where(one => typeof(Exception).IsAssignableFrom(one) && !one.IsAbstract)
            .Select(one => one.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(one => one, StringComparer.Ordinal)
            .ToList());

    /// <summary>The ones the fixture can reach.</summary>
    public static IReadOnlyList<Provoked> Reachable() => new ReadOnlyCollection<Provoked>(
        Known.Where(one => one.ThroughTheFixture).ToList());

    /// <summary>The ones that need a shape the fixture cannot take.</summary>
    public static IReadOnlyList<Provoked> Unreachable() => new ReadOnlyCollection<Provoked>(
        Known.Where(one => one.Why == Without.NotYet).ToList());

    /// <summary>The ones a case in this suite provokes rather than a flag.</summary>
    public static IReadOnlyList<Provoked> ByACase() => new ReadOnlyCollection<Provoked>(
        Known.Where(one => one.Why == Without.NoShape).ToList());

    /// <summary>
    /// The case an entry names, found in this suite, or null where nothing here is called that.
    /// <para>
    /// WW160. Read out of the assembly rather than believed: a name written down is the claim, and
    /// the claim is what went unchecked for twelve entries. A case somebody renamed or deleted
    /// stops being found here, which is the whole point — the alternative is a pairing that goes on
    /// asserting coverage nothing provides.
    /// </para>
    /// </summary>
    /// <param name="named">The case, as <c>Class.Method</c>.</param>
    public static MethodInfo? CaseNamed(string named)
    {
        var split = named.LastIndexOf('.');
        if (split <= 0 || split == named.Length - 1)
            return null;

        var owner = typeof(Provocation).Assembly
            .GetTypes()
            .FirstOrDefault(one => string.Equals(one.Name, named[..split], StringComparison.Ordinal));

        return owner?
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .FirstOrDefault(one => string.Equals(one.Name, named[(split + 1)..], StringComparison.Ordinal));
    }

    /// <summary>Whether that case is one the runner would actually execute.</summary>
    /// <param name="method">The case, as <see cref="CaseNamed" /> found it.</param>
    public static bool IsACase(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);
        return method.GetCustomAttributes(inherit: true)
            .Any(one => one is Xunit.FactAttribute or Xunit.TheoryAttribute);
    }

    /// <summary>
    /// The flags the fixture really has, read out of the article rather than out of a reference.
    /// <para>
    /// The suite cannot see the fixture's types, so the catalogue is asked for the way a person
    /// asks for it — which is the better reading anyway: it is what the built fixture says about
    /// itself, not what a compile-time reference would have said.
    /// </para>
    /// </summary>
    /// <param name="fixture">The built fixture.</param>
    public static IReadOnlyList<string> FlagsOf(string fixture)
    {
        var start = new ProcessStartInfo(fixture) { RedirectStandardOutput = true, UseShellExecute = false };
        start.ArgumentList.Add("--flags");

        using var running = Process.Start(start)!;
        var said = running.StandardOutput.ReadToEnd();
        running.WaitForExit(30_000);

        return new ReadOnlyCollection<string>(said
            .Split('\n')
            .Select(one => one.Trim())
            .Where(one => one.StartsWith("--", StringComparison.Ordinal))
            .Select(FlagName)
            .Where(one => one.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList());
    }

    /// <summary>The pairing as a person reads it, the count first.</summary>
    public static IReadOnlyList<string> Render() => new ReadOnlyCollection<string>(
        [
            $"{Known.Count} refusals: {Reachable().Count} reachable by driving the fixture, "
                + $"{Known.Count(one => one.Why == Without.NoShape)} needing nothing the fixture can be, "
                + $"{Unreachable().Count} needing a shape it cannot take.",
            .. Known.Select(one => $"  {one}"),
        ]);
}
