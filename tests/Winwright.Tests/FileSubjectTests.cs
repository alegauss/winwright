using System.Windows.Automation;

using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW535. A step whose subject is a file on disk, for what an act leaves outside any window.
/// <para>
/// Found by quickshell (QS292). A drop onto an Explorer folder lands as a file, and the check that it
/// arrived byte for byte was a C# test beside the cases, polling for the file and comparing arrays,
/// because nothing a case could write reached a file. None of these needs a desk: a file step builds
/// no subject and resolves no locator, which is the whole of what makes it the third kind.
/// </para>
/// </summary>
public sealed class FileSubjectTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-file-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void A_file_step_loads_with_its_path_and_the_file_it_should_match()
    {
        var step = Assert.Single(Loaded("""
            { "file": "{files}/landing/report.bin", "act": "read", "sameBytesAs": "data/report.bin" }
            """).Steps);

        Assert.Null(step.Locator);
        Assert.Null(step.Tray);
        Assert.Equal("{files}/landing/report.bin", step.File);
        Assert.Equal("data/report.bin", step.SameBytesAs);
        Assert.Equal("file '{files}/landing/report.bin'", step.Addressed);
        Assert.True(step.Checkable);
    }

    [Theory]
    [InlineData("""{ "file": "a.bin", "act": "invoke", "sameBytesAs": "b.bin" }""", "the one act it takes is 'read'")]
    [InlineData("""{ "file": "a.bin", "act": "read" }""", "claims nothing about it")]
    [InlineData("""{ "file": "a.bin", "act": "read", "sameBytesAs": "b.bin", "expect": "x" }""", "carries 'expect'")]
    [InlineData("""{ "file": "a.bin", "act": "read", "sameBytesAs": "b.bin", "reads": "value" }""", "carries 'reads'")]
    [InlineData("""{ "locator": "Edit", "act": "read", "sameBytesAs": "b.bin" }""", "names that file in 'file'")]
    [InlineData("""{ "locator": "Edit", "file": "a.bin", "act": "read", "sameBytesAs": "b.bin" }""", "a step acts on one thing")]
    public void What_a_file_step_cannot_say_is_refused_where_it_is_written(string step, string because)
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => Loaded(step));

        Assert.Contains(because, refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_step_built_in_code_naming_a_file_and_a_locator_is_refused_too()
    {
        // The loader refuses this through the schema's group; a caller building a step in code does
        // not come through the loader, so the type says it as well.
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => Wrote.Step("Edit", "read", ("file", "a.bin"), ("sameBytesAs", "b.bin")));

        Assert.Contains("names a locator and a file", refusal.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_file_holding_the_same_bytes_passes()
    {
        var bytes = Written("data/report.bin", 4096);
        Written("out/report.bin", bytes);

        var verdict = Run("""{ "file": "out/report.bin", "act": "read", "sameBytesAs": "data/report.bin" }""");

        Assert.Equal(RunOutcome.Passed, verdict.Outcome);
    }

    [Fact]
    public void A_file_with_different_bytes_fails_naming_the_lengths_and_the_first_byte_that_differs()
    {
        var bytes = Written("data/report.bin", 64);
        var changed = (byte[])bytes.Clone();
        changed[10] ^= 0xFF;
        Written("out/report.bin", changed);

        var verdict = Run("""{ "file": "out/report.bin", "act": "read", "sameBytesAs": "data/report.bin" }""");

        Assert.Equal(RunOutcome.Failed, verdict.Outcome);
        var said = Said(verdict);
        Assert.Contains("holds 64 bytes", said, StringComparison.Ordinal);
        Assert.Contains("first differ at byte 10", said, StringComparison.Ordinal);
    }

    [Fact]
    public void A_file_that_never_came_fails_saying_what_the_folder_holds()
    {
        Written("data/report.bin", 64);
        Written("out/something else.txt", 3);

        var verdict = Run("""{ "file": "out/report.bin", "act": "read", "sameBytesAs": "data/report.bin" }""");

        Assert.Equal(RunOutcome.Failed, verdict.Outcome);
        var said = Said(verdict);
        Assert.Contains("nothing could be read at", said, StringComparison.Ordinal);
        Assert.Contains("'something else.txt'", said, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_file_that_arrives_while_the_step_waits_passes()
    {
        // Explorer writes a dropped file after the release returns, so the step is waited for as any
        // read-back is. The control is the case above: the same step with nothing arriving fails.
        var bytes = Written("data/report.bin", 1024);
        var later = Task.Run(async () =>
        {
            await Task.Delay(300);
            Written("out/report.bin", bytes);
        });

        var verdict = Run("""{ "file": "out/report.bin", "act": "read", "sameBytesAs": "data/report.bin" }""", resolveMs: 3000);
        await later;

        Assert.Equal(RunOutcome.Passed, verdict.Outcome);
    }

    [Fact]
    public void Comparing_with_a_file_that_is_not_there_stops_the_case_as_its_own_fault()
    {
        Written("out/report.bin", 8);

        var verdict = Run("""{ "file": "out/report.bin", "act": "read", "sameBytesAs": "data/missing.bin" }""");

        Assert.Equal(RunOutcome.Broken, verdict.Outcome);
        Assert.Contains("which is not a file", Said(verdict), StringComparison.Ordinal);
    }

    [Fact]
    public void A_path_naming_the_staged_directory_is_read_from_where_the_fixture_staged()
    {
        Written("data/report.bin", 256);
        File.WriteAllText(Path.Combine(root, ProjectDeclaration.FileName), """{ "timeouts": { "resolve": 500, "poll": 50 } }""");
        var project = ProjectDeclaration.Load(Path.Combine(root, ProjectDeclaration.FileName));

        var declared = ScenarioFile.Read("files.cases.json", """
            {
              "fixtures": [ { "name": "staged", "workingDirectory": "{files}", "files": ["data/report.bin"] } ],
              "cases": [
                {
                  "name": "the staged copy is the file it was copied from",
                  "fixture": "staged",
                  "steps": [ { "file": "{files}/report.bin", "act": "read", "sameBytesAs": "data/report.bin" } ]
                }
              ]
            }
            """);

        declared[0].Fixture.Stage(project.Root);
        try
        {
            var verdict = Suite.Run(declared, Selection.All, Desktop(), project);

            Assert.Equal(RunOutcome.Passed, verdict.Outcome);
        }
        finally
        {
            Directory.Delete(declared[0].Fixture.StagedInto(project.Root), recursive: true);
        }
    }

    [Fact]
    public void A_run_names_the_claims_that_begin_with_their_own_case_name_and_still_passes()
    {
        // WW541, read off a whole run: a file step needs no desk, so the reading is the suite's own
        // render rather than a list built beside it.
        var bytes = Written("data/report.bin", 32);
        Written("out/report.bin", bytes);
        File.WriteAllText(Path.Combine(root, ProjectDeclaration.FileName), """{ "timeouts": { "resolve": 300, "poll": 50 } }""");

        var declared = ScenarioFile.Read("echo.cases.json", """
            {
              "cases": [
                {
                  "name": "the report is copied",
                  "catches": "the report is copied, regression",
                  "steps": [ { "file": "out/report.bin", "act": "read", "sameBytesAs": "data/report.bin" } ]
                }
              ]
            }
            """);

        var verdict = Suite.Run(
            declared, Selection.All, Desktop(), ProjectDeclaration.Load(Path.Combine(root, ProjectDeclaration.FileName)));

        Assert.Equal(RunOutcome.Passed, verdict.Outcome);
        Assert.Equal(["the report is copied"], verdict.Echoing.Select(one => one.Name));
        Assert.Contains(
            // WW547. The whole sentence agrees with one case, not only its noun.
            "1 case that ran says what it catches in words that begin or end with its own name",
            Said(verdict),
            StringComparison.Ordinal);
        Assert.Contains("  the report is copied: the report is copied, regression", Said(verdict), StringComparison.Ordinal);
    }

    /// <summary>
    /// The desktop as a root, which a file step never reads but <see cref="Suite.Run"/> asks for.
    /// <para>
    /// Asked again where UI Automation answers with a COM error. Measured on the host gate, where this
    /// class runs beside a thousand others: <c>UiaGetRootNode</c> returned an unexpected HRESULT about
    /// one run in four, and the red was about the test's plumbing rather than about a file step.
    /// </para>
    /// <para>
    /// WW559. Asked again after a pause that grows, and not at once. A guest run whose desk read
    /// 'shell' failed all ten immediate asks 1.3 s in, while the next cases of the same run drove
    /// windows on that desk: ten asks inside a millisecond are one moment asked ten times, and a
    /// collision that lasts a moment longer fails every one. The pauses add to 2.25 s, so a desk that
    /// really is missing still fails, and is still a red.
    /// </para>
    /// </summary>
    internal static AutomationElement Desktop()
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return AutomationElement.RootElement;
            }
            catch (System.Runtime.InteropServices.COMException) when (attempt < 10)
            {
                // The failure is the call colliding with another, not a desk that is missing, so it
                // is given time to clear before the next ask.
                Thread.Sleep(50 * attempt);
            }
        }
    }

    /// <summary>One case holding <paramref name="step"/> and nothing else, loaded.</summary>
    private static CaseDeclaration Loaded(string step) =>
        Assert.Single(ScenarioFile.Read("file.cases.json", $$"""
            { "cases": [ { "name": "a file", "steps": [ {{step}} ] } ] }
            """));

    /// <summary>That case, run with nothing on the desk, under a project rooted in this test's directory.</summary>
    private SuiteVerdict Run(string step, int resolveMs = 300)
    {
        var path = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(path, $$"""{ "timeouts": { "resolve": {{resolveMs}}, "poll": 50 } }""");

        return Suite.Run([Loaded(step)], Selection.All, Desktop(), ProjectDeclaration.Load(path));
    }

    private byte[] Written(string relative, int length)
    {
        var bytes = new byte[length];
        Random.Shared.NextBytes(bytes);
        Written(relative, bytes);
        return bytes;
    }

    private void Written(string relative, byte[] bytes)
    {
        var path = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
    }

    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));
}
