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
    public void A_path_that_names_no_declaration_says_so()
    {
        // WW196, and distinct from the search below. Somebody named a path and there is nothing at
        // it, so the fix is the path; the case under this one looked everywhere and found none, so
        // the fix is to write one. Reported identically until now.
        var named = Path.Combine(root, "not-a-project", ProjectDeclaration.FileName);

        var refusal = Assert.Throws<DeclarationMissingException>(() => ProjectDeclaration.Load(named));

        Assert.Equal(MissingDeclaration.NotAtThePathNamed, refusal.Arm);
        Assert.Equal(ProjectDeclaration.FileName, refusal.Key);
        Assert.DoesNotContain("every directory above it", refusal.Message, StringComparison.Ordinal);
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

    [Fact]
    public void Every_key_this_build_reads_is_one_the_catalogue_names()
    {
        // WW490. The shape is what the deserialiser reads and the catalogue is what the
        // documentation area publishes, and a key in one and not the other is the drift a page
        // cannot report: a reader is told about a key this build ignores, or writes one this build
        // reads and nothing described. Both directions, because only one of them is visible.
        foreach (var (shape, under) in new[] { (Shaped("Shape"), ""), (Shaped("LanguageShape"), "language") })
        {
            var read = Json(shape);
            Assert.NotEmpty(read);

            var catalogued = ProjectDeclaration.Keys
                .Where(one => string.Equals(one.Under, under, StringComparison.Ordinal))
                .Select(one => one.Name)
                .ToList();

            Assert.Equal(read, catalogued);
        }
    }

    [Fact]
    public void Every_key_says_what_it_is_for_and_what_its_absence_does()
    {
        // Absence is never "nothing happens" here — it is a default that stands, a reading recorded
        // as not taken, or a refusal at the moment something asks. A blank would publish as an empty
        // cell, which reads as a key with no consequence either way.
        Assert.All(ProjectDeclaration.Keys, key =>
        {
            Assert.NotEmpty(key.Holds);
            Assert.NotEmpty(key.Means);
            Assert.NotEmpty(key.Absent);
            Assert.Equal(key.Under.Length > 0 ? $"{key.Under}.{key.Name}" : key.Name, key.Addressed);
        });

        // A key addressed twice is two rows under one anchor on the page, and two answers about
        // one word here.
        Assert.Distinct(ProjectDeclaration.Keys.Select(one => one.Addressed));
    }

    [Fact]
    public void Every_key_the_declaration_answers_about_is_one_it_catalogues()
    {
        // `Declares` is the third list of these names, and it is the one a run asks. A name it
        // answers that the catalogue has never heard of is a key nobody can look up.
        //
        // Read with `Spoken` and not the default: the keys are string literals, and the reading
        // that strips strings leaves `=> captures is not null,` with the key gone — so this would
        // sweep nothing and pass, which is the shape of green this repository is about.
        var arms = Checkout.Members(
                Checkout.At("src", "Winwright", "Projects", "ProjectDeclaration.cs"),
                Checkout.Spoken)
            .Where(one => string.Equals(one.Name, nameof(ProjectDeclaration.Declares), StringComparison.Ordinal))
            .SelectMany(one => System.Text.RegularExpressions.Regex
                .Matches(one.Body, """^\s*"([A-Za-z.]+)" =>""", System.Text.RegularExpressions.RegexOptions.Multiline)
                .Select(match => match.Groups[1].Value))
            .ToList();

        Assert.True(arms.Count > 4, $"only {arms.Count} arm(s) of Declares could be read, so this checked almost nothing");

        var addressed = ProjectDeclaration.Keys.Select(one => one.Addressed).ToHashSet(StringComparer.Ordinal);
        Assert.All(arms, key => Assert.Contains(key, addressed));
    }

    /// <summary>One of the declaration's private JSON shapes, by name.</summary>
    private static Type Shaped(string named) =>
        typeof(ProjectDeclaration).GetNestedType(named, System.Reflection.BindingFlags.NonPublic)
        ?? throw new Xunit.Sdk.XunitException($"ProjectDeclaration declares no {named} for the deserialiser to read");

    /// <summary>The keys a shape reads, in the order it declares them.</summary>
    private static IReadOnlyList<string> Json(Type shape) =>
        shape.GetProperties()
            .Select(one => one
                .GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute), false)
                .Cast<System.Text.Json.Serialization.JsonPropertyNameAttribute>()
                .SingleOrDefault())
            .OfType<System.Text.Json.Serialization.JsonPropertyNameAttribute>()
            .Select(one => one.Name)
            .ToList();
}
