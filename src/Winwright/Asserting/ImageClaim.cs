using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Winwright.Asserting;

/// <summary>What an image file is, read off its decoded pixels. WW573.</summary>
/// <param name="Width">Its width in pixels.</param>
/// <param name="Height">Its height in pixels.</param>
/// <param name="Transparent">Whether any pixel is less than fully opaque.</param>
public sealed record ImageReading(int Width, int Height, bool Transparent)
{
    /// <summary>The reading in the words a step's detail uses.</summary>
    public override string ToString() =>
        $"{Width}x{Height}, {(Transparent ? "with transparent pixels" : "with no transparent pixel")}";
}

/// <summary>
/// The claim a file step makes about an image the application wrote: its size and whether it is
/// clear anywhere. WW573.
/// <para>
/// Found by Modelado (MOD19): the application renders its view again at 2048 pixels on the long side
/// and saves a PNG clear around the model. The bytes depend on the proportions of a viewport that
/// differ from one desk to the next, so <c>sameBytesAs</c> had nothing to compare against, and the case
/// proved the file by dropping it back into the application — which works only for an application
/// that opens what it writes.
/// </para>
/// <para>
/// Written as text, facts joined by commas: <c>width N</c>, <c>height N</c>, <c>longSide N</c>,
/// <c>shortSide N</c>, <c>transparent</c> and <c>opaque</c>. Decoded through Windows' own imaging,
/// so PNG, JPEG, BMP, GIF, TIFF and, where the system has its codec, WebP read the same way, and no
/// dependency joins the engine.
/// </para>
/// </summary>
public sealed record ImageClaim
{
    private readonly IReadOnlyList<(string Fact, int? Number)> facts;

    private ImageClaim(string text, IReadOnlyList<(string Fact, int? Number)> facts)
    {
        Text = text;
        this.facts = facts;
    }

    /// <summary>The facts a claim may name, as a refusal lists them.</summary>
    public const string Spelled =
        "facts joined by commas, each one of 'width N', 'height N', 'longSide N', 'shortSide N', "
        + "'transparent' or 'opaque', such as 'longSide 2048, transparent'";

    /// <summary>How the case wrote it.</summary>
    public string Text { get; }

    /// <summary>The claim a case wrote, or null with what is wrong with it.</summary>
    /// <param name="written">What the case wrote.</param>
    /// <param name="wrong">Why it is no claim, where it is not one.</param>
    public static ImageClaim? Parse(string? written, out string? wrong)
    {
        wrong = null;
        if (string.IsNullOrWhiteSpace(written))
        {
            wrong = "it names no fact";
            return null;
        }

        var read = new List<(string, int?)>();
        foreach (var part in written.Split(','))
        {
            var words = part.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var fact = words.Length > 0 ? words[0] : "";

            if (fact is "transparent" or "opaque" && words.Length == 1)
            {
                read.Add((fact, null));
                continue;
            }

            if (fact is "width" or "height" or "longSide" or "shortSide"
                && words.Length == 2
                && int.TryParse(words[1], NumberStyles.None, CultureInfo.InvariantCulture, out var pixels)
                && pixels > 0)
            {
                read.Add((fact, pixels));
                continue;
            }

            wrong = $"'{part.Trim()}' is no fact about an image";
            return null;
        }

        if (read.Any(one => one.Item1 == "transparent") && read.Any(one => one.Item1 == "opaque"))
        {
            wrong = "it claims the image both transparent and opaque";
            return null;
        }

        return new ImageClaim(written.Trim(), read);
    }

    /// <summary>What an image file is, or null with why it could not be decoded as one.</summary>
    /// <param name="bytes">The file's bytes.</param>
    /// <param name="why">Why it is no image, where it is not one.</param>
    public static ImageReading? Read(byte[] bytes, out string? why)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        why = null;

        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            var decoded = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var frame = decoded.Frames[0];
            return new ImageReading(frame.PixelWidth, frame.PixelHeight, Clear(frame));
        }
        catch (Exception unreadable) when (unreadable is NotSupportedException or FileFormatException
            or ArgumentException or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            why = unreadable.Message;
            return null;
        }
    }

    /// <summary>Whether a reading is what this claims.</summary>
    /// <param name="reading">What the file was read as.</param>
    public bool HeldBy(ImageReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        return facts.All(one => one.Fact switch
        {
            "width" => reading.Width == one.Number,
            "height" => reading.Height == one.Number,
            "longSide" => Math.Max(reading.Width, reading.Height) == one.Number,
            "shortSide" => Math.Min(reading.Width, reading.Height) == one.Number,
            "transparent" => reading.Transparent,
            _ => !reading.Transparent,
        });
    }

    /// <summary>The claim as the case wrote it.</summary>
    public override string ToString() => Text;

    /// <summary>
    /// Whether any pixel is less than fully opaque. A format with no alpha is opaque without a scan;
    /// one with alpha is read a row at a time and stops at the first clear pixel.
    /// </summary>
    private static bool Clear(BitmapSource picture)
    {
        var format = picture.Format;
        if (format != PixelFormats.Bgra32 && format != PixelFormats.Pbgra32 && format != PixelFormats.Rgba64
            && format != PixelFormats.Prgba64 && format != PixelFormats.Rgba128Float && format != PixelFormats.Prgba128Float
            && format != PixelFormats.Indexed1 && format != PixelFormats.Indexed2 && format != PixelFormats.Indexed4
            && format != PixelFormats.Indexed8)
        {
            return false;
        }

        // Converted rather than read as it came, so an indexed picture's palette and a wide one's
        // channels are read the one way.
        var converted = new FormatConvertedBitmap(picture, PixelFormats.Bgra32, null, 0);
        var stride = converted.PixelWidth * 4;
        var row = new byte[stride];
        for (var line = 0; line < converted.PixelHeight; line++)
        {
            converted.CopyPixels(new System.Windows.Int32Rect(0, line, converted.PixelWidth, 1), row, stride, 0);
            for (var at = 3; at < stride; at += 4)
            {
                if (row[at] < 255)
                    return true;
            }
        }

        return false;
    }
}
