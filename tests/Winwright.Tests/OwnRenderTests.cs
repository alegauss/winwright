using Winwright.Capturing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW349's message, across a real window boundary. The engine asks and an application answers, which
/// is the one route to the picture this block calls its default and the engine cannot draw.
/// <para>
/// A window on its own pumping thread on the other end, because that is the whole of what is being
/// proved: the ask is a send that waits for the window's own thread to draw the picture and say so,
/// and a fixture built on the test thread would time out without ever taking the message.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class OwnRenderTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-asked-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void An_application_that_answers_draws_its_own_tree_into_the_file_it_was_given()
    {
        using var application = AnsweringWindow.Open(root);
        var path = Path.Combine(root, "asked.png");

        var asked = OwnRender.Into(application.Handle, path);

        Assert.True(asked.Answered, asked.Sentence());
        Assert.True(File.Exists(path), $"it said it drew one and {path} is not there");

        // A picture of the window and not a rectangle, which is the one reading a render is still
        // subject to: a flat file is not a picture of a window however it was got.
        Assert.False(Colours.In(path).IsFlat);
        Assert.Contains("rendered its own tree", asked.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void An_application_that_does_not_take_the_message_says_so_rather_than_being_waited_on()
    {
        // Every application that has not called Renders.Answer, which is every application until
        // somebody adds the line. It has a window and a message loop, so the send succeeds — what
        // comes back is its window procedure declining, and that has to be a reading rather than a
        // budget spent waiting for a file nobody was ever going to write.
        using var application = AnsweringWindow.Silent();
        var path = Path.Combine(root, "never.png");

        var asked = OwnRender.Into(application.Handle, path);

        Assert.False(asked.Answered);
        Assert.False(File.Exists(path));
        Assert.Contains("Renders.Answer", asked.Sentence(), StringComparison.Ordinal);
        Assert.Contains(OwnRender.RendersInto, asked.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_file_outside_what_the_run_named_is_refused_by_the_application_and_reported_here()
    {
        // The guard is the application's, and this is the harness being told about it. It answers
        // that it drew nothing, which is the same answer as not taking the message — and rightly:
        // from out here both are the application declining to write that file.
        using var application = AnsweringWindow.Open(root);
        var elsewhere = Path.Combine(Path.GetTempPath(), "winwright-not-asked-for.png");

        var asked = OwnRender.Into(application.Handle, elsewhere);

        Assert.False(asked.Answered);
        Assert.False(File.Exists(elsewhere));
    }

    [Fact]
    public void A_window_that_is_not_there_is_a_reading_and_never_a_throw()
    {
        // A handle nothing owns. The engine is asking another process to do something and every way
        // that can fail has to arrive as a reading a run can report: a throw here would be a case
        // about the desk taking down a run about the application.
        var asked = OwnRender.Into(0x1234, Path.Combine(root, "never.png"));

        Assert.False(asked.Answered);
        Assert.False(asked.Sentence().Contains("rendered", StringComparison.Ordinal));
    }

    [Fact]
    public void No_window_at_all_is_named_rather_than_sent_to()
    {
        var asked = OwnRender.Into(0, Path.Combine(root, "never.png"));

        Assert.False(asked.Answered);
        Assert.Contains("no window was named", asked.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void An_answer_a_verdict_can_count_is_a_hole_and_never_a_red()
    {
        // An application that does not answer is a fact about how it was built rather than about
        // what it drew, so a run says it could not look — a failure there would be a red about the
        // harness's own wiring, reported against the application under test.
        using var application = AnsweringWindow.Silent();

        var asked = OwnRender.Into(application.Handle, Path.Combine(root, "never.png"));
        var result = asked.AsAssertion("the window is photographed");

        Assert.True(result.DidNotRun);
        Assert.Equal(RenderAsked.PreconditionName, result.Missing?.Name);
    }

    [Fact]
    public void Nothing_may_be_asked_for_by_passing_nothing()
    {
        Assert.Throws<ArgumentException>(() => OwnRender.Into(1, "  "));
        Assert.Throws<ArgumentOutOfRangeException>(() => OwnRender.Into(1, "x.png", withinMs: 0));
    }
}
