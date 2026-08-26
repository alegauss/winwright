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
/// </summary>
public sealed class PluginTests
{
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
            ["the MCP server"] = "McpTests.The_plugin_wires_the_server_at_the_path_this_tree_builds_it_to",
            ["the hook"] = "GuardTests.The_plugin_registers_the_guard_on_every_tool_a_write_arrives_through",
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
