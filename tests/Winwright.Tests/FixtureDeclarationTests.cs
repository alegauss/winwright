using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW60, and the refusal the whole task is. The states a menu exists to report are the ones where
/// the environment disagrees with the application, and on a developer's machine it never does — so
/// without a sampled environment those assertions are only ever unchecked.
/// <para>
/// What is proved here is that one declaration decides both halves. The environment reaches the
/// launch because the launch is built out of the field, and a fixture where the two could disagree
/// is refused rather than run.
/// </para>
/// </summary>
public class FixtureDeclarationTests
{
    [Fact]
    public void A_fixture_that_samples_nothing_launches_the_application_as_it_comes()
    {
        Assert.False(FixtureDeclaration.Plain.Samples);
        Assert.Empty(FixtureDeclaration.Plain.Launching());
        Assert.Empty(FixtureDeclaration.Plain.Variables);
        Assert.False(FixtureDeclaration.Plain.Shareable);
    }

    [Fact]
    public void The_launch_is_built_out_of_the_environment_field_and_not_beside_it()
    {
        // The enforcement. There is no second place to write the language, so there is nothing for
        // the expectations and the window to disagree about.
        var fixture = FixtureDeclaration.Of("pt-BR", environment: "pt-BR", flag: "--language", arguments: ["--names"]);

        Assert.Equal("pt-BR", fixture.Environment);
        Assert.Equal(["--names", "--language=pt-BR"], fixture.Launching());
    }

    [Fact]
    public void An_argument_deciding_the_environment_a_second_time_is_refused()
    {
        // Whichever the application reads last is the one that decides, so the expectations would
        // describe the field's environment and the window would render the argument's.
        var refusal = Assert.Throws<ScenarioRefusedException>(() => FixtureDeclaration.Of(
            "pt-BR", environment: "pt-BR", flag: "--language", arguments: ["--language=en"]));

        Assert.Contains("decides the environment a second time", refusal.Because);
        Assert.Contains("the expectations read only the first", refusal.Because);
    }

    [Fact]
    public void The_same_flag_written_without_a_value_is_the_same_refusal()
    {
        Assert.Contains(
            "decides the environment a second time",
            Assert.Throws<ScenarioRefusedException>(() => FixtureDeclaration.Of(
                "pt-BR", environment: "pt-BR", flag: "--language", arguments: ["--LANGUAGE"])).Because);
    }

    [Fact]
    public void An_environment_that_reaches_the_launch_nowhere_is_refused()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(
            () => FixtureDeclaration.Of("pt-BR", environment: "pt-BR"));

