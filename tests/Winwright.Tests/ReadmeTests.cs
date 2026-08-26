using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW154. There was no README, and that was not a documentation preference. The shipping rule runs
/// a decision on every task: would an adopter do something differently because this shipped, and if
/// so, hit the surfaces that exist — naming the README's feature list first. That clause had been
/// answered by the surface not existing, every time, for every task.
/// <para>
/// So the file exists, and these hold it. Not a spell-check: what is asserted is that the things an
/// adopter acts on — the exit codes, the packages, the families of verbs, the keys a project
/// declares — are named there, read out of the engine rather than out of the file. A code added
/// later, or a verb family, fails here until the README says so, which is the gate the design asked
/// for stated as arithmetic rather than as a habit.
/// </para>
/// </summary>
public sealed class ReadmeTests
{
    /// <summary>The file, read once. It is at the root, which is where an adopter looks.</summary>
    private static string Text()
    {
        var path = Checkout.At("README.md");
        Assert.True(File.Exists(path), $"there is nothing at {path} for an adopter to read");
        return File.ReadAllText(path);
    }

    [Fact]
    public void Every_exit_code_a_run_can_leave_behind_is_written_down_with_its_number()
    {
        // The member values are the exit codes, and CI reads the number rather than the word. A
        // reader who has to guess which number means "could not be evaluated" is the reader this
        // whole project is about.
        var said = Text();

        foreach (var outcome in Enum.GetValues<RunOutcome>())
        {
            Assert.Contains($"`{(int)outcome}`", said, StringComparison.Ordinal);
            Assert.Contains($"`{outcome}`", said, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_third_verdict_is_explained_and_not_merely_listed()
    {
        // The one an adopter has never met before. A table row naming it is not the same as a
        // sentence saying why it is not a pass and not a failure.
        var said = Text();

        Assert.Contains("never ran", said, StringComparison.Ordinal);
        Assert.Contains("named in the summary", said, StringComparison.Ordinal);
    }

    [Fact]
    public void Both_packages_are_named_with_the_version_the_tree_declares()
    {
        // The two ids and the version an adopter would pin. Read off the props rather than typed,
        // so a release that bumps the version and forgets this file fails here.
        var said = Text();
        var declared = Winwright.Projects.Engine.Declared("the tree", Props());

        Assert.True(declared.Pins, declared.ToString());
        Assert.Contains($"""<PackageReference Include="Winwright" Version="{declared.Version}" />""", said, StringComparison.Ordinal);
        Assert.Contains($"""<PackageReference Include="Winwright.InApp" Version="{declared.Version}" />""", said, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_family_of_verbs_the_engine_offers_is_named()
    {
        // Derived from the catalogue the suite already checks against the engine in both
        // directions, so a family added later reaches this file or fails here. Families and not
        // verbs: seventy rows is a reference, and a reader meets a reference after a README.
        var said = Text();
        var families = Cooperating.Known
            .Select(one => one.Named.Split('.')[0])
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.True(families.Count > 10, $"only {families.Count} famil(ies) were catalogued");
        Assert.All(families, family => Assert.Contains($"`{family}`", said, StringComparison.Ordinal));
    }

    [Fact]
    public void Every_key_a_project_can_declare_is_shown_in_the_example()
    {
        // An adopter writes this file by copying the block. A key that is not in the block is a key
        // nobody uses, which is the same as a key that does not exist.
        var said = Text();

        foreach (var key in new[]
        {
            "executable", "sourceRoot", "sourceIgnore", "fingerprintStore",
            "languageFiles", "language", "timeouts", "attempts", "destructive",
        })
        {
            Assert.Contains($"\"{key}\"", said, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Every_non_goal_the_roadmap_declares_is_said_out_loud()
    {
        // What this tool will not do is the half an adopter needs before they build on it, and it
        // is the half nobody writes down. Read off the governed file rather than retyped.
        var said = Text();

        foreach (var refused in NonGoals())
            Assert.Contains(refused, said, StringComparison.Ordinal);
    }

    [Fact]
    public void What_is_not_built_yet_is_said_where_an_adopter_will_look()
    {
        // Written against what shipped rather than against what is planned. This used to assert
        // that the README said there was no scenario file, which was the honest sentence until
        // WW58 wrote one — and the sentence a section is checked by has to move when the section
        // does, or the check outlives what it was about.
        var said = Text();

        Assert.Contains("Not built yet", said, StringComparison.Ordinal);
        Assert.Contains("A case runs; a suite does not", said, StringComparison.Ordinal);
    }

    [Fact]
    public void The_scenario_format_an_adopter_copies_is_the_format_the_loader_reads()
    {
        // WW58. An adopter writes their first case by copying the block. A field or a vocabulary
        // that is not in it is one nobody uses, and one that is in it and wrong is a refusal on
        // their first run — so both lists are read off the schema rather than retyped here.
        var said = Text();

        var every = Winwright.Scenarios.ScenarioSchema.File
            .Concat(Winwright.Scenarios.ScenarioSchema.Case)
            .Concat(Winwright.Scenarios.ScenarioSchema.Step)
            .Concat(Winwright.Scenarios.ScenarioSchema.Fixture);

        foreach (var field in every)
            Assert.Contains($"`{field.Name}`", said, StringComparison.Ordinal);

        foreach (var verb in Winwright.Scenarios.ActVerb.All)
            Assert.Contains($"`{verb.Name}`", said, StringComparison.Ordinal);

        foreach (var reading in Winwright.Scenarios.ReadBack.All)
            Assert.Contains($"`{reading.Name}`", said, StringComparison.Ordinal);

        // And the example really is one the loader accepts, taken out of the file rather than
        // written twice: a block that does not load is worse than no block.
        var example = Fenced(said, """{"cases":""");
        Assert.Single(Winwright.Scenarios.ScenarioFile.Read("README.md", example));
    }

    /// <summary>The first fenced block whose text, with the spaces out, starts with <paramref name="opening"/>.</summary>
    private static string Fenced(string said, string opening)
    {
        var lines = said.Split('\n');
        var block = new List<string>();
        var inside = false;
        foreach (var line in lines)
        {
            if (line.TrimEnd('\r').StartsWith("```", StringComparison.Ordinal))
            {
                if (inside)
                {
                    var text = string.Join('\n', block);
                    if (Bare(text).StartsWith(Bare(opening), StringComparison.Ordinal))
                        return text;

                    block.Clear();
                }

                inside = !inside;
                continue;
            }

            if (inside)
                block.Add(line.TrimEnd('\r'));
        }

        Assert.Fail($"the README shows no fenced block starting {opening}");
        return "";
    }

    private static string Bare(string text) => text.Replace(" ", "").Replace("\n", "").Replace("\r", "");

    /// <summary>The non-goals, out of the roadmap that governs them.</summary>
    private static IReadOnlyList<string> NonGoals()
    {
        var roadmap = File.ReadAllLines(Checkout.At("docs", "ROADMAP.md"));

        var reading = false;
        var found = new List<string>();
        foreach (var line in roadmap)
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                reading = line.Contains("Non-goals", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            var bullet = line.TrimStart();
            if (!reading || !bullet.StartsWith("- **", StringComparison.Ordinal))
                continue;

            // The bold lead alone. The reason after it is the roadmap's own prose, wrapped over
            // several lines, and a README repeating it word for word would be a second copy of it.
            var ends = bullet.IndexOf("**", 4, StringComparison.Ordinal);
            if (ends > 4)
                found.Add(bullet[4..ends]);
        }

        Assert.NotEmpty(found);
        return found;
    }

    private static string Props() => Checkout.At("Directory.Build.props");
}
