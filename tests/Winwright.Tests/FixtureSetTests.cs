using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW214. WW60 declared fixtures at the file and resolved a case's against its own file's, which is
/// the right scope for the refusal and the wrong one for the declaration: a suite is several files,
/// and the launch three of them need was written three times.
/// <para>
/// The second copy is where the flag gains a value the first does not have, and nothing compared
/// them — so every expectation in the second file described an environment nothing put the window
/// into, which is exactly what the single-file refusal prevents one level down.
/// </para>
/// </summary>
public class FixtureSetTests
{
    private const string Declaring = """
        {
          "fixtures": [ { "name": "pt-BR", "environment": "pt-BR", "flag": "--language", "shareable": true } ],
          "cases": [
            {
              "name": "the labels are in the resolved language",
              "fixture": "pt-BR",
              "steps": [ { "locator": "Edit", "act": "set value", "with": "b", "expect": "b" } ]
            }
          ]
        }
        """;

    private const string Naming = """
        {
          "cases": [
            {
              "name": "the menu is in the resolved language",
              "fixture": "pt-BR",
              "steps": [ { "locator": "Edit", "act": "set value", "with": "c", "expect": "c" } ]
            }
          ]
        }
        """;

    [Fact]
    public void A_suite_collects_every_fixture_every_file_declares()
    {
        var suite = FixtureSet.Across([new ScenarioSource("one.cases.json", Declaring), new ScenarioSource("two.cases.json", Naming)]);

        Assert.Equal(1, suite.Count);
        Assert.Equal(["pt-BR"], suite.Names);
        Assert.Equal("one.cases.json", suite.Whose("pt-BR"));
        Assert.True(suite.Named("PT-BR")!.Shareable);
    }

    [Fact]
    public void A_case_may_name_a_launch_the_file_next_door_declared()
    {
        // The whole task. Without this the pt-BR launch is written once per file, and the second
        // copy is the one that stops passing the language.
        var suite = FixtureSet.Across([new ScenarioSource("one.cases.json", Declaring), new ScenarioSource("two.cases.json", Naming)]);

        var only = Assert.Single(ScenarioFile.Read("two.cases.json", Naming, suite));

        Assert.Equal("pt-BR", only.Fixture.Name);
        Assert.Equal("pt-BR", only.Fixture.Environment);
    }

    [Fact]
    public void The_same_case_read_without_the_suite_is_refused_because_its_own_file_declares_none()
    {
        // The single-file scope is still the scope when nobody offers a wider one, and it still
        // refuses rather than quietly launching the application as it comes.
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("two.cases.json", Naming));

        Assert.Contains("no fixture is called 'pt-BR'", refusal.Because);
    }

    [Fact]
    public void Two_files_declaring_one_name_are_refused_and_both_are_named()
    {
        var drifted = """
            {
              "fixtures": [ { "name": "pt-BR", "environment": "pt-BR", "flag": "--culture" } ],
              "cases": [
                { "name": "b", "steps": [ { "locator": "Edit", "act": "set value", "with": "b", "expect": "b" } ] }
              ]
            }
            """;

        var refusal = Assert.Throws<ScenarioRefusedException>(() => FixtureSet.Across(
            [new ScenarioSource("one.cases.json", Declaring), new ScenarioSource("two.cases.json", drifted)]));

        Assert.Equal("pt-BR", refusal.Subject);
        Assert.Contains("declared in one.cases.json and again in two.cases.json", refusal.Because);
        Assert.Contains("names two launches", refusal.Because);
    }

    [Fact]
    public void A_file_read_against_a_suite_that_already_holds_its_own_fixtures_is_not_a_duplicate()
    {
        // The pass that collects the fixtures includes this file, so folding its own back in has to
        // be the same declaration read twice rather than two of them.
        var suite = FixtureSet.Across([new ScenarioSource("one.cases.json", Declaring)]);

        var only = Assert.Single(ScenarioFile.Read("one.cases.json", Declaring, suite));

        Assert.Equal("pt-BR", only.Fixture.Name);
    }

    [Fact]
    public void A_file_declaring_a_name_another_file_already_owns_is_refused_when_it_is_read()
    {
        var suite = FixtureSet.Across([new ScenarioSource("one.cases.json", Declaring)]);

        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("two.cases.json", """
            {
              "fixtures": [ { "name": "pt-BR", "environment": "pt-BR", "flag": "--culture" } ],
              "cases": [
                { "name": "b", "steps": [ { "locator": "Edit", "act": "set value", "with": "b", "expect": "b" } ] }
              ]
            }
            """, suite));

        Assert.Contains("declared in one.cases.json and again in two.cases.json", refusal.Because);
    }

    [Fact]
    public void A_name_the_suite_does_not_have_is_refused_with_the_names_it_does()
    {
        var suite = FixtureSet.Across([new ScenarioSource("one.cases.json", Declaring)]);

        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("two.cases.json", """
            {
              "cases": [
                {
                  "name": "b",
                  "fixture": "pt-br-ish",
                  "steps": [ { "locator": "Edit", "act": "set value", "with": "b", "expect": "b" } ]
                }
              ]
            }
            """, suite));

        Assert.Contains("no fixture is called 'pt-br-ish'", refusal.Because);
        Assert.Contains("'pt-BR'", refusal.Because);
    }

    [Fact]
    public void An_empty_suite_holds_nothing_and_says_so_rather_than_listing_nothing()
    {
        Assert.Equal(0, FixtureSet.Empty.Count);
        Assert.Empty(FixtureSet.Empty.Names);
        Assert.Null(FixtureSet.Empty.Named("pt-BR"));
        Assert.Empty(FixtureSet.Empty.Whose("pt-BR"));
        Assert.Null(FixtureSet.Empty.Named(" "));
    }

    [Fact]
    public void The_directory_reader_resolves_across_the_files_it_read()
    {
        var root = Directory.CreateTempSubdirectory("winwright-suite-fixtures-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(root, $"one{ScenarioFile.Extension}"), Declaring);
            File.WriteAllText(Path.Combine(root, $"two{ScenarioFile.Extension}"), Naming);

            var files = ScenarioFile.LoadAll(root);

            Assert.Equal(2, files.Count);
            Assert.All(
                ScenarioFile.Across(files),
                one => Assert.Equal("pt-BR", one.Fixture.Name));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void The_directory_reader_refuses_the_drift_before_any_case_resolves_against_either_copy()
    {
        var root = Directory.CreateTempSubdirectory("winwright-suite-drift-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(root, $"one{ScenarioFile.Extension}"), Declaring);
            File.WriteAllText(Path.Combine(root, $"two{ScenarioFile.Extension}"), """
                {
                  "fixtures": [ { "name": "pt-BR", "environment": "pt-BR", "flag": "--culture" } ],
                  "cases": [
                    { "name": "b", "steps": [ { "locator": "Edit", "act": "set value", "with": "b", "expect": "b" } ] }
                  ]
                }
                """);

            var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.LoadAll(root));

            Assert.Equal("pt-BR", refusal.Subject);
            Assert.Contains("names two launches", refusal.Because);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
