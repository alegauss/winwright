using System.Text.Json;
using System.Text.Json.Nodes;

using Winwright.Guarding;
using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW67. The guard denies a write, which makes every judgement in it a false deny waiting to happen —
/// and a hook whose first false deny lands is a hook somebody removes, after which nothing is
/// guarded at all. So what it allows is checked here as carefully as what it refuses.
/// <para>
/// The judgements are exercised on paths that do not exist, because the project a path belongs to is
/// injected. A guard whose decisions can only be provoked by building a tree on disk is a guard whose
/// decisions are checked as often as somebody builds one.
/// </para>
/// </summary>
public sealed class GuardTests
{
    private const string Adopter = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup><PackageReference Include="Winwright" Version="0.1.0" /></ItemGroup>
        </Project>
        """;

    private const string Inside = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup><ProjectReference Include="../../src/Winwright/Winwright.csproj" /></ItemGroup>
        </Project>
        """;

    private const string Script = """
        using Winwright.Acting;
        public static class Smoke
        {
            public static void Run(Subject save) => Act.Invoke(save);
        }
        """;

    [Fact]
    public void A_script_that_drives_a_window_is_denied_and_the_refusal_names_the_verb()
    {
        // The whole of why this exists: the refusal has to arrive before the work, and it has to say
        // what to write instead. A deny that only says no is an obstacle and gets worked around.
        var verdict = Guard.On(Writing("D:/adopter/tests/Smoke.cs", Script), _ => Adopter);

        Assert.True(verdict.Denied);
        Assert.Contains("Smoke.cs", verdict.Because, StringComparison.Ordinal);
        Assert.Contains("Winwright.Acting", verdict.Because, StringComparison.Ordinal);
        Assert.Contains(ScenarioFile.Extension, verdict.Because, StringComparison.Ordinal);
        Assert.Contains("winwright_check", verdict.Because, StringComparison.Ordinal);
        Assert.Contains("winwright_format", verdict.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void The_case_file_the_guard_exists_to_ask_for_is_never_the_thing_it_denies()
    {
        // A guard standing in front of its own replacement is one nobody keeps. Checked with content
        // that would otherwise be denied, so it is the name that settles it and not the text.
        var path = $"D:/adopter/cases/profiles{ScenarioFile.Extension}";

        Assert.False(Guard.On(Writing(path, Script), _ => Adopter).Denied);
        Assert.False(Guard.On(Writing(path.ToUpperInvariant(), Script), _ => Adopter).Denied);
    }

    [Fact]
    public void The_engines_own_tree_drives_windows_on_purpose_and_is_never_denied()
    {
        // Otherwise working on the tool means turning the guard off, and a guard that is off for the
        // people who wrote it is one whose false denies nobody ever hears about.
        Assert.False(Guard.On(Writing("D:/winwright/tests/PickTests.cs", Script), _ => Inside).Denied);
    }

    [Fact]
    public void A_write_naming_none_of_the_driving_namespaces_is_not_a_harness_and_is_allowed()
    {
        var reading = """
            using Winwright.Verdicts;
            public static class Report { public static string Say(RunVerdict read) => read.ToString(); }
            """;

        Assert.False(Guard.On(Writing("D:/adopter/src/Report.cs", reading), _ => Adopter).Denied);
        Assert.False(Guard.On(Writing("D:/adopter/README.md", "how to run the cases"), _ => Adopter).Denied);
    }

    [Fact]
    public void Every_namespace_the_guard_watches_is_a_namespace_the_engine_has()
    {
        // A list of spellings nobody checks is the second copy that goes on naming a namespace after
        // it moved. This is what holds the two together.
        var namespaces = typeof(ScenarioFile).Assembly
            .GetExportedTypes()
            .Select(one => one.Namespace)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(Guard.Driving);
        foreach (var driving in Guard.Driving)
            Assert.Contains(driving, namespaces);
    }

    [Fact]
    public void A_rename_does_not_get_a_script_past_the_guard()
    {
        // The one thing an author can change without changing what they wrote. It reads the content,
        // so a .txt, a .ps1 and a file called nothing in particular all land the same.
        foreach (var named in new[] { "D:/adopter/notes.txt", "D:/adopter/drive.ps1", "D:/adopter/x" })
            Assert.True(Guard.On(Writing(named, Script), _ => Adopter).Denied, named);
    }

    [Fact]
    public void An_edit_is_read_by_what_it_adds_and_a_multi_edit_by_all_of_it()
    {
        // Reading only the top level is how a guard passes the write it was registered for.
        var edit = new JsonObject
        {
            ["tool_name"] = "Edit",
            ["tool_input"] = new JsonObject
            {
                ["file_path"] = "D:/adopter/tests/Smoke.cs",
                ["old_string"] = "// nothing yet",
                ["new_string"] = Script,
            },
        };

        var many = new JsonObject
        {
            ["tool_name"] = "MultiEdit",
            ["tool_input"] = new JsonObject
            {
                ["file_path"] = "D:/adopter/tests/Smoke.cs",
                ["edits"] = new JsonArray
                {
                    new JsonObject { ["old_string"] = "a", ["new_string"] = "// still nothing" },
                    new JsonObject { ["old_string"] = "b", ["new_string"] = Script },
                },
            },
        };

        Assert.True(Guard.On(edit, _ => Adopter).Denied);
        Assert.True(Guard.On(many, _ => Adopter).Denied);
    }

    [Fact]
    public void A_project_nothing_owns_is_still_judged_by_what_the_write_says()
    {
        // A loose script outside any project is the likeliest shape of the thing this is about, so a
        // missing project cannot be the reason it is allowed.
        Assert.True(Guard.On(Writing("D:/scratch/drive.cs", Script), _ => null).Denied);
    }

    [Fact]
    public void A_call_the_guard_cannot_read_is_allowed_and_never_refused()
    {
        // The rule that keeps a guard installed: what it did not understand, it does not deny.
        Assert.False(Guard.On(new JsonObject(), _ => Adopter).Denied);
        Assert.False(Guard.On(new JsonObject { ["tool_name"] = "Write" }, _ => Adopter).Denied);
        Assert.False(Guard.On(Writing("", Script), _ => Adopter).Denied);

        // A tool that is not a write, carrying the same text, is somebody reading and not writing.
        var reading = new JsonObject
        {
            ["tool_name"] = "Read",
            ["tool_input"] = new JsonObject { ["file_path"] = "D:/adopter/tests/Smoke.cs", ["content"] = Script },
        };
        Assert.False(Guard.On(reading, _ => Adopter).Denied);
    }

    [Fact]
    public void The_project_above_a_file_is_found_by_walking_up_and_read()
    {
        // The one judgement that needs a disk, so it gets one: this tree, whose test project really
        // does reference the engine's source.
        var here = Path.Combine(Repository(), "tests", "Winwright.Tests", "GuardTests.cs");
        var project = Guard.Nearest(here);

        Assert.NotNull(project);
        Assert.Contains(Guard.Engine, project, StringComparison.Ordinal);

        // And this file, written from inside the engine's own tree, is allowed by that reading alone.
        Assert.False(Guard.On(Writing(here, Script), Guard.Nearest).Denied);
    }

    [Fact]
    public void The_plugin_registers_the_guard_on_every_tool_a_write_arrives_through()
    {
        var repository = Repository();
        using var hooks = JsonDocument.Parse(File.ReadAllText(Path.Combine(repository, "hooks", "hooks.json")));

        var registered = hooks.RootElement.GetProperty("hooks").GetProperty("PreToolUse").EnumerateArray().ToList();
        var matcher = Assert.Single(registered).GetProperty("matcher").GetString()!;

        // Read off the guard rather than retyped: a tool added to one and not the other is a write
        // the guard was built to judge and never sees.
        foreach (var tool in Guard.Writing)
            Assert.Contains(tool, matcher.Split('|'));

        var command = Assert.Single(registered[0].GetProperty("hooks").EnumerateArray())
            .GetProperty("command").GetString()!;

        // WW221: through a launcher, so a clone that has not been built gets a sentence naming the
        // build rather than a .NET assembly error on every write.
        Assert.Contains("${CLAUDE_PLUGIN_ROOT}", command, StringComparison.Ordinal);
        Assert.Contains("hooks/winwright-guard.cmd", command, StringComparison.Ordinal);

        var launcher = Path.Combine(repository, "hooks", "winwright-guard.cmd");
        Assert.True(File.Exists(launcher), $"the hook names {command} and there is no {launcher}");
        Assert.Contains("Winwright.Guard.dll", File.ReadAllText(launcher), StringComparison.Ordinal);
    }

    private static JsonObject Writing(string path, string content) => new()
    {
        ["tool_name"] = "Write",
        ["tool_input"] = new JsonObject { ["file_path"] = path, ["content"] = content },
    };

    /// <summary>The repository root, found by walking up to the file that declares the version.</summary>
    private static string Repository()
    {
        var walking = new DirectoryInfo(AppContext.BaseDirectory);
        while (walking is not null && !File.Exists(Path.Combine(walking.FullName, "Directory.Build.props")))
            walking = walking.Parent;

        Assert.NotNull(walking);
        return walking.FullName;
    }
}
