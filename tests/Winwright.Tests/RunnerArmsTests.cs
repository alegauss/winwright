using System.Text.RegularExpressions;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW420. What <c>tools/runner-arms.ps1</c> reads out of the runner, held to what the runner writes.
/// <para>
/// The driver proves the runner's failure arms by running it against a scratch tree and reading its
/// output, and it needs a VM to do that, so it runs when somebody runs it and not with this suite.
/// What can be asked here, on any host and in a second, is whether the two scripts still agree
/// about the words between them.
/// </para>
/// <para>
/// The disagreement is quiet in the worst direction. The driver decides whether the runner reached an
/// arm by matching the runner's sentence for it, so a runner that reworded that sentence reads as one
/// that never got there — exit 2, nothing observed — and a broken arm is reported as a desk that
/// refused.
/// </para>
/// </summary>
public sealed class RunnerArmsTests
{
    /// <summary>
    /// Every fragment the driver reads the runner by: the script that writes it, the fragment as both
    /// files spell it, and what the driver concludes from it.
    /// </summary>
    private static readonly (string Writer, string Spelled, string Means)[] Read =
    [
        ("run-tests-vm.ps1", "run-tests-vm: the guest run passed.", "the runner reached its end on a green guest run"),
        ("run-tests-vm.ps1", "run-tests-vm: the guest run exited ", "the runner reached its end on a red one, and with which code"),
        ("run-tests-vm.ps1", "  blame       ", "the gather ran in the guest and named what the collector left"),
        ("run-tests-vm.ps1", "'hangdump.dmp'", "the name the kept dump comes back under"),
        ("run-tests-vm.ps1", "'sequence.xml'", "the name the kept sequence comes back under"),
        ("run-tests-vm.ps1", "'TestResults\\vm'", "where the runner puts what came back, under the tree it carried"),
        ("run-tests-vm.ps1", "the guest run did not answer within ", "the bound gave up on a run that outlived it"),
        ("run-tests-vm.ps1", "the guest tree is ", "the sync refused, on the arm that says who has the tree"),
        ("run-tests-vm.ps1", "held open by ", "the sync's refusal named what holds the tree, rather than nothing"),
        ("holders.ps1", "runs from ", "the holder walk named a process by the executable it runs from under the tree"),
    ];

    /// <summary>The runner's parameters the driver passes, which are WW227's door and WW386's bound.</summary>
    private static readonly string[] Passed = ["Tree", "Name", "Run", "ResultsIn", "Bring", "Bound"];

    [Fact]
    public void Every_sentence_the_driver_reads_is_one_the_runner_still_writes()
    {
        var driver = Driver();

        Assert.All(
            Read,
            one =>
            {
                Assert.True(
                    Tool(one.Writer).Contains(one.Spelled, StringComparison.Ordinal),
                    $"{one.Writer} no longer writes '{one.Spelled}', so the driver cannot tell {one.Means}");

                Assert.True(
                    driver.Contains(one.Spelled, StringComparison.Ordinal),
                    $"the driver no longer reads '{one.Spelled}', so this catalogue names a reading nothing takes");
            });
    }

    [Fact]
    public void Every_parameter_the_driver_passes_is_one_the_runner_declares()
    {
        var runner = Tool("run-tests-vm.ps1");
        var driver = Driver();

        Assert.All(
            Passed,
            one =>
            {
                // A string, a list of them, or a number: -Bring takes several and -Bound is minutes.
                Assert.Matches(
                    new Regex($@"\[(string|int)(\[\])?\] \${one}\b", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)),
                    runner);
                Assert.Contains($"'-{one}'", driver, StringComparison.Ordinal);
            });

        // A parameter bound by -File that the runner does not declare is an error before anything
        // starts, which the driver would read as the runner stopping early and report as a hole.
    }

    [Fact]
    public void The_scratch_verdict_is_not_the_number_a_refusal_exits_with()
    {
        // The driver tells the guest's verdict carried through from the runner never starting by the
        // exit code. The runner's Refuse exits with one fixed number, so a scratch run exiting that
        // same number reads as a refusal, and every arm after it as a pass nobody observed.
        var refusal = Refusal();

        var verdict = Regex.Match(
            Driver(),
            @"^\$verdict = (?<code>\d+)\s*$",
            RegexOptions.Multiline | RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));

        Assert.True(verdict.Success, "the driver no longer declares the verdict its scratch run exits with");
        Assert.NotEqual(refusal, verdict.Groups["code"].Value);
    }

    [Fact]
    public void The_code_the_driver_expects_of_a_provoked_refusal_is_the_one_a_refusal_exits_with()
    {
        // The other side of the case above. The bound and the held tree are refusals on purpose, and
        // the driver holds each to the runner's refusal code: a Refuse that moved to another number
        // would turn both arms red about a runner that refused exactly as it should.
        var expected = Regex.Matches(
            Driver(),
            @"\.Code -eq (?<code>\d+)\)",
            RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));

        Assert.NotEmpty(expected);
        Assert.All(expected, one => Assert.Equal(Refusal(), one.Groups["code"].Value));
    }

    /// <summary>The number the runner's Refuse exits with.</summary>
    private static string Refusal()
    {
        var refusal = Regex.Match(
            Tool("run-tests-vm.ps1"),
            @"function Refuse \{.*?exit (?<code>\d+)",
            RegexOptions.Singleline | RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));

        Assert.True(refusal.Success, "the runner's Refuse no longer ends in an exit code this case can read");
        return refusal.Groups["code"].Value;
    }

    private static string Tool(string named) => File.ReadAllText(Checkout.At("tools", named));

    private static string Driver() => Tool("runner-arms.ps1");
}
