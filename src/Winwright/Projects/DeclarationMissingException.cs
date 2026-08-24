namespace Winwright.Projects;

/// <summary>
/// The ways a declaration can be missing.
/// <para>
/// WW196. Three and not four, and the arithmetic is the point: this is thrown from four places, and
/// two of them are one refusal carrying a different key. A missing <c>executable</c> and a missing
/// <c>timeouts.settle</c> send the author to the same file to add the same kind of line, and giving
/// each its own arm would be counting values rather than refusals.
/// </para>
/// </summary>
public enum MissingDeclaration
{
    /// <summary>Thrown without saying which. Pairs with nothing, and the suite refuses it.</summary>
    Unsaid,

    /// <summary>A path was named and there is no declaration at it.</summary>
    NotAtThePathNamed,

    /// <summary>Nothing was named, and no declaration is in this directory or any above it.</summary>
    NotUpTheTree,

    /// <summary>The declaration was read and does not carry the key a scenario asked for.</summary>
    KeyNotDeclared,
}

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

    /// <summary>The same, saying which of the ways it is missing this one is.</summary>
    /// <param name="arm">Which way.</param>
    /// <param name="key">The declaration that is missing, spelled as the file spells it.</param>
    /// <param name="declaredIn">The file that was read, or where one was looked for.</param>
    /// <param name="wanted">What needed it, so the refusal is actionable without a stack trace.</param>
    public DeclarationMissingException(MissingDeclaration arm, string key, string declaredIn, string wanted)
        : this(key, declaredIn, wanted)
    {
        Arm = arm;
    }

    /// <summary>
    /// Which way it is missing. <see cref="MissingDeclaration.Unsaid" /> where it was thrown without
    /// saying — a refusal nothing can pair, and the check says so.
    /// </summary>
    public MissingDeclaration Arm { get; } = MissingDeclaration.Unsaid;

    /// <summary>The declaration that is missing.</summary>
    public string Key { get; }

    /// <summary>The file that was read, or the place one was looked for.</summary>
    public string DeclaredIn { get; }

    /// <summary>What needed it.</summary>
    public string Wanted { get; }
}
