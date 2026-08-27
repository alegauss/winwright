using Winwright.Concordance;
using Winwright.Projects;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW239. The release rewrote five paths named in a YAML array and then checked four named on a
/// command line, and neither list owned the other. The array was wrong on its first run — it had four
/// paths and the suite went red on the README — which is the net working and is exactly why a net is
/// not an owner: a sixth copy added tomorrow reaches neither.
/// <para>
/// So the line that says which copies to compare is the line that says which copies to raise. What is
/// checked here is that the two halves cannot come apart: a copy named once is raised and read, and a
/// copy nobody named is in neither half rather than in one of them.
/// </para>
/// </summary>
public sealed class RaisingTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-raising-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void Every_copy_the_line_names_is_raised_and_then_reads_the_new_version()
    {
        var tree = Declaring("Directory.Build.props", "0.4.1");
        var driving = Referencing("Driving.csproj", "Winwright", "0.4.1");
        var readme = Documenting("README.md", "Winwright", "0.4.1");
        var plugin = Manifesting("0.4.1");

        var flags = new[]
        {
            "--declared", tree, "--pinned", driving, "--documented", readme, "--manifest", plugin,
        };

        var (said, raised) = Roster.From([.. flags, "--raise", "0.5.0"]).Raise();

        Assert.True(raised, string.Join('\n', said));
        Assert.All(said, one => Assert.Contains("0.4.1 -> 0.5.0", one, StringComparison.Ordinal));

        // Read back through the same flags, which is the property the task is about: the raise and the
        // check are one list, so what was written is what is compared.
        var read = Roster.From(flags).Read();

        Assert.True(read.Agreed, read.Sentence());
        Assert.Contains("0.5.0", read.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_copy_that_cannot_be_read_stops_the_raise_before_anything_is_written()
    {
        // The half a rewrite gets wrong. Raising three files and stopping on the fourth leaves a tree
        // that agrees with nothing, which is worse than a tree nobody touched — so every writable copy
        // says what it currently is before any of them is changed.
        var tree = Declaring("Directory.Build.props", "0.4.1");
        var missing = Path.Combine(root, "NotThere.csproj");

        var (said, raised) = Roster.From(
            ["--declared", tree, "--pinned", missing, "--raise", "0.5.0"]).Raise();

        Assert.False(raised);
        Assert.Contains("nothing was written", Assert.Single(said), StringComparison.Ordinal);

        // And it means it: the file that could have been raised was not.
        Assert.Equal("0.4.1", Engine.Declared("the tree", tree).Version);
    }

    [Fact]
    public void What_a_build_produced_is_named_as_not_raised_rather_than_passed_over()
    {
        // A packed copy is a reading of something already built, so nothing can rewrite it — and a
        // raise that silently skipped it would read as a raise of everything on the line.
        var tree = Declaring("Directory.Build.props", "0.4.1");
        var packed = Packing("Winwright", "0.4.1");

        var (said, raised) = Roster.From(
            ["--declared", tree, "--packed", packed, "--raise", "0.5.0"]).Raise();

        Assert.True(raised, string.Join('\n', said));
        Assert.Contains(said, one => one.Contains("was not raised", StringComparison.Ordinal));
    }

    [Fact]
    public void A_copy_already_at_the_version_is_said_and_not_written()
    {
        var tree = Declaring("Directory.Build.props", "0.5.0");
        var driving = Referencing("Driving.csproj", "Winwright", "0.5.0");

        var (said, raised) = Roster.From(
            ["--declared", tree, "--pinned", driving, "--raise", "0.5.0"]).Raise();

        Assert.True(raised);
        Assert.All(said, one => Assert.Contains("already reads 0.5.0", one, StringComparison.Ordinal));
    }

    [Fact]
    public void A_document_showing_the_reference_twice_is_raised_at_both()
    {
        // The README shows it once per package, and rewriting the first would leave the file
        // disagreeing with itself — which the reading below then reports rather than averaging.
        var path = Path.Combine(root, "README.md");
        File.WriteAllText(
            path,
            """
            Take the engine:

            ```xml
            <PackageReference Include="Winwright" Version="0.4.1" />
            <PackageReference Include="Winwright.InApp" Version="0.4.1" />
            ```
            """);

        var flags = new[] { "--declared", Declaring("Directory.Build.props", "0.4.1"), "--documented", path };

        Assert.True(Roster.From([.. flags, "--raise", "0.5.0"]).Raise().Raised);

        Assert.Equal("0.5.0", Engine.Documented("the readme", path).Version);
        Assert.Equal("0.5.0", Engine.Documented("the readme", path, "Winwright.InApp").Version);
    }

    [Fact]
    public void A_document_that_disagrees_with_itself_is_unpinnable_rather_than_the_first_one_found()
    {
        var path = Path.Combine(root, "README.md");
        File.WriteAllText(
            path,
            """
            <PackageReference Include="Winwright" Version="0.4.1" />
            and later, wrongly:
            <PackageReference Include="Winwright" Version="0.3.0" />
            """);

        var copy = Engine.Documented("the readme", path);

        Assert.False(copy.Pins);
        Assert.Contains("disagrees with itself", copy.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void The_usage_names_both_halves_so_a_reader_learns_the_list_does_two_things()
    {
        Assert.Contains("--documented", Roster.Usage, StringComparison.Ordinal);
        Assert.Contains("--raise", Roster.Usage, StringComparison.Ordinal);
    }

    private string Declaring(string name, string version)
    {
        var path = Path.Combine(root, name);
        File.WriteAllText(path, $"<Project><PropertyGroup><Version>{version}</Version></PropertyGroup></Project>");
        return path;
    }

    private string Referencing(string name, string package, string version)
    {
        var path = Path.Combine(root, name);
        File.WriteAllText(
            path,
            $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="{package}" Version="{version}" />
              </ItemGroup>
            </Project>
            """);
        return path;
    }

    private string Documenting(string name, string package, string version)
    {
        var path = Path.Combine(root, name);
        File.WriteAllText(
            path,
            $"""
            # taking it

            ```xml
            <PackageReference Include="{package}" Version="{version}" />
            ```
            """);
        return path;
    }

    private string Manifesting(string version)
    {
        var into = Directory.CreateDirectory(Path.Combine(root, ".claude-plugin")).FullName;
        File.WriteAllText(Path.Combine(into, "plugin.json"), $$"""{ "name": "winwright", "version": "{{version}}" }""");
        return root;
    }

    private string Packing(string package, string version)
    {
        var into = Directory.CreateDirectory(Path.Combine(root, $"{package}-packages")).FullName;
        var nupkg = Path.Combine(into, $"{package}.{version}.nupkg");

        using (var archive = System.IO.Compression.ZipFile.Open(nupkg, System.IO.Compression.ZipArchiveMode.Create))
        {
            var nuspec = archive.CreateEntry($"{package}.nuspec");
            using var writing = new StreamWriter(nuspec.Open());
            writing.Write(
                $"""
                <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
                  <metadata><id>{package}</id><version>{version}</version></metadata>
                </package>
                """);
        }

        return into;
    }
}
