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
        // The guard is the application's, and this is the harness being told about it.
        using var application = AnsweringWindow.Open(root);
        var elsewhere = Path.Combine(Path.GetTempPath(), "winwright-not-asked-for.png");

        var asked = OwnRender.Into(application.Handle, elsewhere);

        Assert.False(asked.Answered);
        Assert.False(File.Exists(elsewhere));

        // WW362. It used to read as the same answer as not taking the message, and from out here
        // that looked right — both are the application declining to write the file. It is not: one
        // is an application nobody has adopted the half in, and this is two runs that disagree about
        // where pictures go, which is a thing somebody can go and reconcile.
        Assert.Contains("disagree about where pictures go", asked.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void An_application_told_nowhere_to_write_says_so_rather_than_that_it_takes_no_message()
    {
        // WW362, and the fault the attach door always has. A run that launches the application sets
        // the variable from the project; a run attached to one already up launched nothing and has
        // no moment left at which it could have — so the sentence about adding Renders.Answer was
        // advice about a line this application already has, and the real remedy was never printed.
        using var application = AnsweringWindow.Adopted();

        var asked = OwnRender.Into(application.Handle, Path.Combine(root, "never.png"));

        Assert.False(asked.Answered);
        Assert.Contains("has the in-app half", asked.Sentence(), StringComparison.Ordinal);
        Assert.Contains(OwnRender.RendersInto, asked.Sentence(), StringComparison.Ordinal);
        Assert.Contains("attached to one already up", asked.Sentence(), StringComparison.Ordinal);

        // And it is not the other sentence, which is the whole of what this task moved.
        Assert.DoesNotContain("Renders.Answer", asked.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void An_application_carrying_no_half_at_all_still_gets_the_sentence_about_adopting_one()
    {
        // The other side of the same split, kept as a case: an application that answers nothing to
        // the why ask either is one nobody has adopted anything in, and telling it about an
        // environment variable would be the mirror of the mistake WW362 fixed.
        using var application = AnsweringWindow.Silent();

        var asked = OwnRender.Into(application.Handle, Path.Combine(root, "never.png"));

        Assert.False(asked.Answered);
        Assert.Contains("Renders.Answer", asked.Sentence(), StringComparison.Ordinal);
        Assert.DoesNotContain("has the in-app half", asked.Sentence(), StringComparison.Ordinal);
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
    public void An_application_that_answers_draws_the_tree_one_named_popup_is_holding()
    {
        // WW359, across the boundary the design is about. The popup is closed, so there is no window
        // anywhere for a copy of the screen to reach — and this is the picture it could not take.
        using var application = AnsweringWindow.Open(root);
        var path = Path.Combine(root, "flyout.png");

        var asked = OwnRender.PopupInto(application.Handle, AnsweringWindow.PopupNamed, path);

        Assert.True(asked.Answered, asked.Sentence());
        Assert.True(File.Exists(path), $"it said it drew one and {path} is not there");

        // The popup's own child and not the window behind it, read off the file: the child is 90x40
        // and the window is 240x160, so the pixel count says which tree crossed the boundary.
        var picture = Pictures.Of(path);
        Assert.Equal(90 * 40, picture.Pixels);
        Assert.True(picture.HasInk, picture.Sentence());
    }

    [Fact]
    public void A_popup_the_application_does_not_have_is_reported_by_name()
    {
        // Named in the absence rather than counted, because the fix is in the case: a run told only
        // that a picture did not happen cannot tell a wrong name from an application that refused.
        using var application = AnsweringWindow.Open(root);
        var path = Path.Combine(root, "missing.png");

        var asked = OwnRender.PopupInto(application.Handle, "summary", path);

        Assert.False(asked.Answered);
        Assert.False(File.Exists(path));
        Assert.Contains("summary", asked.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_popup_holding_nothing_is_a_refusal_about_the_popup_and_not_about_the_file()
    {
        using var application = AnsweringWindow.Open(root);
        var path = Path.Combine(root, "hollow.png");

        var asked = OwnRender.PopupInto(application.Handle, AnsweringWindow.EmptyPopupNamed, path);

        Assert.False(asked.Answered);
        Assert.False(File.Exists(path));
        Assert.Contains("holding nothing", asked.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void An_application_whose_half_predates_this_ask_declines_it_rather_than_mistaking_it()
    {
        // The reason WW359 is a second registered message rather than a second field on the first
        // one's payload. An in-app half older than the harness driving it has no such message to
        // match, so it leaves it unhandled and answers nothing — which is a reading, and is what a
        // half that read `path\0name` as a path could not have given.
        using var application = AnsweringWindow.Silent();
        var path = Path.Combine(root, "never.png");

        var asked = OwnRender.PopupInto(application.Handle, AnsweringWindow.PopupNamed, path);

        Assert.False(asked.Answered);
        Assert.False(File.Exists(path));
        Assert.Contains("Renders.Answer", asked.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_popup_may_not_be_written_outside_what_the_run_named_and_the_refusal_says_which()
    {
        using var application = AnsweringWindow.Open(root);
        var elsewhere = Path.Combine(Path.GetTempPath(), "winwright-popup-not-asked-for.png");

        var asked = OwnRender.PopupInto(application.Handle, AnsweringWindow.PopupNamed, elsewhere);

        Assert.False(asked.Answered);
        Assert.False(File.Exists(elsewhere));
        Assert.Contains(OwnRender.RendersInto, asked.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_name_carrying_the_separator_is_refused_here_rather_than_sent()
    {
        // The one field that could break the payload apart. Sent, the half at the other end would
        // read a shorter name and photograph whatever that matched — which is the wrong picture, and
        // the whole thing this ask refuses to take.
        var asked = OwnRender.PopupInto(1, "details\0extra", "x.png");

        Assert.False(asked.Answered);
        Assert.Contains("NUL", asked.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void No_window_at_all_is_named_rather_than_sent_a_popup_ask()
    {
        var asked = OwnRender.PopupInto(0, AnsweringWindow.PopupNamed, Path.Combine(root, "never.png"));

        Assert.False(asked.Answered);
        Assert.Contains("no window was named", asked.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_second_window_is_answered_for_where_the_application_answers_for_itself()
    {
        // WW361, and the whole of it. The window is shown after the answering was arranged, so what
        // covers it is the class handler rather than the enumeration of what was already up — the
        // two halves of the fix, and a window opened first would exercise only one of them.
        using var application = AnsweringWindow.Everywhere(root);
        var second = application.AlsoOpen();
        var path = Path.Combine(root, "second.png");

        var asked = OwnRender.Into(second, path);

        Assert.True(asked.Answered, $"{asked.Sentence()} — {application.Sentence()}");
        Assert.True(File.Exists(path));

        // The second window and not the first, read off the file: this one is 160x120 and the one
        // the application opened with is 240x160.
        Assert.Equal(160 * 120, Pictures.Of(path).Pixels);
    }

    [Fact]
    public void A_second_window_is_not_answered_for_where_the_application_named_only_its_first()
    {
        // The negative control, and the defect WW361 was opened about, kept as a case rather than a
        // memory. An adopter who writes the per-window line gets exactly this, and the sentence it
        // comes back with is the one an application that never adopted the half at all gives — two
        // different faults reading alike, which is why the other line had to exist.
        using var application = AnsweringWindow.Open(root);
        var second = application.AlsoOpen();
        var path = Path.Combine(root, "unnamed.png");

        var asked = OwnRender.Into(second, path);

        Assert.False(asked.Answered);
        Assert.False(File.Exists(path));

        // And the first window still answers, so this is about which window and never about whether
        // the application adopted anything.
        Assert.True(OwnRender.Into(application.Handle, Path.Combine(root, "first.png")).Answered);
    }

    [Fact]
    public void An_application_answering_for_itself_says_how_many_windows_that_is()
    {
        // The reading the design asked for. An adopter who hooked one window and meant the
        // application had no way to find that out, and a count is what turns a silent gap into a
        // number somebody can disagree with.
        using var application = AnsweringWindow.Everywhere(root);
        var before = application.Windows;

        application.AlsoOpen();

        Assert.True(application.Windows > before, application.Sentence());
        Assert.Contains("answering renders for", application.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void Nothing_may_be_asked_for_by_passing_nothing()
    {
        Assert.Throws<ArgumentException>(() => OwnRender.Into(1, "  "));
        Assert.Throws<ArgumentOutOfRangeException>(() => OwnRender.Into(1, "x.png", withinMs: 0));

        Assert.Throws<ArgumentException>(() => OwnRender.PopupInto(1, "  ", "x.png"));
        Assert.Throws<ArgumentException>(() => OwnRender.PopupInto(1, "details", "  "));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => OwnRender.PopupInto(1, "details", "x.png", withinMs: 0));
    }
}
