using System.Windows.Media;
using System.Windows.Media.Imaging;

using Winwright.Verdicts;

namespace Winwright.Capturing;

/// <summary>Raised where a picture turned out to be a blank, so nothing was proved by writing it.</summary>
public sealed class BlankPictureException : InvalidOperationException
{
    /// <summary>Say which picture, and what was read.</summary>
    public BlankPictureException(string message)
        : base(message)
    {
    }

    /// <summary>Unused. Present because an exception with no default shapes is awkward to catch.</summary>
    public BlankPictureException()
        : base("the picture carries no pixel that anything drew")
    {
    }

    /// <summary>Unused. Present for the same reason.</summary>
    public BlankPictureException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>What scanning a picture's alpha channel turned out to say.</summary>
public sealed record PictureCheck
{
    internal PictureCheck(string what, long pixels, long drawn, bool tellable, string because)
    {
        What = what;
        Pixels = pixels;
        Drawn = drawn;
        Tellable = tellable;
        Because = because;
    }

    /// <summary>What was read — a path, or the phrase a bitmap is named by.</summary>
    public string What { get; }

    /// <summary>How many pixels the picture has.</summary>
    public long Pixels { get; }

    /// <summary>How many carry an alpha of their own, which is to say how many anything drew.</summary>
    public long Drawn { get; }

    /// <summary>
    /// Whether a blank could be told from a drawing here at all. False where the picture has no
    /// alpha channel: every pixel in one of those is opaque by construction, so a count of drawn
    /// pixels equal to the total means nothing whatever.
    /// </summary>
    public bool Tellable { get; }

    /// <summary>Why it could not be told, where it could not. Empty otherwise.</summary>
    public string Because { get; }

    /// <summary>Whether nothing drew anything. False on a picture this cannot answer for.</summary>
    public bool IsBlank => Tellable && Drawn == 0;

    /// <summary>Whether something drew something.</summary>
    public bool HasInk => Tellable && Drawn > 0;

    /// <summary>What was read, said whichever way it came out.</summary>
    public string Sentence()
    {
        if (!Tellable)
            return $"{What} cannot be told blank or drawn: {Because}.";

        return IsBlank
            ? $"{What} is a blank: none of its {Pixels} pixels carries an alpha of its own, which is what a tree "
                + "that failed to build renders as."
            : $"{What} carries ink: {Drawn} of {Pixels} pixels were drawn.";
    }

    /// <summary>
    /// The result a verdict counts. A picture this cannot answer for is <em>unchecked</em> and not
    /// a pass: the scan never ran, and a green covering a scan that never ran is the whole defect
    /// this project was started over.
    /// </summary>
    public AssertionResult AsAssertion(string named = "the capture is not a blank")
    {
        if (!Tellable)
            return AssertionResult.Unchecked(named, Precondition.Absent("an alpha channel to read", Because));

        return HasInk ? AssertionResult.Pass(named, Sentence()) : AssertionResult.Fail(named, Sentence());
    }
}

/// <summary>
/// Whether a picture is a picture or a blank.
/// <para>
/// A tree that failed to build, or one that was never arranged, renders as a rectangle of
/// transparent pixels — and a caller that checked only that a file was written cannot tell the two
/// apart. Scanning for any pixel carrying an alpha of its own is the whole assertion.
/// </para>
/// <para>
/// It is not a claim that the screen is correct, and it is deliberately not the flat-colour check
/// either: a render composed onto a background is opaque everywhere whatever the tree did, and
/// telling that one apart is a different reading. What this answers is the one question a file on
/// disk cannot: did anything draw.
/// </para>
/// </summary>
public static class Pictures
{
    /// <summary>Read a PNG off disk and scan it.</summary>
    /// <param name="path">The picture.</param>
    /// <exception cref="ArgumentException">Where no path was given.</exception>
    /// <exception cref="FileNotFoundException">Where the file is not there — a missing capture is not a blank one.</exception>
    public static PictureCheck Of(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var full = Path.GetFullPath(path.Trim());
        if (!File.Exists(full))
            throw new FileNotFoundException($"there is no picture at {full} to read", full);

        var decoded = BitmapDecoder.Create(
            new Uri(full), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

        return Read(Path.GetFileName(full), decoded.Frames[0]);
    }

    /// <summary>Scan a picture already in hand.</summary>
    /// <param name="picture">The bitmap.</param>
    /// <param name="named">What to call it in the sentence.</param>
    public static PictureCheck Of(BitmapSource picture, string named = "the render")
    {
        ArgumentNullException.ThrowIfNull(picture);
        return Read(named, picture);
    }

    /// <summary>
    /// The same, refusing a blank. Reach for this where the picture is the whole evidence, and for
    /// <see cref="Of(string)"/> where a run would rather count it than end over it.
    /// </summary>
    /// <param name="path">The picture.</param>
    /// <exception cref="BlankPictureException">Where nothing drew anything.</exception>
    public static PictureCheck Insist(string path)
    {
        var read = Of(path);
        return read.IsBlank ? throw new BlankPictureException(read.Sentence()) : read;
    }

    private static PictureCheck Read(string what, BitmapSource picture)
    {
        var format = picture.Format;
        if (!CarriesAlpha(format))
        {
            return new PictureCheck(
                what,
                (long)picture.PixelWidth * picture.PixelHeight,
                0,
                false,
                $"it is {format}, which has no alpha channel — every pixel in one of those is opaque whatever drew it");
        }

        var bytes = format.BitsPerPixel / 8;
        var stride = picture.PixelWidth * bytes;
        var row = new byte[stride];
        var drawn = 0L;

        // A row at a time rather than the whole picture at once: a capture of a large display is
        // tens of megabytes, and this runs beside a suite rather than alone on the machine.
        for (var line = 0; line < picture.PixelHeight; line++)
        {
            picture.CopyPixels(new System.Windows.Int32Rect(0, line, picture.PixelWidth, 1), row, stride, 0);
            for (var at = 0; at < stride; at += bytes)
            {
                if (AlphaAt(row, at, bytes) != 0)
                    drawn++;
            }
        }

        return new PictureCheck(what, (long)picture.PixelWidth * picture.PixelHeight, drawn, true, "");
    }

    /// <summary>
    /// The alpha byte of one pixel. Last in every BGRA and RGBA layout this reads, and read as the
    /// high byte of the channel in the 64-bit ones so a 16-bit alpha of 1 is not mistaken for none.
    /// </summary>
    private static byte AlphaAt(byte[] row, int at, int bytes) => bytes switch
    {
        4 => row[at + 3],
        8 => (byte)(row[at + 6] | row[at + 7]),
        _ => row[at + bytes - 1],
    };

    /// <summary>
    /// Whether this format has an alpha channel at all. A list rather than a computation, because
    /// the alternative is inferring it from a mask and being wrong about an indexed format whose
    /// palette carries the transparency instead of the pixels.
    /// </summary>
    private static bool CarriesAlpha(PixelFormat format) =>
        format == PixelFormats.Pbgra32
        || format == PixelFormats.Bgra32
        || format == PixelFormats.Prgba64
        || format == PixelFormats.Rgba64
        || format == PixelFormats.Rgba128Float
        || format == PixelFormats.Prgba128Float;
}
