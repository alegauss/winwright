namespace Winwright.Tests;

/// <summary>
/// WW431. How a case inside a serial class says it needs no desk, so the gate can answer it here.
/// <para>
/// WW417's gate runs the classes outside the serial collection, which is the only division this
/// project had: a class needs the desk or it does not. That is the right unit for the collection,
/// which exists to stop two classes fighting over one foreground, and the wrong one for the gate.
/// <c>DeskProbeTests</c> is serial and holds the cases that read the runner's own source; each of
/// them could answer on any machine, and each waits for a guest.
/// </para>
/// <para>
/// Measured twice inside the tasks that were about it. WW414 changed a sentence the skill pins and
/// the guest said so eleven minutes later; WW428 moved a number a case in <c>DeskProbeTests</c>
/// reads, and the same class's silence cost another run.
/// </para>
/// <para>
/// Marked rather than derived, and marked on the cases that need nothing rather than on the ones
/// that need a desk: a case nobody marked stays in the guest, which is the direction that fails
/// safe. What the mark claims is narrow and checked — the case reads files and asserts on what it
/// read, and the class it sits in builds nothing that touches the desk either, because xUnit builds
/// that class for every case it runs.
/// </para>
/// </summary>
internal static class NoDesk
{
    /// <summary>The trait's name, which is what the gate filters on.</summary>
    internal const string Key = "desk";

    /// <summary>
    /// What a case that needs none says.
    /// <para>
    /// The word is "free" and it must not be "none", which was the first spelling and is the one
    /// value this filter cannot be given: VSTest reads <c>desk=none</c> as every case carrying no
    /// such trait, so the gate would have run the whole suite on the operator's desk — the one
    /// failure WW417 says it must never produce. Measured with <c>--list-tests</c> rather than
    /// reasoned about: <c>desk=none</c> selected 2099 of 2115 cases, which is the suite less the
    /// sixteen marked, and <c>desk=free</c> selects the sixteen.
    /// </para>
    /// <para>
    /// Which is why <see cref="NoDeskTests" /> holds the word the gate filters on against the word
    /// the cases carry: a mark and a filter that disagree are a gate that runs everything or
    /// nothing, and both of those look like a gate that is working.
    /// </para>
    /// </summary>
    internal const string Free = "free";

    /// <summary>
    /// What a case carrying the mark may not reach for, and what a class holding one may not build.
    /// <para>
    /// The desk calls are <see cref="DeskAsks" />'s, which is the list this project already holds
    /// for the readings that depend on a foreground, plus the ways a case puts something on the
    /// screen. A marked case naming any of them is red on the host before it can be wrong there.
    /// </para>
    /// </summary>
    internal static IReadOnlyList<string> Reaching { get; } =
    [
        .. DeskAsks.Calls.Select(one => one.Call).Distinct(StringComparer.Ordinal),
        "PumpedDialog.Open(",
        "TrayIconFixture.Add(",
        "Attachable.Launch(",
        "Fixture.Started(",
        "Launched(",
        "Pointer.",
        "Keyboard.Type(",
        "Menu.Enter(",
        "Act.Invoke(",
        "Inspect.Window(",
    ];
}
