using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Winwright.Acting;
using Winwright.Locating;

namespace Winwright.Typing;

/// <summary>
/// WW312. The band, read at both ends: is the injection still clean where the fault is five times
/// likelier?
/// <para>
/// The pairing that put the substitution after <c>SendInput</c> was taken at the engine's own send
/// shape — one call for the whole string — and the band is a property of a shape the engine never
/// uses, one call per code unit some milliseconds apart. So the finding does not carry across on its
/// own: what is measured innocent at one spacing is not measured at all at another. This is the same
/// pairing at the spacings the band is made of.
/// </para>
/// <para>
/// The send is here rather than in the engine, and that is the same refusal WW304 already made: a
/// per-code-unit spacing is an experimental arm and not what this tool drives. The engine is used
/// for what it is for — taking the focus and clearing the box — and the arm's own send is fifty
/// lines of interop that live where the experiment lives.
/// </para>
/// <para>
/// Nothing is read while the send is in flight, which is the one thing this measurement cannot do.
/// A caption read is a cross-process call against the window under test, and taking one between two
/// spaced code units would put the reader inside the interval being swept — the confound WW316 is
/// named for, arriving here through the door the design had already priced out for the batched send.
/// So a round sends, sleeps once, and reads afterwards.
/// </para>
/// </summary>
internal static class Sweep
{
    /// <summary>
    /// The spacings, which are the three the curve is quoted at — and zero, which is the control the
    /// first version of this did not have.
    /// <para>
    /// WW310 read 64ms as five to nine times worse than 32 and than 96, so those three are what make
    /// the band a bracket rather than a slope. Swept alone they answered zero substitutions in 450
    /// rounds, which is not a reading about the band: this arm differs from the engine's path in two
    /// ways at once — the interval, and a read-back this deliberately does not overlap the drain with
    /// — so a null says only that the pair of them together does not fault.
    /// </para>
    /// <para>
    /// Zero separates them. One call per code unit with no interval is WW302's arm, which measured 11
    /// in 400, so a null here cannot be the call count and cannot be the interval: what would be left
    /// is the reader this arm removed. An experiment whose every arm can come back null is one that
    /// cannot say which of its differences did it.
    /// </para>
    /// </summary>
    private static readonly int[] Spacings = [0, 32, 64, 96];

    /// <summary>
    /// How long a round waits for the queue after its last code unit, before anything is read.
    /// <para>
    /// Generous on purpose and not tuned: the arrival record is what this reads, and a settle too
    /// short would read a round the window had not finished delivering and call the missing tail a
    /// substitution. WW312 measured the drain at 2 to 5ms a character once it starts, so this is two
    /// orders above what a nine-character round needs.
    /// </para>
    /// </summary>
    private const int SettleMs = 300;

    /// <summary>
    /// How often the watched arm reads the box while the queue drains, which is the engine's own
    /// poll interval — the arm is meant to resemble <c>Settled</c> and not to sweep a second number.
    /// </summary>
    private const int PollMs = 25;

    /// <summary>
    /// Wait out the drain, either watching it or not. WW312, and this is the difference the sweep
    /// exists to measure.
    /// <para>
    /// The quiet arm sleeps: nothing reads the window between the last code unit and the record. The
    /// watched arm reads the box on the engine's own interval, which is what <c>Settled</c> does the
    /// instant <c>Send</c> returns — and <c>SendInput</c> returns once the events are queued rather
    /// than processed, so those reads land while the packets are still being translated.
    /// </para>
    /// <para>
    /// Both wait the same wall time, which is what makes them comparable. A watched arm that also ran
    /// longer would differ in two ways again, which is the flaw the first version of this sweep had
    /// and the reason it could say nothing.
    /// </para>
    /// </summary>
    /// <param name="box">The box the watched arm reads.</param>
    /// <param name="watched">Whether anything reads the window while the queue drains.</param>
    private static void Settle(Subject box, bool watched)
    {
        if (!watched)
        {
            Thread.Sleep(SettleMs);
            return;
        }

        var until = Stopwatch.StartNew();
        while (until.ElapsedMilliseconds < SettleMs)
        {
            // The value and never the arrival record: this is meant to be the read the engine takes,
            // which is a cross-process call against the control under test. Reading the caption here
            // instead would be a different window's property and a different amount of work.
            box.ReadOnce();
            Thread.Sleep(PollMs);
        }
    }

