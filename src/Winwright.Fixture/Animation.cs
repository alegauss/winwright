using System.Globalization;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Winwright.Fixture;

/// <summary>
/// An animation with a declared length and a declared number of states.
/// <para>
/// A frame sequence is otherwise checked by opening the frames, which is the thing this framework
/// exists to avoid. Here the states are named and counted, so the sequence is checked against
/// numbers: how many there were, in what order they arrived, and how long each one stood.
/// </para>
/// <para>
/// Each state announces its own place — <c>3 of 5</c> — so a check reads the count off the window
/// rather than being told it. An expectation typed into a case is one that goes stale the day the
/// animation gains a state, and this fixture exists to be the thing that never lies about itself.
/// </para>
/// </summary>
public sealed class Animation
{
    /// <summary>How many states it cycles through. Declared here and announced by every one of them.</summary>
    public const int States = 5;

    private readonly TextBlock showing;
    private readonly DispatcherTimer timer;
    private int at;

    private Animation(TextBlock showing, int everyMilliseconds)
    {
        this.showing = showing;
        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(everyMilliseconds) };
        timer.Tick += (_, _) => Advance();

        Advance();
        timer.Start();
    }

    /// <summary>Start one on a text block, advancing every so many milliseconds.</summary>
    /// <param name="showing">The block that announces which state is up.</param>
    /// <param name="everyMilliseconds">How long each state stands.</param>
    public static Animation On(TextBlock showing, int everyMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(showing);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(everyMilliseconds);

        return new Animation(showing, everyMilliseconds);
    }

    /// <summary>How one state announces itself, which is the whole of what a check reads.</summary>
    public static string Says(int state) =>
        string.Create(CultureInfo.InvariantCulture, $"{state} of {States}");

    private void Advance()
    {
        at = at % States + 1;
        showing.Text = Says(at);
    }
}
