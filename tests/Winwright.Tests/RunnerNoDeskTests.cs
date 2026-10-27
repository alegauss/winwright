using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW581. A guest run that opens no window, and says so with <c>-NoDesk</c>.
/// <para>
/// Found by Modelado, whose CLI-only guest runs (model downloads, demo reconstructions) were refused
/// over a desk they never touch. Held to the runner's own source here: the switch is declared, every
/// foreground reading sits behind it, and the claim is taken before it so two runs never interleave.
/// The run itself is proven in the guest.
/// </para>
/// </summary>
public sealed class RunnerNoDeskTests
{
    private static string Runner() => File.ReadAllText(Checkout.At("tools", "run-tests-vm.ps1"));

    [Fact]
    public void The_switch_puts_every_foreground_reading_behind_it_and_keeps_the_claim()
    {
        var runner = Runner();

        Assert.Contains("[switch] $NoDesk", runner, StringComparison.Ordinal);

        var gate = runner.IndexOf("if ($NoDesk) {", StringComparison.Ordinal);
        var kept = runner.IndexOf("$script:DeskWhenItStarted = $desk.State", StringComparison.Ordinal);
        Assert.True(gate > 0 && kept > gate, "the switch does not stand before the desk it skips");

        // Every reading and repair of the foreground is inside the span the switch skips.
        foreach (var call in new[] { "= Read-GuestDesk -Vmx", "= Read-GuestTake -Vmx", "= Clear-GuestDesk -Vmx", "= Restart-GuestShell -Vmx" })
        {
            var at = runner.IndexOf(call, gate, StringComparison.Ordinal);
            Assert.True(at > gate && at < kept, $"'{call}' is outside what -NoDesk skips");
        }

        // And the claim is taken before any of it, so the guest is still this run's alone.
        var claimed = runner.IndexOf("Enter-DeskClaim", runner.IndexOf("function Enter-DeskClaim", StringComparison.Ordinal) + 30, StringComparison.Ordinal);
        Assert.True(claimed > 0 && claimed < gate, "the desk claim is not taken before the switch");
    }
}
