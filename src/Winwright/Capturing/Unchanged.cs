using System.Windows.Media;
using System.Windows.Media.Imaging;

using Winwright.Tracing;
using Winwright.Verdicts;

namespace Winwright.Capturing;

/// <summary>What comparing two renders turned out to say.</summary>
public sealed record RenderMatch
{
    internal RenderMatch(
        string left,
        string right,
        long bytes,
        long differsAt,
        long pixelsDiffering,
        bool pixelsRead,
        string absence)
    {
        Left = left;
        Right = right;
        Bytes = bytes;
        DiffersAt = differsAt;
        PixelsDiffering = pixelsDiffering;
        PixelsRead = pixelsRead;
        Absence = absence;
    }

    /// <summary>What this reading is called wherever it is reported.</summary>
    public const string PreconditionName = "two renders of the same code can be compared";

    /// <summary>The render this was asked about.</summary>
    public string Left { get; }

    /// <summary>The one it was compared against.</summary>
    public string Right { get; }

    /// <summary>How long the first file is, in bytes.</summary>
    public long Bytes { get; }

    /// <summary>The first byte the two disagree on, or -1 where they never do.</summary>
    public long DiffersAt { get; }

    /// <summary>How many pixels draw differently, where the pictures were decoded.</summary>
    public long PixelsDiffering { get; }

    /// <summary>Whether the pictures could be decoded and compared as pictures.</summary>
    public bool PixelsRead { get; }

    /// <summary>Why nothing could be compared, where nothing could. Empty otherwise.</summary>
    public string Absence { get; }

    /// <summary>Whether the comparison happened at all.</summary>
    public bool Was => Absence.Length == 0;

    /// <summary>
    /// Whether the two files are the same bytes. This is the assertion: a change meant to be
    /// invisible must produce a byte-identical file, and no tolerance has to be chosen for that to
    /// mean something — which is the argument every other image comparison eventually turns into.
    /// </summary>
    public bool Identical => Was && DiffersAt < 0;

    /// <summary>
    /// Whether the two draw the same picture, where both could be decoded. Told apart from
    /// <see cref="Identical" /> on purpose: two files that differ and draw the same thing is an
    /// encoder writing something of its own, and a reader sent looking for a visual change would
    /// find none and conclude the check is broken.
    /// </summary>
    public bool SamePicture => Was && PixelsRead && PixelsDiffering == 0;

    /// <summary>What was compared, said whichever way it came out.</summary>
    public string Sentence()
    {
        if (!Was)
            return $"{Left} and {Right} could not be compared: {Absence}.";

        if (Identical)
            return $"{Left} and {Right} are byte-identical across all {Bytes} byte(s).";

        if (!PixelsRead)
            return $"{Left} and {Right} first differ at byte {DiffersAt}, and neither could be decoded to say "
                + "whether the picture changed with them.";

        return PixelsDiffering == 0
            ? $"{Left} and {Right} first differ at byte {DiffersAt} and draw the same picture, so the difference "
                + "is in the file rather than in what was drawn."
            : $"{Left} and {Right} first differ at byte {DiffersAt} and draw differently in "
                + $"{PixelsDiffering} pixel(s).";
    }

    /// <inheritdoc cref="Sentence" />
    public override string ToString() => Sentence();

    /// <summary>
    /// The result a verdict counts. A comparison that could not be made is a hole; anything else is
    /// the assertion itself, and a difference of any kind fails it — including one that draws the
    /// same picture, because the rule is about the file and an encoder that varies is a reason the
    /// rule stops working.
    /// </summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult AsAssertion(string named = "the render is unchanged")
    {
        if (!Was)
            return AssertionResult.Unchecked(named, Precondition.Absent(PreconditionName, Absence));

        return Identical ? AssertionResult.Pass(named, Sentence()) : AssertionResult.Fail(named, Sentence());
    }

    /// <summary>The step a trace records.</summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public TraceStep AsTraceStep(string named = "the render is unchanged") => new()
    {
        Verb = "compare two renders",
        Locator = named,
        Resolved = $"{Left} against {Right}",
        Pattern = "byte for byte",
        ReadBack = Was ? (Identical ? $"{Bytes} byte(s), identical" : $"first differ at {DiffersAt}") : null,
        Verdict = Verdict(),
        Detail = Identical ? null : Sentence(),
    };

    private StepVerdict Verdict()
    {
        if (!Was)
            return StepVerdict.Unchecked;

        return Identical ? StepVerdict.Ok : StepVerdict.Failed;
    }
}

