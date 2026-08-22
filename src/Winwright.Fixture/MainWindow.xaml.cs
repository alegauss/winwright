using System.Windows;

namespace Winwright.Fixture;

/// <summary>
/// The fixture's window.
/// <para>
/// Every loop in this framework was developed against somebody's shipping product, which means a
/// real account, a real transcript directory, a real controller and a machine somebody set up by
/// hand. This removes all of it. It is not a demo and not a sample: it is the surface this
/// framework's own tests drive, and its design goal is to be hard to test in the specific ways
/// Windows is hard.
/// </para>
/// <para>
/// Nothing here reads a clock, a machine name, a setting or a file. That is the property the
/// later shapes lean on — a byte-identical render has nothing to be identical to unless two runs
/// on two desks draw the same pixels.
/// </para>
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>Build the window.</summary>
    public MainWindow() => InitializeComponent();

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
