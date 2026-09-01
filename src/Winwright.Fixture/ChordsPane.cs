using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Winwright.Fixture;

/// <summary>
/// A pane whose commands have no button, no menu entry and no toolbar — only a chord.
/// <para>
/// WW317. This is the shape the task was filed for, and the reason it is worth a surface of its own:
/// an application with no menu is what this engine is best placed to test, because there is nothing
/// to click and a screenshot of it shows an empty window. The commands <em>are</em> the application,
/// the keyboard is the only route to them, and <c>click</c> needs a target that does not exist.
/// </para>
/// <para>
/// Two commands and not one, on purpose. A single chord could be reached by a run that pressed
/// anything at all if the pane reported the same thing either way, so each writes its own name and
/// the check is which one ran. The second carries a different modifier set for the same reason.
/// </para>
/// </summary>
public static class ChordsPane
{
    /// <summary>What the read-out says before either command has run.</summary>
    public const string Nothing = "no command has run";

    /// <summary>Build the pane and add it to the tab control, selected.</summary>
    /// <param name="panes">The tab control to add it to.</param>
    public static TabItem AddTo(TabControl panes)
    {
        ArgumentNullException.ThrowIfNull(panes);

        var rows = new StackPanel { Name = "chordRows", Margin = new Thickness(12) };
        rows.Children.Add(new TextBlock
        {
            Name = "chordsTitle",
            Text = "Chords",
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 8),
        });

        // The read-out, which is the whole surface: what a case reads to find out which command ran.
        var ran = new TextBlock { Name = "chordRan", Text = Nothing, Margin = new Thickness(0, 8, 0, 0) };
        System.Windows.Automation.AutomationProperties.SetName(ran, Nothing);
        rows.Children.Add(ran);

        // Said in the window as well, because a person driving this by hand has no other way to know
        // what it can do — there is nothing on screen that suggests a chord exists.
        rows.Children.Add(new TextBlock
        {
            Name = "chordsHint",
            Text = "Ctrl+Shift+I imports · Ctrl+Alt+F1 writes a bundle. Nothing else reaches them.",
            Margin = new Thickness(0, 12, 0, 0),
            Opacity = 0.7,
        });

        var pane = new TabItem { Name = "chordsPane", Header = "Chords", Content = rows };
        panes.Items.Add(pane);

        // Selected, because a tab's content is realised when it is picked and a pane nobody picked
        // is in no tree for a case to read.
        panes.SelectedItem = pane;

        // On the window and not on the pane, which is measured rather than preferred. Scoped to the
        // TabItem the bindings fire only while the focus is inside it, and a window that has just
        // opened has the focus nowhere in particular — so the first run of this pressed both chords
        // at a window that had no binding to match and read `no command has run` through seventy-one
        // polls, with the press itself reporting Ok. Nothing was wrong with the send.
        //
        // It is also the shape the task is about. An application whose commands are all on chords
        // binds them at its window, because that is what "the command is always available" means;
        // a per-screen binding would be an application with screens, which this one deliberately
        // does not have.
        var window = Window.GetWindow(panes) ?? throw new InvalidOperationException(
            "the chords pane is added before its window exists, so there is nothing to bind on");

        Bind(window, ran, Key.I, ModifierKeys.Control | ModifierKeys.Shift, "imported");
        Bind(window, ran, Key.F1, ModifierKeys.Control | ModifierKeys.Alt, "wrote a bundle");

        return pane;
    }

    /// <summary>
    /// One command, reachable by its chord and by nothing else.
    /// <para>
    /// No <c>Button</c> is given the command and no menu entry is made for it, which is the point:
    /// a surface that also offered a button would let a case pass through the route this exists to
    /// prove is unnecessary.
    /// </para>
    /// </summary>
    /// <param name="where">The window the binding lives on.</param>
    /// <param name="ran">The read-out the command writes to.</param>
    /// <param name="key">The key.</param>
    /// <param name="held">The modifiers held with it.</param>
    /// <param name="said">What the read-out says once it has run.</param>
    private static void Bind(Window where, TextBlock ran, Key key, ModifierKeys held, string said)
    {
        var command = new RoutedCommand();
        where.CommandBindings.Add(new CommandBinding(command, (_, _) =>
        {
            ran.Text = said;

            // Both, for WW238's reason: a caption's words are in its name and in no pattern, so the
            // reading a case can make is the name — and a command that wrote only the text would be
            // invisible to the check that matters.
            System.Windows.Automation.AutomationProperties.SetName(ran, said);
        }));

        where.InputBindings.Add(new KeyBinding(command, key, held));
    }
}
