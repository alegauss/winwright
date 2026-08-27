using System.Diagnostics;
using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Processes;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW252. A miss that stopped being one while it was being explained.
/// <para>
/// <c>Resolve.Diagnose</c> runs after a resolve gave up, so at first sight no chain can match all the
/// way through it. What can is time: the element arrives between the resolve giving up and the
/// diagnosis walking the tree. Measured against claude-tray's Statistics pane, whose numbers render
/// from a transcript scan — the read timed out, the value was there a moment later, and the case
/// reported <c>ArgumentOutOfRangeException: Index was out of range</c> in place of anything about a
/// window. The same arithmetic was written twice, in <c>Diagnose</c> and in
/// <see cref="LocatorMiss.Stopped" />, and the second would have thrown after the first was fixed.
/// </para>
/// <para>
/// What is asserted is the invariant and not the reading, deliberately. The state cannot be
/// fabricated — <see cref="LocatorMiss" />'s constructor is not public, which is right — and it cannot
/// be provoked to order, because the window it needs is the few microseconds between two tree walks.
/// So this hammers a resolve across a content swap and claims only that nothing threw, which is
/// exactly what the defect broke and is false the moment it returns.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ArrivedLateTests : IDisposable
{
    private readonly Settling settling = Attachable.Settling();
    private readonly AutomationElement? root;

    public ArrivedLateTests()
    {
        if (!Desk.Read().CanObserve)
            return;

        // The fixture holds the report page back and swaps the real content in after this long, which
        // is the only door in this repository onto a tree that changes under a reader.
        var launched = settling.Register.Launch(Fixture.Started("--loading=1500"));
        var drawn = Attempt.UntilTrue(() => TopLevelWindows.Largest(launched.Pid) is not null, 20000, 25);

        Assert.True(drawn.Happened, $"the fixture drew no window in {drawn.WaitedMs}ms");
        root = AutomationElement.FromHandle(TopLevelWindows.Largest(launched.Pid)!.Handle);
    }

    public void Dispose() => settling.Dispose();

    [Fact]
    public void Resolving_across_a_window_that_is_still_drawing_never_throws()
    {
        if (root is null)
            return;

        // Across the swap and well past it: the arrival is what the defect needed, and a loop that
        // stopped at the swap would be a loop that never saw the tree settle either.
        var locator = Locator.Parse("Text#reportNote");
        var clock = Stopwatch.StartNew();
        var missed = 0;
        var found = 0;

        while (clock.ElapsedMilliseconds < 3000)
        {
            // Once and never a waiting resolve: what is being hammered is the pair of tree walks
            // inside one attempt, which is where the two indexes were.
            var resolution = Resolve.Once(root, locator);
            if (resolution.Found)
                found++;
            else
                _ = resolution.Miss!.Sentence();

            if (!resolution.Found)
                missed++;
        }

        // Both sides seen, or the loop proved nothing: all-found means it never read a drawing window,
        // and all-missed means the content never arrived and the swap was not crossed.
        Assert.True(missed > 0, $"the report was there on every one of {found} looks, so no walk read a window still drawing");
        Assert.True(found > 0, $"the report never arrived in {clock.ElapsedMilliseconds}ms, so the swap was never crossed");
    }
}
