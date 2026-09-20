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

    /// <summary>
    /// What a run exits with where it was asked to be a launch that dies on startup.
    /// <para>
    /// WW279. One rather than zero because a tray that crashed did not succeed, and its own number
    /// rather than <see cref="UnknownFlag" /> or <see cref="Refused" /> for the reason those two are
    /// apart: a shape that died because it was asked to must be tellable from a fixture driven wrong.
    /// </para>
    /// </summary>
    public const int Died = 1;

    /// <summary>
    /// What a run exits with where the shape it was asked for was refused by the in-app half.
    /// <para>
    /// Its own code and not <see cref="UnknownFlag" />: a shape that provoked the refusal it exists
    /// to provoke did what it was asked, and a run that cannot tell that from a misspelt flag is a
    /// run that would read the fixture working as the fixture being driven wrong.
    /// </para>
    /// </summary>
    public const int Refused = 3;

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

        // First of all, and on the output stream rather than the error one: this is an answer and
        // not a refusal. A catalogue only reachable by misspelling something is a catalogue nobody
        // consults, and a flag nobody knows about is a shape nobody tests against.
        if (shapes.Has("flags"))
        {
            Console.Out.WriteLine(Flags.Catalogue(justified: true).TrimStart('\n'));
            return 0;
        }

        // Before any window and before any render: a check that fingerprints around a launch must
        // see the whole write, not the part that happened to finish first.
        if (shapes.Value("store") is string where)
            Store.Write(where, shapes.Has("mutate"));

        // Before any window: a render of a fixed surface needs no window shown, which is the same
        // reading the harness makes about every capture it takes.
        if (shapes.Value("render") is string path)
            return Rendered(path, shapes);

        // WW260. On the output stream and before any window, exactly as `--flags` is: this is an
        // answer the application gives about itself, and a case derives its expected set from it
        // rather than from a strings file. One per line, because that is all a set needs.
        if (shapes.Has("profiles"))
        {
            foreach (var profile in Store.Profiles)
                Console.Out.WriteLine(profile);

            return 0;
        }

        // WW468. The read-out whose answer is a translation. Everything above answers this machine's
        // data, which reads the same in every language — so no case here could make a set derived
        // from the application disagree with a window launched in another one, and the defect was
        // only ever reachable in claude-tray, where a menu drew `Personal` beside a read-out printing
        // `Pessoal`.
        //
        // The tab headers, because they are already the set the strings well derives from: asked in
        // the same language the two wells must answer identically, and asked in different ones they
        // must not. That is the whole claim, and neither half of it can be written by typing a word.
        if (shapes.Has("tab-names"))
        {
            // WW469. The half of the contract an application owes, in the place an adopter copies
            // from. Without it this printed its ANSI code page, the harness decoded the console's
            // OEM one, and `Relatório` came back as `Relat¾rio` over the single byte they spell
            // differently — a set disagreeing with the window about a name they both held.
            Console.OutputEncoding = new System.Text.UTF8Encoding(false);

            var speaking = Strings.Load(shapes.Value("language") ?? Strings.Cultures[0]);
            foreach (var header in speaking.Under("tabs"))
                Console.Out.WriteLine(header);

            return 0;
        }

        // WW294. The scalar beside `--profiles`: one line, so a case can compare a reading against a
        // fact this application knows and no case could type. The first of the set, deliberately —
        // what matters is that the two answer consistently, which is what a case asserting the marks
        // on a submenu is actually about.
        if (shapes.Has("profile"))
        {
            // WW473. The sampled answer where the launch carried one, which is what makes this
            // read-out able to prove anything about a fixture that samples. A fixture's
            // `environment` reaches the application as `--sample=<value>` — the same argument
            // `Launching()` puts on the window — and until WW473 a read-out was composed out of the
            // project alone, so it answered about the real machine while the window drew the
            // sampled one. A read-out that ignored the flag could not tell the two apart, and the
            // defect would have stayed invisible in the one place built to expose it.
            Console.Out.WriteLine(shapes.Value("sample") ?? Store.Profiles[0]);
            return 0;
        }

        // WW279. Before the resident arm and before any window: what this shape is is a launch that
        // is gone by the time anything looks at it, and a process that drew something first would be
        // a different defect. It exits rather than throwing, because a tray that crashes on startup
        // reaches a run as a process that has left and never as a message anybody caught.
        if (shapes.Has("dies"))
            return Died;

        if (shapes.Has("resident"))
            return Resident();

        // WW358. Before any window, for the reason the resident arm is: this shape is a process with
        // no frame, and one that drew something first would put a real window in front of the shadow
        // — which is exactly what stopped every case in the suite from provoking this.
        //
        // Here rather than in a verb of its own, because the catalogue asks that the code reaching a
        // shape is the code testing its flag. A member that raised the menu without naming
        // `shadowed` would be a shape catalogued behind a flag nothing near it tests, which is the
        // entry this suite refuses to take on trust.
        if (shapes.Has("shadowed"))
        {
            using var menu = Shadowed.Raise();

            // Qualified, and WinForms' rather than WPF's: the menu is a WinForms control and its
            // messages go to the loop that owns it. There is no window to run that loop on, so it is
            // the application-level one and the menu is what keeps the process alive.
            System.Windows.Forms.Application.Run();
            return 0;
        }

        // WW451. Beside the shadowed arm and for the same two reasons: a tray application's only
        // surface is its icon, and the code reaching a shape is the code testing its flag.
        //
        // What is new is who owns it. Every case proving `open tray menu` puts its icon up from
        // inside the test host, so the verb has never answered the question an adopter asks — this
        // shape is that question, and it is a flag on the fixture because a tray under test is
        // another process by definition.
        if (shapes.Value("tray") is string kind)
        {
            using var tray = Trayed.Raise(kind);

            // The loop this shape needs, on the same thread the icon was registered from: the shell
            // delivers the icon's callbacks to the window that owns it, and a drop-down's messages go
            // to the loop that owns the control. There is no window to run either on, so it is the
            // application-level one, and the icon is what keeps the process alive.
            System.Windows.Forms.Application.Run();
            return 0;
        }

        return shapes.Value("pump") == "none" ? Unpumped(shapes) : Pumped(shapes);
    }

    /// <summary>
    /// Render whatever this run asked for and stop. No window is shown, nothing is activated, and
    /// the default surface twice writes the same bytes — which is the whole of what makes the
    /// comparison a check.
    /// <para>
    /// WW146 put three shapes behind this verb, and each of them ends in a refusal rather than a
    /// file. The refusal goes to the error stream and the exit code is its own, so a run can tell a
    /// shape that did what it was asked from a fixture that was driven wrong.
    /// </para>
    /// </summary>
    /// <param name="path">Where the picture goes, where one is written at all.</param>
    /// <param name="shapes">What this run asked the render to be.</param>
    private static int Rendered(string path, Flags shapes)
    {
        // Every arm but one constructs the application so the resources exist, and none of them
        // runs it: a dispatcher loop here would leave the process waiting for a window that is
        // never going to appear. --unbacked is the exception, and withholding it is the shape:
        // the capture background is declared in those resources, so an application that exists
        // answers the question this run exists to leave unanswered.
        if (!shapes.Has("unbacked"))
            _ = new App();

        try
        {
            return Drawn(path, shapes);
        }
        catch (Exception refusal) when (refusal is Winwright.InApp.UnrenderableException
            or Winwright.InApp.NoBackgroundException)
        {
            // The error stream and never a dialog, for the same reason an unknown flag takes it: a
            // fixture that stops on a message box in continuous integration is a run that times
            // out saying nothing at all.
            //
            // The type by name and then the sentence. A run reads this across a process boundary,
            // where an exception is a string and every refusal looks alike — and the pairing this
            // shape exists for names a type, so a case that could only match prose would be
            // agreeing with a transcription rather than with the refusal that fired.
            Console.Error.WriteLine($"{refusal.GetType().Name}: {refusal.Message}");
            return Refused;
        }
    }

    /// <summary>The render itself, in whichever shape was asked for.</summary>
    /// <param name="path">Where the picture goes.</param>
    /// <param name="shapes">What this run asked the render to be.</param>
    private static int Drawn(string path, Flags shapes)
    {
        // A page whose every row is collapsed, and no size named: the size is what is being
        // refused, so naming one here would render the very thing the shape exists to prevent.
        if (shapes.Has("sizeless"))
        {
            Console.Out.WriteLine(Winwright.InApp.Render.ToFile(SizelessPane.Build(), path).Sentence());
            return 0;
        }

        // On no background, and that is half the shape: a blank composed onto a colour is opaque
        // everywhere whatever the tree did. This run writes the file and exits clean — the refusal
        // it exists for belongs to whoever reads the picture back, which is the harness.
        if (shapes.Has("blank"))
        {
            Console.Out.WriteLine(
                Winwright.InApp.Render.ToFile(BlankPane.Build(), path, BlankPane.Size).Sentence());
            return 0;
        }

        // On the background the application itself declares, which is the whole of what the
        // in-app half asks an adopter for: a key in its resources rather than a colour a harness
        // guessed. The receipt then names which source answered — and under --unbacked there is no
        // application and no window, so neither source does.
        var pane = FixedPane.Build();
        Console.Out.WriteLine(
            Winwright.InApp.Render.ToFile(pane, path, Protocol.Background(pane), FixedPane.Size).Sentence());

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
