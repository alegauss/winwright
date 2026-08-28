using Winwright.Scenarios;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW269. `sameAs` compares exactly, and the reset caption on claude-tray's Statistics page cannot go
/// through it: it names when a quota window turns over and counts down while the window is open, so a
/// run crossing a minute boundary reads it one lower and nothing about the application is wrong.
/// Dropping the claim is worse than tolerating the minute — an hour of drift is another profile's
/// window, which is the defect WW81 was filed against.
/// <para>
/// The reason the task gave for not copying the script's answer was wrong, and measured so before this
/// was built: it said the script keyed on `d`, `h` and `m` and that those letters differ in the four
/// other languages that application ships. They do not. Both of its formatters write them as literal
/// ASCII and only `dur.now` is translated — so a comparison that reads the digits and ignores the
/// words is language-independent here for a reason rather than by luck.
/// </para>
/// <para>
/// The claim is exercised through the declaration and the comparison rather than against a live
/// window: what is interesting about it is the arithmetic, and a window counting down in real time is
/// the one fixture that cannot be made to tick on demand.
/// </para>
/// </summary>
public sealed class CountdownTests
{
    /// <summary>Two readings, compared the way a step claiming a countdown compares them.</summary>
    private static bool Ticked(string before, string now)
    {
        var method = typeof(CaseRun).GetMethod(
            "Ticked",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(method);
        return (bool)method!.Invoke(null, [before, now])!;
    }

    [Theory]
    [InlineData("3h 20m", "3h 20m")]      // the run did not cross a boundary
    [InlineData("3h 20m", "3h 19m")]      // it crossed one, which is the whole point
    [InlineData("2d 4h", "2d 4h")]
    [InlineData("45m", "44m")]
    [InlineData("45m", "45m")]
    public void A_caption_that_ticked_by_one_is_the_same_caption(string before, string now)
    {
        Assert.True(Ticked(before, now), $"'{before}' → '{now}'");
    }

    [Theory]
    [InlineData("3h 20m", "3h 18m")]      // two minutes is not a tick, it is a different window
    [InlineData("3h 20m", "2h 20m")]      // an hour of drift is another profile's window — WW81
    [InlineData("3h 20m", "3h 21m")]      // counting *up* is the window having turned over
    [InlineData("3h 20m", "45m")]         // a different shape of caption is a different caption
    [InlineData("45m", "3h 20m")]
    public void Anything_further_apart_than_a_tick_is_not_the_same_caption(string before, string now)
    {
        Assert.False(Ticked(before, now), $"'{before}' → '{now}'");
    }

    [Fact]
    public void The_words_are_never_compared_so_the_claim_survives_a_translation()
    {
        // What the measurement bought. The units this was written against are ASCII in all five
        // languages, and this holds even where they are not: nothing but the digits is read.
        Assert.True(Ticked("2 hours 20 minutes", "2 horas 19 minutos"));
        Assert.True(Ticked("3h 20m", "3 Std. 19 Min."));
    }

    [Fact]
    public void A_reading_with_no_numbers_in_it_is_not_a_countdown()
    {
        // `dur.now` is the one part of the caption that *is* translated, and two readings of it carry
        // no digits at all. Refused rather than matched: a claim that a countdown came back is not
        // settled by two strings that never counted, and answering true would be the unearned green
        // this whole shape exists to refuse.
        Assert.False(Ticked("now", "now"));
        Assert.False(Ticked("now", "agora"));
    }

    [Fact]
    public void A_unit_rolling_over_is_not_tolerated_and_that_is_the_known_limit()
    {
        // Said out loud because a reader will meet it. `3h 00m` a minute later is `2h 59m`, which is a
        // tick apart and moves two of its numbers — so this fails, and the case that met it is a
        // re-run rather than a wrong answer.
        //
        // One minute in every sixty of the ones this exists to tolerate. The alternative is teaching
        // the engine what `h` means, and a format the engine knows is a format it has to be kept in
        // step with — which is what the derived expectation refuses everywhere else.
        Assert.False(Ticked("3h 00m", "2h 59m"));
    }

    [Fact]
    public void It_is_its_own_field_and_never_a_tolerance_on_the_exact_claim()
    {
        // The line the task drew: a percentage is the same number or it is not, and a general
        // tolerance would soften every exact claim in every adopting project to serve one caption.
        // `reads` is named because the shared rule requires it: this compares two readings, so the
        // default would compare whichever pattern answered first.
        var step = StepDeclaration.Of(
            "Text#reset", "read", reads: "name", named: "the second stop", sameCountdownAs: "the first stop");

        Assert.Equal("the first stop", step.SameCountdownAs);
        Assert.Null(step.SameAs);
        Assert.Null(step.Unlike);
        Assert.True(step.Checkable);

        // And the three cannot be combined: they are three ways of comparing with one earlier step.
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text#reset", "read", reads: "name", sameAs: "a", sameCountdownAs: "b", named: "c"));

        Assert.Contains("a step answers one thing", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void It_faces_the_doors_the_exact_claim_faces()
    {
        // It points at an earlier step by name, so every rule about that pointing applies to it —
        // including the easy typo a round trip invites, where both stops read the same element under
        // the same verb and one of them was left unnamed.
        Assert.Contains(
            "which is answered before the window is",
            Assert.Throws<ScenarioRefusedException>(
                () => StepDeclaration.Of(
                    "Text#reset", "read", reads: "name", named: "itself", sameCountdownAs: "itself")).Because,
            StringComparison.Ordinal);
    }
}
