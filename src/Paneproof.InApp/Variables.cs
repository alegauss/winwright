namespace Paneproof.InApp;

/// <summary>
/// The variables the harness names a file or a directory in. WW591.
/// <para>
/// Each has a new name and the one it had before the product was Paneproof, and a harness on an
/// engine older than this package sets only the old one. So the new name is read first and the old
/// one after it, and an application never goes without its report because the two halves were
/// upgraded on different days. Spelled here because this half holds no reference to the engine.
/// </para>
/// </summary>
internal static class Variables
{
    private const string Prefix = "PANEPROOF_";
    private const string OldPrefix = "WINWRIGHT_";

    /// <summary>The value under <paramref name="variable"/>, or under its old name, or null.</summary>
    public static string? Read(string variable)
    {
        var named = Environment.GetEnvironmentVariable(variable);
        if (!string.IsNullOrWhiteSpace(named) || !variable.StartsWith(Prefix, StringComparison.Ordinal))
            return named;

        return Environment.GetEnvironmentVariable(OldPrefix + variable[Prefix.Length..]);
    }
}
