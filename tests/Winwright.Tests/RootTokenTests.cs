using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW542. A token naming the project's root, filled in a step's <c>with</c> and wherever
/// <c>{files}</c> already was.
/// <para>
/// Found by quickshell (QS292): a case typed the guest's checkout path whole into a path box, and was
/// red on any desk where the checkout sat anywhere else. These need no desk; the case that types one
/// into a real window is in <see cref="RootTokenDeskTests"/>.
/// </para>
/// </summary>
public sealed class RootTokenTests
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "winwright-root-token", "checkout");
    private static readonly string Staged = Path.Combine(Path.GetTempPath(), "winwright-root-token", "staged");

    [Fact]
    public void The_root_is_filled_as_a_path_and_as_the_path_OpenSSH_spells()
    {
        Assert.Equal(
            Path.Combine(Root, "cases").Replace('/', '\\'),
            FixtureDeclaration.Rooted("{root}\\cases", Root, Staged, null));

        Assert.Equal(
            "/" + Root.Replace('\\', '/') + "/cases",
            FixtureDeclaration.Rooted("{root/}/cases", Root, Staged, null));

        Assert.Equal(Staged + "\\a.bin", FixtureDeclaration.Rooted("{files}\\a.bin", Root, Staged, null));
    }

    [Fact]
    public void The_configuration_is_filled_in_any_case_beside_the_other_tokens()
    {
        // WW585. A before command running the CLI the same build produced.
        Assert.Equal(
            Path.Combine(Path.GetFullPath(Root), "bin", "Release", "cli.exe"),
            FixtureDeclaration.Rooted("{root}\\bin\\{Configuration}\\cli.exe", Root, Staged, "Release"));
    }

    [Fact]
    public void A_configuration_nothing_filled_is_refused_naming_the_variable()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => FixtureDeclaration.Rooted("bin/{configuration}/cli.exe", Root, Staged, null));

        Assert.Contains("WINWRIGHT_CONFIGURATION", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_before_command_naming_the_configuration_runs_the_build_the_project_names()
    {
        var fixture = FixtureDeclaration.Of(
            "after its CLI", before: [new BeforeCommand("tools/cli/bin/{configuration}/cli.exe", ["--to", "{configuration}"])]);

        var start = fixture.Configured("Release").Preparing(fixture.Before[0], Root);

        Assert.Equal(Path.Combine(Path.GetFullPath(Root), "tools", "cli", "bin", "Release", "cli.exe"), start.FileName);
        Assert.Equal(["--to", "Release"], start.ArgumentList);
    }

    [Fact]
    public void A_fixture_field_naming_the_configuration_with_none_said_is_refused_naming_the_field()
    {
        var fixture = FixtureDeclaration.Of(
            "after its CLI", before: [new BeforeCommand("tools/cli/bin/{configuration}/cli.exe", [])]);

        var refused = Assert.Throws<ScenarioRefusedException>(() => fixture.Preparing(fixture.Before[0], Root));

        Assert.Equal("after its CLI", refused.Subject);
        Assert.Contains("its 'before.run' names {configuration}", refused.Because, StringComparison.Ordinal);
        Assert.Contains("WINWRIGHT_CONFIGURATION", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_step_naming_the_configuration_loads_and_is_filled_at_the_run()
    {
        var step = Wrote.Step("Edit#path", "set value", ("with", "{root}\\bin\\{configuration}"), ("expect", "x"), ("reads", "value"));

        Assert.Equal(Path.Combine(Path.GetFullPath(Root), "bin", "Debug"), step.Rooted(Root, Staged, "Debug").Argument);

        var refused = Assert.Throws<ScenarioRefusedException>(() => step.Rooted(Root, Staged, null));
        Assert.Contains("its 'with' names {configuration}", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_step_fills_its_argument_at_the_run_and_carries_it_as_written_until_then()
    {
        var step = Wrote.Step("Edit#path", "set value", ("with", "{root/}/cases"), ("expect", "x"), ("reads", "value"));

        Assert.Equal("{root/}/cases", step.Argument);
        Assert.Equal("/" + Root.Replace('\\', '/') + "/cases", step.Rooted(Root, Staged, null).Argument);
    }

    [Fact]
    public void A_token_that_names_nothing_is_refused_with_the_ones_there_are()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => Wrote.Step("Edit#path", "set value", ("with", "{roots}/cases"), ("expect", "x"), ("reads", "value")));

        Assert.Contains("'{roots}', which is no token", refused.Because, StringComparison.Ordinal);
        Assert.Contains("'{files}', '{root}', '{root/}', '{configuration}'", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_brace_that_is_not_shaped_like_a_token_is_typed_as_written()
    {
        // Text a case types may hold braces of its own — a line of JSON — and those are not tokens.
        var step = Wrote.Step("Edit#path", "set value", ("with", "{\"a\": 1}"), ("expect", "x"), ("reads", "value"));

        Assert.Equal("{\"a\": 1}", step.Rooted(Root, Staged, null).Argument);
    }

    [Fact]
    public void A_fixture_names_the_root_in_its_variables_and_the_folder_it_opens()
    {
        var fixture = FixtureDeclaration.Of("rooted", opens: "{root}\\landing");

        Assert.Equal(Path.Combine(Path.GetFullPath(Root), "landing"), fixture.Opening(Root));
    }
}
