using System.Diagnostics;

using Paneproof.Projects;

using Xunit;

namespace Paneproof.Tests;

/// <summary>
/// WW591. The names an adopter's suite was written against before the product was Paneproof keep
/// working for a stated period: the new name first, the old one accepted and said so, both refused.
/// <para>
/// Serial because two of these set a variable on this process, which every other case reads too.
/// None of them needs a desk.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class RenamedTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("paneproof-renamed-").FullName;

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(ProjectDeclaration.ConfigurationVariable, null);
        Environment.SetEnvironmentVariable(Renamed.Old(ProjectDeclaration.ConfigurationVariable), null);
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_project_under_the_new_name_is_found_and_says_nothing_about_names()
    {
        Declared(ProjectDeclaration.FileName);

        var found = ProjectDeclaration.Find(Path.Combine(root, "cases"));

        Assert.Equal(ProjectDeclaration.FileName, Path.GetFileName(found.Path));
        Assert.Empty(found.OldNames);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_project_under_the_old_name_is_found_and_says_it_was_read_under_it()
    {
        Declared(Renamed.OldDeclaration);

        var found = ProjectDeclaration.Find(Path.Combine(root, "cases"));

        Assert.Equal(Renamed.OldDeclaration, Path.GetFileName(found.Path));
        var said = Assert.Single(found.OldNames);
        Assert.Contains("winwright.json was read", said, StringComparison.Ordinal);
        Assert.Contains("paneproof.json", said, StringComparison.Ordinal);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void Both_names_in_one_directory_are_refused_naming_both()
    {
        Declared(ProjectDeclaration.FileName);
        Declared(Renamed.OldDeclaration);

        var refused = Assert.Throws<InvalidOperationException>(() => ProjectDeclaration.Find(Path.Combine(root, "cases")));

        Assert.Contains("paneproof.json", refused.Message, StringComparison.Ordinal);
        Assert.Contains("winwright.json", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void The_old_configuration_variable_is_read_where_the_new_one_is_unset_and_said()
    {
        Environment.SetEnvironmentVariable("WINWRIGHT_CONFIGURATION", "Release");

        var loaded = ProjectDeclaration.Load(Declared(ProjectDeclaration.FileName));

        Assert.Equal("Release", loaded.Configuration);
        var said = Assert.Single(loaded.OldNames);
        Assert.Contains("WINWRIGHT_CONFIGURATION was read because PANEPROOF_CONFIGURATION is not set", said, StringComparison.Ordinal);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void Both_configuration_variables_set_are_refused_naming_both()
    {
        Environment.SetEnvironmentVariable("PANEPROOF_CONFIGURATION", "Release");
        Environment.SetEnvironmentVariable("WINWRIGHT_CONFIGURATION", "Debug");

        var refused = Assert.Throws<InvalidOperationException>(() => ProjectDeclaration.Load(Declared(ProjectDeclaration.FileName)));

        Assert.Contains("both PANEPROOF_CONFIGURATION ('Release') and WINWRIGHT_CONFIGURATION ('Debug')", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_launch_carries_each_in_app_variable_under_its_old_name_too()
    {
        // The engine and the in-app half are upgraded on different days, so an application still on
        // the old one reads the old name, and a launch that set only the new one would leave it blind.
        var start = new ProcessStartInfo("x.exe");
        start.Environment["PANEPROOF_SURFACES"] = @"C:\reports\surfaces.tsv";
        start.Environment["PANEPROOF_RENDERS"] = @"C:\renders";
        start.Environment["WINWRIGHT_RENDERS"] = @"C:\the-fixture-said";

        Renamed.Both(start, "PANEPROOF_SURFACES", "PANEPROOF_RENDERS", "PANEPROOF_GEOMETRY");

        Assert.Equal(@"C:\reports\surfaces.tsv", start.Environment["WINWRIGHT_SURFACES"]);

        // An old name the launch already carries is the fixture's, and is left as it said.
        Assert.Equal(@"C:\the-fixture-said", start.Environment["WINWRIGHT_RENDERS"]);
        Assert.False(start.Environment.ContainsKey("WINWRIGHT_GEOMETRY"));
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void Only_the_products_own_variables_have_an_old_name()
    {
        Assert.Equal("WINWRIGHT_GEOMETRY", Renamed.Old("PANEPROOF_GEOMETRY"));
        Assert.Throws<ArgumentException>(() => Renamed.Old("APPDATA"));
    }

    /// <summary>A declaration under <paramref name="name"/> at the root, with a folder of cases below it.</summary>
    private string Declared(string name)
    {
        Directory.CreateDirectory(Path.Combine(root, "cases"));
        var path = Path.Combine(root, name);
        File.WriteAllText(path, """{ "executable": "app.exe" }""");
        return path;
    }
}
