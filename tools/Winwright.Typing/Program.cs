using System.Diagnostics;
using System.Globalization;
using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Windowing;

namespace Winwright.Typing;

/// <summary>
/// WW302. Which side of the send WW249's substitution is on, measured rather than reasoned about.
/// <para>
/// Ten reds narrowed it to one boundary and stopped. WPF, the control and the message pump are out —
/// the recorder in the fixture's own window shows the characters arriving already substituted. The
/// send is out by reading: <c>Keyboard</c> builds one input pair per UTF-16 code unit into a single
/// array, and <c>SendInput</c> queues them before returning. What is left is between insertion and
/// <c>WM_CHAR</c>, and reading cannot divide it further.
/// </para>
/// <para>
/// So this compares the two shapes. One call carrying every code unit is what the engine does; one
/// call per code unit is the other side of the same boundary. If the substitution follows the batch,
/// the array is implicated despite the argument that rules it out, and the argument is what is
/// wrong. If it follows both alike, the queue survives having been tested rather than assumed.
/// </para>
/// <para>
/// It counts and never fails. A diagnostic that goes red is one somebody turns off, and the number
/// wanted here is a rate — which no assertion can carry.
/// </para>
/// </summary>
public static class Program
{
    /// <summary>The exit code for a machine that could not run the experiment at all.</summary>
    private const int Unrunnable = 3;

    /// <summary>
    /// How many rounds each arm types, unless a caller says otherwise.
    /// <para>
    /// WW249 fails about one guest run in four at five rounds a run, so a substitution is somewhere
    /// near one round in twenty. Four hundred each way is twenty or so events per arm — enough that
    /// a difference between the arms is a difference, and not so many that the run outlasts patience.
    /// </para>
    /// </summary>
    private const int Rounds = 400;

    public static int Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var rounds = args.Length > 0 && int.TryParse(args[0], CultureInfo.InvariantCulture, out var many) && many > 0
            ? many
            : Rounds;

        var executable = Fixture();
        if (executable is null)
        {
            Console.Error.WriteLine("the fixture is not built beside this tool; build the solution first");
            return Unrunnable;
        }

        using var fixture = Process.Start(new ProcessStartInfo(executable));
        if (fixture is null)
        {
            Console.Error.WriteLine($"the fixture would not start: {executable}");
            return Unrunnable;
        }

