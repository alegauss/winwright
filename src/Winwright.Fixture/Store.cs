using System.IO;
using System.Text;

namespace Winwright.Fixture;

/// <summary>
/// A store of the fixture's own, which a run is allowed to break.
/// <para>
/// The fingerprint check protects the store of whoever is running it, which makes it the one
/// assertion that cannot be developed against a real product without putting somebody's settings
/// at risk. This one belongs to the fixture, is written from constants, and is offered up to be
/// mutated on request — so both the clean run and the caught mutation are observable and nothing
/// real is ever touched.
/// </para>
/// <para>
/// The mutation rewrites a value <em>of the same length</em>, deliberately. That is the exact
/// accident the fingerprint exists for: a settings file repointed from one profile to another of
/// the same name, which a comparison by size or by write time calls unchanged.
/// </para>
/// </summary>
public static class Store
{
    /// <summary>What a clean run writes, every time, on every desk.</summary>
    public const string Settled = @"{ ""profile"": ""alpha"", ""verbose"": false }";

    /// <summary>What a mutating run leaves instead. The same length, and a different machine.</summary>
    public const string Mutated = @"{ ""profile"": ""bravo"", ""verbose"": false }";

    /// <summary>The file the mutation rewrites, relative to the store.</summary>
    public const string SettingsFile = "settings.json";

    /// <summary>A second file, so a fingerprint of one file is not what the check is proved against.</summary>
    public const string ProfilesFile = "profiles.json";

    /// <summary>
    /// Write the store, mutating it where the run asked. Called before any window: a check that
    /// fingerprints around a launch must see the whole write, not the part that finished first.
    /// </summary>
    /// <param name="directory">Where the store lives.</param>
    /// <param name="mutating">Whether to leave the settings changed.</param>
    public static void Write(string directory, bool mutating)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        var full = Path.GetFullPath(directory.Trim());
        Directory.CreateDirectory(full);

        // No BOM and a fixed newline: an encoding preamble and a line ending are both content, and
        // a store that wrote either differently on two runs would fail a check about neither.
        var utf8 = new UTF8Encoding(false);
        File.WriteAllText(Path.Combine(full, SettingsFile), mutating ? Mutated : Settled, utf8);
        File.WriteAllText(Path.Combine(full, ProfilesFile), @"[ ""alpha"", ""bravo"" ]", utf8);
    }
}
