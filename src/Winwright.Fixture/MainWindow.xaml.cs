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
    /// <summary>Build the window in whatever shape this run was asked for.</summary>
    /// <param name="shapes">The flags the run carried, or null for the default surface.</param>
    public MainWindow(Flags? shapes = null)
    {
        Shapes = shapes ?? Flags.Read([]);
        InitializeComponent();

        if (Shapes.Value("title") is string named)
            Title = named;

        if (Shapes.Has("names"))
            NamesPane.AddTo(panes);

        if (Shapes.Has("absences"))
            AbsencesPane.AddTo(panes);
    }

    /// <inheritdoc />
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Here and nowhere earlier: a window has no handle until its source exists, and the
        // compositor is asked about a handle.
        if (Shapes.Value("backdrop") is string kind)
            Backdrop.Set(new System.Windows.Interop.WindowInteropHelper(this).Handle, kind);
    }

    /// <summary>What this run was asked to be.</summary>
    public Flags Shapes { get; }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
