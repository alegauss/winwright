using System.Text.RegularExpressions;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW430. The reading that says what the gate's half costs the guest.
/// <para>
/// The gate answers those cases on the host in seconds and the guest runs them again, and the
/// obvious saving is not available: WW117's roll call refuses a run where the cases discovered and
/// the cases recorded disagree, which is why a green here means what it says. What was missing is
/// the number — the suite reports one duration, so nobody could say whether the second run of them
/// cost the guest twenty seconds or two minutes.
/// </para>
/// <para>
/// What this holds is that the reading is taken and taken against the gate's own derivation. A
/// second list of classes that need no desk is the drift WW424 is about, and this one would be
/// wrong in the direction that hides things: a class left off it reads as a case that needed a desk
/// all along.
/// </para>
/// </summary>
public sealed class GatedCostTests
{
    private static string Runner() => File.ReadAllText(Checkout.At("tools", "run-tests-vm.ps1"));

    [Fact]
    public void The_runner_reads_what_the_half_that_needs_no_desk_cost()
    {
        var runner = Runner();

        Assert.Contains("  gated       ", runner, StringComparison.Ordinal);
        Assert.Contains("Get-GatedClasses -Suite", runner, StringComparison.Ordinal);

        // Case time, and the sentence says so: the desk-free cases run in parallel in the guest, so
        // what not running them would give back is less than the number and never more.
        Assert.Contains("spent in cases", runner, StringComparison.Ordinal);
    }

    [Fact]
    public void The_classes_it_calls_desk_free_are_the_gates_own_and_not_a_second_list()
    {
        // WW424's shape, one file over: the gate derives the split from the collection this project
        // already uses to say which classes need a desk, and a runner that named them itself would
        // be a list nothing counts.
        var runner = Runner();

        var named = Regex
            .Matches(runner, @"'\w+Tests'", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))
            .Select(one => one.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            named.Count == 0,
            $"the runner names {named.Count} test class(es) of its own rather than asking the gate: "
                + string.Join(", ", named));
    }

    [Fact]
    public void The_gate_still_derives_that_split_from_the_collection_the_suite_declares()
    {
        // The other half of the same claim, and the control on it: the reading above is worth
        // nothing if what it asks has stopped answering. `Get-GatedClasses` skips a file carrying
        // the serial collection and takes every other public sealed class.
        var gate = File.ReadAllText(Checkout.At("tools", "host-gate.ps1"));

        Assert.Contains("function Get-GatedClasses", gate, StringComparison.Ordinal);
        Assert.Contains(@"\[Collection\(WindowFixture\.Serial\)\]", gate, StringComparison.Ordinal);

        // And this class is one of the ones it would take, which is what makes the reading cover
        // anything at all: it needs no desk and says so by declaring no collection.
        //
        // Asked as a pattern and never as the text. The gate skips a file whose text holds that
        // attribute anywhere — deliberately coarse, so it runs less than it could and never more —
        // so a case spelling it plainly would take its own class out of the half it is about, which
        // is how this first went red.
        var mine = File.ReadAllText(Checkout.At("tests", "Winwright.Tests", $"{nameof(GatedCostTests)}.cs"));

        Assert.Contains($"public sealed class {nameof(GatedCostTests)}", mine, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex(@"(?m)^\[Collection\(", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)), mine);
    }
}
