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
/// <para>
/// Three arms, and the third is what the first two could not say. Both of them erased the box in an
/// act of the engine's own before the spaced send, so the queue was empty when it started — and both
/// then faulted nowhere at any spacing, which is a null about two differences at once. The engine
/// does not do that: it sends the backspaces and the text back to back, so the second batch is
/// injected while the first is still being translated. That is the shape WW310 read the band in.
/// </para>
/// <para>
/// <b>What it answered, on the guest, 150 rounds in each of eighteen cells (2026-09-01).</b> Two
/// substitutions in 2700 rounds, and both at no spacing at all — one on the watched arm and one on
/// the whole arm, neither with a dirty injection. Every one of the fifteen spaced cells read zero.
/// </para>
/// <para>
/// So the whole arm is not a null about two differences: it reproduces WW249 at the same rate the
/// other arms do, which is the engine's own, and then it is silenced by 32ms exactly as they are.
/// <b>The band does not reproduce.</b> WW310 read 7.2, 7.6 and 9.6% across 48 to 64ms; those three
/// cells here are 450 rounds that would have carried about 35 substitutions between them, and they
/// carried none. Nothing this sweep can vary produces a rate with a shape.
/// </para>
/// <para>
/// The other half of that reading is the one worth acting on and it is not this task's to act on: a
/// spacing of 32ms suppressed the fault across 750 rounds of the engine's own shape, and 32ms is a
/// quarter of the 128ms WW304 priced and refused. Filed rather than taken, because a suppression
/// measured against a 0.7% baseline is about five events short of being a measurement.
/// </para>
/// </summary>
internal static class Sweep
{
    /// <summary>
    /// The spacings WW310 read the curve at, and zero, which is the control the first version of
    /// this did not have.
    /// <para>
    /// WW310 read 48 to 64ms as five to nine times worse than 32 and than 96, so the band is a
    /// bracket rather than a slope, and the two shoulders are as much of the reading as the middle.
    /// Swept over the quiet and watched arms alone they answered zero substitutions in 450 rounds,
    /// which is not a reading about the band: those arms differ from the engine's path in more than
    /// the interval, so a null says only that the differences together do not fault.
    /// </para>
    /// <para>
    /// Zero separates them. One call per code unit with no interval is WW302's arm, which measured 11
    /// in 400, so a null here cannot be the call count and cannot be the interval: what would be left
    /// is the reader those arms removed. An experiment whose every arm can come back null is one that
    /// cannot say which of its differences did it.
    /// </para>
    /// </summary>
    private static readonly int[] Spacings = [0, 32, 48, 64, 80, 96];

    /// <summary>
    /// What differs between one round and the next, which is the whole of what this sweep varies
    /// besides the interval.
    /// </summary>
    private enum Arm
    {
        /// <summary>Nothing reads the window between the last code unit and the record.</summary>
        Quiet,

        /// <summary>The box is read on the engine's own poll interval while the queue drains.</summary>
        Watched,

        /// <summary>
        /// WW310's shape, which is the engine's: the erase and the send are one act, so the
        /// backspaces are still being translated when the first code unit of the text goes in.
        /// </summary>
        Whole,
    }

    /// <summary>
    /// The arms, in the order they run at each spacing. WW312, and the third one is the task.
    /// <para>
    /// The first two are the sweep as it stood, kept rather than replaced: they are what said the
    /// reader provokes the fault — 600 quiet rounds faulted nowhere and the watched arm at no
    /// spacing faulted 3 of 150, which is the engine's own rate — and a new arm with nothing beside
    /// it would be a second null with nothing to be null against.
    /// </para>
    /// <para>
    /// What the third adds is the one difference nobody had removed on purpose. Both of the others
    /// erase in an act of their own, so the queue is empty before the spaced send starts. The engine
    /// does not: <c>Erase</c> puts two backspace events per character into one call and <c>Send</c>
    /// follows immediately, so the text is injected while the erase is still draining. That is the
    /// shape WW310 swept the band in, and it is what these two took out without saying so.
    /// </para>
    /// </summary>
    private static readonly Arm[] Arms = [Arm.Quiet, Arm.Watched, Arm.Whole];

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
    /// The engine's own wait, which is what the whole arm needs beside the engine's own send.
    /// <para>
    /// <c>Settled</c> polls the control on this interval and stops the moment the reading is what
    /// was expected, so a clean round reads two or three times and a faulted one reads until the
    /// deadline. That asymmetry is the engine's and is kept: an arm that read for a fixed 300ms
    /// would differ from the path it is meant to resemble in the one dimension this sweep is about.
    /// </para>
    /// <para>
    /// The engine's second exit — the reading carries WW249's signature — is deliberately not here.
    /// It exists so <c>Run</c> can resend, and this arm resends nothing: what is being counted is
    /// the raw rate, which is the number the repair hides.
    /// </para>
    /// </summary>
    /// <param name="box">The box being typed into.</param>
    /// <param name="expected">What it should read once the queue has drained.</param>
    internal static void Drain(Subject box, string expected)
    {
        var until = Stopwatch.StartNew();
        while (until.ElapsedMilliseconds < SettleMs)
        {
            var values = box.ReadOnce().Values;
            if (string.Equals(values.Value ?? values.Text, expected, StringComparison.Ordinal))
                return;

            Thread.Sleep(PollMs);
        }
    }