        Assert.Contains("nothing carries it to the launch", refusal.Because);
        Assert.Contains("the window would render another", refusal.Because);
    }

    [Fact]
    public void An_environment_may_travel_as_a_variable_instead_of_as_a_flag()
    {
        var fixture = FixtureDeclaration.Of(
            "pt-BR",
            environment: "pt-BR",
            variables: new Dictionary<string, string> { ["DOTNET_CLI_UI_LANGUAGE"] = "pt-BR" });

        Assert.True(fixture.Samples);
        Assert.Empty(fixture.Launching());
        Assert.Equal("pt-BR", fixture.Variables["DOTNET_CLI_UI_LANGUAGE"]);
    }

    [Fact]
    public void A_flag_with_no_environment_to_pass_through_it_is_refused()
    {
        Assert.Contains(
            "names no environment to pass through it",
            Assert.Throws<ScenarioRefusedException>(
                () => FixtureDeclaration.Of("plainish", flag: "--language")).Because);
    }

    [Fact]
    public void A_fixture_says_what_the_expectations_were_read_against()
    {
        Assert.Equal(
            "pt-BR: sampling pt-BR, shareable.",
            FixtureDeclaration.Of("pt-BR", environment: "pt-BR", flag: "--language", shareable: true).Sentence());

        Assert.Equal("as it comes: the application as it comes.", FixtureDeclaration.Plain.Sentence());
    }

    [Fact]
    public void An_unnamed_fixture_or_a_blank_argument_is_refused()
    {
        Assert.Contains(
            "a fixture is named",
            Assert.Throws<ScenarioRefusedException>(() => FixtureDeclaration.Of(" ")).Because);

        Assert.Contains(
            "a blank argument says nothing",
            Assert.Throws<ScenarioRefusedException>(() => FixtureDeclaration.Of("a", arguments: [" "])).Because);
    }

    [Fact]
    public void The_launch_it_describes_carries_its_arguments_and_its_variables()
    {
        var fixture = FixtureDeclaration.Of(
            "pt-BR",
            environment: "pt-BR",
            flag: "--language",
            arguments: ["--names"],
            variables: new Dictionary<string, string> { ["WINWRIGHT_SAMPLE"] = "1" });

        var start = fixture.Starting(@"C:\app\YourApp.exe");

        Assert.Equal(@"C:\app\YourApp.exe", start.FileName);
        Assert.Equal(["--names", "--language=pt-BR"], start.ArgumentList);
        Assert.Equal("1", start.Environment["WINWRIGHT_SAMPLE"]);
        Assert.False(start.UseShellExecute);
    }

    [Fact]
    public void A_case_names_a_fixture_its_own_file_declares_and_nothing_else()
    {
        var cases = ScenarioFile.Read("one.cases.json", """
            {
              "fixtures": [
                { "name": "pt-BR", "environment": "pt-BR", "flag": "--language", "shareable": true }
              ],
              "cases": [
                {
                  "name": "the menu is labelled in the resolved language",
                  "fixture": "pt-BR",
                  "onlyReads": true,
                  "steps": [ { "locator": "Edit", "act": "set value", "with": "b", "expect": "b" } ]
                }
              ]
            }
            """);

        var only = Assert.Single(cases);
        Assert.Equal("pt-BR", only.Fixture.Name);
        Assert.True(only.Fixture.Shareable);
        Assert.True(only.OnlyReads);
    }

    [Fact]
    public void A_case_naming_a_fixture_nothing_declares_is_refused_with_the_ones_there_are()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            {
              "fixtures": [ { "name": "pt-BR", "environment": "pt-BR", "flag": "--language" } ],
              "cases": [
                {
                  "name": "a",
                  "fixture": "pt-br-ish",
                  "steps": [ { "locator": "Edit", "act": "set value", "with": "b", "expect": "b" } ]
                }
              ]
            }
            """));

        Assert.Equal("one.cases.json cases[0].fixture", refusal.Subject);
        Assert.Contains("no fixture is called 'pt-br-ish'", refusal.Because);
        Assert.Contains("'pt-BR'", refusal.Because);
    }

    [Fact]
    public void A_case_naming_a_fixture_in_a_file_that_declares_none_says_that_rather_than_listing_nothing()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            {
              "cases": [
                {
                  "name": "a",
                  "fixture": "pt-BR",
                  "steps": [ { "locator": "Edit", "act": "set value", "with": "b", "expect": "b" } ]
                }
              ]
            }
            """));

        Assert.Contains("this file declares no 'fixtures'", refusal.Because);
    }

    [Fact]
    public void A_fixtures_own_refusal_arrives_at_its_address_in_the_file()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            {
              "fixtures": [ { "name": "pt-BR", "environment": "pt-BR" } ],
              "cases": [
                { "name": "a", "steps": [ { "locator": "Edit", "act": "set value", "with": "b", "expect": "b" } ] }
              ]
            }
            """));

        Assert.StartsWith("one.cases.json fixtures[0] (", refusal.Subject);
        Assert.Contains("nothing carries it to the launch", refusal.Because);
    }

    [Fact]
    public void A_fixture_declared_twice_is_refused_because_a_case_naming_it_names_two()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            {
              "fixtures": [
                { "name": "pt-BR", "environment": "pt-BR", "flag": "--language" },
                { "name": "PT-BR", "environment": "pt-BR", "flag": "--culture" }
              ],
              "cases": [
                { "name": "a", "steps": [ { "locator": "Edit", "act": "set value", "with": "b", "expect": "b" } ] }
              ]
            }
            """));

        Assert.Contains("so a case naming it names two", refusal.Because);
    }

    [Fact]
    public void A_key_the_file_itself_does_not_have_is_refused_rather_than_ignored()
    {
        // The same hole one level up: a misspelled 'fixtres' that loads is every case in the file
        // launched against the application as it comes, describing an environment nothing set up.
        var refusal = Assert.Throws<ScenarioRefusedException>(() => ScenarioFile.Read("one.cases.json", """
            {
              "fixtres": [ { "name": "pt-BR", "environment": "pt-BR", "flag": "--language" } ],
              "cases": [
                { "name": "a", "steps": [ { "locator": "Edit", "act": "set value", "with": "b", "expect": "b" } ] }
              ]
            }
            """));

        Assert.Contains("there is no such field", refusal.Because);
        Assert.Contains("fixtures", refusal.Because);
    }

    [Fact]
    public void A_file_reports_only_the_fixtures_its_cases_actually_name()
    {
        var path = Path.Combine(
            Directory.CreateTempSubdirectory("winwright-fixtures-").FullName, $"one{ScenarioFile.Extension}");

        try
        {
            File.WriteAllText(path, """
                {
                  "fixtures": [
                    { "name": "pt-BR", "environment": "pt-BR", "flag": "--language" },
                    { "name": "nobody uses this", "arguments": ["--names"] }
                  ],
                  "cases": [
                    {
                      "name": "a",
                      "fixture": "pt-BR",
                      "steps": [ { "locator": "Edit", "act": "set value", "with": "b", "expect": "b" } ]
                    }
                  ]
                }
                """);

            Assert.Equal(["pt-BR"], ScenarioFile.Load(path).Fixtures.Select(one => one.Name));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }
}