    /// <summary>
    /// Run the sweep and print what each spacing did.
    /// </summary>
    /// <param name="box">The text box under test.</param>
    /// <param name="arrived">The caption the arriving characters are written to.</param>
    /// <param name="packets">The caption the injected code units are written to.</param>
    /// <param name="rounds">How many rounds each spacing types.</param>
    public static void Run(Subject box, Subject arrived, Subject packets, int rounds)
    {
        Console.WriteLine(
            $"WW312: the band at both ends, {rounds} round(s) at each of {string.Join("ms, ", Spacings)}ms."
                + " Each round takes the focus through the engine, sends one SendInput per code unit"
                + " that far apart, waits once and only then reads. `substituted` is what the window"
                + " received differing from what was sent; `dirty` is how many of those had an"
                + " injection that already differed — which is where the fault would be the send's.");

        var faults = new Dictionary<int, int>();
        foreach (var (spacing, watched) in Spacings.SelectMany(one => new[] { (one, false), (one, true) }))
        {
            var substituted = 0;
            var dirty = 0;
            var unread = 0;
            var examples = new List<string>();

            var clock = Stopwatch.StartNew();
            for (var round = 1; round <= rounds; round++)
            {
                var typing = $"WW249-{round}";

                // The engine, for the two things it is for here. Typing nothing erases what is there
                // and leaves the focus where the spaced send needs it, and both happen before the
                // send rather than during it.
                Keyboard.Type(box, "");

                Spaced.Send(typing, spacing);
                Settle(box, watched);

                var got = Tail(arrived, typing.Length);
                var sent = Tail(packets, typing.Length);

                if (got is null || sent is null)
                {
                    unread++;
                    continue;
                }

                if (string.Equals(got, typing, StringComparison.Ordinal))
                    continue;

                substituted++;
                var clean = string.Equals(sent, typing, StringComparison.Ordinal);
                if (!clean)
                    dirty++;

                if (examples.Count < 6)
                    examples.Add($"sent {typing}, injected {sent}, arrived {got}");
            }

            clock.Stop();

            // The watched arm is the one the engine's own path resembles, so it is the one the
            // verdict reads. The quiet arm is kept beside it as the difference being measured.
            if (watched)
                faults[spacing] = substituted;

            var rate = rounds == 0 ? 0 : (double)substituted / rounds;
            Console.WriteLine(
                $"  {spacing,3}ms {(watched ? "watched" : "quiet  ")}  {substituted,3} substituted of {rounds} ({rate:P1}),"
                    + $" {dirty} with a dirty injection, {unread} unread,"
                    + $" {clock.Elapsed.TotalSeconds:F0}s");

            foreach (var one in examples)
                Console.WriteLine($"           {one}");
        }

        Console.WriteLine(Verdict(faults));
    }

    /// <summary>
    /// What the sweep says, which depends first on whether it saw the fault at all.
    /// <para>
    /// Written as three outcomes because the first version had one, and it was the wrong one: it read
    /// "no dirty injection" as evidence that the fault is in the translation, which is a sentence
    /// about the injections of substituted rounds. With no substituted rounds there are no such
    /// injections, and the run printed a conclusion off an empty set.
    /// </para>
    /// </summary>
    /// <param name="faults">How many rounds substituted at each spacing, in the swept order.</param>
    private static string Verdict(IReadOnlyDictionary<int, int> faults)
    {
        if (faults.Values.All(one => one == 0))
        {
            return "No watched arm faulted either, so the reader is not what the quiet run left out"
                + " and this send shape does not reproduce WW249 for some third reason. Nothing here"
                + " is a reading about the band: an arm that never faults has no rate to have a shape.";
        }

        var quietWasNull = "The quiet arms faulted nowhere and the watched ones do, which puts WW249"
            + " behind something reading the window while its queue drains — the engine's own"
            + " Settled polls on this interval the instant Send returns, and SendInput returns when"
            + " the events are queued rather than processed.";

        return faults.TryGetValue(0, out var control) && control > 0 && faults.Any(one => one.Key > 0 && one.Value > control * 2)
            ? quietWasNull + " And the rate is not flat across the spacings, which is the band with a"
                + " mechanism under it for the first time: a reader on a fixed interval against a send"
                + " on another is two cadences, and two cadences beat."
            : quietWasNull + " The spacings do not separate here, so the band is not read by this run"
                + " even though the reader is.";
    }

