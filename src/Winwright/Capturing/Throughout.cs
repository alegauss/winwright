using Winwright.Tracing;
using Winwright.Verdicts;
using Winwright.Windowing;

namespace Winwright.Capturing;

/// <summary>
/// What stood over a region on both sides of a capture, rather than at the instant before it.
/// <para>
/// WW195. WW187 put the readings where a caller cannot forget them and takes them immediately
/// before the write. That is the closest observable instant and it is written down as such — but it
/// is not the instant the picture was taken. A window that arrives between the reading and the write
/// is in the copy and in nothing else, and the receipt says the region was clear, because it was, a
/// moment earlier. Smaller than the hole WW38 closed and the same shape: a capture reported as
/// proving something it does not.
/// </para>
/// <para>
/// Cheap to close because the reading is cheap. <see cref="Obstruction.Reading" /> walks the z order
/// down to the window under test and stops; taking it twice costs one more walk. Clear before and
/// clear after is clear throughout — unless something arrived and left inside the take, which is a
/// claim about a window nobody could photograph either way.
/// </para>
/// <para>
/// Three answers and not two, which is why this is a shape rather than a second field. Clear both
/// times is what the receipt already meant. Covered before is what WW40 refuses. Covered afterwards
/// and not before is its own thing: the desk changed under the capture, which is neither the
/// window's fault nor a reading nobody took.
/// </para>
/// </summary>
/// <param name="Before">What stood over the region immediately before the take.</param>
/// <param name="After">
/// And immediately after. Null where only one reading was taken, which is a real answer and not a
/// default: a caller that read once cannot claim the region held still, and this says so instead of
/// letting the single reading stand for both.
/// </param>
public sealed record RegionThroughout(Obstruction Before, Obstruction? After)
{
    /// <summary>What this reading is called wherever it is reported.</summary>
    public const string Named = "the region held still while the capture was taken";

    /// <summary>Whether the region was read on both sides at all.</summary>
    public bool Twice => After is not null;

    /// <summary>Whether the readings that were taken were taken.</summary>
    public bool Was => Before.Was && (After is null || After.Was);

    /// <summary>
    /// Whether nothing stood over it in any reading taken. Weaker than <see cref="HeldStill" /> on
    /// purpose: one clear reading is a true thing to have read, and it is not the claim this type is
    /// named for.
    /// </summary>
    public bool Clear => Was && Before.Clear && (After is null || After.Clear);

    /// <summary>
    /// Whether the region really held still: read at both ends, clear at both. This is the claim
    /// <see cref="Named" /> makes, and a capture read once cannot make it.
    /// </summary>
    public bool HeldStill => Twice && Clear;

    /// <summary>
    /// Whether the desk changed under the capture: clear when it started and covered when it
    /// finished. This is the case the second reading exists for.
    /// </summary>
    public bool Changed => Before.Clear && After is { Was: true, Clear: false };

    /// <summary>Whether it was already covered before the take, which WW40 already refuses.</summary>
    public bool WasCovered => Before is { Was: true, Clear: false };

    /// <summary>What was read, said in whichever of the four ways it went.</summary>
    public string Sentence()
    {
        if (!Twice)
            return $"the region was read once and not again, so nothing says whether it held still "
                + $"while the capture was taken: {Before.Sentence()}";

        if (WasCovered)
            return Before.Sentence();

        if (Changed)
            return $"the region was clear when the capture started and not when it finished, so the "
                + $"desk changed under it: {After!.Sentence()}";

        return Was
            ? $"nothing stood over {Before.Region} at either end of the capture."
            : $"the region could not be read on both sides of the capture: {Before.Sentence()} {After!.Sentence()}";
    }

    /// <inheritdoc cref="Sentence" />
    public override string ToString() => Sentence();

    /// <summary>
    /// The three-state reading. Not read where either end is missing — a capture read once did not
    /// observe whether the region held still, and saying it did is the whole defect.
    /// </summary>
    public Finding AsFinding() => new(Named, Twice && Was ? Clear : null, Sentence());

    /// <summary>
    /// The result a verdict counts. A window somebody else moved is the desk's whichever end it
    /// arrived at, so this is a hole either way rather than a defect in the code under test.
    /// </summary>
    /// <param name="named">What the assertion claims, as the scenario spells it.</param>
    public AssertionResult AsAssertion(string named) => HeldStill
        ? AssertionResult.Pass(named, Sentence())
        : AssertionResult.Unchecked(named, Precondition.Absent(Obstruction.PreconditionName, Sentence()));

    /// <summary>The step a trace records.</summary>
    public TraceStep AsTraceStep(string named) => new()
    {
        Verb = "read what stood over the region, before and after",
        Locator = named,
        Resolved = Before.Region.ToString(),
        Pattern = Twice ? "the z order above the window, twice" : "the z order above the window, once",
        ReadBack = Was ? $"{Before.Over.Count} before, {After?.Over.Count.ToString() ?? "not read"} after" : null,
        Verdict = HeldStill ? StepVerdict.Ok : StepVerdict.Unchecked,
        Detail = HeldStill ? null : Sentence(),
    };

    /// <summary>
    /// One reading standing for itself, for a caller that took the picture some other way. It says
    /// so: nothing here will claim the region held still on the strength of a single look.
    /// </summary>
    /// <param name="before">What was read, whenever it was read.</param>
    public static RegionThroughout Once(Obstruction before)
    {
        ArgumentNullException.ThrowIfNull(before);
        return new RegionThroughout(before, null);
    }

    /// <summary>
    /// Read the region, run the take, read it again. The sequencing lives here for the reason WW187
    /// moved the first reading into the door: a caller that has to remember the second one is a
    /// caller who will one day take the picture and not the reading.
    /// </summary>
    /// <param name="window">The window being photographed.</param>
    /// <param name="region">The part of the screen the capture is about.</param>
    /// <param name="take">What actually writes the picture.</param>
    public static RegionThroughout Around(nint window, WindowBounds region, Action take)
    {
        ArgumentNullException.ThrowIfNull(take);

        var before = Obstruction.Reading(window, region);
        take();
        return new RegionThroughout(before, Obstruction.Reading(window, region));
    }
}