    /// <summary>
    /// Run the sweep and print what each spacing did.
    /// </summary>
    /// <param name="run">The fixture this arm measures, and how many rounds it was asked for.</param>
    public static void Run(TypingRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var (box, arrived, packets, rounds) = (run.Box, run.Arrived, run.Injected, run.Rounds);

        Console.WriteLine(
            $"WW312: the band at both ends, {rounds} round(s) on each of {Arms.Length} arms at each of"
                + $" {string.Join("ms, ", Spacings)}ms. Every round sends one SendInput per code unit"
                + " that far apart. `quiet` erases in an act of its own and reads nothing until the"
                + " queue has drained; `watched` erases the same way and reads the box on the engine's"
                + " poll interval while it drains; `whole` is the engine's shape — the erase and the"
                + " send are one act, so the backspaces are still being translated when the text goes"
                + " in. `substituted` is what the window received differing from what was sent;"
                + " `dirty` is how many of those had an injection that already differed — which is"
                + " where the fault would be the send's.");

        var faults = new Dictionary<(Arm Arm, int Spacing), int>();
        foreach (var spacing in Spacings)
        {
            foreach (var arm in Arms)
                faults[(arm, spacing)] = Measure(box, arrived, packets, rounds, arm, spacing);
        }

        Console.WriteLine(Verdict(faults));
    }

