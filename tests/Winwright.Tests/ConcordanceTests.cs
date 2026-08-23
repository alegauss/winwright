using Winwright.Concordance;
using Winwright.Projects;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW142. The agreement reading has had an exit code since WW70 and nothing left it behind, so the
/// copies of the engine in play were compared as often as somebody opened the file.
/// <para>
/// What is checked here is the half the reading cannot check for itself: what a command line turned
/// into. A dropped flag is a copy that left the reading without lowering any count a reader sees,
/// and two copies that agree is a green â€” the same shape as a suite that lost twenty-two tests and
/// still printed a pass.
/// </para>
/// </summary>
public sealed class ConcordanceTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-concord-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void Three_copies_naming_one_version_agree_and_the_gate_exits_zero()
    {
        var tree = Declaring("Directory.Build.props", "0.4.1");
        var adopter = Referencing("Adopter.csproj", "Winwright.InApp", "0.4.1");
        var packed = Packing("Winwright.InApp", "0.4.1");

        var roster = Roster.From(
            ["--declared", tree, "--package", "Winwright.InApp", "--packed", packed, "--pinned", adopter]);

        Assert.True(roster.Readable);
        Assert.Equal(3, roster.Copies.Count);
        Assert.Equal(0, roster.Read().ExitCode);
    }

    [Fact]
    public void A_pinned_version_left_behind_is_what_the_gate_stops_on()
    {
        var tree = Declaring("Directory.Build.props", "0.5.0");
        var adopter = Referencing("Adopter.csproj", "Winwright.InApp", "0.4.1");

        var read = Roster.From(["--declared", tree, "--package", "Winwright.InApp", "--pinned", adopter]).Read();

        Assert.Equal(Concord.Behind, read.Verdict);
        Assert.Equal(1, read.ExitCode);
        Assert.Contains("Adopter.csproj's reference to Winwright.InApp is 0.4.1", read.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void The_package_id_is_read_left_to_right_so_one_line_can_name_two_packages()
    {
        var engine = Packing("Winwright", "0.4.1");
        var inApp = Packing("Winwright.InApp", "0.4.1");

        var roster = Roster.From(
            ["--packed", engine, "--package", "Winwright.InApp", "--packed", inApp]);

        Assert.True(roster.Readable);
        Assert.All(roster.Copies, one => Assert.True(one.Pins));
        Assert.Equal("the Winwright package in " + engine, roster.Copies[0].Where);
        Assert.Equal("the Winwright.InApp package in " + inApp, roster.Copies[1].Where);
    }

    [Fact]
    public void A_flag_this_does_not_know_is_refused_rather_than_skipped()
    {
        var tree = Declaring("Directory.Build.props", "0.4.1");

        var roster = Roster.From(["--declared", tree, "--pnned", tree]);

        Assert.False(roster.Readable);
        Assert.Contains("--pnned", roster.Complaint, StringComparison.Ordinal);
        Assert.Empty(roster.Copies);
    }

    [Fact]
    public void A_flag_with_nothing_after_it_is_refused_rather_than_read_as_absent()
    {
        var roster = Roster.From(["--declared"]);

        Assert.False(roster.Readable);
        Assert.Contains("was given nothing to read", roster.Complaint, StringComparison.Ordinal);
    }

    [Fact]
    public void One_copy_is_refused_by_the_line_rather_than_by_an_exception()
    {
        var roster = Roster.From(["--declared", Declaring("Directory.Build.props", "0.4.1")]);

        Assert.False(roster.Readable);
        Assert.Contains("at least two", roster.Complaint, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => roster.Read());
    }

    [Fact]
    public void An_empty_line_is_unusable_and_says_so_rather_than_agreeing_with_itself()
    {
        var roster = Roster.From([]);

        Assert.False(roster.Readable);
        Assert.Empty(roster.Copies);
        Assert.Contains("--declared", Roster.Usage, StringComparison.Ordinal);
    }

    [Fact]
    public void A_copy_that_cannot_be_pinned_stops_the_gate_even_where_the_rest_agree()
    {
        var tree = Declaring("Directory.Build.props", "0.4.1");
        var vendored = Path.Combine(root, "Vendoring.csproj");
        File.WriteAllText(
            vendored,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <ProjectReference Include="..\..\src\Winwright\Winwright.csproj" />
              </ItemGroup>
            </Project>
            """);

        var read = Roster.From(["--declared", tree, "--pinned", vendored]).Read();

        Assert.Equal(Winwright.Projects.Concord.Unpinnable, read.Verdict);
        Assert.False(read.Agreed);
        Assert.Equal(1, read.ExitCode);
    }

    [Fact]
    public void This_repository_read_against_itself_is_the_gate_the_workflow_stands_in()
    {
        var repository = Repository();
        var roster = Roster.From(
        [
            "--declared", Path.Combine(repository, "Directory.Build.props"),
            "--running",
            "--package", "Winwright.InApp",
            "--pinned", Path.Combine(repository, "samples", "Adopter", "Adopter.csproj"),
        ]);

        Assert.True(roster.Readable, roster.Complaint);
        Assert.Equal(0, roster.Read().ExitCode);
    }

    /// <summary>The repository root, found by walking up to the file that declares the version.</summary>
    private static string Repository()
    {
        var walking = new DirectoryInfo(AppContext.BaseDirectory);
        while (walking is not null && !File.Exists(Path.Combine(walking.FullName, "Directory.Build.props")))
            walking = walking.Parent;

        Assert.NotNull(walking);
        return walking.FullName;
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

    /// <summary>A package on disk, which is a zip with a nuspec in it and nothing else that matters here.</summary>
    private string Packing(string package, string version)
    {
        var into = Directory.CreateDirectory(Path.Combine(root, $"{package}-{version}-packages")).FullName;
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

