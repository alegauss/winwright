using System.Globalization;
using System.Xml.Linq;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW373. The bound on a single case, and that it is a bound rather than a budget.
/// <para>
/// A case that deadlocks used to wedge the run instead of failing it. Nothing ended it but a person
/// noticing and killing testhost inside the guest — measured on WW361, where the run had been going
/// long enough to have finished twice, and the guest console showed two windows and nothing else.
/// </para>
/// <para>
/// What made it cost a session rather than a minute is that a wedge and a slow suite read
/// identically from outside. The honest response to a run taking too long is to wait longer, and
/// what separates the two is a desk somebody is watching.
/// </para>
/// <para>
/// A property in a project file is configuration and not code, and this is here because it is
/// exactly the kind of line that goes away in an edit nobody reviews: a timeout deleted fails
/// nothing, and the next wedge is discovered the way the last one was.
/// </para>
/// </summary>
public sealed class WedgeTests
{
    /// <summary>This suite's own project, which is where the bound is declared.</summary>
    private static XDocument Project() =>
        XDocument.Parse(File.ReadAllText(Checkout.At("tests", "Winwright.Tests", "Winwright.Tests.csproj")));

    /// <summary>What one property is set to, or null where the project does not set it.</summary>
    /// <param name="named">The property's name, as MSBuild spells it.</param>
    private static string? Declared(string named) =>
        Project().Descendants().FirstOrDefault(one => one.Name.LocalName == named)?.Value.Trim();

    [Fact]
    public void A_case_that_wedges_is_ended_and_named_rather_than_waited_on()
    {
        Assert.Equal("true", Declared("VSTestBlameHang"));

        // And what it leaves behind. The sequence file comes with the abort either way and is what
        // turns a wedge into a red with an address; the dump is what says why one happened, which is
        // the question WW361 needed a stack for. `none` would pass the line above and answer that
        // question with nothing.
        Assert.Equal("mini", Declared("VSTestBlameHangDumpType"));
    }

    [Fact]
    public void The_bound_is_far_above_the_slowest_case_this_suite_honestly_has()
    {
        // The one way a timeout goes wrong: it stops being a bound and becomes the thing that
        // decides a red. Measured on the guest — SuiteRunTests' capture project runs 158 seconds and
        // the next slowest is fifteen — so anything under about three minutes is a case away from
        // failing on a busy machine, and the number wants to be several times that rather than a
        // little over it.
        var said = Declared("VSTestBlameHangTimeout");

        Assert.False(string.IsNullOrWhiteSpace(said), "this suite declares no bound on a single case");

        var minutes = said!.EndsWith('m')
            ? double.Parse(said[..^1], CultureInfo.InvariantCulture)
            : TimeSpan.Parse(said, CultureInfo.InvariantCulture).TotalMinutes;

        Assert.True(
            minutes >= 8,
            $"the bound on a single case is {said}, which is close enough to the slowest honest one "
                + "to be what decides a red on a busy guest");
    }
}
