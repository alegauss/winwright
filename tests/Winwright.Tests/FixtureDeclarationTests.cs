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

        var start = fixture.Starting(@"C:\app\YourApp.exe", @"C:\checkout");

        Assert.Equal(@"C:\app\YourApp.exe", start.FileName);
        Assert.Equal(["--names", "--language=pt-BR"], start.ArgumentList);
        Assert.Equal("1", start.Environment["WINWRIGHT_SAMPLE"]);
        Assert.False(start.UseShellExecute);
    }

    /// <summary>
    /// WW508. The defect quickshell found: a fixture passing `--import cases/fixtures/MobaXterm.ini`
    /// was passing a path the launched application resolved against whichever directory the test
    /// runner happened to be in, so the same argument meant three things under `run-tests.cmd`,
    /// under `dotnet test` and in the guest.
    /// </summary>
    [Fact]
    public void A_launch_that_declares_no_directory_starts_in_the_project_root()
    {
        var start = FixtureDeclaration.Of("plain").Starting(@"C:\app\YourApp.exe", @"C:\checkout");

        Assert.Equal(@"C:\checkout", start.WorkingDirectory);
    }

    [Fact]
    public void A_declared_directory_is_resolved_against_that_same_root()
    {
        var fixture = FixtureDeclaration.Of("imported", workingDirectory: "cases/fixtures");

        Assert.Equal(@"C:\checkout\cases\fixtures", fixture.StartsIn(@"C:\checkout"));
        Assert.Equal(@"C:\checkout\cases\fixtures", fixture.Starting(@"C:\app\YourApp.exe", @"C:\checkout").WorkingDirectory);
    }

    /// <summary>
    /// The rule <see cref="ProjectDeclaration"/> already applies to every path it declares, which is
    /// why this one is not its own: a directory that is already absolute is left where it is, and one
    /// naming a variable is expanded.
    /// </summary>
    [Fact]
    public void A_directory_resolves_the_way_every_other_declared_path_does()
    {
        Assert.Equal(
            @"D:\elsewhere", FixtureDeclaration.Of("absolute", workingDirectory: @"D:\elsewhere").StartsIn(@"C:\checkout"));

        System.Environment.SetEnvironmentVariable("WINWRIGHT_WW508", @"D:\expanded");
        try
        {
            Assert.Equal(
                @"D:\expanded",
                FixtureDeclaration.Of("expanded", workingDirectory: "%WINWRIGHT_WW508%").StartsIn(@"C:\checkout"));
        }
        finally
        {
            System.Environment.SetEnvironmentVariable("WINWRIGHT_WW508", null);
        }
    }

    /// <summary>
    /// Declared rather than resolved, because a resolved path is this machine's and a report two
    /// people compare has to say the same thing on both. Said only where the fixture named one.
    /// </summary>
    [Fact]
    public void A_report_names_the_directory_a_fixture_declared_and_says_nothing_where_it_declared_none()
    {
        Assert.Equal(
            "imported: the application as it comes, starting in cases/fixtures.",
            FixtureDeclaration.Of("imported", workingDirectory: "cases/fixtures").Sentence());

        Assert.Equal("plain: the application as it comes.", FixtureDeclaration.Of("plain").Sentence());
    }

    /// <summary>
    /// WW509. The defect quickshell found at QS217: the client reads its saved sessions from a file
    /// under the user's AppData, so a case that searches the session list had nowhere to put a store
    /// holding known ones. A variable alone does not reach an application that reads no flag.
    /// </summary>
    [Fact]
    public void A_staged_file_reaches_the_launch_through_the_token_a_variable_names()
    {
        var fixture = FixtureDeclaration.Of(
            "with sessions",
            variables: new Dictionary<string, string> { ["APPDATA"] = FixtureDeclaration.Staged },
            files: ["cases/fixtures/sessions.json"]);

        var into = fixture.StagedInto(@"C:\checkout");
        var start = fixture.Starting(@"C:\app\YourApp.exe", @"C:\checkout");

        Assert.Equal(into, start.Environment["APPDATA"]);
        Assert.DoesNotContain(FixtureDeclaration.Staged, start.Environment["APPDATA"]);
    }

    /// <summary>
    /// Under the system temp and not the checkout: an engine that writes into an adopter's working
    /// tree is one that turns up in their `git status`. Per project and per fixture, which is what a
    /// launch is keyed by.
    /// </summary>
    [Fact]
    public void Where_files_are_staged_is_this_project_and_this_fixture_and_is_not_the_checkout()
    {
        var one = FixtureDeclaration.Of("sessions", workingDirectory: FixtureDeclaration.Staged);
        var other = FixtureDeclaration.Of("no sessions", workingDirectory: FixtureDeclaration.Staged);

        var mine = one.StagedInto(@"C:\checkout");

        Assert.StartsWith(System.IO.Path.GetTempPath(), mine, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"C:\checkout", mine, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(mine, other.StagedInto(@"C:\checkout"));
        Assert.NotEqual(mine, one.StagedInto(@"D:\elsewhere"));
        Assert.Equal(mine, one.StagedInto(@"C:\checkout"));
    }

    /// <summary>
    /// The token works in `workingDirectory` too, which makes staging and starting one story: put
    /// the files somewhere of this launch's own, and start the application in it.
    /// </summary>
    [Fact]
    public void The_token_resolves_in_the_working_directory_as_well_as_in_a_variable()
    {
        var fixture = FixtureDeclaration.Of("in its own", workingDirectory: FixtureDeclaration.Staged);

        Assert.Equal(fixture.StagedInto(@"C:\checkout"), fixture.StartsIn(@"C:\checkout"));
    }

    /// <summary>
    /// WW60's refusal one field over. Files staged where nothing is looking are files the case was
    /// written to read and never read — and the reading it takes instead is the real machine's,
    /// which is green and about the wrong store.
    /// </summary>
    [Fact]
    public void A_fixture_staging_files_that_the_launch_names_nowhere_is_refused()
    {
        var refusal = Assert.Throws<ScenarioRefusedException>(() => FixtureDeclaration.Of(
            "nothing looks",
            variables: new Dictionary<string, string> { ["WINWRIGHT_ROLE"] = "reader" },
            files: ["cases/fixtures/sessions.json"]));

        Assert.Contains(FixtureDeclaration.Staged, refusal.Because, StringComparison.Ordinal);
        Assert.Contains("cannot be looking at them", refusal.Because, StringComparison.Ordinal);
    }

    /// <summary>
    /// A value naming no token is handed over as written, because the alternative WW509's first
    /// draft asked for — every value resolved as a path — turns `reader` into a directory.
    /// </summary>
    [Fact]
    public void A_variable_that_names_no_token_is_passed_exactly_as_it_was_written()
    {
        var fixture = FixtureDeclaration.Of(
            "plain values",
            variables: new Dictionary<string, string> { ["WINWRIGHT_ROLE"] = "reader", ["SHAPE"] = "{\"json\":1}" });

        var start = fixture.Starting(@"C:\app\YourApp.exe", @"C:\checkout");

        Assert.Equal("reader", start.Environment["WINWRIGHT_ROLE"]);
        Assert.Equal("{\"json\":1}", start.Environment["SHAPE"]);
    }

    /// <summary>
    /// Emptied first, because a launch inheriting what the last one left is the global state this
    /// field exists to remove. A fixture declaring no files still gets the directory, so a variable
    /// naming the token with nothing staged is an empty store of this launch's own.
    /// </summary>
    [Fact]
    public void Staging_puts_the_declared_files_in_that_directory_and_empties_it_first()
    {
        var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"ww509-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(root, "cases", "fixtures"));
        System.IO.File.WriteAllText(System.IO.Path.Combine(root, "cases", "fixtures", "sessions.json"), "[]");

        var fixture = FixtureDeclaration.Of(
            "with sessions",
            workingDirectory: FixtureDeclaration.Staged,
            files: ["cases/fixtures/sessions.json"]);

        try
        {
            var into = fixture.Stage(root);
            Assert.Equal("[]", System.IO.File.ReadAllText(System.IO.Path.Combine(into, "sessions.json")));

            // What the application wrote last time, which the next launch must not inherit.
            System.IO.File.WriteAllText(System.IO.Path.Combine(into, "written-by-the-app.json"), "{}");

            Assert.Equal(into, fixture.Stage(root));
            Assert.False(System.IO.File.Exists(System.IO.Path.Combine(into, "written-by-the-app.json")));
            Assert.Equal("[]", System.IO.File.ReadAllText(System.IO.Path.Combine(into, "sessions.json")));

            // And one that stages nothing still gets an emptied directory of its own.
            var empty = FixtureDeclaration.Of("empty store", workingDirectory: FixtureDeclaration.Staged);
            Assert.Empty(System.IO.Directory.GetFiles(empty.Stage(root)));
        }
        finally
        {
            System.IO.Directory.Delete(root, recursive: true);
            foreach (var one in new[] { fixture, FixtureDeclaration.Of("empty store", workingDirectory: FixtureDeclaration.Staged) })
            {
                if (System.IO.Directory.Exists(one.StagedInto(root)))
                    System.IO.Directory.Delete(one.StagedInto(root), recursive: true);
            }
        }
    }

    /// <summary>
    /// Named as the fixture wrote it and as it resolved, because the two are the whole question: a
    /// reader who sees only one cannot tell a missing file from a path that meant something else.
    /// </summary>
    [Fact]
    public void Staging_a_file_that_is_not_there_is_refused_naming_both_what_was_written_and_where_it_looked()
    {
        var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"ww509-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(root);

        var fixture = FixtureDeclaration.Of(
            "missing", workingDirectory: FixtureDeclaration.Staged, files: ["cases/fixtures/sessions.json"]);

        try
        {
            var refusal = Assert.Throws<ScenarioRefusedException>(() => fixture.Stage(root));

            Assert.Contains("cases/fixtures/sessions.json", refusal.Because, StringComparison.Ordinal);
            Assert.Contains(System.IO.Path.Combine(root, "cases", "fixtures", "sessions.json"), refusal.Because, StringComparison.Ordinal);
        }
        finally
        {
            System.IO.Directory.Delete(root, recursive: true);
            if (System.IO.Directory.Exists(fixture.StagedInto(root)))
                System.IO.Directory.Delete(fixture.StagedInto(root), recursive: true);
        }
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
    public void A_case_naming_a_fixture_nothing_in_the_suite_declares_says_that_rather_than_listing_nothing()
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

        Assert.Contains("the suite declares no fixtures", refusal.Because);
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
