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
            FixtureDeclaration.Rooted("{root}\\cases", Root, Staged));

        Assert.Equal(
            "/" + Root.Replace('\\', '/') + "/cases",
            FixtureDeclaration.Rooted("{root/}/cases", Root, Staged));

        Assert.Equal(Staged + "\\a.bin", FixtureDeclaration.Rooted("{files}\\a.bin", Root, Staged));
    }

    [Fact]
    public void A_step_fills_its_argument_at_the_run_and_carries_it_as_written_until_then()
    {
        var step = Wrote.Step("Edit#path", "set value", ("with", "{root/}/cases"), ("expect", "x"), ("reads", "value"));

        Assert.Equal("{root/}/cases", step.Argument);
        Assert.Equal("/" + Root.Replace('\\', '/') + "/cases", step.Rooted(Root, Staged).Argument);
    }

    [Fact]
    public void A_token_that_names_nothing_is_refused_with_the_ones_there_are()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => Wrote.Step("Edit#path", "set value", ("with", "{roots}/cases"), ("expect", "x"), ("reads", "value")));

        Assert.Contains("'{roots}', which is no token", refused.Because, StringComparison.Ordinal);
        Assert.Contains("'{files}', '{root}', '{root/}'", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_brace_that_is_not_shaped_like_a_token_is_typed_as_written()
    {
        // Text a case types may hold braces of its own — a line of JSON — and those are not tokens.
        var step = Wrote.Step("Edit#path", "set value", ("with", "{\"a\": 1}"), ("expect", "x"), ("reads", "value"));

        Assert.Equal("{\"a\": 1}", step.Rooted(Root, Staged).Argument);
    }

    [Fact]
    public void A_fixture_names_the_root_in_its_variables_and_the_folder_it_opens()
    {
        var fixture = FixtureDeclaration.Of("rooted", opens: "{root}\\landing");

        Assert.Equal(Path.Combine(Path.GetFullPath(Root), "landing"), fixture.Opening(Root));
    }
}
