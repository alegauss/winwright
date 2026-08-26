using System.Text.Json;

using Xunit;

namespace Winwright.Tests;


/// <summary>
/// WW65, and the criterion the whole of block H rests on: the plugin is the whole installation. Two
/// commands wire it and committing one file wires every clone — which is only true while everything
/// the plugin carries is actually declared by it and actually points at something this tree
/// produces.
/// <para>
/// The per-surface wiring is read where that surface is tested: <see cref="McpTests"/> reads the
/// server's command against the framework this tree targets, <see cref="GuardTests"/> reads the
/// hook's matcher against the tools the guard judges, and <see cref="SkillTests"/> reads the skill's
/// name against its own directory. What is missing without this file is the count: a surface that
/// exists, is tested, and is declared by nothing arrives in no adopter's session, and every test
/// about it goes on passing.
/// </para>
/// <para>
/// Serial, and settling on the way out: WW221's check runs a launcher, which puts a process on the
/// machine beside the cases that need the foreground — and the directory it ran out of cannot be
/// deleted until that process has left, not merely stopped.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class PluginTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-unbuilt-").FullName;

    /// <summary>
    /// After the settling in each case, which is what the ordering here is for: a <c>using</c> inside
    /// the case lets go before this runs, so nothing is still holding what this deletes.
    /// </summary>
    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void Everything_the_plugin_carries_is_declared_by_it_and_points_at_this_tree()
    {
        var repository = Repository();
        using var manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(repository, ".claude-plugin", "plugin.json")));

        // The server, declared by a path the manifest names. A declaration pointing at nothing is a
        // session with no tools, and the manifest is the only place that says where to look.
        // Only the leading "./" comes off. Trimming dots would eat the one in '.claude-plugin', which
        // is a path this repository really has and a directory it really does not.
        var declared = manifest.RootElement.GetProperty("mcpServers").GetString()!;
        var relative = declared.StartsWith("./", StringComparison.Ordinal) ? declared[2..] : declared;
        var wiring = Path.Combine(repository, relative.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(wiring), $"the manifest declares {declared} and there is no {wiring}");

        // The hook and the skill, at the two paths the harness looks for without being told. They
        // need no manifest key, which is exactly why nothing else would notice them going missing.
        Assert.True(
            File.Exists(Path.Combine(repository, "hooks", "hooks.json")),
            "the plugin registers no hook, so a hand-written harness is the path of least resistance again");
        Assert.True(
            File.Exists(Path.Combine(repository, "skills", "winwright", "SKILL.md")),
            "the plugin carries no skill, so which loop answers which question is prose somebody has to find");

        // And the marketplace offers this plugin out of this repository, or the first of the two
        // commands installs something else.
        using var marketplace = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(repository, ".claude-plugin", "marketplace.json")));

        var offered = marketplace.RootElement.GetProperty("plugins").EnumerateArray().ToList();
        var named = manifest.RootElement.GetProperty("name").GetString();
        Assert.Contains(named, offered.Select(one => one.GetProperty("name").GetString()));
    }

    [Fact]
    public void Every_surface_the_plugin_declares_is_one_a_case_in_this_suite_reads_back()
    {
        // The catalogue shape this project uses everywhere else, at the smallest scale it is useful:
        // a surface added to the plugin and paired with nothing is a surface whose wiring is checked
        // as often as somebody installs the plugin and looks.
        var read = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["the MCP server"] = "McpTests.The_plugin_wires_the_server_through_the_launcher_that_can_say_it_is_not_built",
            ["the hook"] = "GuardTests.The_plugin_registers_the_guard_on_every_tool_a_write_arrives_through",
            ["the launchers"] = "PluginTests.A_launcher_with_nothing_built_under_it_says_so_and_names_the_build",
            ["the skill"] = "SkillTests.The_skill_sits_where_the_plugin_looks_for_it_and_is_named_for_its_own_directory",
            ["the version it carries"] = "ConcordanceTests.The_plugin_an_adopter_installs_is_a_copy_of_the_engine_and_is_read_as_one",
        };

        var here = typeof(PluginTests).Assembly;
        foreach (var (surface, paired) in read)
        {
            var split = paired.Split('.');
            var type = here.GetTypes().SingleOrDefault(one => one.Name == split[0]);

            Assert.NotNull(type);
            Assert.True(
                type.GetMethod(split[1]) is not null,
                $"{surface} is paired with {paired}, and there is no such case");
        }
    }

    [Theory]
    [InlineData("hooks", "winwright-guard.cmd", "nothing in this session is refusing a hand-written harness")]
    [InlineData("tools", "winwright-mcp.cmd", "this session has no winwright tools")]
    public void A_launcher_with_nothing_built_under_it_says_so_and_names_the_build(
        string directory, string launcher, string names)
    {
        // WW221, provoked rather than waited for. The launcher finds its assembly relative to itself,
        // so a copy of it in an empty tree is exactly the fresh clone an adopter has after the two
        // install commands — and what that adopter used to get was a .NET error about a missing file.
        var into = Directory.CreateDirectory(Path.Combine(root, directory)).FullName;
        var copied = Path.Combine(into, launcher);
        File.Copy(Path.Combine(Repository(), directory, launcher), copied);

        var ran = Run(copied, into);

        // Loud: it exits non-zero and says both what is missing and what to run. Not blocking: exit 2
        // on a PreToolUse hook denies the write, and denying every write because a build is missing is
        // the guard standing in front of everything rather than in front of a harness script.
        Assert.Equal(1, ran.ExitCode);
        Assert.Contains(names, ran.Said, StringComparison.Ordinal);
        Assert.Contains("dotnet build -c Release", ran.Said, StringComparison.Ordinal);

        // On stderr, because stdout on the server is the protocol and stdout on the hook is the
        // decision: a sentence written to either would be read as one of those.
        Assert.Empty(ran.Out.Trim());
    }

    /// <summary>
    /// Run a launcher and keep both streams apart, which is half of what is asserted.
    /// <para>
    /// Through the register, because nothing here starts a process outside it — and inside a settling
    /// register, because this class deletes the directory the run happened in and stopped is not
    /// gone. <c>Attachable.Launch</c> is the wrong door for this one: it waits until the process says
    /// what it is running, and a launcher that refuses in milliseconds can be gone before it does.
    /// </para>
    /// <para>
    /// The two streams go to files rather than to pipes because <c>LaunchedProcess</c> does not hand
    /// them out, and that is right — the register is about what is running, not about what it says.
    /// Widening it so a test could read a pipe would be the test deciding the engine's shape.
    /// </para>
    /// </summary>
    private static (int ExitCode, string Out, string Said) Run(string launcher, string into)
    {
        var output = Path.Combine(into, "out.txt");
        var errors = Path.Combine(into, "err.txt");

        // The redirection is written into a wrapper beside the launcher rather than passed as an
        // argument: `cmd /c` wants it as part of one command string, and .NET quotes each argument it
        // is given, so the operators arrive as literals and nothing is redirected at all. Measured —
        // the first version of this created neither file.
        //
        // `< NUL` is why the input is closed: the server would otherwise sit waiting for a message
        // nobody is going to send, and this is about the run that never gets that far.
        var wrapper = Path.Combine(into, "run.cmd");
        File.WriteAllText(
            wrapper,
            $"""
            @echo off
            call "{launcher}" < NUL > "{output}" 2> "{errors}"
            exit /b %ERRORLEVEL%
            """);

        using var settling = Attachable.Settling();
        var started = settling.Register.Launch(
            new System.Diagnostics.ProcessStartInfo(wrapper) { UseShellExecute = false, CreateNoWindow = true });

        Assert.True(started.WaitForExit(30000), $"{launcher} did not exit");
        return (started.ExitCode, File.ReadAllText(output), File.ReadAllText(errors));
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
}
