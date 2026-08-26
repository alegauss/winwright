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

        // And it is the only one, which is what keeps the refusal narrow.
        Assert.Equal(["focused"], ReadBack.All.Where(one => one.Always).Select(one => one.Name));
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
        // The measurement, and it is not what was expected. Nothing in the vocabulary reads a WPF
        // label's words: the content is in the name, exactly as with a Win32 Static. The one reading
        // that answers is 'focused', with "not focused" — which is about the element and not about
        // what it says, and is why a step may no longer claim that reading answers.
        Assert.Equal(["focused"], answered);
        Assert.Null(ReadBack.Named("text").Of(label));
        Assert.Null(ReadBack.Named("anything").Of(label));
    }
}
