using System.Diagnostics;

using Winwright.Asserting;
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

        // Thirteen, and the count is still the point: a runner cannot list twelve and be right,
        // and a fourteenth added later is this list rather than an audit of every runner.
        //
        // Three groups now. Six are about the desk - can anything be observed here at all - six
        // are about the application on it, and WW157 added one about whether the desk is this
        // run's alone. Counted together because a runner asks for the reading once; the first six
        // told apart by Machine, because only they answer for the run as a whole.
        Assert.Equal(13, read.Measurements.Count);
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

        // Derived, because the absolute number is not what this asserts. It was 7, then 13, then
        // 14, and each move caught this case for a change it is not about. What it claims is one
        // line per measurement with the reading above them, and that holds at any count.
        var read = Preamble.Of(Attached());
        Assert.Equal(read.Measurements.Count + 1, rendered.Count);
        Assert.StartsWith($"this run measured {read.Measurements.Count} conditions", rendered[0]);
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

    [Fact]
    public void A_run_with_no_store_declared_says_it_took_no_fingerprint_rather_than_nothing()
    {
        // WW151, and the half that is easy to get wrong. A run that took no fingerprint because no
        // project declared a store has nothing to say; a run that took one and found it clean has
        // something to say. Reporting them the same way is the shape this project keeps refusing.
        var read = Preamble.Of(Attached());

        Assert.Null(read.Store);
        var finding = read.LeftAsFound();

        Assert.False(finding.Was);
        Assert.Null(finding.Holds);
        Assert.Contains("no project declared a store", finding.Sentence, StringComparison.Ordinal);
        Assert.StartsWith("  not read ", finding.ToString(), StringComparison.Ordinal);

        // And it reaches the one line a reader skims, not only the line-per-finding list below it.
        var joined = read.Including(finding);
        Assert.Contains("1 reading(s) not taken", joined.Sentence(), StringComparison.Ordinal);
        Assert.Contains(joined.Render(), one => one.StartsWith("  not read " + StoreChange.Named, StringComparison.Ordinal));
    }

    [Fact]
    public void A_run_that_left_the_declared_store_alone_says_so_without_being_asked()
    {
        // The fingerprint is taken by the reading every run takes, so nothing here calls for it —
        // which is the whole task: the promise used to hold exactly as often as an author wrote
        // both halves, and the forgotten half is the second one.
        var read = Preamble.Of(Attached(), Declared());

        Assert.NotNull(read.Store);
        Assert.Equal("", read.StoreAbsence);

        var finding = read.LeftAsFound();

        Assert.True(finding.Holds, finding.Sentence);
        Assert.Contains("left the machine as it found it", finding.Sentence, StringComparison.Ordinal);
        Assert.StartsWith("  agrees  ", finding.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_run_that_rewrote_a_setting_is_a_finding_and_never_a_failed_assertion()
    {
        // The accident the whole type was built for: the same number of bytes and a different
        // machine. It is not a failure — the application did what it was driven to do — and not a
        // precondition either, since nothing may be excused by it.
        var declaration = Declared();
        var read = Preamble.Of(Attached(), declaration);
        Assert.NotNull(read.Store);

        var settings = Path.Combine(declaration.FingerprintStore, "settings.json");
        var was = File.ReadAllText(settings);
        File.WriteAllText(settings, was.Replace("alpha", "gamma", StringComparison.Ordinal));
        Assert.Equal(was.Length, File.ReadAllText(settings).Length);

        var finding = read.LeftAsFound();

        Assert.False(finding.Holds);
        Assert.Contains("changed the machine of whoever ran it", finding.Sentence, StringComparison.Ordinal);
        Assert.Contains("settings.json", finding.Sentence, StringComparison.Ordinal);

        // A finding, so it never excuses an assertion and never enters the condition set.
        var joined = read.Including(finding);
        Assert.DoesNotContain(joined.Conditions, one => one.Name == StoreChange.Named);
        Assert.Contains(joined.Differing, one => one.Named == StoreChange.Named);
    }

    /// <summary>A project declaring enough for the measurements that need one.</summary>
    private ProjectDeclaration Declared()
    {
        var strings = Path.Combine(root, "strings.en.json");
        File.WriteAllText(strings, """{ "tabs": { "report": "Report" } }""");

        // WW151: a store of this case's own, so the fingerprint has something real to read and
        // nothing anybody owns is at risk of being read, let alone reported as moved.
        var store = Directory.CreateDirectory(Path.Combine(root, "store")).FullName;
        File.WriteAllText(Path.Combine(store, "settings.json"), """{ "profile": "alpha" }""");

        var path = Path.Combine(root, "winwright.json");
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Environment.ProcessPath)}},
              "sourceRoot": {{System.Text.Json.JsonSerializer.Serialize(root)}},
              "fingerprintStore": {{System.Text.Json.JsonSerializer.Serialize(store)}},
              "languageFiles": [{{System.Text.Json.JsonSerializer.Serialize(strings)}}]
            }
            """);

        return ProjectDeclaration.Load(path);
    }
}
