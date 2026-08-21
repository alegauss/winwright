namespace Winwright.Projects;

/// <summary>
/// A scenario asked for something the project never declared. It is thrown rather than defaulted
/// because the alternative is a run that quietly means something different on the next checkout:
/// the refusal names the key, so moving a scenario somewhere it cannot work says so in one line.
/// </summary>
public sealed class DeclarationMissingException : Exception
{
    /// <param name="key">The declaration that is missing, spelled as the file spells it.</param>
    /// <param name="declaredIn">The file that was read, or where one was looked for.</param>
    /// <param name="wanted">What needed it, so the refusal is actionable without a stack trace.</param>
    public DeclarationMissingException(string key, string declaredIn, string wanted)
        : base($"{declaredIn} declares no '{key}', and {wanted} needs one")
    {
        Key = key;
        DeclaredIn = declaredIn;
        Wanted = wanted;
    }

    /// <summary>The declaration that is missing.</summary>
    public string Key { get; }

    /// <summary>The file that was read, or the place one was looked for.</summary>
    public string DeclaredIn { get; }

    /// <summary>What needed it.</summary>
    public string Wanted { get; }
}
