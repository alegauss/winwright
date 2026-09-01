using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Winwright.Tests;

/// <summary>Why a type the fixture carries is not a shape a case reaches for.</summary>
internal enum Carrying
{
    /// <summary>It is a shape, reached through the flag named beside it.</summary>
    AShape,

    /// <summary>
    /// It is what a value flag draws where none of the awkward shapes beside it was asked for — the
    /// plain one, reached by the flag opening the route rather than by a test naming this shape.
    /// <para>
    /// One arm and not a looser rule. <c>FixedPane</c> is reached from a member testing
    /// <c>blank</c> and <c>sizeless</c>, which is the code saying <em>neither of those</em>; the
    /// flag that decides whether any of it runs is tested higher up the same file. Widening the
    /// check to accept a flag from anywhere in the file would have let every shape claim any flag,
    /// so the widening is named here and paid for only by the shapes that need it.
    /// </para>
    /// </summary>
    TheDefaultRoute,

    /// <summary>It is how the fixture starts, parses what it was asked for, or talks to a harness —
    /// present on every run, reached by nothing, and justified by the fixture existing at all.</summary>
    ThePlumbing,
}

/// <summary>One type the fixture carries, and the flag whose reason justifies it.</summary>
/// <param name="Named">The type, which in this tree is its file's name.</param>
/// <param name="Kind">Whether it is a shape or the plumbing under them.</param>
/// <param name="Flag">The flag that reaches it, without the dashes. Empty for plumbing.</param>
/// <param name="Because">The sentence a reader needs, for plumbing.</param>
internal sealed record FixtureShape(string Named, Carrying Kind, string Flag = "", string Because = "")
{
    public override string ToString() => Kind == Carrying.AShape
        ? $"{Named,-14} --{Flag}"
        : $"{Named,-14} (the plumbing): {Because}";
}

/// <summary>
/// WW212, and it is what was left after the premise was wrong. Block K's criterion says every shape
/// the fixture carries names the real defect it reproduces, and a shape that can name none is
/// removed rather than maintained forever.
/// <para>
/// That is held, and by more than habit. <c>Flag</c> carries a <c>Because</c> beside what it does;
/// <c>FixtureTests.Every_shape_the_fixture_carries_names_the_defect_it_reproduces</c> runs the built
/// fixture with <c>--flags</c> and asserts the shapes and the reasons come out equal, so a shape
/// cannot be added without one; and the case beside it refuses a reason that is the description
/// again. The first reading of this task measured the doc comments on the pane classes instead,
/// found eight of eleven named no task, and filed a gap that was not there.
/// </para>
/// <para>
/// What was left is the link. The reason lives on the flag and the shape is a class, and nothing
/// joined them. Eleven classes are each reached from flag-gated code because somebody did it right,
/// not because anything reads it — and a pane wired into the window unconditionally would draw on
/// every run, carry no reason, and look exactly like one that has held since WW146.
/// </para>
/// <para>
/// So both ways, over the types rather than over a list: every type the fixture carries is either a
/// shape naming the flag that justifies it, or the plumbing saying why it is not one.
/// </para>
/// </summary>
internal static class Surfaces
{
    /// <summary>Where the fixture's sources are.</summary>
    internal static string Tree => Path.Combine(Checkout.Engine, "Winwright.Fixture");

