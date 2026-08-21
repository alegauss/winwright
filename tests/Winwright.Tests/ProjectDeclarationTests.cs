using Winwright.Projects;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW5. The executable, the source root, the language files, the timeouts and the fingerprint
/// store are facts about a project. Move a scenario to another checkout and it behaves the same,
/// or it refuses naming the declaration that is missing.
/// </summary>
public class ProjectDeclarationTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-decl-").FullName;

    public void Dispose()
    {
        Directory.Delete(root, recursive: true);
        GC.SuppressFinalize(this);
    }

    private ProjectDeclaration Declare(string json, string? at = null)
    {
        var directory = at is null ? root : Path.Combine(root, at);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, ProjectDeclaration.FileName);
        File.WriteAllText(path, json);
        return ProjectDeclaration.Load(path);
    }

    [Fact]
    public void A_relative_path_resolves_against_the_declaration_and_not_the_working_directory()
    {
        var declaration = Declare("""{ "executable": "bin/Release/ClaudeTray.exe" }""");

        Assert.Equal(Path.Combine(root, "bin", "Release", "ClaudeTray.exe"), declaration.Executable);
    }

    [Fact]
    public void What_the_project_never_declared_is_refused_by_name()
    {
        var declaration = Declare("""{ "executable": "bin/ClaudeTray.exe" }""");

        var refusal = Assert.Throws<DeclarationMissingException>(() => declaration.SourceRoot);

        Assert.Equal("sourceRoot", refusal.Key);
        Assert.Contains("declares no 'sourceRoot'", refusal.Message);
        Assert.Contains("checking whether the binary is stale", refusal.Message);
    }

    [Fact]
    public void Declares_answers_without_refusing()
    {
        var declaration = Declare("""{ "executable": "bin/ClaudeTray.exe" }""");

        Assert.True(declaration.Declares("executable"));
        Assert.False(declaration.Declares("sourceRoot"));
        Assert.False(declaration.Declares("languageFiles"));
    }

    [Fact]
    public void The_engine_seeds_three_timeouts_so_a_bare_declaration_still_runs()
    {
        var declaration = Declare("{}");

        Assert.Equal(5000, declaration.Timeouts.For("resolve"));
        Assert.Equal(2000, declaration.Timeouts.For("act"));
        Assert.Equal(15000, declaration.Timeouts.For("launch"));
    }

    [Fact]
    public void A_declared_timeout_replaces_the_seeded_one_and_a_new_name_is_simply_this_projects()
    {
        var declaration = Declare("""{ "timeouts": { "resolve": 12000, "menu": 800 } }""");

        Assert.Equal(12000, declaration.Timeouts.For("resolve"));
        Assert.Equal(2000, declaration.Timeouts.For("act"));
        Assert.Equal(800, declaration.Timeouts.For("menu"));
    }

    [Fact]
    public void A_timeout_nothing_declares_is_refused_rather_than_invented()
    {
        var declaration = Declare("{}");

        var refusal = Assert.Throws<DeclarationMissingException>(() => declaration.Timeouts.For("menu"));

        Assert.Equal("timeouts.menu", refusal.Key);
    }

    [Fact]
    public void A_wait_of_nothing_is_refused_at_load()
    {
        var refusal = Assert.Throws<ArgumentException>(() => Declare("""{ "timeouts": { "act": 0 } }"""));

        Assert.Contains("a wait of nothing is not a wait", refusal.Message);
    }

    [Fact]
    public void Language_files_resolve_too_and_keep_their_declared_order()
    {
        var declaration = Declare("""{ "languageFiles": ["lang/en.json", "lang/pt.json"] }""");

        Assert.Equal(
            [Path.Combine(root, "lang", "en.json"), Path.Combine(root, "lang", "pt.json")],
            declaration.LanguageFiles);
    }

    [Fact]
    public void The_declaration_is_found_by_walking_up_from_the_scenario()
    {
        Declare("""{ "executable": "bin/ClaudeTray.exe" }""");
        var deep = Directory.CreateDirectory(Path.Combine(root, "tests", "scenarios", "tray")).FullName;

        var found = ProjectDeclaration.Find(deep);

        Assert.Equal(root, found.Root);
        Assert.Equal(Path.Combine(root, "bin", "ClaudeTray.exe"), found.Executable);
    }

    [Fact]
    public void The_nearest_declaration_wins_over_one_further_up()
    {
        Declare("""{ "executable": "outer.exe" }""");
        Declare("""{ "executable": "inner.exe" }""", at: "nested");

        Assert.Equal(Path.Combine(root, "nested", "inner.exe"), ProjectDeclaration.Find(Path.Combine(root, "nested")).Executable);
    }

    [Fact]
    public void A_checkout_that_declares_nothing_refuses_and_says_where_it_looked()
    {
        var elsewhere = Directory.CreateTempSubdirectory("winwright-bare-").FullName;
        try
        {
            var refusal = Assert.Throws<DeclarationMissingException>(() => ProjectDeclaration.Find(elsewhere));

            Assert.Equal(ProjectDeclaration.FileName, refusal.Key);
            Assert.Contains("every directory above it", refusal.Message);
        }
        finally
        {
            Directory.Delete(elsewhere, recursive: true);
        }
    }

    [Fact]
    public void Comments_and_a_trailing_comma_are_read_because_a_person_edits_this_file()
    {
        var declaration = Declare("""
            {
              // what this project drives
              "executable": "bin/ClaudeTray.exe",
            }
            """);

        Assert.Equal(Path.Combine(root, "bin", "ClaudeTray.exe"), declaration.Executable);
    }

    [Fact]
    public void An_environment_variable_is_expanded_so_an_installed_application_can_be_named()
    {
        var declaration = Declare("""{ "executable": "%SystemRoot%/notepad.exe" }""");

        Assert.Equal(
            Path.GetFullPath(Path.Combine(System.Environment.GetEnvironmentVariable("SystemRoot")!, "notepad.exe")),
            declaration.Executable);
    }
}
