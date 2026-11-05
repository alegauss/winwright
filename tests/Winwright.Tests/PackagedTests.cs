using System.IO.Compression;
using System.Xml.Linq;

using Winwright.Projects;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW122. The block's criterion says the in-app half is one package reference, and nothing in
/// either project declared a package id, a version beyond the tree-wide one, or a pack step — so
/// the only way to adopt it was a project reference into this repository, which the agreement
/// check already reads as unpinnable by construction and which an application shipping to its
/// users cannot take at all.
/// <para>
/// What is checked here is the declaration and the reading. That an adopter really compiles
/// against the packed article is checked by a project rather than by an assertion: samples/Adopter
/// takes one package reference and is built in continuous integration from what was just packed.
/// </para>
/// </summary>
public sealed class PackagedTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-packed-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    /// <summary>The repository root, walked up from where the suite is running.</summary>
    private static string Repository() => Checkout.Root;

    /// <summary>A package carrying whatever nuspec is asked for, which is what a real one is.</summary>
    private string Packed(string file, string id, string version)
    {
        var path = Path.Combine(root, file);
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        using var writing = new StreamWriter(archive.CreateEntry($"{id}.nuspec").Open());
        writing.Write(
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
              <metadata><id>{id}</id><version>{version}</version></metadata>
            </package>
            """);

        return path;
    }

    private static string Declared(string project, string element)
    {
        var document = XDocument.Load(Path.Combine(Repository(), project));
        var found = document.Root!
            .Elements().Where(one => one.Name.LocalName == "PropertyGroup")
            .SelectMany(group => group.Elements())
            .FirstOrDefault(one => one.Name.LocalName == element);

        Assert.NotNull(found);
        return found.Value.Trim();
    }

    [Fact]
    public void Both_halves_declare_a_package_id_so_there_is_something_to_reference()
    {
        Assert.Equal("Winwright.InApp", Declared("src/Winwright.InApp/Winwright.InApp.csproj", "PackageId"));
        Assert.Equal("Winwright", Declared("src/Winwright/Winwright.csproj", "PackageId"));
        Assert.Equal("true", Declared("src/Winwright.InApp/Winwright.InApp.csproj", "IsPackable"));
        Assert.Equal("true", Declared("src/Winwright/Winwright.csproj", "IsPackable"));
    }

    [Fact]
    public void The_adopter_takes_the_in_app_half_by_one_package_reference_and_nothing_else()
    {
        var document = XDocument.Load(Path.Combine(Repository(), "samples/Adopter/Adopter.csproj"));
        var references = document.Root!
            .Elements().Where(one => one.Name.LocalName == "ItemGroup")
            .SelectMany(group => group.Elements())
            .ToList();

        // One package reference, no path into this repository, and no reference to the engine: an
        // application that took the engine would ship a test harness to its users.
        var package = Assert.Single(references, one => one.Name.LocalName == "PackageReference");
        Assert.Equal("Winwright.InApp", package.Attribute("Include")!.Value);
        Assert.DoesNotContain(references, one => one.Name.LocalName == "ProjectReference");
        Assert.DoesNotContain(references, one =>
            one.Attribute("Include")?.Value == "Winwright");
    }

    [Fact]
    public void A_built_package_names_its_version_out_of_the_nuspec_it_carries()
    {
        var copy = Engine.Packed("the package that was built", Packed("Winwright.0.1.0.nupkg", "Winwright", "0.1.0"));

        Assert.True(copy.Pins);
        Assert.Equal("0.1.0", copy.Version);
        Assert.Equal("the package that was built is 0.1.0", copy.ToString());
    }

    [Fact]
    public void A_directory_is_searched_for_the_one_package_that_was_asked_for()
    {
        Packed("Winwright.0.1.0.nupkg", "Winwright", "0.1.0");
        Packed("Winwright.InApp.0.2.0.nupkg", "Winwright.InApp", "0.2.0");

        Assert.Equal("0.2.0", Engine.Packed("the in-app package", root, "Winwright.InApp").Version);
        Assert.Equal("0.1.0", Engine.Packed("the engine package", root, "Winwright").Version);
    }

    [Fact]
    public void A_directory_holding_two_builds_of_the_same_package_is_unpinnable_and_says_which()
    {
        Packed("Winwright.0.1.0.nupkg", "Winwright", "0.1.0");
        Packed("Winwright.0.2.0.nupkg", "Winwright", "0.2.0");

        var copy = Engine.Packed("the package that was built", root);

        // Which one an adopter would take is not answerable from a folder, and guessing is how the
        // whole reading would come to agree with something nobody is going to run.
        Assert.Equal(Pinning.Unpinnable, copy.Pinning);
        Assert.Contains("holds 2 builds", copy.Because);
        Assert.Contains("Winwright.0.2.0.nupkg", copy.Because);
    }

    [Fact]
    public void A_directory_with_nothing_packed_is_an_absence_rather_than_a_version()
    {
        var copy = Engine.Packed("the package that was built", root);

        Assert.Equal(Pinning.Absent, copy.Pinning);
        Assert.Contains("no Winwright package was built", copy.Because);
    }

    [Fact]
    public void A_file_named_for_one_package_and_carrying_another_is_refused()
    {
        // The confusion this reading exists to refuse: the file name is what a folder listing
        // shows and the nuspec is what an adopter actually restores.
        var wrong = Packed("Winwright.0.1.0.nupkg", "SomethingElse", "9.9.9");

        var copy = Engine.Packed("the package that was built", wrong);

        Assert.Equal(Pinning.Absent, copy.Pinning);
        Assert.Contains("no readable nuspec version", copy.Because);
    }

    [Fact]
    public void A_symbol_package_is_not_the_package_even_where_it_shares_the_name()
    {
        Packed("Winwright.0.1.0.nupkg", "Winwright", "0.1.0");
        Packed("Winwright.0.1.0.symbols.nupkg", "Winwright", "0.1.0");

        Assert.Equal("0.1.0", Engine.Packed("the package that was built", root).Version);
    }

    [Fact]
    public void What_was_packed_and_what_an_adopter_asked_for_can_now_be_read_together()
    {
        // The loop this closes: before the halves were packable, one side of this comparison was a
        // path, and a path makes the whole reading unpinnable however well the other side agrees.
        //
        // The packed side is built at whatever the tree declares rather than at a version written
        // here, and that is WW230's own lesson taken one layer up: this test failed the moment the
        // tree became a prerelease, which is a red about the test having gone stale rather than about
        // the two copies disagreeing. A version spelled twice is the second spelling that goes on
        // saying the old thing after the first moves — and the check under test is the one that exists
        // to say so.
        var declared = Engine.Declared("the source tree", Path.Combine(Repository(), "Directory.Build.props")).Version;
        Assert.NotNull(declared);
        Packed($"Winwright.InApp.{declared}.nupkg", "Winwright.InApp", declared);

        var agreed = Agreement.Between(
            Engine.Packed("the package that was built", root, "Winwright.InApp"),
            Engine.Pinned("the adopter", Path.Combine(Repository(), "samples/Adopter/Adopter.csproj"), "Winwright.InApp"));

        Assert.True(agreed.Agreed, agreed.Sentence());
        Assert.Equal(0, agreed.ExitCode);
    }

    [Fact]
    public void The_adopter_and_the_tree_are_the_same_version_rather_than_two_near_ones()
    {
        // If the tree's version moves and the sample's reference does not, this is what says so —
        // which is the same check pointed at this repository instead of at somebody else's.
        var agreed = Agreement.Between(
            Engine.Declared("the source tree", Path.Combine(Repository(), "Directory.Build.props")),
            Engine.Pinned("the adopter", Path.Combine(Repository(), "samples/Adopter/Adopter.csproj"), "Winwright.InApp"));

        Assert.True(agreed.Agreed, agreed.Sentence());
    }
}
