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

        // Before any window and before any render: a check that fingerprints around a launch must
        // see the whole write, not the part that happened to finish first.
        if (shapes.Value("store") is string where)
            Store.Write(where, shapes.Has("mutate"));

        // Before any window: a render of a fixed surface needs no window shown, which is the same
        // reading the harness makes about every capture it takes.
        if (shapes.Value("render") is string path)
            return Rendered(path);

        if (shapes.Has("resident"))
            return Resident();

        return shapes.Value("pump") == "none" ? Unpumped(shapes) : Pumped(shapes);
    }

    /// <summary>
    /// Render the fixed surface and stop. No window is shown, nothing is activated, and the same
    /// call twice writes the same bytes — which is the whole of what makes the comparison a check.
    /// </summary>
    private static int Rendered(string path)
    {
        // The application is constructed so the resources exist, and never run: a dispatcher loop
        // here would leave the process waiting for a window that is never going to appear.
        _ = new App();

        // On the background the application itself declares, which is the whole of what the
        // in-app half asks an adopter for: a key in its resources rather than a colour a harness
        // guessed. The receipt then names which source answered.
        var pane = FixedPane.Build();
        var picture = Winwright.InApp.Render.ToFile(pane, path, Protocol.Background(pane), FixedPane.Size);
        Console.Out.WriteLine(picture.Sentence());
        return 0;
    }

    /// <summary>
    /// A process that runs and shows nothing. It is the ordinary state of a tray application, it
    /// runs on every developer machine this tool was written on, and a refusal that fired on it
    /// would make every capture take an override — which is an override everybody passes always,
    /// and therefore a check nobody has.
    /// </summary>
    private static int Resident()
    {
        var app = new App
        {
            // Explicit, because the default closes an application the moment its last window goes
            // and this one never had a first.
            ShutdownMode = ShutdownMode.OnExplicitShutdown,
        };

        return app.Run();
    }

    /// <summary>The ordinary host: a dispatcher that runs, so input arrives.</summary>
    private static int Pumped(Flags shapes)
    {
        var app = new App();

        // The one arm worth its own branch: a run whose only window is a toast has no main window
        // for the process object to name, which is what an enumerating launcher exists for.
        if (shapes.Value("toast") == "only")
            return app.Run(Toast.Raise(null));

        var window = new MainWindow(shapes);
        if (shapes.Has("toast"))
            window.Loaded += (_, _) => Toast.Raise(window);

        if (shapes.Value("intrude") is string over)
            window.ContentRendered += (_, _) => Intruder.Raise(Intruder.Read(over));

        return app.Run(window);
    }

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