    /// <summary>
    /// The last <paramref name="most"/> code units a caption carries, or null where it could not be
    /// read at all — which is a different fact from a caption that read short.
    /// </summary>
    /// <param name="caption">The caption to read.</param>
    /// <param name="most">How many code units of the tail are wanted.</param>
    private static string? Tail(Subject caption, int most)
    {
        var read = caption.Read();
        if (read.Facts?.Says is not { } said)
            return null;

        // The packet caption heads its units with two counts, which are not units. Cut at the last
        // separator rather than at a fixed offset: the counts grow by a digit as a run goes on, and
        // an offset that was right at round 9 would be reading a digit at round 10.
        var at = said.LastIndexOf(": ", StringComparison.Ordinal);
        if (at >= 0)
            said = said[(at + 2)..];

        return said.Length < most ? null : said[^most..];
    }
}

/// <summary>
/// One <c>SendInput</c> per UTF-16 code unit, a named interval apart. WW312.
/// <para>
/// Its own interop and not the engine's, which is deliberate twice over. The engine's is internal,
/// and making it visible for an experiment would be the arm arriving in the engine by the back door
/// — the thing WW304 refused when it declined to keep a spacing knob there at all.
/// </para>
/// </summary>
internal static class Spaced
{
    /// <summary>
    /// Send one string, one call per code unit.
    /// </summary>
    /// <param name="text">What to send.</param>
    /// <param name="spacingMs">How long to wait between one code unit and the next.</param>
    public static void Send(string text, int spacingMs)
    {
        for (var at = 0; at < text.Length; at++)
        {
            Win32.Input[] pair = [Typed(text[at], 0), Typed(text[at], KeyUp)];
            SendInput((uint)pair.Length, pair, Marshal.SizeOf<Win32.Input>());

            // After each code unit including the last, because the interval is what is being swept
            // and a final one skipped would make the last character of every round the one nothing
            // followed. The settle after this is what the round actually waits on.
            if (spacingMs > 0)
                Thread.Sleep(spacingMs);
        }
    }

    private const uint KeyUp = 0x0002;
    private const uint KeyUnicode = 0x0004;
    private const uint InputKeyboard = 1;

    /// <summary>
    /// One code unit as Unicode rather than as a virtual key, which is the shape WW249 is about: the
    /// engine sends exactly this, and only the number of calls and the interval differ here.
    /// </summary>
    /// <param name="character">The code unit to send.</param>
    /// <param name="flags">What else the event carries — nothing, or the key going up.</param>
    private static Win32.Input Typed(char character, uint flags) => new()
    {
        Type = InputKeyboard,
        Payload = new Win32.InputPayload
        {
            Key = new Win32.KeyInput { Scan = character, Flags = KeyUnicode | flags },
        },
    };

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint many, Win32.Input[] inputs, int size);

    /// <summary>
    /// The layout <c>SendInput</c> reads, spelled here because the engine's copy is internal to it.
    /// The union is written out explicitly so its size is right on x64, which is the one thing about
    /// this struct that is easy to get wrong and silent when it is.
    /// </summary>
    internal static class Win32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct Input
        {
            public uint Type;
            public InputPayload Payload;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct InputPayload
        {
            [FieldOffset(0)]
            public KeyInput Key;

            /// <summary>
            /// What makes the union the size the mouse arm needs. Never written and never read — a
            /// payload sized to its keyboard arm alone is a struct <c>SendInput</c> rejects, and the
            /// rejection is a return of zero rather than anything that says why.
            /// </summary>
            [FieldOffset(0)]
            public MouseInput Mouse;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KeyInput
        {
            public ushort VirtualKey;
            public ushort Scan;
            public uint Flags;
            public uint Time;
            public nint Extra;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MouseInput
        {
            public int X;
            public int Y;
            public uint Data;
            public uint Flags;
            public uint Time;
            public nint Extra;
        }
    }
}
