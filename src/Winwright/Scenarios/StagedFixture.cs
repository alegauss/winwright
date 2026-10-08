using System.Diagnostics;

namespace Winwright.Scenarios;

/// <summary>
/// A fixture staged for one launch: the directory its files are in exists and holds them, so a
/// launch built from this is a launch the application can be started with. WW515.
/// <para>
/// WW509 left two calls and no order. <c>Stage</c> emptied the directory a fixture's files go in
/// and copied them there; <c>Starting</c> built the start info, resolving
/// <see cref="FixtureDeclaration.Staged"/> to that same directory. Both derived the path from the
/// root and the fixture's name, so they could not disagree about <em>where</em> — and nothing said
/// one had to happen before the other. A caller that built the launch without staging first got a
/// start info whose <c>APPDATA</c> pointed at a directory that might not exist, or held what an
/// earlier run left; the application then reported a store it could not read, and the red was about
/// the application.
/// </para>
/// <para>
/// So there is one order and it is the only one this type can be reached by.
/// <see cref="FixtureDeclaration.Stage"/> is what makes one, and <see cref="Starting"/> is here
/// rather than on the declaration — a launch cannot be composed from a fixture that was never
/// staged, because the method that composes it is on the thing staging returns. WW508 made the same
/// argument one field over and took the same answer: <c>root</c> is a parameter rather than a
/// default, because a default there would have been the defect that task removed, spelled as a
/// choice nobody made.
/// </para>
/// <para>
/// It carries no files of its own and copies nothing. What it knows is where they went, which is
/// the one fact a launch needs and the one a case can read back.
/// </para>
/// </summary>
public sealed class StagedFixture
{
    /// <summary>Made by <see cref="FixtureDeclaration.Stage"/> and by nothing else.</summary>
    /// <param name="fixture">The declaration this was staged from.</param>
    /// <param name="root">The project's root, which its relative paths resolve against.</param>
    /// <param name="into">The directory its files are in, which exists by now.</param>
    internal StagedFixture(FixtureDeclaration fixture, string root, string into)
    {
        Fixture = fixture;
        Root = root;
        Into = into;
    }

    /// <summary>What was staged.</summary>
    public FixtureDeclaration Fixture { get; }

    /// <summary>The project's root, the directory its declaration sits in.</summary>
    public string Root { get; }

    /// <summary>
    /// The directory this launch's files are in. What <see cref="FixtureDeclaration.Staged"/>
    /// resolves to, and a real directory rather than a path that would be one.
    /// </summary>
    public string Into { get; }

    /// <summary>The directory the application is started in, resolved.</summary>
    public string StartsIn => Fixture.StartsIn(Root);

    /// <summary>
    /// How to start the application under test with this fixture in force.
    /// <para>
    /// Takes no root, because this already knows it: the root a launch resolves against and the
    /// root its files were staged under are the same root by construction now, where two calls
    /// taking it separately could be given two.
    /// </para>
    /// </summary>
    /// <param name="executable">The application, usually the project's own.</param>
    public ProcessStartInfo Starting(string executable) => Fixture.Starting(executable, Root);

    /// <summary>The one line a listing shows.</summary>
    public override string ToString() => $"{Fixture.Sentence()[..^1]}, staged in {Into}.";
}