        try
        {
            return Measured(fixture, rounds);
        }
        finally
        {
            if (!fixture.HasExited)
                fixture.Kill(entireProcessTree: true);
        }
    }

    private static int Measured(Process fixture, int rounds)
    {
        var drawn = Attempt.UntilTrue(() => TopLevelWindows.Largest(fixture.Id) is not null, 20000, 25);
        if (!drawn.Happened)
        {
            Console.Error.WriteLine($"the fixture drew no window in {drawn.WaitedMs}ms");
            return Unrunnable;
        }

        var root = AutomationElement.FromHandle(TopLevelWindows.Largest(fixture.Id)!.Handle);

        // The Config tab, which WPF does not build until it is visited. A pattern act needs no
        // foreground, which is why it is the right way to reach the state a synthesised act is
        // then measured in.
        var selected = Act.Select(On(root, "TabItem#configPane"));
        if (!selected.Attempted)
        {
            Console.Error.WriteLine($"the config tab would not select: {selected}");
            return Unrunnable;
        }

        var box = On(root, "Edit#profile");

        // Alternated round by round rather than run in two halves. The desk drifts — a background
        // task starts, a toast arrives — and two halves would hand the whole of that drift to
        // whichever arm was running at the time.
        var batch = new Tally("one call carrying every code unit");
        var split = new Tally("one call per code unit");

        for (var round = 1; round <= rounds; round++)
        {
            Round(box, $"WW302-{round}", batch, Whole);
            Round(box, $"WW302-{round}", split, OneAtATime);
        }

        Console.WriteLine($"{rounds} round(s) each way, typing at the fixture's WPF text box.");
        Console.WriteLine(batch.ToString());
        Console.WriteLine(split.ToString());
        Console.WriteLine(Verdict(batch, split));
        return 0;
    }

    /// <summary>
    /// One round: type it, read it back, and record what came out where it differs.
    /// </summary>
    /// <param name="box">The text box under test.</param>
    /// <param name="typing">What this round sends, different each round so a box that kept the last round's text shows up.</param>
    /// <param name="tally">Where the outcome is counted.</param>
    /// <param name="how">Which shape of send to use.</param>
    private static void Round(Subject box, string typing, Tally tally, Action<Subject, string> how)
    {
        how(box, typing);

        var read = box.Read().Values.Value;
        tally.Saw(typing, read);
    }

    /// <summary>The engine's own path: one <c>Type</c>, so one array and one call.</summary>
    private static void Whole(Subject box, string typing) => Keyboard.Type(box, typing);

    /// <summary>
    /// The same text as one <c>Type</c> per code unit, so one <c>SendInput</c> each.
    /// <para>
    /// The first replaces what is there and the rest append, which is what makes the two arms end at
    /// the same text. They differ in more than the batching — each call re-reads focus — and that is
    /// worth saying rather than hiding: what this divides is whether a batch is necessary for the
    /// substitution, not whether batching alone is sufficient for it.
    /// </para>
    /// </summary>
    /// <param name="box">The text box under test.</param>
    /// <param name="typing">What this round sends.</param>
    private static void OneAtATime(Subject box, string typing)
    {
        for (var at = 0; at < typing.Length; at++)
            Keyboard.Type(box, typing[at].ToString(), replacingWhatIsThere: at == 0);
    }

    /// <summary>
    /// What the two arms together say, in the one sentence somebody reads.
    /// <para>
    /// Stated as what was counted and never as a conclusion. Whether twenty against two is a
    /// difference is a judgement, and a tool that ran the experiment once is the worst thing to make
    /// it — the point of the numbers is that a reader can weigh them.
    /// </para>
    /// </summary>
    /// <param name="batch">The arm sending one call for the whole string.</param>
    /// <param name="split">The arm sending one call per code unit.</param>
    private static string Verdict(Tally batch, Tally split) =>
        batch.Wrong == 0 && split.Wrong == 0
            ? "Neither arm substituted anything, so this run divides nothing: WW249 is rare, and a"
                + " run that saw none of it is a run with nothing to say rather than an acquittal."
            : $"Substituted {batch.Wrong} against {split.Wrong}. What that is worth is a reading,"
                + " and both arms ran under the same desk in the same minutes.";

    /// <summary>
    /// The fixture's executable, in the configuration this tool was itself built in.
    /// <para>
    /// Found by path and not by reference. An application under test is launched from its own
    /// output: referencing it would copy the apphost without its assembly, and every launch would
    /// die at CLR startup — which is measured, in the suite that learned it first.
    /// </para>
    /// </summary>
    private static string? Fixture()
    {
        var here = new DirectoryInfo(AppContext.BaseDirectory);
        var framework = here.Name;
        var configuration = here.Parent!.Name;

        // tools/Winwright.Typing/bin/<configuration>/<framework> — four up is the checkout.
        var checkout = here.Parent!.Parent!.Parent!.Parent!.Parent!.FullName;
        var path = Path.Combine(
            checkout, "src", "Winwright.Fixture", "bin", configuration, framework, "Winwright.Fixture.exe");

        return File.Exists(path) ? path : null;
    }

    private static Subject On(AutomationElement root, string locator) =>
        Subject.Unguarded(root, Locator.Parse(locator), deadlineMs: 8000, pollMs: 25);
}

/// <summary>
/// One arm's count, and where the substitutions landed.
/// <para>
/// The positions are kept because WW249's rule is about them: the substituted character is always
/// the last one sent, at no fixed position. An arm that broke that rule would be the finding, and a
/// count alone could not show it.
/// </para>
/// </summary>
/// <param name="named">Which shape of send this arm used.</param>
public sealed class Tally(string named)
{
    private readonly List<string> examples = [];

    /// <summary>How many rounds this arm ran.</summary>
    public int Rounds { get; private set; }

    /// <summary>How many of them read back as something other than what was sent.</summary>
    public int Wrong { get; private set; }

    /// <summary>Record one round.</summary>
    /// <param name="sent">What was typed.</param>
    /// <param name="read">What the box read back.</param>
    public void Saw(string sent, string? read)
    {
        Rounds++;
        if (string.Equals(sent, read, StringComparison.Ordinal))
            return;

        Wrong++;

        // Bounded, because an arm that goes wrong every round would otherwise print a page per
        // round and bury the counts the run is for.
        if (examples.Count < 20)
            examples.Add($"sent {sent}, read {read ?? "<nothing>"}");
    }

    /// <summary>The arm's line in the report.</summary>
    public override string ToString()
    {
        var rate = Rounds == 0 ? "" : $" ({(double)Wrong / Rounds:P1})";
        var listed = examples.Count == 0 ? "" : $"\n    {string.Join("\n    ", examples)}";
        return $"  {named}: {Wrong} of {Rounds} substituted{rate}{listed}";
    }
}
