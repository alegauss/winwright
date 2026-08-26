using System.Text.RegularExpressions;

using Winwright.Mcp;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW69. The whole content of an instruction file is loaded on every turn against a budget, which is
/// why the catalogue belongs in a skill and only the rules belong in the file the harness reads. A
/// skill has two costs and they are nothing like each other: the description sits in context on every
/// turn whether or not the skill is ever used, and the body is paid once, when a window is actually
/// in play. So both are measured here rather than assumed to be small.
/// <para>
/// The other half is that the skill names verbs. A skill naming a type the engine renamed is worse
/// than no skill: it sends an agent confidently at something that is not there, and nothing about
/// that goes red. Every name it spells is read back against the assembly.
/// </para>
/// </summary>
public sealed class SkillTests
{
    /// <summary>
    /// What the description may cost. Tight, because this is the half nobody chose to load: it is in
    /// context on turns that never touch a window, so every character is paid on all of them.
    /// </summary>
    private const int Description = 700;

    /// <summary>
    /// What the body may cost. Looser, because it arrives only when a window is in play — but a
    /// budget all the same, or "load the skill" becomes the same instruction file it replaced.
    /// </summary>
    private const int Body = 6000;

    [Fact]
    public void The_skill_fits_its_budget_on_both_of_the_costs_it_has()
    {
        var (description, body) = Read();

        Assert.InRange(description.Length, 1, Description);
        Assert.InRange(body.Length, 1, Body);
    }

    [Fact]
    public void The_description_is_what_decides_the_skill_loads_when_a_window_is_in_play()
    {
        // The claim WW69 is actually about: it loads on a window and not on every turn. What makes
        // that true is the description, so the description has to name the occasion.
        var (description, _) = Read();

        Assert.Contains("window", description, StringComparison.OrdinalIgnoreCase);
        foreach (var occasion in new[] { "WPF", "WinForms", "UI Automation", "locator", "scenario" })
            Assert.Contains(occasion, description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Every_verb_the_skill_names_is_a_verb_the_engine_exports()
    {
        // A skill naming a renamed type sends an agent at something that is not there, and nothing
        // about that goes red on its own. This is what makes it go red.
        var (_, body) = Read();
        var engine = typeof(Winwright.Scenarios.ScenarioFile).Assembly;
        var types = engine.GetExportedTypes().Select(one => one.Name).ToHashSet(StringComparer.Ordinal);
        var namespaces = engine.GetExportedTypes()
            .Select(one => one.Namespace)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        var named = 0;
        foreach (var spelled in Backticked(body))
        {
            if (spelled.StartsWith("Winwright.", StringComparison.Ordinal))
            {
                Assert.Contains(spelled, namespaces);
                named++;
                continue;
            }

            if (!Regex.IsMatch(spelled, @"^[A-Z][A-Za-z0-9]*(\.[A-Za-z][A-Za-z0-9]*)?$"))
                continue;

            Assert.Contains(spelled.Split('.')[0], types);
            named++;
        }

        // A walk that matched nothing would pass this test while checking none of it.
        Assert.True(named > 15, $"only {named} names were read back, which is too few to be the skill");
    }

    [Fact]
    public void Every_tool_the_skill_tells_an_agent_to_ask_is_a_tool_the_server_offers()
    {
        var (_, body) = Read();
        var offered = Served.Tools.Select(one => one.Name).ToList();

        var told = Backticked(body).Where(one => one.StartsWith("winwright_", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(told);
        foreach (var tool in told.Distinct(StringComparer.Ordinal))
            Assert.Contains(tool, offered);

        // And the other direction: a tool the skill never mentions is a tool nothing sends anybody to.
        foreach (var tool in offered)
            Assert.Contains(tool, told);
    }

    [Fact]
    public void The_skill_sits_where_the_plugin_looks_for_it_and_is_named_for_its_own_directory()
    {
        var path = Path.Combine(Repository(), "skills", "winwright", "SKILL.md");
        Assert.True(File.Exists(path), $"there is no {path} for the plugin to carry");

        var frontmatter = Frontmatter(File.ReadAllText(path));
        var name = Regex.Match(frontmatter, @"^name:\s*(\S+)\s*$", RegexOptions.Multiline).Groups[1].Value;

        // The harness resolves a skill by its directory, so a name that disagrees with the directory
        // is a skill nothing reaches by the name written in it.
        Assert.Equal("winwright", name);
    }

    /// <summary>Every token the skill puts in backticks, in the order it spells them.</summary>
    private static IEnumerable<string> Backticked(string body) =>
        Regex.Matches(body, "`([^`\n]+)`").Select(one => one.Groups[1].Value);

    /// <summary>The description and the body, which are the two costs and are measured apart.</summary>
    private static (string Description, string Body) Read()
    {
        var said = File.ReadAllText(Path.Combine(Repository(), "skills", "winwright", "SKILL.md"));
        var frontmatter = Frontmatter(said);
        var description = Regex.Match(frontmatter, "description:\\s*\"(.*)\"", RegexOptions.Singleline);

        Assert.True(description.Success, "the skill declares no description, so nothing decides when it loads");
        return (description.Groups[1].Value, said[(said.IndexOf("\n---", 4, StringComparison.Ordinal) + 4)..]);
    }

    private static string Frontmatter(string said)
    {
        var closing = said.IndexOf("\n---", 4, StringComparison.Ordinal);
        Assert.True(said.StartsWith("---", StringComparison.Ordinal) && closing > 0, "the skill has no frontmatter");
        return said[3..closing];
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
