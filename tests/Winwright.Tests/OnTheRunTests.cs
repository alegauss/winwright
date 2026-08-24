using System.Xml.Linq;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW138. The roll call landed as its own process with its own tests, reached two ways: a step in
/// the workflow, which is unconditional and therefore real, and a script at the root, typed by
/// whoever remembers to type it. Every developer running the suite runs the middle command of the
/// three — the one every .NET project has — and got the same pass the check exists to withdraw.
/// <para>
/// A check that has to be invoked separately is not a check the project has, it is a check the
/// project offers. So the ordinary command carries it, and what is asserted here is that the
/// project still says so: the run this case is part of is itself the proof it works.
/// </para>
/// </summary>
public sealed class OnTheRunTests
{
    private static XElement Project()
    {
        var path = Checkout.At("tests", "Winwright.Tests", "Winwright.Tests.csproj");
        Assert.True(File.Exists(path), path);
        return XDocument.Load(path).Root!;
    }

    private static XElement Target() =>
        Assert.Single(
            Project().Elements().Where(one => one.Name.LocalName == "Target"),
            one => one.Attribute("Name")?.Value == "TakeTheRoll");

    [Fact]
    public void The_ordinary_command_carries_the_check()
    {
        // Hung off the test run itself, so a bare invocation is the checked one and there is
        // nothing shorter to type.
        Assert.Equal("VSTest", Target().Attribute("AfterTargets")?.Value);
    }

    [Fact]
    public void A_filtered_run_is_left_alone_because_it_is_short_on_purpose()
    {
        // The false red that would get the whole thing turned off: somebody narrowing to one case
        // is short of discovery deliberately, and reporting that as a lost host teaches them to
        // disable the check rather than to read it.
        var when = Target().Attribute("Condition")!.Value;

        Assert.Contains("'$(VSTestTestCaseFilter)' == ''", when);
    }

    [Fact]
    public void One_obvious_switch_turns_it_off_for_everything_else()
    {
        var when = Target().Attribute("Condition")!.Value;

        Assert.Contains("'$(RollCall)' == 'true'", when);
        Assert.Contains(
            Project().Elements().Where(one => one.Name.LocalName == "PropertyGroup").SelectMany(one => one.Elements()),
            one => one.Name.LocalName == "RollCall" && one.Value.Trim() == "true");
    }

    [Fact]
    public void The_run_asks_for_the_results_the_roll_reads()
    {
        // A run writes no results file unless somebody asks for one, and the roll reads results.
        // Without this default the check would be there and have nothing to check.
        var properties = Project()
            .Elements().Where(one => one.Name.LocalName == "PropertyGroup")
            .SelectMany(one => one.Elements())
            .ToList();

        var logger = Assert.Single(properties, one => one.Name.LocalName == "VSTestLogger");
        Assert.Contains("trx", logger.Value);
        Assert.Contains("$(VSTestLogger)' == ''", logger.Attribute("Condition")!.Value);
    }

    [Fact]
    public void The_refusal_carries_what_the_roll_said_rather_than_what_msbuild_says()
    {
        // This block's criterion is that a degraded run is legible without reading the log, and
        // MSBuild's own words for a command that exited non-zero are "the command exited with
        // code 1". What the roll said is the legible part, so the error carries it.
        var error = Assert.Single(Target().Elements(), one => one.Name.LocalName == "Error");

        Assert.Contains("@(RollCallSaid", error.Attribute("Text")!.Value);
        Assert.Contains("the roll call refused this run", error.Attribute("Text")!.Value);
    }

    [Fact]
    public void Each_run_reads_and_writes_a_directory_of_its_own()
    {
        // WW150. Both files defaulted to TestResults itself, so two runs on one machine had each
        // roll call reading one run's listing against the other's results — which comes out as
        // names discovered and never recorded, the exact shape of a host that died. A check
        // invented to stop a false green would have produced a false red.
        var properties = Project()
            .Elements().Where(one => one.Name.LocalName == "PropertyGroup")
            .SelectMany(one => one.Elements())
            .ToList();

        var where = Assert.Single(properties, one => one.Name.LocalName == "RollCallResults");

        // Under TestResults and never TestResults itself, which is the whole of the repair.
        Assert.EndsWith(@"TestResults\$(RollCallRun)", where.Value, StringComparison.Ordinal);

        // Named by the run and overridable by a caller that has to know the name in advance, which
        // the VM runner does: it copies the files back out of the guest by an exact path.
        var named = Assert.Single(properties, one => one.Name.LocalName == "RollCallRun");

        Assert.Contains("'$(RollCallRun)' == ''", named.Attribute("Condition")!.Value);
        Assert.Contains("HHmmss-fff", named.Value, StringComparison.Ordinal);
        Assert.Contains("Guid", named.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void The_listing_is_written_into_a_directory_this_run_made()
    {
        // The redirect writes a file into that directory, and it existing because VSTest happened
        // to put the trx there first is true today and not a thing to depend on one target away.
        Assert.Contains(Target().Elements(), one => one.Name.LocalName == "MakeDir");
    }

    [Fact]
    public void Discovery_costs_no_second_build()
    {
        // A check that doubles the wait is a check somebody disables. The listing comes off the
        // assembly that was just run, and the inner call turns this target off so it does not
        // list the tests forever.
        var listing = Target().Elements().First(one => one.Name.LocalName == "Exec").Attribute("Command")!.Value;

        Assert.Contains("--no-build", listing);
        Assert.Contains("--list-tests", listing);
        Assert.Contains("-p:RollCall=false", listing);
    }
}
