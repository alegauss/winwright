using Winwright.Scenarios;

namespace Winwright.Tests;

/// <summary>
/// One step, written the way a case writes one. WW391.
/// <para>
/// <see cref="StepDeclaration.Of" /> took twenty-nine parameters and this suite named them, which
/// is how a field came to join the format in five places rather than two. It takes what a case
/// wrote now — fields by the names the format spells them — and this is the two-line convenience
/// that keeps a test reading like the file it stands for: the subject and the act, then whatever
/// else the step says.
/// </para>
/// <para>
/// Nothing here knows a field's name. A claim added to the format is exercised by writing it in a
/// test, and this does not have to be told about it — which is the whole of what the task was for,
/// and would be undone by a helper carrying one parameter per field.
/// </para>
/// </summary>
internal static class Wrote
{
    /// <summary>Declare one, refusals and all.</summary>
    /// <param name="locator">What it acts on, or null where a <c>tray</c> says instead.</param>
    /// <param name="act">Which act.</param>
    /// <param name="wrote">Everything else the step says, by field name.</param>
    internal static StepDeclaration Step(
        string? locator, string act, params (string Field, object? Value)[] wrote) =>
        StepDeclaration.Of(new Written([("locator", locator), ("act", act), .. wrote]));
}
