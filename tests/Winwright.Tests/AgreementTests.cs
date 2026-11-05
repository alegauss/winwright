using Winwright.Projects;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW70. Three copies of the engine can be in play and quietly disagree — and a stale copy does
/// not fail, it agrees with a rule that has moved.
/// <para>
/// The last two tests are this repository read against itself: the assembly that is running and
/// the tree that declares its version have to be one answer, or the reading is a rule nothing in
/// here obeys.
/// </para>
/// </summary>
public sealed class AgreementTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-agreement-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private string Project(string name, string xml)
    {
        var path = Path.Combine(root, name);
        File.WriteAllText(path, xml);
        return path;
    }

    private static EngineCopy Pinned(string where, string version) =>
        new(where, version, Pinning.Pinned);

    [Fact]
    public void Copies_naming_one_version_agree_and_the_gate_exits_zero()
    {
        var read = Agreement.Between(Pinned("the plugin", "0.4.1"), Pinned("the assembly", "0.4.1"));

        Assert.True(read.Agreed);
        Assert.Equal(Concord.Agreed, read.Verdict);
        Assert.Equal(0, read.ExitCode);
        Assert.Equal("all 2 copies of the engine are 0.4.1.", read.Sentence());
    }

    [Fact]
    public void A_stale_copy_is_a_refusal_rather_than_a_quiet_agreement()
    {
        var read = Agreement.Between(
            Pinned("the plugin", "0.4.1"), Pinned("continuous integration", "0.4.1"), Pinned("the assembly", "0.3.0"));

        Assert.Equal(Concord.Behind, read.Verdict);
        Assert.Equal(1, read.ExitCode);
        Assert.Contains("the assembly is 0.3.0", read.Sentence());
        Assert.Contains("agrees with a rule that has moved", read.Sentence());
    }

    [Fact]
    public void The_same_version_spelled_two_ways_is_one_version()
    {
        var read = Agreement.Between(Pinned("the plugin", "1.0"), Pinned("the assembly", "1.0.0.0"));

        Assert.True(read.Agreed);
        Assert.Single(read.Versions);
    }

    [Fact]
    public void The_build_metadata_the_sdk_appends_is_not_part_of_the_version()
    {
        // Without this every built copy disagrees with the file that declared it, which would make
        // the whole reading go red on a repository that is perfectly in step with itself.
        var read = Agreement.Between(Pinned("the source tree", "0.1.0"), Pinned("the assembly", "0.1.0+2b41f9c"));

        Assert.True(read.Agreed);
    }

    [Fact]
    public void One_copy_that_cannot_be_pinned_makes_the_whole_reading_unpinnable()
    {
        var read = Agreement.Between(
            Pinned("the plugin", "0.4.1"),
            Pinned("continuous integration", "0.4.1"),
            new EngineCopy("the consuming project", null, Pinning.Unpinnable, "it references it by path"));

        // Not agreement between the two that happened to be readable: 'behind' is a claim about a
        // known distance, and no such claim survives one copy that could be anything.
        Assert.Equal(Concord.Unpinnable, read.Verdict);
        Assert.Equal(1, read.ExitCode);
        Assert.Contains("what could be read is 0.4.1", read.Sentence());
    }

    [Fact]
    public void An_unpinnable_reading_still_names_the_disagreement_it_can_see()
    {
        var read = Agreement.Between(
            Pinned("the plugin", "0.4.1"),
            Pinned("the assembly", "0.3.0"),
            new EngineCopy("the consuming project", null, Pinning.Absent, "it references nothing"));

        Assert.Equal(Concord.Unpinnable, read.Verdict);
        Assert.Contains("already disagree: 0.4.1, 0.3.0", read.Sentence());
    }

    [Fact]
    public void One_copy_is_refused_because_it_agrees_with_itself_everywhere()
    {
        var refused = Assert.Throws<ArgumentException>(() => Agreement.Between(Pinned("the assembly", "0.4.1")));

        Assert.Contains("one copy agrees with itself on every machine there is", refused.Message);
    }

    [Fact]
    public void A_tree_that_declares_a_version_is_read_out_of_its_props()
    {
        var props = Project(
            "Directory.Build.props",
            "<Project><PropertyGroup><Version>2.3.4</Version></PropertyGroup></Project>");

        var copy = Engine.Declared("the source tree", props);

        Assert.True(copy.Pins);
        Assert.Equal("2.3.4", copy.Version);
    }

    [Fact]
    public void A_tree_that_declares_none_is_absent_rather_than_the_one_the_build_invents()
    {
        var props = Project("bare.props", "<Project><PropertyGroup><Nullable>enable</Nullable></PropertyGroup></Project>");

        var copy = Engine.Declared("the source tree", props);

        Assert.Equal(Pinning.Absent, copy.Pinning);
        Assert.Contains("declares no <Version>", copy.Because);
    }

    [Fact]
    public void A_package_reference_with_a_range_is_a_promise_and_not_a_version()
    {
        var project = Project(
            "consumer.csproj",
            """<Project><ItemGroup><PackageReference Include="Winwright" Version="[1.0,2.0)" /></ItemGroup></Project>""");

        var copy = Engine.Pinned("the consuming project", project);

        Assert.Equal(Pinning.Unpinnable, copy.Pinning);
        Assert.Contains("which is a range rather than a version", copy.Because);
    }

    [Fact]
    public void A_wildcard_and_an_unexpanded_property_are_ranges_too()
    {
        var wildcard = Project(
            "wild.csproj",
            """<Project><ItemGroup><PackageReference Include="Winwright" Version="1.*" /></ItemGroup></Project>""");
        var property = Project(
            "prop.csproj",
            """<Project><ItemGroup><PackageReference Include="Winwright" Version="$(WinwrightVersion)" /></ItemGroup></Project>""");

        Assert.Equal(Pinning.Unpinnable, Engine.Pinned("a project", wildcard).Pinning);
        Assert.Equal(Pinning.Unpinnable, Engine.Pinned("a project", property).Pinning);
    }

    [Fact]
    public void A_project_reference_is_unpinnable_because_it_is_whatever_the_tree_built()
    {
        var project = Project(
            "vendored.csproj",
            """<Project><ItemGroup><ProjectReference Include="..\..\src\Winwright\Winwright.csproj" /></ItemGroup></Project>""");

        var copy = Engine.Pinned("the consuming project", project);

        Assert.Equal(Pinning.Unpinnable, copy.Pinning);
        Assert.Contains("depends on the tree it was built from", copy.Because);
    }

    [Fact]
    public void A_pinned_package_reference_reads_as_the_version_it_names()
    {
        var attribute = Project(
            "attr.csproj",
            """<Project><ItemGroup><PackageReference Include="Winwright" Version="0.4.1" /></ItemGroup></Project>""");
        var element = Project(
            "elem.csproj",
            """<Project><ItemGroup><PackageReference Include="Winwright"><Version>0.4.1</Version></PackageReference></ItemGroup></Project>""");

        Assert.Equal("0.4.1", Engine.Pinned("a project", attribute).Version);
        Assert.Equal("0.4.1", Engine.Pinned("a project", element).Version);
    }

    [Fact]
    public void A_project_that_never_references_the_engine_is_absent_and_not_unpinnable()
    {
        var project = Project("elsewhere.csproj", """<Project><ItemGroup><PackageReference Include="xunit" Version="2.9.0" /></ItemGroup></Project>""");

        var copy = Engine.Pinned("the consuming project", project);

        Assert.Equal(Pinning.Absent, copy.Pinning);
        Assert.Contains("does not reference Winwright", copy.Because);
    }

    [Fact]
    public void A_file_that_is_not_there_is_absent_rather_than_a_throw()
    {
        Assert.Equal(Pinning.Absent, Engine.Declared("a tree", Path.Combine(root, "nope.props")).Pinning);
        Assert.Equal(Pinning.Absent, Engine.Pinned("a project", Path.Combine(root, "nope.csproj")).Pinning);
    }

    [Fact]
    public void The_assembly_being_called_names_the_version_it_was_built_with()
    {
        var copy = Engine.Running();

        Assert.True(copy.Pins, copy.Because);
        Assert.Equal("the assembly being called", copy.Where);
    }

    [Fact]
    public void This_repository_agrees_with_itself()
    {
        // The reading pointed at the tree it is part of. If this goes red, either the props stopped
        // declaring a version or a copy was built from something other than what is checked in.
        var props = Path.Combine(Repository(), "Directory.Build.props");

        var read = Agreement.Between(Engine.Running(), Engine.Declared("the source tree", props));

        Assert.True(read.Agreed, read.ToString());
    }

    [Fact]
    public void The_report_spells_a_version_the_same_way_the_sentence_does()
    {
        // WW153. The sentence dropped the build metadata and the report did not, so the gate
        // printed both spellings at once: all copies are 0.1.0, and the assembly being called is
        // 0.1.0+694044e37… — a version string no file in the tree holds, beside ones that hold
        // theirs, leaving a reader to know which half of it is decoration.
        var read = Agreement.Between(
            Pinned("the tree", "0.4.1"),
            Pinned("the assembly being called", "0.4.1+694044e37cdff1f8ad593f1fa3735e05af09d218"));

        Assert.True(read.Agreed, read.ToString());

        var rendered = read.Render();
        Assert.All(rendered, one => Assert.DoesNotContain("+694044e", one, StringComparison.Ordinal));
        Assert.All(rendered, one => Assert.Contains(read.Versions[0], one, StringComparison.Ordinal));
    }

    [Fact]
    public void The_build_a_copy_was_made_from_is_moved_rather_than_thrown_away()
    {
        // Which commit built the running copy is the whole reason anybody reads this report. It
        // leaves the column it was breaking and says what it is.
        var read = Agreement.Between(
            Pinned("the tree", "0.4.1"),
            Pinned("the assembly being called", "0.4.1+694044e37cdff1f8ad593f1fa3735e05af09d218"));

        var running = Assert.Single(read.Render(), one => one.Contains("the assembly being called", StringComparison.Ordinal));

        Assert.Contains("(build 694044e37cdff1f8ad593f1fa3735e05af09d218)", running, StringComparison.Ordinal);
        Assert.DoesNotContain(read.Render(), one => one.Contains("the tree (build", StringComparison.Ordinal));
    }

    [Fact]
    public void Every_row_lines_up_whatever_the_longest_version_turns_out_to_be()
    {
        // Twelve was the width, and a version somebody is entitled to declare overran it — so the
        // copy most in need of being read against the others was the one that did not line up.
        var read = Agreement.Between(
            Pinned("the tree", "1.0.0-preview.7.24405.7+abcdef0"),
            Pinned("the package", "1.0.0-preview.7.24405.7"),
            new EngineCopy("the consumer", null, Pinning.Unpinnable, "it references it by path"));

        var rendered = read.Render();

        // Read off the rows rather than typed: what is asserted is that every description starts
        // in the same column, and how wide that turns out to be is the versions' business.
        var columns = new[] { "the tree", "the package", "the consumer" }
            .Select(name => Assert.Single(rendered, one => one.Contains(name, StringComparison.Ordinal))
                .IndexOf(name, StringComparison.Ordinal))
            .ToList();

        Assert.Single(columns.Distinct());
        Assert.All(rendered, one => Assert.StartsWith("  ", one, StringComparison.Ordinal));

        // And the widest cell is not truncated to fit, which would invent a spelling of its own.
        Assert.Contains(rendered, one => one.Contains("1.0.0-preview.7.24405.7 ", StringComparison.Ordinal));
        Assert.Contains(rendered, one => one.Contains("(unpinnable)", StringComparison.Ordinal));
    }

    private static string Repository() => Checkout.Root;
}