/// <summary>
/// Whether a change meant to be invisible was.
/// <para>
/// WW46. freewilly's window skill states the rule: a change meant to be invisible must produce a
/// byte-identical file, and the render is deterministic, verified by re-capturing unchanged code.
/// Three findings about theme handling in that project came from this and from nothing else, and
/// its test suite saw none of them.
/// </para>
/// <para>
/// It also avoids choosing a tolerance, which is the argument every other image comparison
/// eventually turns into — and choosing one is choosing how much of a change to stop reporting.
/// </para>
/// <para>
/// Bytes first and pixels second, because those are two answers. Two files that differ and draw the
/// same picture is an encoder writing something of its own into them, and a reader told only that
/// the render changed would go looking for a visual difference, find none, and conclude the check
/// is broken.
/// </para>
/// </summary>
public static class Unchanged
{
    /// <summary>
    /// Compare two renders. Neither is the expected one: this answers whether they are the same,
    /// and which of them is the baseline is the caller's business.
    /// </summary>
    /// <param name="left">One render.</param>
    /// <param name="right">The other.</param>
    /// <exception cref="ArgumentException">Where either path is missing.</exception>
    public static RenderMatch Between(string left, string right)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(left);
        ArgumentException.ThrowIfNullOrWhiteSpace(right);

        var one = Path.GetFullPath(left.Trim());
        var other = Path.GetFullPath(right.Trim());
        var named = $"'{Path.GetFileName(one)}'";
        var against = $"'{Path.GetFileName(other)}'";

        // A render that was never written is not a render that changed. Named separately, because
        // a caller told "they differ" about a file that is not there would go looking at the wrong
        // one of the two.
        if (!File.Exists(one))
            return new RenderMatch(named, against, 0, 0, 0, false, $"there is no render at {one}");

        if (!File.Exists(other))
            return new RenderMatch(named, against, 0, 0, 0, false, $"there is no render at {other}");

        var first = File.ReadAllBytes(one);
        var second = File.ReadAllBytes(other);

        var differsAt = FirstDifference(first, second);
        if (differsAt < 0)
            return new RenderMatch(named, against, first.LongLength, -1, 0, true, "");

        var (differing, read) = Drawn(one, other);
        return new RenderMatch(named, against, first.LongLength, differsAt, differing, read, "");
    }

    /// <summary>The first byte two files disagree on, or -1 where they never do.</summary>
    private static long FirstDifference(byte[] left, byte[] right)
    {
        var shared = Math.Min(left.LongLength, right.LongLength);
        for (long at = 0; at < shared; at++)
        {
            if (left[at] != right[at])
                return at;
        }

        return left.LongLength == right.LongLength ? -1 : shared;
    }

    /// <summary>
    /// How many pixels the two draw differently, and whether they could be read as pictures at all.
    /// <para>
    /// Converted to one format before comparing, for the reason the colour count gives: the
    /// alternative is a switch over every pixel format these might arrive in, and being wrong about
    /// an indexed one whose palette carries the colour.
    /// </para>
    /// </summary>
    private static (long Differing, bool Read) Drawn(string left, string right)
    {
        try
        {
            var one = Bgra(left);
            var other = Bgra(right);

            if (one.PixelWidth != other.PixelWidth || one.PixelHeight != other.PixelHeight)
                return ((long)one.PixelWidth * one.PixelHeight, true);

            var stride = one.PixelWidth * 4;
            var here = new byte[stride];
            var there = new byte[stride];
            var differing = 0L;

            for (var line = 0; line < one.PixelHeight; line++)
            {
                var row = new System.Windows.Int32Rect(0, line, one.PixelWidth, 1);
                one.CopyPixels(row, here, stride, 0);
                other.CopyPixels(row, there, stride, 0);

                for (var at = 0; at < stride; at += 4)
                {
                    if (BitConverter.ToUInt32(here, at) != BitConverter.ToUInt32(there, at))
                        differing++;
                }
            }

            return (differing, true);
        }
        catch (Exception unreadable)
            when (unreadable is NotSupportedException or FileFormatException or IOException)
        {
            // Reported as unread rather than as no difference. A file this cannot decode may draw
            // anything at all, and answering zero would be a green about a picture nobody saw.
            return (0, false);
        }
    }

    private static BitmapSource Bgra(string path)
    {
        var decoded = BitmapDecoder.Create(
            new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

        return new FormatConvertedBitmap(decoded.Frames[0], PixelFormats.Bgra32, null, 0);
    }
}
