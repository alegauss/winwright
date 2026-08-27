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

        if (Shapes.Has("ranges"))
            RangesPane.AddTo(panes);

        if (Shapes.Has("pickers"))
            PickersPane.AddTo(panes);

        if (Shapes.Value("rows") is string rows)
            RowsPane.AddTo(panes, rows);

        if (Shapes.Has("peerless"))
            Peerless.AddTo(panes);

        if (Shapes.Value("loading") is string howLong)
            LoadFor(int.Parse(howLong, System.Globalization.CultureInfo.InvariantCulture));

        if (Shapes.Value("language") is string tag)
            Speak(Strings.Load(tag));

        if (Shapes.Value("animate") is string everySoOften)
        {
            // On the Status pane and selected, because a pane nobody picked is realised into no
            // tree at all - which would be an animation running where nothing can read it.
            panes.SelectedItem = statusPane;
            animation = Animation.On(
                animationState, int.Parse(everySoOften, System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    /// <summary>Held so it is not collected while it is still meant to be running.</summary>
    private Animation? animation;

    /// <summary>What language this window is in, where a run asked for one.</summary>
    public Strings? Speaking { get; private set; }

    /// <summary>
    /// Label the window from a set of strings. Every label comes from the file, including the one
    /// carrying a placeholder — a fixture that quietly left that one out would be a fixture the
    /// rule it exists for could never be developed against.
    /// </summary>
    private void Speak(Strings said)
    {
        Speaking = said;

        reportPane.Header = said.Says("tabs.report");
        statusPane.Header = said.Says("tabs.status");
        configPane.Header = said.Says("tabs.config");
        save.Content = said.Says("buttons.save");
        close.Content = said.Says("buttons.close");
        localizedLabel.Text = said.Says(Strings.PlaceholderKey);

        // WW43: the loading note is a declared string like every other caption here, so a check
        // reading the project's language files finds it rather than matching a hard-coded phrase.
        loadingNote.Text = said.Says(Strings.LoadingKey);
    }

    /// <summary>Held for the window's life, because letting go puts every popup back.</summary>
    private Winwright.InApp.PopupsHeld? popups;

    /// <inheritdoc />
    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        // Here and not on Loaded: content rendered is the first moment every rectangle is the one
        // that was actually drawn, and a surface reported before that is a surface nobody saw.
        // Asked for and never assumed: the window is unactivated by default so a suite raising it
        // thirty times a run does not decide the foreground, and a person driving it by hand says
        // so once and gets the window in front of them.
        if (Shapes.Has("show"))
        {
            Activate();
            Topmost = true;
            Topmost = false;
        }

        // WW199. Here and not where the backdrop is set, which was measured rather than reasoned
        // about: DWMWA_CLOAK on a window the compositor has not composed yet is accepted and reports
        // NotCloaked afterwards. Content rendered is the first moment there is something to stop
        // drawing, which is the same reason the surface is reported here and not on Loaded.
        if (Shapes.Has("cloak"))
            Cloak.Set(new System.Windows.Interop.WindowInteropHelper(this).Handle);

        popups ??= Protocol.Hold(this);
        Protocol.Report(this, panes, panes.SelectedContent as FrameworkElement);
    }

    /// <summary>
    /// Show the loading note in the report page's place, and swap the real content in after the
    /// duration this run asked for.
    /// <para>
    /// The note replaces the content rather than sitting beside it: a page showing both is not a
    /// page that is still loading, and a check reading it would be right to say so.
    /// </para>
    /// </summary>
    /// <param name="milliseconds">How long to stay loading. Zero finishes on the first tick.</param>
    private void LoadFor(int milliseconds)
    {
        loadingNote.Text = (Speaking ?? Strings.Load("en")).Says(Strings.LoadingKey);
        loadingNote.Visibility = Visibility.Visible;
        reportNote.Visibility = Visibility.Collapsed;
        reportSwatch.Visibility = Visibility.Collapsed;

        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(milliseconds),
        };

        timer.Tick += (sender, _) =>
        {
            ((System.Windows.Threading.DispatcherTimer)sender!).Stop();
            loadingNote.Visibility = Visibility.Collapsed;
            reportNote.Visibility = Visibility.Visible;
            reportSwatch.Visibility = Visibility.Visible;
        };

        timer.Start();
    }

    /// <inheritdoc />
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Here and nowhere earlier: a window has no handle until its source exists, and the
        // compositor is asked about a handle.
        var window = new System.Windows.Interop.WindowInteropHelper(this).Handle;

        if (Shapes.Value("backdrop") is string kind)
            Backdrop.Set(window, kind);
    }

    /// <summary>What this run was asked to be.</summary>
    public Flags Shapes { get; }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
