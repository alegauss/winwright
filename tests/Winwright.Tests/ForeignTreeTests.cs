using System.Windows.Automation;

using Winwright.Locating;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW124. Found by driving something this repository did not build. Windows gives a window's own
/// system menu the automation id <c>Item 1</c>, and inspect rendered that element as
/// <c>MenuItem#Item 1[name="Sistema"]</c> — which the grammar refuses at the space, reporting that
/// it expected <c>&gt;</c> or the end and found <c>1</c>.
/// <para>
/// The name field was quoted for exactly this reason and the id was not, so an id was assumed to
/// be an identifier. Every fixture in the suite names its own controls, so nothing here ever
/// produced one with a space in it, and the existing check asserted the property against a window
/// this repository builds — which is the shape of check that only ever proves what the author
/// already assumed.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ForeignTreeTests : IDisposable
{
    private readonly List<PumpedDialog> opened = [];

    public void Dispose()
    {
        foreach (var dialog in opened)
            dialog.Dispose();
    }

    /// <summary>A window with a caption, which is what brings the system menu with it.</summary>
    private PumpedDialog Framed()
    {
        var dialog = PumpedDialog.OpenFramed("winwright statistics");
        opened.Add(dialog);
        return dialog;
    }

    /// <summary>
    /// The locator on a rendered line: what is there before the rectangle. The double space is
    /// looked for outside the quotation marks, because a name is somebody else's string and a real
    /// one contains runs of spaces.
    /// </summary>
    private static string Step(string line)
    {
        var text = line.TrimStart();
        var quoted = false;
        for (var at = 0; at < text.Length - 1; at++)
        {
            if (text[at] == '\\')
            {
                at++;
                continue;
            }

            if (text[at] == '"')
                quoted = !quoted;
            else if (!quoted && text[at] == ' ' && text[at + 1] == ' ')
                return text[..at];
        }

        return text;
    }

    [Fact]
    public void An_id_with_a_space_is_rendered_quoted_so_the_grammar_reads_the_whole_of_it()
    {
        var facts = new ElementFacts(
            "Sistema", "Item 1", "MenuItem", "", false, true, new Winwright.Windowing.WindowBounds(0, 0, 10, 10),
            new HashSet<string>(StringComparer.Ordinal));

        var step = facts.AsLocatorStep().ToString();

        Assert.Equal("""MenuItem#"Item 1"[name="Sistema"]""", step);
        Assert.True(Locator.TryParse(step, out var parsed, out var because), $"'{step}' is refused: {because}");
        Assert.Equal("Item 1", parsed!.Steps[0].AutomationId);
    }

    [Fact]
    public void An_ordinary_id_is_still_written_bare()
    {
        // The change is not "quote everything": a bare id is what a reader writes and what every
        // scenario in this repository already contains.
        var facts = new ElementFacts(
            "", "saveButton", "Button", "", false, true, new Winwright.Windowing.WindowBounds(0, 0, 10, 10),
            new HashSet<string>(StringComparer.Ordinal));

        Assert.Equal("Button#saveButton", facts.AsLocatorStep().ToString());
    }

    [Theory]
    [InlineData("Item 1")]
    [InlineData("a\"quoted\"id")]
    [InlineData("with:a:colon")]
    [InlineData("[bracketed]")]
    [InlineData("a > b")]
    [InlineData("a\\backslash")]
    [InlineData("two  spaces")]
    public void Whatever_an_application_calls_a_control_the_step_round_trips(string id)
    {
        // The general rule rather than the one shape that was found: an id is somebody else's
        // string, and every character in it survives the trip out and back.
        var facts = new ElementFacts(
            "", id, "Button", "", false, true, new Winwright.Windowing.WindowBounds(0, 0, 10, 10),
            new HashSet<string>(StringComparer.Ordinal));

        var step = facts.AsLocatorStep().ToString();

        Assert.True(Locator.TryParse(step, out var parsed, out var because), $"'{step}' is refused: {because}");
        Assert.Equal(id, parsed!.Steps[0].AutomationId);
    }

    [Theory]
    [InlineData("Claude Code\nSessão 5h restante: 38%")]
    [InlineData("carriage\r\nreturn")]
    [InlineData("a\ttab")]
    public void A_name_that_runs_to_several_lines_still_renders_as_one(string name)
    {
        // The second one of these, found the same way and by the same run: a tray icon's name is a
        // tooltip and a real one runs to several lines. Rendered raw, a verb whose whole claim is
        // one line per element was printing three, of which only the first could be copied.
        var facts = new ElementFacts(
            name, "", "Button", "", false, true, new Winwright.Windowing.WindowBounds(0, 0, 10, 10),
            new HashSet<string>(StringComparer.Ordinal));

        var step = facts.AsLocatorStep().ToString();

        Assert.DoesNotContain('\n', step);
        Assert.DoesNotContain('\r', step);
        Assert.True(Locator.TryParse(step, out var parsed, out var because), $"'{step}' is refused: {because}");
        Assert.Equal(name, parsed!.Steps[0].Name);
    }

    [Fact]
    public void The_rendered_tree_has_exactly_one_line_for_every_element()
    {
        // The claim the escaping protects, said as arithmetic. A single name carrying a line break
        // used to make this off by however many lines that name had.
        var dialog = Framed();
        var tree = Inspect.Window(dialog.Frame, depth: 4)!;

        var walked = tree.Walk().Count();
        var elided = tree.Walk().Count(one => one.Elided > 0);

        Assert.Equal(walked + elided, Inspect.Render(tree).Count);
    }

    [Fact]
    public void The_system_menu_windows_puts_on_every_framed_window_is_addressable()
    {
        // The element that found this, on a real window rather than in a constructed record. It is
        // chrome nobody here wrote, which is the whole point: the first tree walked that somebody
        // else built broke on the first line under the title bar.
        var dialog = Framed();
        var tree = Inspect.Window(dialog.Frame, depth: 3)!;

        var chrome = tree.Walk().FirstOrDefault(one => one.Facts.AutomationId.Contains(' ', StringComparison.Ordinal));
        Assert.NotNull(chrome);

        var step = chrome.Facts.AsLocatorStep().ToString();
        Assert.True(Locator.TryParse(step, out var parsed, out var because), $"'{step}' is refused: {because}");
        Assert.True(Resolve.Once(dialog.Root, parsed!).Found, $"'{step}' was copied from the tree and matched nothing");
    }

    [Fact]
    public void Every_line_printed_for_a_window_nobody_here_wrote_is_one_that_parses()
    {
        // The claim in full, against the chrome Windows brings rather than the controls this suite
        // creates: whichever line a reader copied, the locator they got works.
        var dialog = Framed();
        var lines = Inspect.Render(Inspect.Window(dialog.Frame, depth: 4)!);

        foreach (var line in lines.Skip(1).Where(one => !one.Contains("not walked", StringComparison.Ordinal)))
        {
            var step = Step(line);
            Assert.True(Locator.TryParse(step, out _, out var because), $"inspect printed '{step}', refused: {because}");
        }
    }

    [Fact]
    public void The_same_holds_for_the_shell_which_is_the_largest_tree_nobody_here_owns()
    {
        // The taskbar is another process's window and it is always there. If any shape of id, name
        // or class defeats the grammar, this is where it turns up rather than in a fixture.
        var tray = Winwright.Acting.NotificationArea.Tray();
        Assert.NotNull(tray);

        var lines = Inspect.Render(Inspect.Under(tray, depth: 4)!);

        foreach (var line in lines.Skip(1).Where(one => !one.Contains("not walked", StringComparison.Ordinal)))
        {
            var step = Step(line);
            Assert.True(Locator.TryParse(step, out _, out var because), $"inspect printed '{step}', refused: {because}");
        }
    }
}
