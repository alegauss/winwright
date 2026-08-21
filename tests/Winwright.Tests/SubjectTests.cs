using System.Runtime.InteropServices;
using System.Windows.Automation;

using Winwright.Locating;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW22. Holding a pattern and comparing its value before an act against its value after compares
/// the reading with itself and can never fail.
/// <para>
/// The trap and the fix are in the same test below, deliberately: the held pattern is shown
/// changing under the reader's feet, and the snapshot taken at the same instant is shown not to.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class SubjectTests : IDisposable
{
    private const uint WsPopup = 0x80000000;
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;

    private readonly List<nint> created = [];

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowTextW(nint window, string text);

    public void Dispose()
    {
        for (var index = created.Count - 1; index >= 0; index--)
            DestroyWindow(created[index]);
    }

    private nint Create(string className, string? title, uint style, int width, int height, nint parent = 0)
    {
        var window = CreateWindowExW(0, className, title, style, 20, 20, width, height, parent, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
        return window;
    }

    private (nint Frame, nint Edit) Dialog(string text = "alpha")
    {
        var frame = Create("Static", "winwright statistics", WsPopup | WsVisible, 420, 300);
        var edit = Create("Edit", text, WsChild | WsVisible, 200, 24, frame);
        return (frame, edit);
    }

    private Subject SubjectFor(nint frame, string locator) =>
        new(AutomationElement.FromHandle(frame), Locator.Parse(locator), 2000, pollMs: 20);

    [Fact]
    public void A_reading_is_a_value_and_the_live_view_beside_it_is_not()
    {
        var (frame, edit) = Dialog();
        var subject = SubjectFor(frame, "Edit");

        // The trap, held the way an author naturally would.
        var held = (ValuePattern)AutomationElement.FromHandle(edit).GetCurrentPattern(ValuePattern.Pattern);
        var liveBefore = held.Current;

        var before = subject.Read();
        Assert.Equal("alpha", before.Values.Value);

        Assert.True(SetWindowTextW(edit, "beta"));

        // The held view now answers the new question, which is why comparing it with itself
        // across an act can never fail.
        Assert.Equal("beta", liveBefore.Value);

        // The snapshot still says what it said.
        Assert.Equal("alpha", before.Values.Value);
        Assert.Equal("beta", subject.Read().Values.Value);
    }

    [Fact]
    public void Two_readings_either_side_of_a_change_differ()
    {
        var (frame, edit) = Dialog();
        var subject = SubjectFor(frame, "Edit");

        var before = subject.Read();
        SetWindowTextW(edit, "beta");
        var after = subject.Read();

        Assert.NotEqual(before.Values, after.Values);
        Assert.Equal("alpha", before.Values.Value);
        Assert.Equal("beta", after.Values.Value);
    }

    [Fact]
    public void The_subject_holds_no_element_for_anything_to_go_stale()
    {
        var elements = typeof(Subject)
            .GetProperties()
            .Where(property => property.PropertyType == typeof(AutomationElement));

        Assert.Empty(elements);
    }

    [Fact]
    public void An_element_that_went_away_is_a_diagnosed_miss_and_not_an_exception()
    {
        var (frame, edit) = Dialog();
        var subject = SubjectFor(frame, "Edit");
        Assert.True(subject.ReadOnce().Found);

        // Holding the element is what the subject deliberately does not do.
        var held = AutomationElement.FromHandle(edit);
        Assert.True(DestroyWindow(edit));
        created.Remove(edit);

        var reading = subject.ReadOnce();

        Assert.False(reading.Found);
        Assert.NotNull(reading.Miss);
        Assert.Equal(MissKind.Absent, reading.Miss.Kind);
        Assert.Throws<ElementNotAvailableException>(() => _ = held.Current.Name);
    }

    [Fact]
    public void Re_resolving_finds_the_replacement_rather_than_the_thing_that_went()
    {
        var (frame, edit) = Dialog();
        var subject = SubjectFor(frame, """Edit[name="alpha"]""");
        Assert.True(subject.ReadOnce().Found);

        DestroyWindow(edit);
        created.Remove(edit);
        Create("Edit", "alpha", WsChild | WsVisible, 200, 24, frame);

        Assert.True(subject.ReadOnce().Found);
    }

    [Fact]
    public void Reading_something_that_is_not_there_answers_with_the_miss_and_no_values()
    {
        var (frame, _) = Dialog();

        var reading = SubjectFor(frame, """Slider[name="Volume"]""").ReadOnce();

        Assert.False(reading.Found);
        Assert.Null(reading.Facts);
        Assert.Same(PatternValues.None, reading.Values);
    }

    [Fact]
    public void An_element_offering_no_patterns_reads_as_nothing_rather_than_as_a_failure()
    {
        // A child, because a step searches the descendants of the root and never the root itself.
        var frame = Create("Static", "winwright bare", WsPopup | WsVisible, 300, 200);
        Create("Static", "a label", WsChild | WsVisible, 120, 20, frame);

        var reading = SubjectFor(frame, """Text[name="a label"]""").ReadOnce();

        Assert.True(reading.Found);
        Assert.Null(reading.Values.Value);
        Assert.Null(reading.Values.Range);
    }

    [Fact]
    public void The_values_of_a_text_box_carry_what_it_says_and_whether_it_is_writable()
    {
        var (frame, _) = Dialog();

        var values = SubjectFor(frame, "Edit").ReadOnce().Values;

        Assert.Equal("alpha", values.Value);
        Assert.False(values.IsReadOnly);
        Assert.Equal("alpha", values.Text);
    }

    [Fact]
    public void A_subject_with_no_deadline_at_all_is_refused()
    {
        var (frame, _) = Dialog();
        var root = AutomationElement.FromHandle(frame);

        Assert.Throws<ArgumentOutOfRangeException>(() => new Subject(root, Locator.Parse("Edit"), 0));
    }
}
