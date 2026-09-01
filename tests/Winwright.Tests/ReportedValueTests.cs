using Winwright.Asserting;
using Winwright.Projects;
using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW294. `reportedSets` answers a list, and most of what an application knows about itself is not one.
/// <para>
/// Measured reading claude-tray's check script to schedule its migrations: `Expected-ProfileState`
/// pulls eight facts out of a single read-out and only the first is a set — the profile labels. The
/// rest are single values: which profile the icon follows, which one the environment selects, whether
/// those two agree, the shared-transcript directory, and two toggle positions. A case can type none of
/// them, because they are this machine's state rather than the product's vocabulary.
/// </para>
/// <para>
/// `label` is the near miss and answers a different question: it derives from the project's strings,
/// which is right for a word the product ships and wrong for a fact about the desk.
/// </para>
/// </summary>
public sealed class ReportedValueTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-reported-value-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    /// <summary>A project declaring how the application is asked for one value.</summary>
    private ProjectDeclaration Declaring(string named, string arguments)
    {
        var path = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "reportedValues": { {{System.Text.Json.JsonSerializer.Serialize(named)}}: [{{arguments}}] }
            }
            """);

        return ProjectDeclaration.Load(path);
    }

    [Fact]
    public void The_value_is_what_the_application_says_it_is()
    {
        // Nothing here names a profile, and nothing can: it is whatever this machine has. That is the
        // whole difference between this and `expect`.
        var value = DerivedSet.ReportedValue(
            "the profile in use", Declaring("inUse", "\"--profile\""), "inUse");

        Assert.False(string.IsNullOrWhiteSpace(value));

        // And it agrees with the set well, which is what makes a case comparing a mark against it
        // mean anything: the two read-outs answer about one application or neither is evidence.
        var both = Path.Combine(root, "both", ProjectDeclaration.FileName);
        Directory.CreateDirectory(Path.GetDirectoryName(both)!);
        File.WriteAllText(
            both,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "reportedSets": { "all": ["--profiles"] },
              "reportedValues": { "inUse": ["--profile"] }
            }
            """);

        var declared = ProjectDeclaration.Load(both);

        Assert.Contains(
            DerivedSet.ReportedValue("the profile in use", declared, "inUse"),
            DerivedSet.Reported("the profiles", declared, "all").Expected);
    }

    [Fact]
    public void A_read_out_that_answers_several_lines_is_a_set_and_says_so()
    {
        // The refusal that keeps the two wells apart. `--profiles` prints one per line, which is a set
        // — asked for a value, it has answered a different question, and guessing which line was meant
        // is the invention this whole shape refuses.
        var refused = Assert.Throws<UnderivableSetException>(
            () => DerivedSet.ReportedValue("the profile", Declaring("inUse", "\"--profiles\""), "inUse"));

        Assert.Contains("a value is one", refused.Message, StringComparison.Ordinal);
        Assert.Contains("reportedSet", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_value_the_project_does_not_declare_is_refused_with_the_ones_it_does()
    {
        var refused = Assert.Throws<UnderivableSetException>(
            () => DerivedSet.ReportedValue("the account", Declaring("inUse", "\"--profile\""), "account"));

        Assert.Contains("'inUse'", refused.Message, StringComparison.Ordinal);
        Assert.Contains("account", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_step_expecting_a_reported_value_makes_one_claim_and_not_two()
    {
        // It is `expect` with the value read rather than typed, so writing both is the same claim
        // twice and the run would honour whichever the code reads first.
        var step = StepDeclaration.Of("Text#profile", "read", reads: "name", expectReported: "inUse");

        Assert.Equal("inUse", step.ExpectReported);
        Assert.Null(step.Expected);
        Assert.True(step.Checkable);

        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of(
                "Text#profile", "read", reads: "name", expected: "Pessoal", expectReported: "inUse", named: "a"));

        Assert.Contains("a step answers one thing", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void It_is_refused_beside_the_other_well_as_well_as_beside_a_typed_value()
    {
        // WW323, and the defect that task was filed for. `expectReported` was checked against
        // `expect` and against nothing else, so this loaded — and then `CaseRun` resolved the
        // declared string and the branch under it overwrote that with the reported value. The
        // comparison was against one well while the red named the other's key, and a reader of it
        // went to a strings file to correct a label the run had never compared.
        //
        // All three of the label family, because the hole was one field's list and the point of
        // closing it is that no list decides any more.
        foreach (var refused in new[]
        {
            Assert.Throws<ScenarioRefusedException>(
                () => StepDeclaration.Of(
                    "Text#profile", "read", reads: "name", label: "menu.open", expectReported: "inUse")),
            Assert.Throws<ScenarioRefusedException>(
                () => StepDeclaration.Of(
                    "Text#profile", "read", reads: "name", notLabel: "menu.open", expectReported: "inUse")),
            Assert.Throws<ScenarioRefusedException>(
                () => StepDeclaration.Of(
                    "Text#profile", "read", reads: "name", beginsWithLabel: "menu.open", expectReported: "inUse")),
        })
        {
            Assert.Contains("a step answers one thing", refused.Because, StringComparison.Ordinal);
            Assert.Contains("'expectReported'", refused.Because, StringComparison.Ordinal);
        }

        // And the field the case actually wrote is what the refusal names, so an author is told
        // which line to delete rather than which family it belongs to.
        var beside = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of(
                "Text#profile", "read", reads: "name", beginsWithLabel: "menu.open", expectReported: "inUse"));

        Assert.Contains("'beginsWithLabel'", beside.Because, StringComparison.Ordinal);
        Assert.DoesNotContain("'label'", beside.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void The_declaration_refuses_a_value_with_no_arguments_where_it_was_written()
    {
        var path = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "reportedValues": { "inUse": [] }
            }
            """);

        Assert.Contains(
            "with no arguments",
            Assert.Throws<ArgumentException>(() => ProjectDeclaration.Load(path)).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_tray_step_carrying_it_is_refused_with_the_other_claims()
    {
        // WW258's rule reaches the newest field without anybody adding it there: a tray icon has no
        // patterns to read, so a claim about a reading is refused whatever well its value came from.
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of(null, "read", tray: "winwright under test", expectReported: "inUse"));

        Assert.Contains("'expectReported'", refused.Because, StringComparison.Ordinal);
    }
}