    /// <summary>Every type it carries, paired with what justifies it.</summary>
    internal static IReadOnlyList<FixtureShape> Known { get; } = new ReadOnlyCollection<FixtureShape>(
    [
        new("AbsencesPane", Carrying.AShape, "absences"),
        new("Animation", Carrying.AShape, "animate"),
        new("AnnouncesPane", Carrying.AShape, "announces"),
        new("Backdrop", Carrying.AShape, "backdrop"),
        new("BlankPane", Carrying.AShape, "blank"),
        new("Cloak", Carrying.AShape, "cloak"),
        new("Intruder", Carrying.AShape, "intrude"),
        new("NamesPane", Carrying.AShape, "names"),
        new("Peerless", Carrying.AShape, "peerless"),
        new("PickersPane", Carrying.AShape, "pickers"),
        new("RowsPane", Carrying.AShape, "rows"),
        new("RangesPane", Carrying.AShape, "ranges"),
        new("SizelessPane", Carrying.AShape, "sizeless"),
        new("Store", Carrying.AShape, "store"),
        new("Strings", Carrying.AShape, "language"),
        new("Toast", Carrying.AShape, "toast"),

        // The plain pane a render is taken of where nothing awkward was asked for, so the reason on
        // --render is the reason it is drawn. Filed as a shape on the first attempt and the check
        // said otherwise in one run: the code reaching it tests blank and sizeless, which is where
        // this arm came from rather than from wanting the entry to pass.
        new("FixedPane", Carrying.TheDefaultRoute, "render"),

        new("Program", Carrying.ThePlumbing,
            Because: "the entry point, which is what reads the flags every shape below is reached "
                + "through — it cannot be behind one of them"),
        new("App", Carrying.ThePlumbing,
            Because: "the presentation application object, present before any flag has been looked at"),
        new("MainWindow", Carrying.ThePlumbing,
            Because: "the window itself. Every run that draws anything draws this, and the shapes "
                + "are what go on it"),
        new("Flags", Carrying.ThePlumbing,
            Because: "the flags, which is where the reasons are kept — a shape justified by a flag "
                + "cannot be justified by the type declaring the flag"),
        new("Arrivals", Carrying.ThePlumbing,
            Because: "what Windows delivers to the window, recorded on every run and behind no flag "
                + "— WW249's flake cannot be provoked on purpose, so the reading has to be running "
                + "already, and what it reads is a property of the window rather than of any shape"),
        new("Protocol", Carrying.ThePlumbing,
            Because: "what the fixture and a harness agree on: the channels a run is asked to write "
                + "to and the background a capture is drawn against, read on every run"),
    ]);

    /// <summary>Every type the fixture carries, read out of its sources.</summary>
    internal static IReadOnlyList<string> Carried() => carried.Value;

    /// <summary>The flags a member naming each type also tests, which is what gates it.</summary>
    internal static IReadOnlyDictionary<string, IReadOnlyList<string>> Gating() => gating.Value;

    /// <summary>
    /// The same widened to the whole file, which only <see cref="Carrying.TheDefaultRoute" /> may
    /// be checked against — a value flag opens the route at the top and the default is taken far
    /// below it, in a member that names the alternatives instead.
    /// </summary>
    internal static IReadOnlyDictionary<string, IReadOnlyList<string>> GatingInFile() => inFile.Value;

    /// <summary>
    /// Every flag the built fixture declares, with the reason it prints beside it.
    /// <para>
    /// Read out of the running fixture and not out of its source, and not by calling into it: this
    /// suite references that project with <c>ReferenceOutputAssembly="false"</c> on purpose, because
    /// the fixture is a real application here and not a library. So the flags arrive the way an
    /// adopter would meet them — by asking it — which is also the reading WW122 already checks the
    /// count of reasons against.
    /// </para>
    /// </summary>
    internal static IReadOnlyDictionary<string, string> Declared() => declared.Value;

    private static readonly Lazy<IReadOnlyDictionary<string, string>> declared = new(Ask);

    private static IReadOnlyDictionary<string, string> Ask()
    {
        var start = new System.Diagnostics.ProcessStartInfo(Fixture.Executable())
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };

        start.ArgumentList.Add("--flags");

        using var running = System.Diagnostics.Process.Start(start)!;
        var said = running.StandardOutput.ReadToEnd();
        running.WaitForExit(30_000);

