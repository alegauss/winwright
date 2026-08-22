using System.Xml.Linq;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW123. Both halves reference nothing of each other, and both project files say why in a
/// comment. A comment is not a check: one line added in either project would merge them, the build
/// would stay green, and the consequence would be found by whoever shipped a test harness inside
/// their product or a presentation stack inside a headless runner.
/// <para>
/// The separation is load-bearing in both directions. The engine is taken by the harness driving
/// an application; the in-app half is taken by that application. An application referencing the
/// engine ships a harness to its users; a harness referencing the in-app half inherits a drawing
/// stack it never needed, and the two module initialisers that each ask for per-monitor awareness
/// stop being independent — which is the reading WW121 measured rather than argued.
/// </para>
/// <para>
/// Read two ways on purpose. The project file is where the edit would be made; the built assembly
/// is what actually ships, and the compiler drops a reference nothing used — so a check on either
/// alone would miss half of what it is for.
/// </para>
/// </summary>
public sealed class SeparationTests
{
    private const string Engine = "src/Winwright/Winwright.csproj";
    private const string InApp = "src/Winwright.InApp/Winwright.InApp.csproj";
    private const string Fixture = "src/Winwright.Fixture/Winwright.Fixture.csproj";
    private const string Adopter = "samples/Adopter/Adopter.csproj";

    /// <summary>The repository root, walked up from where the suite is running.</summary>
    private static string Repository()
    {
        var walking = new DirectoryInfo(AppContext.BaseDirectory);
        while (walking is not null && !File.Exists(Path.Combine(walking.FullName, "Winwright.slnx")))
            walking = walking.Parent;

        Assert.NotNull(walking);
        return walking.FullName;
    }

    /// <summary>Everything one project file references, by the name it references it under.</summary>
    private static IReadOnlyList<string> References(string project)
    {
        var document = XDocument.Load(Path.Combine(Repository(), project));
        return document.Root!
            .Elements().Where(one => one.Name.LocalName == "ItemGroup")
            .SelectMany(group => group.Elements())
            .Where(one => one.Name.LocalName is "ProjectReference" or "PackageReference")
            .Select(one => one.Attribute("Include")?.Value?.Trim() ?? "")
            .Where(one => one.Length > 0)
            .ToList();
    }

    /// <summary>Whether a set of references names an assembly, by path or by package id.</summary>
    private static bool Names(IReadOnlyList<string> references, string assembly) =>
        references.Any(one =>
            string.Equals(one, assembly, StringComparison.OrdinalIgnoreCase)
            || one.EndsWith($"{assembly}.csproj", StringComparison.OrdinalIgnoreCase)
            || one.EndsWith($"\\{assembly}.csproj", StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<string> Referenced(System.Reflection.Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(one => one.Name ?? "").ToList();

    [Fact]
    public void The_engine_project_references_no_in_app_half()
    {
        // An engine that took the in-app half would inherit a drawing stack it never needed, and
        // the two module initialisers asking for per-monitor awareness would stop being separate.
        Assert.False(Names(References(Engine), "Winwright.InApp"), string.Join(", ", References(Engine)));
    }

    [Fact]
    public void The_in_app_project_references_no_engine()
    {
        // The one that would actually hurt somebody: an application referencing the engine ships a
        // test harness to its users.
        Assert.False(Names(References(InApp), "Winwright"), string.Join(", ", References(InApp)));
    }

    [Fact]
    public void The_engine_project_references_nothing_at_all()
    {
        // The non-goal spelled as a check: a package in the engine is a package every adopting
        // project inherits, and the whole engine is two in-box assemblies and nothing else.
        Assert.Empty(References(Engine));
    }

    [Fact]
    public void The_in_app_project_references_nothing_at_all()
    {
        Assert.Empty(References(InApp));
    }

    [Fact]
    public void The_built_engine_carries_no_reference_to_the_in_app_half()
    {
        // What actually ships, rather than what the file says. The compiler drops a reference
        // nothing used, so this and the project-file check are two different questions.
        var referenced = Referenced(typeof(Winwright.Locating.Subject).Assembly);

        Assert.DoesNotContain("Winwright.InApp", referenced);
    }

    [Fact]
    public void The_built_in_app_half_carries_no_reference_to_the_engine()
    {
        var referenced = Referenced(typeof(Winwright.InApp.Coordinates).Assembly);

        Assert.DoesNotContain("Winwright", referenced);
    }

    [Fact]
    public void The_application_under_test_takes_the_in_app_half_and_not_the_engine()
    {
        // The rule applied to this repository's own adopting project, which is the one that would
        // actually regress: the fixture is an application, so it takes what an application takes.
        var references = References(Fixture);

        Assert.True(Names(references, "Winwright.InApp"), string.Join(", ", references));
        Assert.False(Names(references, "Winwright"), string.Join(", ", references));
    }

    [Fact]
    public void The_sample_adopter_takes_the_in_app_half_and_not_the_engine()
    {
        var references = References(Adopter);

        Assert.True(Names(references, "Winwright.InApp"), string.Join(", ", references));
        Assert.False(Names(references, "Winwright"), string.Join(", ", references));
    }

    [Fact]
    public void The_check_would_notice_the_day_the_separation_stopped_holding()
    {
        // A check that cannot fail is the green this project exists to withdraw, so the reader is
        // pointed at a project that does reference both — this suite, which is the harness.
        var suite = References("tests/Winwright.Tests/Winwright.Tests.csproj");

        Assert.True(Names(suite, "Winwright"), string.Join(", ", suite));
        Assert.True(Names(suite, "Winwright.InApp"), string.Join(", ", suite));
    }
}
