using System.Runtime.InteropServices;

using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW39. A window can be visible by its style bits and painted by nothing.
/// <para>
/// The first test is the whole finding, and it is the one worth reading: after the compositor
/// cloaks the window, every question the window manager can answer still says it is visible. That
/// is why a filter reading <c>IsWindowVisible</c> reports a screenful of windows nobody can see.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class CloakingTests
{
    // Declared here rather than reached for in the engine: the claim under test is what the
    // window manager itself still says about a cloaked window, so it is asked of Windows direct.
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint window);

    // Four ints and not a long. Declared as one, this call writes sixteen bytes into an
    // eight-byte slot and the test host dies of it partway through an unrelated class, which
    // reads as tests quietly going missing from the run rather than as anything failing.
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint window, out Rect rectangle);

    [Fact]
    public void A_cloaked_window_keeps_every_style_bit_that_says_it_is_visible()
    {
        using var dialog = PumpedDialog.Open("winwright cloaked");
        Assert.Equal(Cloak.NotCloaked, Cloaking.Of(dialog.Frame));

        dialog.Cloak();

        // Nothing about the window changed except that the compositor stopped drawing it, which
        // is precisely the case no style-bit test can see.
        Assert.True(IsWindowVisible(dialog.Frame));
        Assert.True(GetWindowRect(dialog.Frame, out _));
        Assert.Equal(Cloak.ByTheApplication, Cloaking.Of(dialog.Frame));
        Assert.False(Cloaking.IsPainted(dialog.Frame));
    }

    [Fact]
    public void The_listing_reports_it_until_it_is_cloaked_and_then_does_not()
    {
        using var dialog = PumpedDialog.Open("winwright listed");

        Assert.Contains(
            TopLevelWindows.OfProcess(Environment.ProcessId), window => window.Handle == dialog.Frame);

        dialog.Cloak();

        Assert.DoesNotContain(
            TopLevelWindows.OfProcess(Environment.ProcessId), window => window.Handle == dialog.Frame);
    }

    [Fact]
    public void Asking_for_everything_still_reports_it_and_says_who_cloaked_it()
    {
        using var dialog = PumpedDialog.Open("winwright everything");
        dialog.Cloak();

        var found = Assert.Single(
            TopLevelWindows.OfProcess(Environment.ProcessId, visibleOnly: false),
            window => window.Handle == dialog.Frame);

        // The filter has a way back through it: the fact is on the record rather than being the
        // reason a window is missing from one.
        Assert.True(found.Visible);
        Assert.False(found.OnScreen);
        Assert.Equal(Cloak.ByTheApplication, found.Cloak);
        Assert.Contains("cloaked (the application cloaked it", found.ToString());
    }

    [Fact]
    public void A_window_on_the_screen_says_so_and_explains_nothing()
    {
        using var dialog = PumpedDialog.Open("winwright plain");

        var found = Assert.Single(
            TopLevelWindows.OfProcess(Environment.ProcessId), window => window.Handle == dialog.Frame);

        Assert.True(found.OnScreen);
        Assert.Equal(Cloak.NotCloaked, found.Cloak);
        Assert.Null(Cloaking.Because(Cloak.NotCloaked));
        Assert.DoesNotContain("cloaked", found.ToString());
    }

    [Fact]
    public void A_handle_naming_no_window_is_unknown_rather_than_fit_to_photograph()
    {
        // Not NotCloaked, which is the answer that would matter: it would report a window that
        // no longer exists as one a capture may go ahead with.
        Assert.Equal(Cloak.Unknown, Cloaking.Of(0));
        Assert.Equal(Cloak.Unknown, Cloaking.Of(0x7FFFFFFF));
        Assert.False(Cloaking.IsPainted(0));
        Assert.Contains("would not say", Cloaking.Because(Cloak.Unknown));
    }

    [Fact]
    public void Every_cloak_this_engine_names_has_a_sentence_a_refusal_can_print()
    {
        foreach (var cloak in Enum.GetValues<Cloak>().Where(value => value != Cloak.NotCloaked))
            Assert.False(string.IsNullOrWhiteSpace(Cloaking.Because(cloak)), cloak.ToString());
    }
}
