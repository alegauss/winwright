using Winwright.Acting;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW30, against the act the rule was written about. claude-tray's submenu was the coin toss —
/// three runs in ten reported one that did not expand against a build with nothing wrong with it.
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class RetryOnAMenuTests : IDisposable
{
    private readonly PumpedDialog dialog = PumpedDialog.OpenWithMenu(
        "winwright statistics",
        new PumpedDialog.MenuEntry(
            "File",
            new PumpedDialog.MenuEntry("New"),
            new PumpedDialog.MenuEntry("Recent", new PumpedDialog.MenuEntry("one.txt"))));

    public void Dispose()
    {
        for (var attempt = 0; attempt < 6 && Menu.Highlighted() is not null; attempt++)
            Menu.Dismiss(1);

        dialog.Dispose();
    }

    [Fact]
    public void Expanding_a_real_submenu_through_the_cap_reports_what_it_took()
    {
        Menu.Enter(dialog.Frame);
        Menu.To(dialog.Frame, "Recent");

        var attempted = Retry.Bounded(
            () => Menu.Expand(dialog.Frame, settleMs: 800, pollMs: 20),
            walk => walk.Reached);

        Assert.True(attempted.Succeeded);
        Assert.Equal("one.txt", attempted.Last.Highlighted);

        var step = Retry.Recorded(attempted.Last.AsTraceStep(), attempted);
        Assert.Equal(attempted.Attempts, step.Attempts);
        Assert.Equal(Winwright.Tracing.StepVerdict.Ok, step.Verdict);
    }

    [Fact]
    public void An_entry_that_never_expands_still_goes_red_after_the_cap()
    {
        Menu.Enter(dialog.Frame);
        Menu.To(dialog.Frame, "New");

        // "New" has no submenu, so Right walks on to the next top-level menu instead. The point
        // is what happens at the end of the attempts: it is red, and it says how many it had.
        var attempted = Retry.Bounded(
            () => Menu.Expand(dialog.Frame, settleMs: 300, pollMs: 20),
            walk => walk.Highlighted == "one.txt",
            cap: 2);

        Assert.False(attempted.Succeeded);
        Assert.Equal(2, attempted.Attempts);
        Assert.Equal("did not work in 2 attempts.", attempted.ToString());
    }
}
