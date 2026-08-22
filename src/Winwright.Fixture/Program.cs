using System.Windows;

namespace Winwright.Fixture;

/// <summary>
/// The fixture's entry point.
/// <para>
/// Written by hand rather than generated, and the reason is a measurement: a flag this fixture
/// does not know has to be refused <em>before</em> the presentation stack starts. Refusing from
/// inside the application's startup event leaves a message loop running with no window to close,
/// and the process then hangs until something kills it — which is a refusal that reads to a run
/// exactly like the timeout it exists to prevent.
/// </para>
/// <para>
/// It is also where the window's host is chosen. The difference between a window whose dispatcher
/// runs and one whose never does is invisible in every picture and decides whether keyboard input
/// arrives at all — one product shipped windows that took no keystrokes while every screenshot of
/// them looked perfect. Both are here, behind one flag.
/// </para>
/// </summary>
public static class Program
{
    /// <summary>What a run exits with where it was asked for a shape this fixture does not have.</summary>
    public const int UnknownFlag = 2;

    /// <summary>Read the flags, then draw whatever they asked for.</summary>
    /// <param name="arguments">The command line.</param>
    [STAThread]
    public static int Main(string[] arguments)
    {
        Flags shapes;
        try
        {
            shapes = Flags.Read(arguments ?? []);
        }
        catch (UnknownFlagException refused)
        {
            // The error stream and never a dialog: a fixture that stops on a message box in
            // continuous integration is a run that times out saying nothing at all.
            Console.Error.WriteLine(refused.Message);
            return UnknownFlag;
        }

        return shapes.Value("pump") == "none" ? Unpumped(shapes) : Pumped(shapes);
    }

    /// <summary>The ordinary host: a dispatcher that runs, so input arrives.</summary>
    private static int Pumped(Flags shapes) => new App().Run(new MainWindow(shapes));

    /// <summary>
    /// The host that is wrong in the one way no picture shows. The window is created and shown,
    /// which is enough for the render thread to compose it and for the compositor to hand a
    /// perfect screenshot to anybody who asks — and then this thread stops, so no message is ever
    /// dispatched to it and no keystroke reaches anything.
    /// </summary>
    private static int Unpumped(Flags shapes)
    {
        var app = new App();
        var window = new MainWindow(shapes);
        window.Show();

        // Deliberately never Run: this is the defect, reproduced. The process is left to be
        // stopped by whatever launched it, which is what the register already does.
        //
        // Slept and not waited, and measured rather than assumed: a managed wait on a
        // single-threaded apartment pumps messages while it blocks, so a window parked on one
        // answers everything it is sent and is not dead at all. Sleeping is the one way to stop
        // without pumping — which is itself the reason this defect is so easy to ship.
        Thread.Sleep(Timeout.Infinite);

        GC.KeepAlive(app);
        return 0;
    }
}
