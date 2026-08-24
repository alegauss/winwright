using System.Xml.Linq;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW178. Block K's second criterion says the fixture runs with no account, no network, no second
/// display and no real data, on a clean checkout of this repository alone. Its other two are checked
/// in both directions against the assemblies; this one was checked nowhere.
/// <para>
/// It was also true when this was written, which is what makes this a read-back and not a repair.
/// The difference matters because the claim is one of the three deciding whether a block is
/// finished, and the reading was resting on whoever last looked — the failure WW176 measured.
/// </para>
/// <para>
/// Read off the sources rather than off the built assembly, deliberately. What the criterion is
/// about is what the fixture <em>asks of a machine</em>, and the framework it is built on references
/// networking and identity whether or not a line of this fixture calls any of it. A check against
/// the reference graph would answer about the framework; a check against the fixture's own text
/// answers about the fixture.
/// </para>
/// </summary>
public sealed class FixtureNeedsTests
{
    /// <summary>
    /// What asking the machine for something looks like in source, and what each one would mean.
    /// <para>
    /// Named rather than counted: a case that failed with "the fixture reaches for something" and
    /// no more is one whose reader has to go and find out what, which is the shape of report this
    /// project keeps refusing.
    /// </para>
    /// </summary>
    private static readonly (string Reaching, string Means)[] Asks =
    [
        ("HttpClient", "the network"),
        ("WebClient", "the network"),
        ("Socket", "the network"),
        ("Dns.", "the network"),
        ("WebRequest", "the network"),
        ("Environment.UserName", "an account"),
        ("Environment.UserDomainName", "an account"),
        ("WindowsIdentity", "an account"),
        ("CredentialCache", "an account"),
        ("Screen.AllScreens", "a second display"),
        ("EnumDisplayMonitors", "a second display"),
        ("MonitorFromWindow", "a second display"),
        ("VirtualScreen", "a second display"),
        ("SpecialFolder", "somebody's real data"),
        ("GetFolderPath", "somebody's real data"),
    ];

    private const string Project = "src/Winwright.Fixture/Winwright.Fixture.csproj";

    private static string Repository() => Checkout.Root;

    private static IReadOnlyList<string> Sources() =>
        Checkout.SourcesIn(Checkout.At("src", "Winwright.Fixture")).ToList();

    /// <summary>Every reference the fixture's project file declares, by the name it declares.</summary>
    private static IReadOnlyList<(string Kind, string Include)> Declared()
    {
        var document = XDocument.Load(Path.Combine(Repository(), Project));
        return document.Root!
            .Elements().Where(one => one.Name.LocalName == "ItemGroup")
            .SelectMany(group => group.Elements())
            .Where(one => one.Name.LocalName is "ProjectReference" or "PackageReference")
            .Select(one => (one.Name.LocalName, one.Attribute("Include")?.Value?.Trim() ?? ""))
            .Where(one => one.Item2.Length > 0)
            .ToList();
    }

    [Fact]
    public void The_fixture_asks_the_machine_for_nothing_the_criterion_says_it_does_not()
    {
        var reaching = new List<string>();

        foreach (var file in Sources())
        {
            // WW202. Read as code, so a comment explaining why the fixture must not reach for
            // somebody's real data is not itself reported as the fixture reaching for it.
            var text = string.Join('\n', File.ReadLines(file).Select(Checkout.Code));
            foreach (var (ask, means) in Asks)
            {
                if (text.Contains(ask, StringComparison.Ordinal))
                    reaching.Add($"{Path.GetFileName(file)} names {ask}, which is {means}");
            }
        }

        Assert.True(reaching.Count == 0, string.Join("; ", reaching));
    }

    [Fact]
    public void A_clean_checkout_of_this_repository_is_the_whole_of_what_it_needs()
    {
        // No package at all, and one project reference: the in-app half, which is what an adopting
        // application takes. Anything else is a thing somebody has to install before the proving
        // ground will run, and a proving ground with a prerequisite proves it on fewer machines.
        var declared = Declared();

        Assert.DoesNotContain(declared, one => one.Kind == "PackageReference");

        var only = Assert.Single(declared, one => one.Kind == "ProjectReference");
        Assert.EndsWith("Winwright.InApp.csproj", only.Include, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void The_store_it_writes_is_one_it_was_handed_and_never_one_it_went_looking_for()
    {
        // The "no real data" half. The fixture has a store only because a case gave it a directory:
        // the flag takes a path, and nothing in the sources reaches for a well-known folder — which
        // is the line between a fixture that risks nothing and one that risks somebody's settings.
        var flags = Fixture.Catalogue();

        Assert.Contains("--store=<", flags, StringComparison.Ordinal);
        Assert.Contains("--mutate", flags, StringComparison.Ordinal);
        Assert.Contains("[needs --store]", flags, StringComparison.Ordinal);
    }

    [Fact]
    public void The_check_would_notice_the_day_the_fixture_started_asking()
    {
        // A check that cannot fail is the green this project exists to withdraw. The engine reads
        // the desk, so it names several of these on purpose — which makes it the control: the same
        // reading pointed at a tree that does ask comes back with something.
        // WW202. Checkout's walk and Checkout's reading, both. This hand-rolled the enumeration and
        // left out obj while keeping bin — which is the exclusion WW193 was filed over — and read
        // the sources raw, which is what that task was about. A control that reads differently from
        // the check it controls is not a control.
        var engine = Checkout.SourcesIn(Checkout.Engine).ToList();

        var found = engine
            .SelectMany(file => Asks.Where(ask => File.ReadLines(file)
                .Select(Checkout.Code)
                .Any(line => line.Contains(ask.Reaching, StringComparison.Ordinal))))
            .Select(one => one.Reaching)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.True(found.Count > 0, "the engine asks the machine for none of these either, so this check proves nothing");
    }

    [Fact]
    public void Every_ask_says_what_it_would_mean_rather_than_only_that_it_is_forbidden()
    {
        Assert.All(
            Asks,
            one =>
            {
                Assert.False(string.IsNullOrWhiteSpace(one.Reaching));
                Assert.Contains(
                    one.Means,
                    new[] { "the network", "an account", "a second display", "somebody's real data" });
            });

        // And all four halves of the criterion are covered, so the list cannot quietly shrink to
        // the easy ones and still read as a check of the whole claim.
        Assert.Equal(4, Asks.Select(one => one.Means).Distinct(StringComparer.Ordinal).Count());
    }
}
