using System.Runtime.InteropServices;

using Winwright.Asserting;
using Winwright.Locating;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW55. Diagnosing a failure cost a throwaway script that dumped the tree, so the reading was
/// done twice — once by the check that went red, once by a person asking it the same question
/// again.
/// <para>
/// The last test is the claude-tray case against a live window: a template part nobody filled in,
/// where the red carries the tree and the offending line is in it.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class DiagnosisTests : IDisposable
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

    public void Dispose()
    {
        for (var index = created.Count - 1; index >= 0; index--)
            DestroyWindow(created[index]);
    }

    private nint Create(string className, string? title, uint style, nint parent = 0)
    {
        var window = CreateWindowExW(0, className, title, style, 20, 20, 320, 200, parent, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
        return window;
    }

    private static ElementFacts Facts(string type, string name, string id = "") =>
        new(name, id, type, type, false, true, new WindowBounds(0, 0, 10, 10), new HashSet<string>(StringComparer.Ordinal));

    private static InspectedElement Node(ElementFacts facts, params InspectedElement[] children) =>
        new(facts, children, 0);

    /// <summary>A window with as many buttons as asked for, which is a tree with a known size.</summary>
    private static InspectedElement Wide(int buttons, string named = "Save")
    {
        var children = Enumerable
            .Range(0, buttons)
            .Select(index => Node(Facts("Button", $"{named} {index}")))
            .ToArray();

        return Node(Facts("Window", "winwright statistics"), children);
    }

    [Fact]
    public void A_red_carries_the_tree_that_was_in_hand_when_it_went_red()
    {
        var tree = Node(
            Facts("Window", "winwright statistics"),
            Node(Facts("Button", "Save")),
            Node(Facts("Edit", "", "profile")));

        var diagnosed = Diagnosis.Of(AssertionResult.Fail("the profile box is named", "it has no name at all"), tree);

        Assert.True(diagnosed.WasRead);
        Assert.Equal(3, diagnosed.Total);
        Assert.Equal(0, diagnosed.Dropped);

        var rendered = diagnosed.ToString();
        Assert.Contains("the profile box is named - it has no name at all", rendered);
        Assert.Contains("#profile", rendered);
    }

    [Fact]
    public void The_element_the_check_was_reading_is_marked_so_the_eye_lands_on_it()
    {
        var subject = Facts("Edit", "", "profile");
        var tree = Node(Facts("Window", "winwright statistics"), Node(Facts("Button", "Save")), Node(subject));

        var diagnosed = Diagnosis.Of(AssertionResult.Fail("the profile box is named", "no name"), tree, subject);

        Assert.True(diagnosed.Marks);
        var marked = Assert.Single(diagnosed.View, line => line.IsSubject);
        Assert.Contains("#profile", marked.Text);
        Assert.StartsWith("> ", marked.ToString());
        Assert.Contains("the one it read marked >", diagnosed.ToString());
    }

    [Fact]
    public void A_subject_read_a_moment_earlier_still_matches_the_line_that_describes_it()
    {
        // The subject was read before the act and the tree after it, so the two records differ in
        // everything that moves. What addresses an element is what may not differ.
        var subject = Facts("Edit", "", "profile") with { IsEnabled = false, Bounds = new WindowBounds(4, 4, 90, 30) };
        var tree = Node(Facts("Window", "winwright statistics"), Node(Facts("Edit", "", "profile")));

        Assert.True(Diagnosis.Of(AssertionResult.Fail("named", "no"), tree, subject).Marks);
    }

    [Fact]
    public void The_dump_is_bounded_and_says_how_much_it_cut()
    {
        var diagnosed = Diagnosis.Of(AssertionResult.Fail("a red", "because"), Wide(60), null, budget: 10);

        Assert.Equal(10, diagnosed.View.Count);
        Assert.Equal(61, diagnosed.Total);
        Assert.Equal(51, diagnosed.Dropped);
        Assert.Contains("10 of 61 elements", diagnosed.ToString());
        Assert.Contains("... 51 elements not shown below", diagnosed.ToString());
    }

    [Fact]
    public void The_window_is_kept_around_the_subject_rather_than_around_the_root()
    {
        var subject = Facts("Button", "Save 30");

        var diagnosed = Diagnosis.Of(AssertionResult.Fail("a red", "because"), Wide(60), subject, budget: 10);

        Assert.True(diagnosed.Marks, "the budget dropped the one line the failure pointed at");
        Assert.Equal(10, diagnosed.View.Count);
        Assert.Equal(51, diagnosed.Dropped);

        var rendered = diagnosed.ToString();
        Assert.Contains("elements not shown above", rendered);
        Assert.Contains("elements not shown below", rendered);
    }

    [Fact]
    public void A_tree_that_could_not_be_read_says_so_instead_of_being_absent()
    {
        var diagnosed = Diagnosis.Of(AssertionResult.Fail("a red", "because"), null);

        Assert.False(diagnosed.WasRead);
        Assert.Empty(diagnosed.View);
        Assert.Contains("the control view could not be read", diagnosed.ToString());
    }

    [Fact]
    public void A_handle_naming_no_window_is_an_absence_rather_than_a_throw()
    {
        var diagnosed = Diagnosis.OfWindow(AssertionResult.Fail("a red", "because"), 0);

        Assert.False(diagnosed.WasRead);
        Assert.Contains("the control view could not be read", diagnosed.ToString());
    }

    [Fact]
    public void A_pass_is_refused_because_a_dump_under_every_green_is_never_read()
    {
        var refused = Assert.Throws<ArgumentException>(
            () => Diagnosis.Of(AssertionResult.Pass("the profile box is named", "it is named \"Profile\""), Wide(2)));

        Assert.Contains("a report nobody reads to the end", refused.Message);
    }

    [Fact]
    public void A_hole_is_refused_because_the_tree_is_not_what_explains_one()
    {
        var refused = Assert.Throws<ArgumentException>(
            () => Diagnosis.Of(AssertionResult.Unchecked("the menu opens", Fixtures.FreeNotificationArea), Wide(2)));

        Assert.Contains("the precondition it lacked", refused.Message);
    }

    [Fact]
    public void The_diagnosis_opens_with_the_line_the_summary_would_have_printed_alone()
    {
        var failure = AssertionResult.Fail("the profile box is named", "it has no name at all");

        var rendered = Diagnosis.Of(failure, Wide(2)).Render();

        Assert.Equal(VerdictSummary.Line(failure), rendered[0]);
    }

    [Fact]
    public void A_live_window_with_an_unfilled_template_part_hands_the_red_the_line_that_shows_it()
    {
        var frame = Create("Static", "winwright statistics", WsPopup | WsVisible);
        Create("Button", "{profileName}", WsChild | WsVisible, frame);

        var tree = Inspect.Window(frame);
        var subject = Assert.Single(tree!.Walk(), element => element.Facts.ControlType == "Button").Facts;
        var check = Names.Of(subject);

        // The check is red on its own, and the dump under it is the throwaway script's output.
        Assert.Equal(Named.Templated, check.Verdict);
        var diagnosed = Diagnosis.Of(check.AsAssertion("the profile button is named"), tree, subject);

        Assert.True(diagnosed.Marks);
        Assert.Contains("{profileName}", diagnosed.ToString());
    }
}
