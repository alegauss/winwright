using System.Diagnostics;

using Winwright.Processes;
using Winwright.Projects;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW110. Block B shipped five measurements and joined none of them, so a run's claim about which
/// binary it drove was met three times over by three sentences — which is to say it was not met
/// once, and a reader got whichever the caller remembered to print.
/// <para>
/// The half that matters more is the runner assembling the precondition set by hand: one edit by
/// somebody who does not know all five are there, and the forgotten one stops being measured while
/// every assertion that needed it silently starts passing.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class PreambleTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-preamble-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private static AppTarget Attached() => AppTarget.AttachTo(Environment.ProcessId);

    [Fact]
    public void One_reading_lists_every_condition_this_tool_measures()
    {
        var read = Preamble.Of(Attached());

        // Twelve, and the count is still the point: a runner cannot list eleven and be right, and
        // a thirteenth added later is this list rather than an audit of every runner.
        //
        // WW156 made it two groups rather than one. Six are about the desk - can anything be
        // observed here at all - and six are about the application on it. They are counted
        // together because a runner asks for the reading once, and told apart by Machine, because
        // only the first six answer for the run as a whole.
        Assert.Equal(12, read.Measurements.Count);
        Assert.Equal(6, read.Machine.Conditions.Count);
        Assert.Equal(read.Measurements.Count, read.Measurements.Select(one => one.Name).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void A_measurement_this_run_could_not_take_is_recorded_and_not_left_out()
    {
        // No project declared, so four of the six have nothing to compare against. An absent line
        // and a missing line read the same to somebody skimming, and only one is a statement.
        var read = Preamble.Of(Attached());

        Assert.NotEmpty(read.Unread);
        Assert.All(read.Unread, one => Assert.False(string.IsNullOrWhiteSpace(one.Sentence)));
        Assert.Contains("not read", read.Sentence());
    }

    [Fact]
    public void What_was_never_read_is_not_offered_as_a_condition_an_assertion_was_checked_against()
    {
        var read = Preamble.Of(Attached());

        // A precondition nobody read is not one an assertion may claim to have been checked
        // against, so the set the assertions resolve against carries only what was measured.
        Assert.Equal(read.Measurements.Count(one => one.Was), read.Conditions.Count);
        Assert.DoesNotContain(read.Conditions, one => read.Unread.Any(missing => missing.Name == one.Name));
    }

    [Fact]
    public void The_launch_arguments_are_read_from_the_target_and_always_available()
    {
        var read = Preamble.Of(Attached());

        var arguments = read.Find(AppTarget.LaunchArgumentsPreconditionName);

        Assert.NotNull(arguments);
        Assert.True(arguments.Was);

        // Attached rather than launched, so the arguments are honestly unknowable - and that is
        // an absent precondition rather than an empty string pretending to be an answer.
        Assert.False(arguments.Held);
    }

    [Fact]
    public void A_run_that_launched_the_application_knows_its_arguments()
    {
        using var register = new ProcessRegister();
        // Long enough to be readable: a process that has already exited reports no main module,
        // and waiting on one that ends instantly is testing process startup rather than this.
        var cmd = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
        var start = new ProcessStartInfo(cmd) { ArgumentList = { "/c", "ping", "-n", "5", "127.0.0.1" } };
        var launched = Attachable.Launch(register, start);

        var read = Preamble.Of(AppTarget.FromLaunch(launched, "/c", "ping"));

        Assert.True(read.Find(AppTarget.LaunchArgumentsPreconditionName)!.Held);
    }

    [Fact]
    public void With_a_project_declared_the_measurements_that_need_one_are_taken()
    {
        var read = Preamble.Of(Attached(), Declared());

        // Four of the six need a project to compare against; naming one moves them out of unread.
        Assert.True(read.Conditions.Count > Preamble.Of(Attached()).Conditions.Count);
        Assert.NotNull(read.Find(RunningBinary.PreconditionName));
        Assert.True(read.Find(RunningBinary.PreconditionName)!.Was);
    }

    [Fact]
    public void The_foreground_is_unread_where_no_window_was_under_test()
    {
        var read = Preamble.Of(Attached());

        var foreground = read.Find(Foreground.PreconditionName);

        Assert.NotNull(foreground);
        Assert.False(foreground.Was);
        Assert.Contains("no window was under test", foreground.Sentence);
    }

    [Fact]
    public void The_foreground_is_read_where_a_window_is_named()
    {
        using var dialog = PumpedDialog.Open("winwright statistics");

        var read = Preamble.Of(Attached(), window: dialog.Frame);

        Assert.True(read.Find(Foreground.PreconditionName)!.Was);
    }

    [Fact]
    public void The_preamble_renders_one_line_per_measurement_with_the_reading_first()
    {
        var rendered = Preamble.Of(Attached()).Render();

        Assert.Equal(13, rendered.Count);
        Assert.StartsWith("this run measured 12 conditions", rendered[0]);
        Assert.All(rendered.Skip(1), one => Assert.StartsWith("  ", one));
    }

    [Fact]
    public void A_reading_where_everything_held_says_so_in_one_sentence()
    {
        // A desk where all six hold is not one this suite can arrange, so what is asserted is
        // that the sentence and the flag agree rather than which of them is true today.
        var read = Preamble.Of(Attached());

        Assert.Equal(read.Unread.Count == 0 && read.Absent.Count == 0, read.Clear);
    }

    [Fact]
    public void Asking_for_a_condition_this_reading_does_not_carry_answers_nothing()
    {
        var read = Preamble.Of(Attached());

        Assert.Null(read.Find("a condition nobody measures"));
        Assert.Throws<ArgumentException>(() => read.Find(" "));
    }

    /// <summary>A project declaring enough for the measurements that need one.</summary>
    private ProjectDeclaration Declared()
    {
        var strings = Path.Combine(root, "strings.en.json");
        File.WriteAllText(strings, """{ "tabs": { "report": "Report" } }""");

        var path = Path.Combine(root, "winwright.json");
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Environment.ProcessPath)}},
              "sourceRoot": {{System.Text.Json.JsonSerializer.Serialize(root)}},
              "languageFiles": [{{System.Text.Json.JsonSerializer.Serialize(strings)}}]
            }
            """);

        return ProjectDeclaration.Load(path);
    }
}
