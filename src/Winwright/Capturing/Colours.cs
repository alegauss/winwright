using System.Windows.Media;
using System.Windows.Media.Imaging;

using Winwright.Tracing;
using Winwright.Verdicts;

namespace Winwright.Capturing;

/// <summary>What counting a picture's distinct colours turned out to say.</summary>
public sealed record ColourCheck
{
    internal ColourCheck(string what, long pixels, int distinct, bool capped, bool counted, string because)
    {
        What = what;
        Pixels = pixels;
        Distinct = distinct;
        Capped = capped;
        Counted = counted;
        Because = because;
    }

    /// <summary>What was read — a path, or the phrase a bitmap is named by.</summary>
    public string What { get; }

    /// <summary>How many pixels the picture has.</summary>
    public long Pixels { get; }

    /// <summary>
    /// How many distinct colours were found, up to the point counting stopped. Read with
    /// <see cref="Capped" />: a picture of a real window has thousands and counting them all would
    /// be work nobody asked for.
    /// </summary>
    public int Distinct { get; }

    /// <summary>Whether counting stopped early because enough had been found to answer.</summary>
    public bool Capped { get; }

    /// <summary>Whether the picture could be counted at all.</summary>
    public bool Counted { get; }

    /// <summary>Why it could not be, where it could not. Empty otherwise.</summary>
    public string Because { get; }

    /// <summary>
    /// Whether the whole picture is one colour. A flat rectangle is not a picture of a window, and
    /// this is the reading that says so on a format with no alpha channel — which is every screen
    /// copy, and exactly the picture <see cref="PictureCheck" /> cannot answer for.
    /// </summary>
    public bool IsFlat => Counted && Distinct <= 1;

    /// <summary>What was read, said whichever way it came out.</summary>
    public string Sentence()
    {
        if (!Counted)
            return $"{What} could not be counted for colour: {Because}.";

        if (IsFlat)
            return $"{What} is one colour across all {Pixels} of its pixels, which is what a display that was "
                + "rendering nothing copies as — not a picture of a window.";

        var many = Capped ? $"at least {Distinct}" : $"{Distinct}";
        return $"{What} carries {many} distinct colour(s) across {Pixels} pixel(s).";
    }

    /// <inheritdoc cref="Sentence" />
    public override string ToString() => Sentence();

    /// <summary>
    /// The result a verdict counts. A picture this could not count is <em>unchecked</em> and not a
    /// pass, for the reason the alpha scan beside it gives: the scan never ran, and a green
    /// covering a scan that never ran is the defect this project was started over.
    /// </summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult AsAssertion(string named = "the capture is not one flat colour")
    {
        if (!Counted)
            return AssertionResult.Unchecked(named, Precondition.Absent("a picture that could be counted", Because));

        return IsFlat ? AssertionResult.Fail(named, Sentence()) : AssertionResult.Pass(named, Sentence());
    }

    /// <summary>The step a trace records.</summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public TraceStep AsTraceStep(string named = "the capture is not one flat colour") => new()
    {
        Verb = "count colours",
        Locator = named,
        Resolved = What,
        ReadBack = Counted ? $"{(Capped ? "at least " : "")}{Distinct} of {Pixels} pixel(s)" : null,
        Verdict = Verdict(),
        Detail = Counted && !IsFlat ? null : Sentence(),
    };

    private StepVerdict Verdict()
    {
        if (!Counted)
            return StepVerdict.Unchecked;

        return IsFlat ? StepVerdict.Failed : StepVerdict.Ok;
    }
}

/// <summary>
/// Whether a picture is a picture or a rectangle of one colour.
/// <para>
/// WW42. Measured in freewilly while shipping a task: a copy of the notification area came back as
/// exactly one distinct colour, with the session present, the shell running and the environment
/// reporting an interactive desktop. The display was simply not rendering anything a copy could
/// read. Without this the script would have written that file and exited zero, and the reader would
/// have had a picture of nothing that claimed to be a picture of something.
/// </para>
/// <para>
/// Deliberately not the alpha scan, which <see cref="Pictures" /> already says is a different
/// reading. That one answers whether anything drew, and cannot answer at all on a format with no
/// alpha channel — which is every screen copy, and a screen copy is what this was measured on.
/// </para>
/// </summary>
public static class Colours
{
    /// <summary>
    /// How many distinct colours are enough to answer. A picture of a real window has thousands,
    /// and the question is whether it has one — so counting stops as soon as the answer cannot
    /// change, and a flat picture is the only one that costs a whole scan.
    /// </summary>
    public const int Enough = 8;

    /// <summary>Read a PNG off disk and count it.</summary>
    /// <param name="path">The picture.</param>
    /// <exception cref="ArgumentException">Where no path was given.</exception>
    /// <exception cref="FileNotFoundException">Where the file is not there — a missing capture is not a flat one.</exception>
    public static ColourCheck In(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var full = Path.GetFullPath(path.Trim());
        if (!File.Exists(full))
            throw new FileNotFoundException($"there is no picture at {full} to count", full);

        var decoded = BitmapDecoder.Create(
            new Uri(full), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

        return Count(Path.GetFileName(full), decoded.Frames[0]);
    }

    /// <summary>Count a picture already in hand.</summary>
    /// <param name="picture">The bitmap.</param>
    /// <param name="named">What to call it in the sentence.</param>
    public static ColourCheck In(BitmapSource picture, string named = "the capture")
    {
        ArgumentNullException.ThrowIfNull(picture);
        return Count(named, picture);
    }

    private static ColourCheck Count(string what, BitmapSource picture)
    {
        var pixels = (long)picture.PixelWidth * picture.PixelHeight;
        if (pixels == 0)
            return new ColourCheck(what, 0, 0, false, false, "it has no pixels to count");

        // Converted rather than read in whatever it arrived as. The alternative is a switch over
        // every pixel format this might meet, and being wrong about an indexed one whose palette
        // carries the colour — which would read a picture of a window as a picture of its indices.
        var converted = new FormatConvertedBitmap(picture, PixelFormats.Bgra32, null, 0);
        var stride = converted.PixelWidth * 4;
        var row = new byte[stride];
        var seen = new HashSet<uint>();

        // A row at a time, for the reason the alpha scan gives: a capture of a large display is
        // tens of megabytes, and this runs beside a suite rather than alone on the machine.
        for (var line = 0; line < converted.PixelHeight; line++)
        {
            converted.CopyPixels(new System.Windows.Int32Rect(0, line, converted.PixelWidth, 1), row, stride, 0);
            for (var at = 0; at < stride; at += 4)
            {
                seen.Add(BitConverter.ToUInt32(row, at));
                if (seen.Count >= Enough)
                    return new ColourCheck(what, pixels, seen.Count, true, true, "");
            }
        }

        return new ColourCheck(what, pixels, seen.Count, false, true, "");
    }
}