        var found = new Dictionary<string, string>(StringComparer.Ordinal);
        var name = "";
        foreach (var line in said.Split('\n').Select(one => one.TrimEnd('\r').Trim()))
        {
            if (line.StartsWith("--", StringComparison.Ordinal))
            {
                var end = line.IndexOfAny([' ', '=']);
                name = end < 0 ? line[2..] : line[2..end];
                found[name] = "";
            }
            else if (name.Length > 0 && line.StartsWith("because ", StringComparison.Ordinal))
            {
                found[name] = line["because ".Length..];
            }
        }

        return found;
    }

    /// <summary>The reading a person gets: the count first, then a line each.</summary>
    internal static IReadOnlyList<string> Render() => new ReadOnlyCollection<string>(
    [
        $"{Carried().Count} type(s) the fixture carries: "
            + $"{Known.Count(one => one.Kind == Carrying.AShape)} shapes each reached through a flag "
            + $"that says why it is here, and {Known.Count(one => one.Kind == Carrying.ThePlumbing)} "
            + "under them",
        .. Known.Select(one => $"  {one}"),
    ]);

    /// <summary>
    /// A flag being tested, as the fixture's own code spells it. Kept with its name, which is the
    /// half a reading over <see cref="Checkout.Code" /> would have deleted — and did, on the first
    /// attempt, which then reported every shape in the fixture as gated by nothing.
    /// </summary>
    private static readonly Regex Tested = new(
        "(?:Shapes|shapes|flags)\\.(?:Has|Value)\\(\\s*\"([a-z-]+)\"",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(5));

    private static readonly Lazy<IReadOnlyList<string>> carried = new(Sweep);

    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>> gating =
        new(() => Gates(whole: false));

    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>> inFile =
        new(() => Gates(whole: true));

    /// <summary>
    /// Everything hand-written under the fixture. What a build generated is left out by name rather
    /// than by folder: the designer's partial for the window lands beside the source in <c>obj</c>,
    /// which <see cref="Checkout.Written" /> already skips, while the assembly attributes do not.
    /// </summary>
    private static IReadOnlyList<string> Sweep() => Checkout.SourcesIn(Tree)
        .Select(one => Path.GetFileNameWithoutExtension(one))
        .Where(one => !one.Contains(".g", StringComparison.Ordinal))
        .Where(one => !one.EndsWith("AssemblyInfo", StringComparison.Ordinal))
        .Where(one => !one.EndsWith("GlobalUsings", StringComparison.Ordinal))
        .Where(one => !one.EndsWith("AssemblyAttributes", StringComparison.Ordinal))
        .Select(one => one.Replace(".xaml", "", StringComparison.Ordinal))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(one => one, StringComparer.Ordinal)
        .ToList();

    /// <param name="whole">
    /// Whether a flag counts from anywhere in the file that reaches the type, rather than only from
    /// the member that names it. The narrow reading is the rule; this is what the one declared arm
    /// above is measured against.
    /// </param>
    private static IReadOnlyDictionary<string, IReadOnlyList<string>> Gates(bool whole)
    {
        var found = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var members = Checkout.SourcesIn(Tree)
            .SelectMany(one => Checkout.Members(one, Checkout.Spoken))
            .ToList();

        foreach (var type in Carried())
        {
            // Named from somewhere other than its own file, which is where a shape is reached from.
            // A type that only ever names itself is reached by nobody and has no gate to find.
            var elsewhere = members.Where(one =>
                !string.Equals(one.Owner, type, StringComparison.Ordinal)
                && !string.Equals(one.Owner, $"{type}.xaml", StringComparison.Ordinal));

            var naming = elsewhere.Where(one => one.Body.Contains($"{type}.", StringComparison.Ordinal)).ToList();
            var reading = whole
                ? elsewhere.Where(one => naming.Exists(each =>
                    string.Equals(each.Owner, one.Owner, StringComparison.Ordinal)))
                : naming;

            found[type] = reading
                .SelectMany(one => Tested.Matches(one.Body).Select(each => each.Groups[1].Value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(one => one, StringComparer.Ordinal)
                .ToList();
        }

        return found;
    }
}
