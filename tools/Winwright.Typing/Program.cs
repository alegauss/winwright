using System.Diagnostics;
using System.Globalization;
using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Windowing;

namespace Winwright.Typing;

/// <summary>
/// WW249. What the repair costs and what it leaves behind, measured rather than reasoned about.
/// <para>
/// The question this tool was built for is answered. WW302 divided the batch from the spacing, WW304
/// swept the spacing and priced it out of reach, and WW310 read the fault's shape off 130 failures
/// and found one shape. The engine now sends as it always did and repeats the send where the reading
/// carries that shape, which is the repair the arms were being compared to choose between.
/// </para>
/// <para>
/// So the arms are gone and what is left is one path measured against itself. A <c>Type</c> reports
/// how many times it had to send again, which is the raw fault rate this tool used to have to
/// construct an arm to see, and whether the text arrived in the end, which is what the repair fails
/// to fix. Those two numbers are the whole reading: the first says the fault is still there, and the
/// second says whether anything survives three sends of it.
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
    /// How many rounds this types, unless a caller says otherwise.
    /// <para>
    /// WW249 fails about one send in fifty, so four hundred rounds is a handful of events — enough
    /// that the repair is seen firing and not so many that the run outlasts patience. What it cannot
    /// do at this length is put a bound on the survivors: three resends puts those past one in a
    /// million, and no run anybody sits through reaches that. A run that reads zero survivors has
    /// failed to refute the repair rather than confirmed it, and the report below says so.
    /// </para>
    /// </summary>
    private const int Rounds = 400;

    /// <summary>
    /// How many rounds in a row may arrive wrong before this stops calling them rounds.
    /// <para>
    /// Four, and the number barely matters: what it is guarding against is not a rare event repeating
    /// but a desk that went away, and one of those never comes back on its own. A run that types into
    /// nothing still reads the box, and what it reads is the last thing that landed there — which is
    /// how a host run once reported 251 substitutions in a row and looked like data.
    /// </para>
    /// </summary>
    private const int Void = 4;

    /// <summary>
    /// How many faulted rounds have their arrivals printed. WW312: a reading nobody reads to the end
    /// is a reading nobody reads, and the shape of the row is the question rather than the census.
    /// </summary>
    private const int MostArrivals = 12;

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

        // WW312. The arrival side, which nothing had ever read: the fixture records how long each
        // character waited behind the one before it as its own thread pulled it off the queue.
        // Every reading this fault has been measured with until now is on the sending side — how
        // many calls, how far apart — and the fault is a character ARRIVING where a different one
        // was sent.
        var waited = On(root, "Text#waited");

        // WW312. The two halves of the same round, so a fault can be attributed. `arrived` is what
        // the window's thread pulled off the queue as WM_CHAR; `injected` is the code unit each of
        // those was injected as, read by the fixture's low-level hook before the queue ever had it.
        // If the injected units already carry the substitution it entered at SendInput, and if they
        // do not it entered at the translation — and nothing has ever put the two side by side at
        // the moment one went wrong. The first attempt at this pairing read the keydown's own words
        // and found the code unit in neither of them, which is why the reading moved upstream.
        var arrived = On(root, "Text#arrived");
        var packets = On(root, "Text#injected");
        var tally = new Tally();

        // Read only where the repair fired, and never per round. A read is a cross-process call and
        // this one is against the window under test, so taking it every round would put the reader
        // inside the thing it is measuring — which is exactly what WW316 had to take out of here.
        var arrivals = new List<string>();

        // WW313. Kept per round, because this rate moves within a run: the fixture appends every
        // value it is sent to a read-out under the box, so the window grows all run, and one measured
        // run slowed from 4600 to 11325ms a round while its failures rose 6, 6, 11, 22 by quarter. A
        // total taken across that is an average of two different machines.
        var faulted = new List<bool>(rounds);
        var spent = new List<double>(rounds);

        var lost = 0;
        var ran = 0;
        for (var round = 1; round <= rounds; round++)
        {
            // Named rather than built at the call, because it is now part of the reading: a faulted
            // round prints what was sent above what was injected and what arrived, and a report that
            // left the sent string to be reconstructed from the round number would be asking its
            // reader to do the comparison the tool exists to make.
            var typing = $"WW249-{round}";
            var typed = Round(box, typing, tally, out var took);
            faulted.Add(typed.Resends > 0);
            spent.Add(took.TotalMilliseconds);
            ran = round;

            if (typed.Resends > 0 && arrivals.Count < MostArrivals)
            {
                arrivals.Add(
                    $"round {round}"
                        + $"{Environment.NewLine}    gaps     {Gaps(waited)}"
                        + $"{Environment.NewLine}    sent     {typing}"
                        + $"{Environment.NewLine}    injected {Injected(packets, TwoRounds)}"
                        + $"{Environment.NewLine}    arrived  {Tailed(arrived, TwoRounds)}");
            }

            lost = typed.Arrived ? 0 : lost + 1;
            if (lost >= Void)
            {
                Console.Error.WriteLine(
                    $"the desk stopped taking input at round {round - lost + 1}: the box has read back"
                        + $" wrong for {lost} rounds running, which is a lost foreground and not a rate.");
                Console.Error.WriteLine(
                    "Nothing above it is a measurement either, because a run that loses the desk cannot"
                        + " say when it started losing it. Run it on a desk nobody is using.");
                return Unrunnable;
            }
        }

        Console.WriteLine($"{ran} round(s), typing at the fixture's WPF text box.");
        Console.WriteLine(tally.ToString());
        Console.Write(Drift(faulted, spent));

        if (arrivals.Count > 0)
        {
            Console.WriteLine(
                $"WW312: the two ends of {arrivals.Count} of the faulted round(s), most recent last. "
                    + "`injected` is what SendInput was handed, read by the fixture's low-level hook "
                    + "before the queue had it; `arrived` is what the window's thread pulled off the "
                    + "queue as WM_CHAR. Where `injected` ends in the sent string and `arrived` does "
                    + "not, the substitution was made between them and not by the send. `gaps` is "
                    + "milliseconds between arrivals, and each row is the tail of the record: the "
                    + "SECOND group is the resend the repair made and the FIRST is the send that "
                    + "went wrong. Two counts head `injected` — packets dequeued and injections "
                    + "seen — and a run where they disagree is a run whose hook missed part of it.");

            foreach (var one in arrivals)
                Console.WriteLine($"  {one}");
        }

        Console.WriteLine(Verdict(tally));
        return 0;
    }

    /// <summary>
    /// One round: type it, and record what the engine says it had to do to get it there.
    /// </summary>
    /// <param name="box">The text box under test.</param>
    /// <param name="typing">What this round sends, different each round so a box that kept the last round's text shows up.</param>
    /// <param name="tally">Where the outcome is counted.</param>
    /// <param name="took">How long the whole act took, for the drift reading beside the counts.</param>
    /// <returns>What the engine reported about the act.</returns>
    private static TypedResult Round(Subject box, string typing, Tally tally, out TimeSpan took)
    {
        var clock = Stopwatch.StartNew();
        var typed = Keyboard.Type(box, typing);
        clock.Stop();

        took = clock.Elapsed;
        tally.Saw(typed, took);
        return typed;
    }

    /// <summary>
    /// What the window's own thread saw, as the fixture recorded it. WW312.
    /// <para>
    /// Read through the automation name rather than the text, for WW238's reason: a caption's words
    /// are in its name and in no pattern. Answered as a sentence where the caption could not be read
    /// at all, because a row of nothing and a caption that was not there are different facts.
    /// </para>
    /// </summary>
    /// <param name="waited">The caption the gaps are written to.</param>
    private static string Gaps(Subject waited) => Says(waited, Tail);

    /// <summary>
    /// The tail of one of the fixture's read-outs, as its automation name carries it. WW312.
    /// </summary>
    /// <param name="caption">The caption to read.</param>
    /// <param name="most">How many space-separated values of the tail to keep.</param>
    private static string Says(Subject caption, int most)
    {
        var read = caption.Read();
        if (!read.Found)
            return "<no caption to read>";

        if (read.Facts?.Says is not { } said)
            return "<the caption says nothing>";

        // The tail and never the whole record, which is the correction the first reading forced.
        // The caption holds the last four hundred code units — about twenty rounds — and printing
        // all of it put the round this fault happened on somewhere in the middle of a wall of
        // numbers, with nothing saying which part was which.
        //
        // Two sends are what a reader needs and the order is the point: this is read after
        // Keyboard.Type returns, so the LAST group is the resend the repair made and the one before
        // it is the send that went wrong. Reading the last group as the faulty one turns the
        // repair's own latency into a finding about the fault.
        var all = said.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return all.Length <= most ? said : string.Join(' ', all[^most..]);
    }

    /// <summary>The last <paramref name="most"/> code units of a read-out that carries no separator.</summary>
    /// <param name="caption">The caption to read.</param>
    /// <param name="most">How many code units of the tail to keep.</param>
    private static string Tailed(Subject caption, int most)
    {
        var read = caption.Read();
        if (read.Facts?.Says is not { } said)
            return read.Found ? "<the caption says nothing>" : "<no caption to read>";

        return said.Length <= most ? said : said[^most..];
    }

    /// <summary>
    /// The code units <c>SendInput</c> was given, as the fixture's hook read them. WW312, and the
    /// whole point of the pairing: this beside the characters that arrived says whether a
    /// substitution was already in what was injected, or was made of a correct injection by the
    /// translation.
    /// <para>
    /// The counts are kept and only the units are shortened. They are taken at opposite ends of the
    /// same path — one counted as the hook saw the injection, one as the window's thread dequeued the
    /// packet — so a run where they disagree is a run whose instrument missed part of what it was
    /// reading, and the tail of the units would say nothing about a round it did not see.
    /// </para>
    /// </summary>
    /// <param name="caption">The caption the injected units are written to.</param>
    /// <param name="most">How many code units of the tail to keep.</param>
    private static string Injected(Subject caption, int most)
    {
        var read = caption.Read();
        if (read.Facts?.Says is not { } said)
            return read.Found ? "<the caption says nothing>" : "<no caption to read>";

        var at = said.LastIndexOf(": ", StringComparison.Ordinal);
        if (at < 0)
            return said;

        var counts = said[..(at + 2)];
        var units = said[(at + 2)..];
        return units.Length <= most ? said : counts + units[^most..];
    }

    /// <summary>
    /// How many gaps are printed for a faulted round: two sends of a nine-character round and the
    /// pauses before each. WW312.
    /// </summary>
    private const int Tail = 20;

    /// <summary>How many code units of the character and packet records are printed: two rounds.</summary>
    private const int TwoRounds = 22;

    /// <summary>
    /// WW313. Whether the rate held still while the run ran, said in quarters.
    /// <para>
    /// The reason it is here rather than in a footnote: the fault's rate is a property of the desk at
    /// a moment, not of the engine, and a run whose quarters disagree cannot have its total compared
    /// against another run's. A repair read as "2% before, 0.4% after" across two evenings that
    /// differed by that much internally would be reading the evening.
    /// </para>
    /// </summary>
    /// <param name="faulted">Whether the repair fired each round, in the order they ran.</param>
    /// <param name="spent">Each round's elapsed time, in that same order.</param>
    private static string Drift(IReadOnlyList<bool> faulted, IReadOnlyList<double> spent)
    {
        const int Quarters = 4;
        if (faulted.Count < Quarters * 2)
            return "";

        var said = new List<string>();
        var size = faulted.Count / Quarters;
        for (var quarter = 0; quarter < Quarters; quarter++)
        {
            // The last quarter takes the remainder, so no round is left out of the reading — a
            // division that dropped three rounds would be a drift measurement with a hole in it.
            var from = quarter * size;
            var upto = quarter == Quarters - 1 ? faulted.Count : from + size;

            var hit = faulted.Skip(from).Take(upto - from).Count(one => one);
            var took = spent.Skip(from).Take(upto - from).DefaultIfEmpty(0).Average();
            said.Add($"{from + 1}-{upto}: {hit} faulted, {took:F0}ms a round");
        }

        return $"WW313: by quarter — {string.Join("; ", said)}. The time is beside the count because"
            + " the fixture appends every value it is sent to a read-out under the box, so a rate that"
            + " climbs while the round slows is a window getting heavier rather than a phenomenon"
            + " getting likelier.\n";
    }

    /// <summary>
    /// What the run says, in the one sentence somebody reads.
    /// <para>
    /// Stated as what was counted, and explicit about which way a zero reads. At four hundred rounds
    /// a survivor is not expected, so finding none is the run failing to refute the repair — and a
    /// tool that called that a confirmation would call it one on every run it ever made.
    /// </para>
    /// </summary>
    private static string Verdict(Tally tally)
    {
        if (tally.Faulted == 0)
        {
            return "The fault never appeared, so this run says nothing about the repair: it exercised"
                + " a path that had nothing to repair. WW249 is rare, and a run that saw none of it is"
                + " a run with nothing to say rather than an acquittal.";
        }

        return tally.Survived == 0
            ? $"The fault appeared {tally.Faulted} times and was repaired every time. That refutes"
                + " nothing on its own — three resends put a survivor past one act in a million, which"
                + " is far beyond what this many rounds could see — so what it shows is the repair"
                + " firing on the real fault and the reading landing right afterwards."
            : $"{tally.Survived} of {tally.Faulted} substitutions outlived {tally.Resends} resends,"
                + " which at this fault's measured rate should not happen in a run this length. Either"
                + " the desk was doing something else or the fault has a second shape.";
    }

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
/// What the run counted, and what the survivors looked like.
/// <para>
/// The examples are kept for the survivors alone. A repaired round is one the engine already
/// classified — it repeated the send because the reading matched WW249's shape — and printing those
/// would be printing the rule back. A survivor is the round that shape did not explain, and there is
/// nothing else in this run worth a person's eyes.
/// </para>
/// </summary>
public sealed class Tally
{
    private readonly List<string> examples = [];
    private TimeSpan spent;

