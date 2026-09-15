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
/// The disagreement is quiet in the worst direction. The driver decides whether the runner reached
/// its end by matching the runner's last line, so a runner that reworded that line reads as one that
/// stopped early — exit 2, nothing observed — and a broken arm is reported as a desk that refused.
/// </para>
/// </summary>
public sealed class RunnerArmsTests
{
    /// <summary>
    /// Every fragment the driver reads the runner by, spelled as both files spell it, and what the
    /// driver concludes from it.
    /// </summary>
    private static readonly (string Spelled, string Means)[] Read =
    [
        ("run-tests-vm: the guest run passed.", "the runner reached its end on a green guest run"),
        ("run-tests-vm: the guest run exited ", "the runner reached its end on a red one, and with which code"),
        ("  blame       ", "the gather ran in the guest and named what the collector left"),
        ("'hangdump.dmp'", "the name the kept dump comes back under"),
        ("'sequence.xml'", "the name the kept sequence comes back under"),
        ("'TestResults\\vm'", "where the runner puts what came back, under the tree it carried"),
    ];

    /// <summary>The runner's parameters the driver passes, which are WW227's door into its failure path.</summary>
    private static readonly string[] Passed = ["Tree", "Name", "Run", "ResultsIn", "Bring"];

    [Fact]
    public void Every_sentence_the_driver_reads_is_one_the_runner_still_writes()
    {
        var runner = Runner();
        var driver = Driver();

        Assert.All(
            Read,
            one =>
            {
                Assert.True(
                    runner.Contains(one.Spelled, StringComparison.Ordinal),
                    $"the runner no longer writes '{one.Spelled}', so the driver cannot tell {one.Means}");

                Assert.True(
                    driver.Contains(one.Spelled, StringComparison.Ordinal),
                    $"the driver no longer reads '{one.Spelled}', so this catalogue names a reading nothing takes");
            });
    }

    [Fact]
    public void Every_parameter_the_driver_passes_is_one_the_runner_declares()
    {
        var runner = Runner();
        var driver = Driver();

        Assert.All(
            Passed,
            one =>
            {
                // A string or a list of them: -Bring is the one that takes several.
                Assert.Matches(
                    new Regex($@"\[string(\[\])?\] \${one}\b", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)),
                    runner);
                Assert.Contains($"-{one} ", driver, StringComparison.Ordinal);
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
        var refusal = Regex.Match(
            Runner(),
            @"function Refuse \{.*?exit (?<code>\d+)",
            RegexOptions.Singleline | RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));

        var verdict = Regex.Match(
            Driver(),
            @"^\$verdict = (?<code>\d+)\s*$",
            RegexOptions.Multiline | RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));

        Assert.True(refusal.Success, "the runner's Refuse no longer ends in an exit code this case can read");
        Assert.True(verdict.Success, "the driver no longer declares the verdict its scratch run exits with");

        Assert.NotEqual(refusal.Groups["code"].Value, verdict.Groups["code"].Value);
    }

    private static string Runner() => File.ReadAllText(Checkout.At("tools", "run-tests-vm.ps1"));

    private static string Driver() => File.ReadAllText(Checkout.At("tools", "runner-arms.ps1"));
}
