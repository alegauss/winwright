using System.Collections.ObjectModel;

namespace Winwright.Typing;

/// <summary>
/// One experiment this tool can run, as data. WW354.
/// </summary>
/// <param name="Name">The second word a person types, and the one thing spelled once.</param>
/// <param name="Task">The task it was built for, so a reader can find why it exists.</param>
/// <param name="Drives">What it drives and what it reports, in the sentence a person reads.</param>
/// <param name="NeedsRanges">
/// Whether the fixture has to be launched with <c>--ranges</c>. It is a property of the arm rather
/// than a branch beside the launch: the pane is built when the window is, so a run that asked for it
/// afterwards would be measuring a window that had just been rebuilt.
/// </param>
public sealed record TypingArm(string Name, string Task, string Drives, bool NeedsRanges = false)
{
    /// <summary>The line a listing shows, which is what a refusal prints and what the .cmd echoes.</summary>
    public override string ToString() => $"{Name,-8} {Task}: {Drives}";
}

/// <summary>
/// Every arm there is, named once. WW354.
/// <para>
/// The words were in two places and neither knew about the other: a paragraph each in
/// <c>run-typing.cmd</c>, where a person reads what the tool can do, and a <c>string.Equals</c> each
/// in <c>Program</c>, where the second word is parsed. WW341 added the fourth by editing both by
/// hand.
/// </para>
/// <para>
/// A word in the switch and not the .cmd is a measurement nobody can find. The other way round is
/// worse, and so is a typo: the word matched nothing, nothing refused it, and the tool ran its
/// default experiment and printed that experiment's numbers under the run a person started for
/// something else. This is the shape the verbs, the fixture flags, the desk facts, the capture arms
/// and the renderings are each already held to, reached at last by the one tool that sits outside
/// the suite.
/// </para>
/// <para>
/// The default is not in this list and that is deliberate. It is what a bare run does — the engine's
/// own send, measured against itself — so it is the absence of an arm rather than one of them, and
/// putting it here would make "no second word" a name somebody could misspell.
/// </para>
/// </summary>
public static class Arms
{
    /// <summary>Every arm, in the order <c>run-typing.cmd</c> introduces them.</summary>
    public static IReadOnlyList<TypingArm> All { get; } = new ReadOnlyCollection<TypingArm>(
    [
        new(
            "sweep",
            "WW312",
            "one SendInput per code unit at six spacings, reading what was injected beside what "
                + "arrived, so a fault inside WW310's band can be attributed to the send or to what "
                + "happens after it"),
        new(
            "delay",
            "WW329",
            "the send the engine does have with the pause it did not take — erase and send in one "
                + "act, then wait 0, 50 or 150ms before looking at the box — reporting the "
                + "milliseconds a round beside the rate"),
        new(
            "acts",
            "WW341",
            "the only arm that types nothing: a click, a traversal key and a nudge, each compared "
                + "against a reading taken afterwards with time to settle, which separates an act "
                + "read too early from one that never arrived",
            NeedsRanges: true),
        new(
            "provoke",
            "WW342",
            "the read taken apart rather than delayed — quiet, peek, poke and read — so what the "
                + "fifty milliseconds pay for is attributable to the call out of this process or to "
                + "the message loop run on the target's thread"),
    ]);

    /// <summary>
    /// The arm that word names, or null where it names none.
    /// <para>
    /// Null for an empty word too, which is the default experiment and not a miss —
    /// <see cref="Unrecognised" /> is what tells the two apart, because they are answered
    /// differently and a caller that could not separate them would refuse a bare run.
    /// </para>
    /// </summary>
    /// <param name="word">The second word a run was given.</param>
    public static TypingArm? Named(string? word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return null;

        var wanted = word.Trim();
        foreach (var arm in All)
        {
            if (string.Equals(arm.Name, wanted, StringComparison.OrdinalIgnoreCase))
                return arm;
        }

        return null;
    }

    /// <summary>Whether a word was given and names no arm, which is the one case that is refused.</summary>
    /// <param name="word">The second word a run was given.</param>
    public static bool Unrecognised(string? word) =>
        !string.IsNullOrWhiteSpace(word) && Named(word) is null;

    /// <summary>What a refusal says, with every arm there is under it.</summary>
    /// <param name="word">The word that named none.</param>
    public static IReadOnlyList<string> Refusing(string? word) => new ReadOnlyCollection<string>(
    [
        $"'{word?.Trim()}' is not an experiment this tool has, so nothing was run. A word it does "
            + "not recognise used to fall through to the default, which printed the engine's own "
            + "typing numbers under a run started for something else.",
        "There are these, and a bare run with none of them measures the engine's own send:",
        .. All.Select(one => $"  {one}"),
    ]);
}