    /// <summary>
    /// Run one arm at one spacing, print its row, and answer how many rounds were substituted.
    /// </summary>
    /// <param name="box">The text box under test.</param>
    /// <param name="arrived">The caption the arriving characters are written to.</param>
    /// <param name="packets">The caption the injected code units are written to.</param>
    /// <param name="rounds">How many rounds to type.</param>
    /// <param name="arm">Which shape of round.</param>
    /// <param name="spacing">How far apart the code units go.</param>
    private static int Measure(
        Subject box, Subject arrived, Subject packets, int rounds, Arm arm, int spacing)
    {
        var substituted = 0;
        var dirty = 0;
        var unread = 0;
        var examples = new List<string>();

        // The whole arm erases what the round before it left, which is the engine's own act and the
        // reason the focus is taken once here rather than per round: the engine takes it inside the
        // act, and an act of its own between rounds is exactly the drained queue this arm exists to
        // stop arranging. Nothing else touches the desk while a run is going, so it stays taken.
        Keyboard.Type(box, "");
        var standing = 0;

        var clock = Stopwatch.StartNew();
        for (var round = 1; round <= rounds; round++)
        {
            var typing = $"WW249-{round}";

            if (arm == Arm.Whole)
            {
                Spaced.Clear(standing);
                standing = typing.Length;
                Spaced.Send(typing, spacing);
                Drain(box, typing);
            }
            else
            {
                // The engine, for the two things it is for here. Typing nothing erases what is there
                // and leaves the focus where the spaced send needs it, and both happen before the
                // send rather than during it — which is the difference the whole arm removes.
                Keyboard.Type(box, "");
                Spaced.Send(typing, spacing);
                Settle(box, arm == Arm.Watched);
            }

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
            if (!string.Equals(sent, typing, StringComparison.Ordinal))
                dirty++;

            if (examples.Count < 4)
                examples.Add($"sent {typing}, injected {sent}, arrived {got}");
        }

        clock.Stop();

        var rate = rounds == 0 ? 0 : (double)substituted / rounds;
        Console.WriteLine(
            $"  {spacing,3}ms {arm.ToString().ToLowerInvariant(),-7}  {substituted,3} substituted of {rounds} ({rate:P1}),"
                + $" {dirty} with a dirty injection, {unread} unread,"
                + $" {clock.Elapsed.TotalSeconds:F0}s");

        foreach (var one in examples)
            Console.WriteLine($"           {one}");

        return substituted;
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
    /// <param name="faults">How many rounds substituted, by arm and spacing.</param>
    private static string Verdict(IReadOnlyDictionary<(Arm Arm, int Spacing), int> faults)
    {
        if (faults.Values.All(one => one == 0))
        {
            return "No arm faulted anywhere, so this run says nothing at all: an arm that never"
                + " faults has no rate to have a shape, and three of them that never fault do not"
                + " tell each other apart either. It is a reading about the desk and not about WW249.";
        }

        var said = new List<string> { Ranked(faults) };

        // The band first, because it is the question. Read on the whole arm alone: the other two are
        // here to say what the whole arm added, and a band read across all three would be a shape
        // taken over rounds that differ in something other than the interval.
        var whole = faults.Where(one => one.Key.Arm == Arm.Whole).ToDictionary(one => one.Key.Spacing, one => one.Value);
        var control = whole.GetValueOrDefault(0);
        var band = whole.Where(one => one.Key is >= 48 and <= 64).Sum(one => one.Value);
        var shoulders = whole.Where(one => one.Key is 32 or 80 or 96).Sum(one => one.Value);

        if (whole.Values.All(one => one == 0))
        {
            said.Add("The whole arm — the engine's own shape, erasing and sending in one act —"
                + " faulted nowhere, so WW310's band does not survive being reconstructed this way."
                + " Either the shape is not what WW310 swept, or the fault has moved since.");
        }
        else if (band > shoulders * 2)
        {
            said.Add($"The band survives the reconstruction: {band} substitution(s) across 48-64ms"
                + $" against {shoulders} across 32, 80 and 96 together. What the whole arm has and"
                + " the other two do not is an erase still draining when the text is injected, so"
                + " that is where the band lives — two batches in the queue at once, and the"
                + " interval deciding how far into the first the second lands.");
        }
        else
        {
            said.Add($"The whole arm faults ({whole.Values.Sum()} in total) and does not band:"
                + $" {band} across 48-64ms against {shoulders} across 32, 80 and 96. So the engine's"
                + " shape reproduces WW249 and the spacing is not what shapes its rate here, which"
                + " leaves WW310's curve unexplained by anything this sweep varies.");
        }

        if (control > 0 && band == 0 && shoulders == 0)
            said.Add("Every fault on that arm is at no spacing at all, which is WW302's arm again.");

        var quiet = faults.Where(one => one.Key.Arm == Arm.Quiet).Sum(one => one.Value);
        var watched = faults.Where(one => one.Key.Arm == Arm.Watched).Sum(one => one.Value);
        said.Add($"Beside it the quiet arms total {quiet} and the watched ones {watched}, which is the"
            + " earlier reading either confirmed or withdrawn: those two erase in an act of their own,"
            + " so their queue is empty before the spaced send starts.");

        return string.Join(Environment.NewLine, said);
    }

    /// <summary>Every cell that faulted, worst first, so the shape is readable without the table.</summary>
    /// <param name="faults">How many rounds substituted, by arm and spacing.</param>
    private static string Ranked(IReadOnlyDictionary<(Arm Arm, int Spacing), int> faults)
    {
        var worst = faults.Where(one => one.Value > 0)
            .OrderByDescending(one => one.Value)
            .Select(one => $"{one.Key.Arm.ToString().ToLowerInvariant()} at {one.Key.Spacing}ms: {one.Value}");

        return $"Faulted: {string.Join(", ", worst)}.";
    }

    /// <summary>
    /// The last <paramref name="most"/> code units a caption carries, or null where it could not be
    /// read at all — which is a different fact from a caption that read short.
    /// </summary>
    /// <param name="caption">The caption to read.</param>
    /// <param name="most">How many code units of the tail are wanted.</param>
    internal static string? Tail(Subject caption, int most)
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
        ArgumentNullException.ThrowIfNull(text);

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

    /// <summary>
    /// One <c>SendInput</c> carrying every code unit, which is the engine's own send. WW329.
    /// <para>
    /// <see cref="Send"/> with no spacing is one call per code unit and measured the same rate —
    /// WW302 read 11 in 400 against 14 batched — but the same rate is not the same shape, and the
    /// question WW329 asks is about the drain a batch produces. So the arm that sweeps the pause
    /// before the first read sends what the engine sends rather than something measured to match it.
    /// </para>
    /// </summary>
    /// <param name="text">What to send.</param>
    public static void Batch(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var inputs = new List<Win32.Input>(text.Length * 2);
        foreach (var character in text)
        {
            inputs.Add(Typed(character, 0));
            inputs.Add(Typed(character, KeyUp));
        }

        if (inputs.Count == 0)
            return;

        var array = inputs.ToArray();
        SendInput((uint)array.Length, array, Marshal.SizeOf<Win32.Input>());
    }

    /// <summary>
    /// Put the caret after what is there and erase it, in one call. WW312, and the whole of what the
    /// third arm adds.
    /// <para>
    /// This is <c>Keyboard.Erase</c> with <c>MoveToTheEnd</c> in front of it, and it is here for the
    /// reason the send is: the engine's copy is internal, and what matters is not the keystrokes but
    /// that nothing waits between them and the text. The engine sends this array and calls
    /// <c>Send</c> on the next line, so the backspaces are still being translated when the first code
    /// unit of the text is injected — which is the one thing both other arms take out by erasing in
    /// an act of their own.
    /// </para>
    /// <para>
    /// The scan code is not optional and not a nicety: a virtual key sent with a scan code of zero
    /// does nothing at all, measured — End did not move the caret and Backspace erased nothing, so
    /// text meant to replace was inserted in front of what was there.
    /// </para>
    /// </summary>
    /// <param name="characters">How many characters are standing in the box.</param>
    public static void Clear(int characters)
    {
        var inputs = new List<Win32.Input>((characters + 1) * 2)
        {
            Pressed(VkEnd, 0),
            Pressed(VkEnd, KeyUp),
        };

        for (var each = 0; each < characters; each++)
        {
            inputs.Add(Pressed(VkBack, 0));
            inputs.Add(Pressed(VkBack, KeyUp));
        }

        var array = inputs.ToArray();
        SendInput((uint)array.Length, array, Marshal.SizeOf<Win32.Input>());
    }

    /// <summary>
    /// The caret to the end, in a call of its own. WW368.
    /// <para>
    /// <see cref="Clear"/> sends End and the backspaces in one <c>SendInput</c>; the engine sends
    /// them in two, because <c>MoveToTheEnd</c> and <c>Erase</c> are separate calls a line apart.
    /// So a round of the engine's is three calls into the queue and a round of the arms' is two,
    /// and no arm had ever separated the two shapes — which is one of the things that could be
    /// carrying the rate the arms read as zero and the act still reads.
    /// </para>
    /// </summary>
    public static void End()
    {
        Win32.Input[] pair = [Pressed(VkEnd, 0), Pressed(VkEnd, KeyUp)];
        SendInput((uint)pair.Length, pair, Marshal.SizeOf<Win32.Input>());
    }

    /// <summary>
    /// The backspaces alone, in a call of their own. WW368, and the other half of the split.
    /// </summary>
    /// <param name="characters">How many characters are standing in the box.</param>
    public static void Erase(int characters)
    {
        if (characters <= 0)
            return;

        var inputs = new List<Win32.Input>(characters * 2);
        for (var each = 0; each < characters; each++)
        {
            inputs.Add(Pressed(VkBack, 0));
            inputs.Add(Pressed(VkBack, KeyUp));
        }

        var array = inputs.ToArray();
        SendInput((uint)array.Length, array, Marshal.SizeOf<Win32.Input>());
    }

    private const uint KeyUp = 0x0002;
    private const uint KeyUnicode = 0x0004;
    private const uint InputKeyboard = 1;
    private const ushort VkBack = 0x08;
    private const ushort VkEnd = 0x23;

    /// <summary>MAPVK_VK_TO_VSC: the scan code a virtual key has on the layout in force.</summary>
    private const uint VirtualKeyToScan = 0;

    /// <summary>One virtual key, carrying the scan code the layout in force gives it.</summary>
    /// <param name="virtualKey">The key.</param>
    /// <param name="flags">What else the event carries — nothing, or the key going up.</param>
    private static Win32.Input Pressed(ushort virtualKey, uint flags) => new()
    {
        Type = InputKeyboard,
        Payload = new Win32.InputPayload
        {
            Key = new Win32.KeyInput
            {
                VirtualKey = virtualKey,
                Scan = (ushort)MapVirtualKeyW(virtualKey, VirtualKeyToScan),
                Flags = flags,
            },
        },
    };

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKeyW(uint code, uint mapping);

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
