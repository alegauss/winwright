using Winwright.Acting;
using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW544. A modifier pressed alone, which is how a classic menu bar is shown. Found by quickshell
/// (QS354). The tap itself is driven in <see cref="DragTests"/>; these are what parses and what stays
/// refused.
/// </summary>
public sealed class ChordAloneTests
{
    [Theory]
    [InlineData("Alt", "Alt")]
    [InlineData("alt", "Alt")]
    [InlineData("Ctrl", "Ctrl")]
    [InlineData("Win", "Win")]
    public void A_modifier_alone_is_a_tap_reported_as_alone(string written, string key)
    {
        Assert.True(Chord.TryParse(written, out var chord, out _));

        Assert.True(chord.Alone);
        Assert.Empty(chord.Modifiers);
        Assert.Equal(key, chord.Key);
        Assert.Equal($"{key}, alone", chord.Text);
    }

    [Fact]
    public void Two_modifiers_and_no_key_stay_refused()
    {
        // A chord that lost its key, not a tap: the tap is one modifier on its own.
        Assert.False(Chord.TryParse("Ctrl+Alt", out _, out var because));
        Assert.Contains("no key this can press", because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_chord_with_a_key_is_not_alone()
    {
        Assert.True(Chord.TryParse("Alt+F", out var chord, out _));
        Assert.False(chord.Alone);
    }

    [Fact]
    public void A_press_step_takes_a_modifier_alone()
    {
        var step = Wrote.Step("Text#dragOrder", "press", ("with", "Alt"));

        Assert.Equal("Alt", step.Argument);
    }
}
