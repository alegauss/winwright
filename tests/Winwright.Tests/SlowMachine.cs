using Winwright.Locating;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// What a case does when a deadline this suite chose ran out and nothing about the thing under test
/// was ever shown to be wrong.
/// <para>
/// WW211. <c>Waits.Until("wrote", ...)</c> failed with "pid N never wrote what it drew, or wrote it
/// and it read as nothing". On a guest busy with its own antivirus that sentence is false twice
/// over: the fixture wrote, and this suite stopped looking at 5000 ms. WW203 read it correctly and
/// repaired half of it — the cold start and the layout now have their own budget — and two guest
/// runs since have still come in at 5 s, on two cases whose only shared line is the wait.
/// </para>
/// <para>
/// Raising the number is the move that keeps this coming back, because the number is not the fault.
/// The fault is the verdict. A deadline this suite chose is reporting a claim about the application
/// under test, which is the misattribution <see cref="BusyDesk" /> exists to stop everywhere else.
/// </para>
/// <para>
/// It is not <see cref="BusyDesk" /> and cannot be. That gate is <c>DeskFacts.Names</c> — the engine
/// says which of its conditions are the desk's, and a slow machine is not one of its readings at
/// all. Borrowing the gate would mean declaring an engine precondition to excuse a wait in a test
/// suite, which is the dependency the wrong way round. So this carries its own gate, and it is
/// narrower rather than looser.
/// </para>
/// <para>
/// The gate is what separates the two ways a wait ends. A write that landed and read as nothing is a
/// fact about the fixture and stays red — that is the confusion WW164 was filed about, and excusing
/// it would withdraw the check WW164 added. A write that never landed at all, inside a budget this
/// file declares, is a machine that was not given time. The caller proves which one it met before it
/// is allowed to say either.
/// </para>
/// </summary>
internal static class SlowMachine
{
    /// <summary>
    /// That the hole is honest: a deadline this suite declared, one that actually ran out, and one
    /// whose absence a reader can act on.
    /// </summary>
    /// <param name="named">Which declared deadline ran out.</param>
    /// <param name="waited">What the wait answered.</param>
    /// <param name="absent">
    /// That nothing was produced at all — the caller's own proof that this is the machine and not
    /// the thing under test. A caller passing false here is asking to excuse a real red.
    /// </param>
    internal static void Excusing(string named, Waited waited, bool absent)
    {
        ArgumentNullException.ThrowIfNull(waited);

        // Declared here and not typed at the call site, which is the whole of WW143's argument: a
        // budget nobody can find is a budget nobody can tune, and one invented on the spot is a
        // number this suite would then be excusing itself against.
        //
        // Asked of the declaration rather than by calling For and catching what it throws. A refusal
        // caught here would reach a reader as a broken harness, when what happened is that somebody
        // excused a wait this file never declared — and that is the finding, not the accident.
        Assert.True(
            Waits.Declared.All.ContainsKey(named),
            $"'{named}' is not a deadline this suite declares: "
                + string.Join(", ", Waits.Declared.All.Keys.Order(StringComparer.Ordinal)));

        var budget = Waits.Declared.For(named);

        Assert.False(waited.Happened, $"'{named}' happened, so there is nothing to excuse");

        // It used the budget rather than answering false and stopping. A wait that came back early
        // did not run out of time, and whatever went wrong is not the machine's doing.
        Assert.True(
            waited.WaitedMs >= budget,
            $"'{named}' gave up after {waited.WaitedMs}ms of the {budget}ms this suite declares, so "
                + "it did not run out of time and this is not the machine's to excuse");

        // And it looked more than once, so the budget was spent looking rather than slept through.
        Assert.True(waited.Polls > 1, $"'{named}' looked {waited.Polls} time(s) in {waited.WaitedMs}ms");

        Assert.True(
            absent,
            $"'{named}' ran out and the thing waited for was partly there, which is a fact about "
                + "what wrote it rather than about how long this suite waited");
    }

    /// <summary>The sentence a run that met one prints, which is what makes the hole readable.</summary>
    /// <param name="named">Which declared deadline ran out.</param>
    /// <param name="what">What was being waited for.</param>
    /// <param name="waited">What the wait answered.</param>
    internal static string Sentence(string named, string what, Waited waited)
    {
        ArgumentNullException.ThrowIfNull(waited);

        return $"unchecked: {what} — nothing after {waited.WaitedMs}ms over {waited.Polls} look(s), "
            + $"against the {Waits.Declared.For(named)}ms this suite declares for '{named}'. This "
            + "machine was not given time; nothing here is a claim about the fixture.";
    }
}
