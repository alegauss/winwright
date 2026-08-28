using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW295. WW273 let a locator name an element by a string the project declares and WW294 let an
/// expectation be a value the application reports. The two did not meet, and the claim that needs them
/// to is the one claude-tray's submenu is actually about: <em>the check mark is on exactly one entry,
/// and that entry is the profile the icon follows</em>. The icon's profile comes from the read-out and
/// the entry is one of many the locator matched, so a case has to select <em>that</em> one — and the
/// only interpolation a locator had resolved through the strings.
/// <para>
/// Two wells and two spellings, kept apart by the prefix rather than by lookup order. `{a.key}` is
/// answered before the window is and reads the same on every desk; `{report:name}` is answered by
/// asking the application, which is the point — no case can name what this machine's icon follows.
/// </para>
/// </summary>
public sealed class ReportedLocatorTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-reported-locator-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    /// <summary>A project declaring both wells, so a locator can be built out of either.</summary>
    private ProjectDeclaration Both()
    {
        File.WriteAllText(
            Path.Combine(root, "strings.en.json"),
            """{ "nav": { "about": "About" } }""");

        var path = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}},
              "languageFiles": ["strings.en.json"],

              // Declared because this desk is not in English: the strings well refuses rather than
              // answering in a language nobody is looking at, which is the rule the second well is
              // deliberately not subject to — a machine's own state has no language.
              "language": { "fallback": "en" },
              "reportedValues": { "inUse": ["--profile"] }
            }
            """);

        return ProjectDeclaration.Load(path);
    }

    /// <summary>One step's locator, as the run would have substituted it.</summary>
    private static string Substituted(ProjectDeclaration project, string locator)
    {
        var declared = ScenarioFile.Read(
            "reported.cases.json",
            $$"""
            {
              "cases": [
                {
                  "name": "a",
                  "steps": [
                    {
                      "locator": {{System.Text.Json.JsonSerializer.Serialize(locator)}},
                      "act": "read",

                      // The reading is the check mark and never the name: the locator matched on the
                      // name, so a step reading it back would have its answer fixed before the act —
                      // which is the shape a real claim about this submenu takes anyway.
                      "reads": "toggle",
                      "answers": true,
                      "named": "the entry the application pointed at"
                    }
                  ]
                }
              ]
            }
            """);

        // Through the engine's own substitution rather than a copy of it, which is what makes this a
        // check about the run and not about a helper written beside it.
        var naming = typeof(CaseRun).GetMethod(
            "Naming",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(naming);

        var step = Assert.Single(Assert.Single(declared).Steps);
        var named = (StepDeclaration)naming!.Invoke(
            null, [step, project, null, new Dictionary<string, string>(StringComparer.Ordinal)])!;

        return named.Addressed;
    }

    [Fact]
    public void A_locator_can_be_built_out_of_what_the_application_reports()
    {
        // Nothing here names a profile, and nothing can: it is whatever this machine has. That is the
        // whole point — the entry a check mark should be on is chosen by the application's own answer.
        var project = Both();
        var reported = Substituted(project, """MenuItem[name="{report:inUse}"]""");

        Assert.DoesNotContain("{report:", reported, StringComparison.Ordinal);
        Assert.Contains("MenuItem[name=", reported, StringComparison.Ordinal);

        // And it is the value the read-out gives, read through the same door a case would.
        Assert.Contains(
            Winwright.Asserting.DerivedSet.ReportedValue("the profile", project, "inUse"),
            reported,
            StringComparison.Ordinal);
    }

    [Fact]
    public void The_strings_well_is_untouched_and_still_answers_its_own_spelling()
    {
        // The control for the one above. Adding a second well must not change when or how the first
        // resolves — `{a.key}` is answered before the window is, and nothing about it launches
        // anything.
        Assert.Contains(
            "About",
            Substituted(Both(), """Text[name="{nav.about}"]"""),
            StringComparison.Ordinal);
    }

    [Fact]
    public void One_locator_may_carry_both_and_each_goes_to_its_own_well()
    {
        var project = Both();
        var reported = Substituted(project, """Pane[name="{nav.about}"] > MenuItem[name="{report:inUse}"]""");

        Assert.Contains("About", reported, StringComparison.Ordinal);
        Assert.Contains(
            Winwright.Asserting.DerivedSet.ReportedValue("the profile", project, "inUse"),
            reported,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_reported_name_the_project_does_not_declare_is_refused_with_the_ones_it_does()
    {
        // The refusal arrives as a scenario refusal rather than as a locator that matched nothing: a
        // name no project declares is wrong on every machine, and a red about an element would send
        // the reader to the application for something the file got wrong.
        // Unwrapped here rather than in the helper: a helper that caught would be a reading that
        // swallows and answers a value, which this suite has a rule about — and the envelope is
        // reflection's rather than anything the run would raise.
        var wrapped = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => Substituted(Both(), """MenuItem[name="{report:nothingDeclaresThis}"]"""));

        var refused = Assert.IsType<ScenarioRefusedException>(wrapped.InnerException);

        Assert.Contains("nothingDeclaresThis", refused.Because, StringComparison.Ordinal);
        Assert.Contains("'inUse'", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void The_application_is_asked_once_however_many_braces_name_it()
    {
        // A locator is substituted per step and per member of a repeated case, so asking the
        // application for each would be a process per substitution — and a value that moved between
        // two of them would leave one step selecting something the next one does not.
        var project = Both();
        var asked = new Dictionary<string, string>(StringComparer.Ordinal);

        var naming = typeof(CaseRun).GetMethod(
            "Naming",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var declared = ScenarioFile.Read(
            "reported.cases.json",
            """
            {
              "cases": [
                {
                  "name": "a",
                  "steps": [
                    { "locator": "MenuItem[name=\"{report:inUse}\"]", "act": "read", "reads": "toggle", "answers": true }
                  ]
                }
              ]
            }
            """);

        var step = Assert.Single(Assert.Single(declared).Steps);

        naming!.Invoke(null, [step, project, null, asked]);
        naming.Invoke(null, [step, project, null, asked]);

        // One entry, whatever the run asked for: the second substitution read what the first learnt.
        Assert.Equal(["inUse"], asked.Keys);
    }
}
