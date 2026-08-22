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

        return new App().Run(new MainWindow(shapes));
    }
}
