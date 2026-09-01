using System.Windows.Automation;

using Winwright.Locating;
using Winwright.Processes;
using Winwright.Scenarios;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// What a label answers, measured rather than assumed.
/// <para>
/// WW237 left this open. Its own case proved <c>answers</c> against an <c>Edit</c>, because a Win32
/// <c>Static</c> resolves as a Text control whose content is its <em>name</em> and offers no
/// TextPattern — so <c>reads: text</c> answered nothing against one. Whether a WPF
/// <c>TextBlock</c> behaves the same way decides whether claude-tray's panes case can make its three
/// readable claims at all, and guessing it was the WW226 mistake.
/// </para>
/// <para>
/// So it is a case rather than a paragraph: the fixture draws a real WPF label, and this reads every
/// reading the vocabulary has off it. Whatever the answer is, it is written down here and a change in
/// WPF or in UI Automation moves this rather than surprising a migration.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class LabelReadingTests : IDisposable
{
    private readonly Settling settling = Attachable.Settling();
    private readonly AutomationElement? root;

    public LabelReadingTests()
    {
        if (!Desk.Read().CanObserve)
            return;

        var launched = settling.Register.Launch(Fixture.Started("--names"));
        var drawn = Attempt.UntilTrue(() => TopLevelWindows.Largest(launched.Pid) is not null, 20000, 25);

        Assert.True(drawn.Happened, $"the fixture drew no window in {drawn.WaitedMs}ms");
        root = AutomationElement.FromHandle(TopLevelWindows.Largest(launched.Pid)!.Handle);
    }

    public void Dispose() => settling.Dispose();

    [Fact]
    public void A_reading_that_always_answers_cannot_be_claimed_to_answer()
    {
        // The hole this measurement found, and it was mine: 'focused' says "not focused" for every
        // element that resolved, so a step claiming it answers holds whenever the locator matched.
        // Existence wearing the words of a reading, which arrived with WW225 and WW237 two tasks
        // apart without either noticing.
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text#profileLabel", "read", reads: "focused", answers: true));

        Assert.Contains("could never be false", refused.Because, StringComparison.Ordinal);

        // And which readings are under it, which is what keeps the refusal narrow. Two, and they are
        // the two that are about the element rather than about a pattern it offers: WW325 added
        // 'enabled' and it has exactly this shape — every element that resolved is enabled or is
        // not. A reading joining them without declaring itself is the hole above arriving again, so
        // the list is written out here rather than counted.
        Assert.Equal(["enabled", "focused"], ReadBack.All.Where(one => one.Always).Select(one => one.Name));

        var refusedToo = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text#profileLabel", "read", reads: "enabled", answers: true));

        Assert.Contains("could never be false", refusedToo.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_wpf_label_carries_its_words_in_its_name_and_not_in_a_pattern()
    {
        if (root is null)
            return;

        var label = Subject.Unguarded(root, Locator.Parse("Text#profileLabel"), 4000, pollMs: 25).Read();

        Assert.True(label.Found, "the names pane draws no Text#profileLabel");
        Assert.Equal("Profile", label.Facts!.Name);

        // The measurement. Every reading the vocabulary offers, against a label that plainly has
        // words in it — and if they all answer nothing, then a case cannot claim a label reads.
        var answered = ReadBack.All
            .Where(one => one.Of(label) is { } said && said.Trim().Length > 0)
            .Select(one => one.Name)
            .ToList();

        // Named and not counted: "at least one answers" is satisfied by a vocabulary that changed
        // under it, and which one a case has to write is the whole reason for asking.
        //
        // The measurement, and it is not what was expected. No pattern reading reads a WPF label's
        // words: the content is in the name, exactly as with a Win32 Static, which is why WW238 added
        // 'name'. The other two answer about the element rather than about what it says — 'focused'
        // with "not focused" and, since WW325, 'enabled' with "enabled" — which is why a step may
        // claim neither of them answers.
        //
        // Named and not counted for that reason too: a reading added tomorrow that answers here
        // without declaring itself Always is the unearned green arriving again, and this list is
        // where it shows up.
        Assert.Equal(["name", "enabled", "focused"], answered);
        Assert.Equal("Profile", ReadBack.Named("name").Of(label));
        Assert.Null(ReadBack.Named("text").Of(label));
        Assert.Null(ReadBack.Named("anything").Of(label));
    }

    [Fact]
    public void The_name_reading_is_refused_where_the_locator_matched_on_the_name()
    {
        // WW238. The reading answers what a locator can select by, so a step naming the element by its
        // name and then reading it asserts what chose the element: Resolve matches a name by equality,
        // so 'Profile' is the only answer there was.
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text[name=\"Profile\"]", "read", reads: "name", expected: "Profile"));

        Assert.Contains("already matched on that", refused.Because, StringComparison.Ordinal);
        Assert.Contains("'Profile'", refused.Because, StringComparison.Ordinal);

        // Whatever the claim is: 'answers' holds because the locator matched, which is the same
        // unearned green wearing a different field.
        Assert.Throws<ScenarioRefusedException>(
            () => StepDeclaration.Of("Text[name=\"Profile\"]", "read", reads: "name", answers: true));

        // And the sentence has to send the author to the locator, because the useful shape below is
        // one locator field away rather than a different check.
        Assert.Contains("select the element another way", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void Naming_the_element_some_other_way_and_reading_its_name_is_the_useful_shape()
    {
        var step = StepDeclaration.Of("Text#profileLabel", "read", reads: "name", expected: "Profile");

        Assert.Equal("Profile", step.Expected);
        Assert.True(step.Checkable);

        // Not Always, unlike 'focused': a blank name answers nothing, so "this label says something"
        // stays a claim that can be false and 'answers' is allowed to make it.
        Assert.False(ReadBack.Named("name").Always);
        StepDeclaration.Of("Text#profileLabel", "read", reads: "name", answers: true);
    }

    [Fact]
    public void What_a_name_reads_as_is_defined_once_for_the_reading_and_for_a_sweep()
    {
        // WW238. 'covers' compares the names a locator matched, which is this reading over a set, and
        // the two deriving it apart is how a sweep comes to count a pane of blank controls as having
        // read the empty string.
        var blank = new ElementFacts("   ", "", "Text", "", false, true, default, new HashSet<string>());

        Assert.Null(blank.Says);
        Assert.Equal("Profile", (blank with { Name = "Profile" }).Says);
    }
}
