using System.Reflection;

using Winwright.Tracing;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW163. An assertion a run makes and a step a trace records are two halves of the same act, and
/// nine of sixteen results answered only the first — a nudged range, a derived set, a falsifiable
/// expectation, a layout reading, a name check, a store change, a timed-out read, a containment
/// check and a picture check.
/// <para>
/// This block's criterion says the trace of a failed run carries the locator, what it resolved to,
/// what was read back and the verdict for every step before the one that broke. A result that can
/// answer the verdict and nothing else leaves a reader holding the summary with no way back to the
/// observation behind it, and the re-run this block exists to make unnecessary is what is left.
/// </para>
/// <para>
/// The pairing is read off the assembly rather than kept in a list here. A tenth result added later
/// with a verdict and no step fails this, which is the whole point: the alternative is a trace that
/// quietly covers less than the last person assumed.
/// </para>
/// </summary>
public sealed class RecordedResultTests
{
    /// <summary>Every public type in the engine that answers a verdict a run counts.</summary>
    private static IReadOnlyList<Type> Answering() =>
        typeof(Winwright.Locating.Subject).Assembly
            .GetExportedTypes()
            .Where(one => one.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Any(method => method.Name == "AsAssertion" && method.ReturnType == typeof(AssertionResult)))
            .OrderBy(one => one.Name, StringComparer.Ordinal)
            .ToList();

    /// <summary>The step that type answers, where it answers one.</summary>
    private static MethodInfo? Recording(Type answering) =>
        answering.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(one => one.Name == "AsTraceStep" && one.ReturnType == typeof(TraceStep));

    [Fact]
    public void Every_result_that_answers_a_verdict_answers_the_step_behind_it()
    {
        var answering = Answering();

        Assert.True(answering.Count > 10, $"only {answering.Count} result(s) answer a verdict, which is unexpected");

        var silent = answering.Where(one => Recording(one) is null).Select(one => one.Name).ToList();
        Assert.True(
            silent.Count == 0,
            $"{silent.Count} of {answering.Count} result(s) answer a verdict and no step: {string.Join(", ", silent)}");
    }

    [Fact]
    public void A_step_asks_for_no_more_than_the_verdict_beside_it_does()
    {
        // A caller that can name the assertion can name the step, and one that cannot needs
        // neither. Anything else is a step somebody has to find an extra argument for at the moment
        // they are trying to record a failure, which is a step nothing will record.
        foreach (var answering in Answering())
        {
            var verdict = answering.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(one => one.Name == "AsAssertion" && one.ReturnType == typeof(AssertionResult));

            var wanted = Recording(answering)!.GetParameters().Select(one => one.Name).ToList();
            var already = verdict.GetParameters().Select(one => one.Name).ToHashSet(StringComparer.Ordinal);

            var extra = wanted.Where(one => !already.Contains(one!)).ToList();
            Assert.True(
                extra.Count == 0,
                $"{answering.Name}.AsTraceStep asks for {string.Join(", ", extra)}, which its own verdict does not");
        }
    }

    [Fact]
    public void The_reading_is_taken_off_the_assembly_and_not_off_a_list_kept_here()
    {
        // The control. A check that found nothing would pass the two above whatever the engine did,
        // which is the shape of a green covering a scan that never ran.
        var answering = Answering();

        Assert.Contains(answering, one => one.Name == "StoreChange");
        Assert.Contains(answering, one => one.Name == "NudgeResult");
        Assert.Contains(answering, one => one.Name == "PictureCheck");
        Assert.DoesNotContain(answering, one => one.Name == "TraceStep");

        // And the reading runs one way and not both. An act that landed is not itself a claim, so
        // ActResult answers a step and no verdict — a check that demanded the pair in both
        // directions would be asking every recorded step to be an assertion, which is a different
        // rule and not this one.
        Assert.DoesNotContain(answering, one => one.Name == "ActResult");
        Assert.NotNull(Recording(typeof(Winwright.Acting.ActResult)));
    }
}
