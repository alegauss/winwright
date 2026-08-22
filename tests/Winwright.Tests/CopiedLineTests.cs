using System.Runtime.InteropServices;
using System.Windows.Automation;

using Winwright.Asserting;
using Winwright.Locating;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW113. A step searches the descendants of the root it is given and never the root itself, which
/// is right and is what every other locator engine does. The trouble was that inspect printed the
/// root as its first line in exactly the shape of every other line — so the flow this block exists
/// for, read the tree and copy the line, had one line in it that quietly did not work, and the
/// miss came back diagnosed as absent.
/// <para>
/// What is checked here is the whole page rather than one line: a locator assembled from anything
/// inspect printed resolves, and the one line that is not a locator says so.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class CopiedLineTests : IDisposable
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

    private nint Create(string className, string? title, uint style, int w, int h, nint parent = 0)
    {
        var window = CreateWindowExW(
            0, className, title, style,
            parent == 0 ? OffScreen.Left : 20, parent == 0 ? OffScreen.Top : 20, w, h, parent, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
        return window;
    }

    /// <summary>A frame whose children are told apart by name, so no copied step is ambiguous.</summary>
    private nint Dialog()
    {
        var frame = Create("Static", "winwright statistics", WsPopup | WsVisible, 420, 300);
        Create("Button", "Save", WsChild | WsVisible, 90, 28, frame);
        Create("Button", "Cancel", WsChild | WsVisible, 90, 28, frame);
        Create("Edit", "alpha", WsChild | WsVisible, 200, 24, frame);
        return frame;
    }

    /// <summary>The locator on a rendered line: what is there before the rectangle.</summary>
    private static string Step(string line)
    {
        var text = line.TrimStart();
        var rectangle = text.IndexOf("  ", StringComparison.Ordinal);
        return rectangle < 0 ? text : text[..rectangle];
    }

    [Fact]
    public void The_root_is_marked_as_the_root_rather_than_printed_as_something_to_copy()
    {
        var lines = Inspect.Render(Inspect.Window(Dialog())!);

        Assert.StartsWith(Inspect.RootMark, lines[0]);
        Assert.False(
            Locator.TryParse(Step(lines[0]), out _, out _),
            "the root's line still reads as a locator, which is the thing that did not work");
    }

    [Fact]
    public void The_root_line_still_says_which_element_the_rest_are_relative_to()
    {
        var tree = Inspect.Window(Dialog())!;
        var lines = Inspect.Render(tree);

        // Marked is not hidden: a reader needs to know what the locators below are measured from,
        // so the root's line carries the same facts as any other, in a shape nobody copies.
        Assert.Contains(tree.Facts.ControlType, lines[0]);
        Assert.Contains(tree.Facts.ToString(), lines[0]);
        Assert.Contains("420x300 at ", lines[0]);
    }

    [Fact]
    public void Every_line_below_the_root_is_a_locator_that_resolves_against_it()
    {
        var frame = Dialog();
        var root = AutomationElement.FromHandle(frame);
        var lines = Inspect.Render(Inspect.Window(frame)!);

        // The claim in full: whichever line a reader started from, the locator they copied works.
        foreach (var line in lines.Skip(1).Where(one => !one.Contains("not walked", StringComparison.Ordinal)))
        {
            var step = Step(line);
            Assert.True(Locator.TryParse(step, out var locator, out var because), $"'{step}' is refused: {because}");
            Assert.True(Resolve.Once(root, locator!).Found, $"'{step}' was copied from the tree and matched nothing");
        }
    }

    [Fact]
    public void The_line_the_root_used_to_print_is_the_one_that_matches_nothing()
    {
        // Why the mark rather than a nicer step: the root's own step is well formed and addresses
        // nothing under the root, so printing it in the copyable shape was the defect itself.
        var frame = Dialog();
        var root = AutomationElement.FromHandle(frame);
        var tree = Inspect.Window(frame)!;

        var asStep = Locator.Parse(tree.Facts.AsLocatorStep().ToString());

        Assert.False(Resolve.Once(root, asStep).Found);
    }

    [Fact]
    public void A_dump_under_a_red_marks_its_root_too()
    {
        var frame = Dialog();
        var tree = Inspect.Window(frame)!;
        var subject = Assert.Single(tree.Walk(), element => element.Facts.Name == "Cancel").Facts;

        var diagnosed = Diagnosis.Of(AssertionResult.Fail("the cancel button is named", "no name"), tree, subject);

        // The tree under a failure is read the same way and copied from the same way, so the line
        // that does not work would not work there either.
        var view = diagnosed.View.Select(one => one.Text).ToList();
        Assert.StartsWith(Inspect.RootMark, view[0]);
        Assert.All(
            view.Skip(1).Where(one => !one.Contains("not walked", StringComparison.Ordinal)),
            one => Assert.True(Locator.TryParse(Step(one), out _, out _), one));
    }

    [Fact]
    public void One_element_asked_for_on_its_own_is_still_a_step_because_it_is_not_a_root()
    {
        var tree = Inspect.Window(Dialog())!;
        var button = Assert.Single(tree.Walk(), element => element.Facts.Name == "Save");

        Assert.DoesNotContain(Inspect.RootMark, Inspect.Line(button));
        Assert.StartsWith("Button", Inspect.Line(button));
    }
}
