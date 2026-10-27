using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW577. A step that waits a timeout of its own, by the name the project declares it under.
/// <para>
/// Found by Modelado: a cloud job takes minutes between Send and the result, and the one step reading
/// it could wait that long only once the whole project's <c>resolve</c> was raised to ten minutes,
/// which every <c>absent</c> step in the gate would then wait out. Driven with file steps, which wait
/// over <c>resolve</c> like any read and need no desk: a file that arrives after the project's own
/// wait is read by the step that names a longer one, and missed by the same step without it.
/// </para>
/// </summary>
public sealed class NamedWaitTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-named-wait-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public async Task A_step_naming_a_longer_wait_reads_what_arrives_after_the_projects_own()
    {
        var verdict = await Arriving("""{ "file": "out/result.bin", "act": "read", "sameBytesAs": "data/result.bin", "waits": "slow" }""");

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public async Task The_same_step_without_it_gives_up_at_the_projects_own_wait()
    {
        // The control: a step that passed whatever it waited would pass this as well.
        var verdict = await Arriving("""{ "file": "out/result.bin", "act": "read", "sameBytesAs": "data/result.bin" }""");

        Assert.Equal(RunOutcome.Failed, verdict.Outcome);
    }

    [Fact]
    public void A_wait_the_project_does_not_declare_is_refused_before_the_launch()
    {
        File.WriteAllText(Path.Combine(root, ProjectDeclaration.FileName), """{ "timeouts": { "resolve": 300 } }""");
        var project = ProjectDeclaration.Load(Path.Combine(root, ProjectDeclaration.FileName));
        var declared = ScenarioFile.Read("wait.cases.json", """
            { "cases": [ { "name": "a cloud job", "steps": [
              { "file": "out/result.bin", "act": "read", "sameBytesAs": "data/result.bin", "waits": "cloud" } ] } ] }
            """);

        var gap = Assert.Single(Suite.Undeclared(declared, project));

        Assert.Equal("timeouts.cloud", gap.Key);
        Assert.Contains("'timeouts.cloud'", gap.Because(project.Path), StringComparison.Ordinal);
    }

    [Fact]
    public void A_named_wait_stands_in_for_both_waits_a_step_takes_and_for_nothing_else()
    {
        var timeouts = Timeouts.Declared(new Dictionary<string, int> { ["cloud"] = 600000 }, "winwright.json");

        var stretched = timeouts.Stretched("cloud");

        Assert.Equal(600000, stretched.For("resolve"));
        Assert.Equal(600000, stretched.For("act"));
        Assert.Equal(timeouts.For("launch"), stretched.For("launch"));
        Assert.Equal(timeouts.For("poll"), stretched.For("poll"));
        Assert.Equal(5000, timeouts.For("resolve"));
    }

    /// <summary>
    /// Run <paramref name="step"/> while the file it reads arrives a second and a half in, under a
    /// project whose own resolve is 300 ms and whose 'slow' wait is ten seconds.
    /// </summary>
    private async Task<SuiteVerdict> Arriving(string step)
    {
        var bytes = new byte[256];
        Random.Shared.NextBytes(bytes);
        Directory.CreateDirectory(Path.Combine(root, "data"));
        Directory.CreateDirectory(Path.Combine(root, "out"));
        await File.WriteAllBytesAsync(Path.Combine(root, "data", "result.bin"), bytes);

        var path = Path.Combine(root, ProjectDeclaration.FileName);
        await File.WriteAllTextAsync(path, """{ "timeouts": { "resolve": 300, "poll": 50, "slow": 10000 } }""");
        var project = ProjectDeclaration.Load(path);
        var declared = ScenarioFile.Read("wait.cases.json", $$"""{ "cases": [ { "name": "a slow result", "steps": [ {{step}} ] } ] }""");

        var late = Task.Run(async () =>
        {
            await Task.Delay(1500);
            await File.WriteAllBytesAsync(Path.Combine(root, "out", "result.bin"), bytes);
        });

        var verdict = await Task.Run(() => Suite.Run(declared, Selection.All, FileSubjectTests.Desktop(), project));
        await late;
        return verdict;
    }

    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));
}
