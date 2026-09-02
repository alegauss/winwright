using System.Windows.Controls;

using Winwright.InApp;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW76. The same eight-line runner sits in twenty-seven test files of one project, each with its
/// own timeout and its own message for a thread that does not finish.
/// <para>
/// It is not boilerplate that happens to repeat. Controls cannot be constructed off that apartment
/// at all, and a suite that hangs on a UI primitive reports nothing whatever — no pass, no failure,
/// no name. This suite carried eight copies of it before this task, and carries none now.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ApartmentTests
{
    [Fact]
    public void Work_runs_on_a_single_threaded_apartment_and_hands_its_answer_back()
    {
        var state = Apartment.Run(() => Thread.CurrentThread.GetApartmentState());

        Assert.Equal(ApartmentState.STA, state);
    }

    [Fact]
    public void A_control_can_be_constructed_inside_it_and_nowhere_else()
    {
        // The reason the runner exists at all: this throws on the thread the test is running on.
        var built = Apartment.Run(() => new Border { Width = 10, Height = 10 }.Width);

        Assert.Equal(10, built);
        Assert.Throws<InvalidOperationException>(() => new Border());
    }

    [Fact]
    public void What_the_work_threw_arrives_as_itself_with_its_own_stack()
    {
        // Most of a refusal's value is that a test can assert its type. A runner that wrapped
        // would turn every one of them into the same exception and lose the whole point.
        var refused = Assert.Throws<UnrenderableException>(
            () => Apartment.Run<int>(() => throw new UnrenderableException("it laid out to nothing")));

        Assert.Equal("it laid out to nothing", refused.Message);
        Assert.Contains(nameof(ApartmentTests), refused.StackTrace);
    }

    [Fact]
    public void Work_that_does_not_finish_is_a_named_timeout_and_not_a_suite_that_stops()
    {
        using var never = new ManualResetEventSlim(false);

        var timedOut = Assert.Throws<ApartmentTimeoutException>(
            () => Apartment.Run(() => never.Wait(), TimeSpan.FromMilliseconds(200), "the render"));

        Assert.Contains("the render did not finish within 0.2s", timedOut.Message);
        Assert.Contains("still running and cannot safely be stopped", timedOut.Message);

        // Released so the thread ends rather than sitting on the event for the run's lifetime.
        never.Set();
    }

    [Fact]
    public void Work_with_no_name_is_still_named_something_in_the_timeout()
    {
        using var never = new ManualResetEventSlim(false);

        var timedOut = Assert.Throws<ApartmentTimeoutException>(
            () => Apartment.Run(() => never.Wait(), TimeSpan.FromMilliseconds(150)));

        Assert.StartsWith("the work did not finish", timedOut.Message);
        never.Set();
    }

    [Fact]
    public void Work_with_no_answer_runs_the_same_way()
    {
        var ran = false;

        Apartment.Run(() => ran = true);

        Assert.True(ran);
    }

    [Fact]
    public void A_length_of_time_that_is_not_one_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Apartment.Run(() => 1, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => Apartment.Run(() => 1, TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void Nothing_to_run_is_refused_rather_than_answered()
    {
        Assert.Throws<ArgumentNullException>(() => Apartment.Run((Func<int>)null!));
        Assert.Throws<ArgumentNullException>(() => Apartment.Run((Action)null!));
    }

    [Fact]
    public void Each_run_gets_an_apartment_of_its_own()
    {
        // Two runs sharing one would let a control built by the first be reached by the second,
        // which is a test that passes for a reason no application would ever reproduce.
        var first = Apartment.Run(() => Environment.CurrentManagedThreadId);
        var second = Apartment.Run(() => Environment.CurrentManagedThreadId);

        Assert.NotEqual(first, second);
        Assert.NotEqual(Environment.CurrentManagedThreadId, first);
    }

    [Fact]
    public void The_thread_is_named_after_the_work_so_a_hung_run_can_be_looked_at()
    {
        var named = Apartment.Run(() => Thread.CurrentThread.Name, named: "the sessions capture");

        Assert.Equal("winwright: the sessions capture", named);
    }

    [Fact]
    public void This_suite_carries_no_copy_of_the_runner_any_more()
    {
        // The deletion is the proof. Four fixtures still make their own threads and are meant to:
        // each pumps a message loop for the lifetime of a window, which is not what this runs — the
        // runner is bounded and hands its answer back, and a window has to outlive the call that
        // made it. WW347 added the third, which holds an open WPF popup up, and WW349 the fourth,
        // whose window has to be pumping to take the message the harness sends it.
        // This file is left out because it names the very string it is looking for.
        var copies = Directory
            .EnumerateFiles(Sources(), "*.cs")
            .Where(file => Path.GetFileName(file) != $"{nameof(ApartmentTests)}.cs")
            // WW202. Code and never prose: this is the fifth sweep in the suite to be pointed at
            // the difference, and the first four each found it by going red.
            .Where(file => File.ReadLines(file).Select(Checkout.Code)
                .Any(line => line.Contains("SetApartmentState", StringComparison.Ordinal)))
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(["AnsweringWindow.cs", "PumpedDialog.cs", "PumpedFlyout.cs", "TrayIconFixture.cs"], copies);
    }

    private static string Sources() => Checkout.At("tests", "Winwright.Tests");
}