    /// <summary>How many rounds ran.</summary>
    public int Rounds { get; private set; }

    /// <summary>How many of them hit the substitution at least once — the fault's own rate.</summary>
    public int Faulted { get; private set; }

    /// <summary>How many sends were repeated in total, across every round that needed one.</summary>
    public int Resends { get; private set; }

    /// <summary>How many rounds read back wrong even so.</summary>
    public int Survived { get; private set; }

    /// <summary>What one round's whole act took, averaged.</summary>
    public double MeanMs => Rounds == 0 ? 0 : spent.TotalMilliseconds / Rounds;

    /// <summary>Record one round.</summary>
    /// <param name="typed">What the engine reported about the act.</param>
    /// <param name="took">How long the act took.</param>
    public void Saw(TypedResult typed, TimeSpan took)
    {
        ArgumentNullException.ThrowIfNull(typed);

        Rounds++;
        spent += took;

        if (typed.Resends > 0)
        {
            Faulted++;
            Resends += typed.Resends;
        }

        if (typed.Arrived)
            return;

        Survived++;

        // Bounded, because a desk that went away would otherwise print a page a round and bury the
        // counts the run is for.
        if (examples.Count < 20)
            examples.Add($"sent {typed.Expected()}, read {typed.ReadBack ?? "<nothing>"}");
    }

    /// <summary>The run's lines in the report.</summary>
    public override string ToString()
    {
        var rate = Rounds == 0 ? "" : $" ({(double)Faulted / Rounds:P1})";
        var took = Rounds == 0 ? "" : $", {MeanMs:F1}ms a round";

        var repaired = Faulted == 0
            ? ""
            : $"\n  repaired by {Resends} resend(s) in total, {(double)Resends / Faulted:F2} a fault";

        var listed = examples.Count == 0 ? "" : $"\n    {string.Join("\n    ", examples)}";
        return $"  the send substituted a code unit in {Faulted} of {Rounds} rounds{rate}{took}"
            + $"{repaired}\n  {Survived} of them read back wrong anyway{listed}";
    }
}
