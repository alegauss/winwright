using System.Diagnostics;

namespace Winwright.Processes;

/// <summary>
/// The two keys a binary is recognised by, and the path it was read from. Two, because one is not
/// enough: the file version catches the ordinary case and cannot catch a Debug build against an
/// installed Release, which carry the same version between releases.
/// </summary>
/// <param name="Path">Where it was read from.</param>
/// <param name="FileVersion">Its file version, or null where it carries none.</param>
/// <param name="WrittenUtc">When it was written, which is the key the version cannot be.</param>
public sealed record BinaryIdentity(string Path, string? FileVersion, DateTime WrittenUtc)
{
    /// <summary>Read both keys off a file.</summary>
    /// <exception cref="FileNotFoundException">Where there is no such file.</exception>
    public static BinaryIdentity Of(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var full = System.IO.Path.GetFullPath(path);
        if (!File.Exists(full))
            throw new FileNotFoundException($"there is no binary at {full} to identify", full);

        var version = FileVersionInfo.GetVersionInfo(full).FileVersion;
        return new BinaryIdentity(full, string.IsNullOrWhiteSpace(version) ? null : version, File.GetLastWriteTimeUtc(full));
    }

    /// <summary>The version as a summary prints it, with a word for the file that carries none.</summary>
    public string Version => FileVersion ?? "no version";

    /// <summary>When it was written, stamped the way every other timestamp in this project is.</summary>
    public string Written => WrittenUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");

    /// <summary>Both keys and the path, which is what a run says about the binary it drove.</summary>
    public override string ToString() => $"{Path} ({Version}, built {Written})";
}
