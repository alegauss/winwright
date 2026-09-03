using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

using Winwright.Capturing;
using Winwright.InApp;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW349. The off-screen render is this project's default route and the one the engine cannot take:
/// a render draws a visual tree, and nothing outside a process has one. So the run asks and the
/// application answers.
/// <para>
/// The half a case can run without two processes is here — the rule the message handler runs, driven
/// directly. What the message itself does is <see cref="OwnRenderTests" />, which needs an
/// application on the other end of it.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class RendersTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-renders-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void The_two_halves_agree_on_the_variable_and_on_the_message_they_never_share_a_type_for()
    {
        // The engine references no part of the in-app half and never will, so every name between
        // them is spelled twice. This is where a drift of one letter goes red, which is the only
        // place it could: a run against a real application would simply be told it does not answer.
        Assert.Equal(Renders.PathVariable, OwnRender.RendersInto);
        Assert.Equal(Renders.Registered, OwnRender.Registered);

        // WW359 put a second message and five answers on the same seam, and the numbers are as much
        // the wire as the names are. An engine reading 3 where this half meant 4 would report the
        // wrong refusal about a real popup, which is a worse answer than no answer.
        Assert.Equal(Renders.RegisteredPopup, OwnRender.RegisteredPopup);
        Assert.NotEqual(Renders.Registered, Renders.RegisteredPopup);

        Assert.Equal((int)PopupRendered.Drawn, OwnRender.Drawn);
        Assert.Equal((int)PopupRendered.NoSuchPopup, OwnRender.NoSuchPopup);
        Assert.Equal((int)PopupRendered.MoreThanOnePopup, OwnRender.MoreThanOnePopup);
        Assert.Equal((int)PopupRendered.PopupHoldsNothing, OwnRender.PopupHoldsNothing);
        Assert.Equal((int)PopupRendered.PathRefused, OwnRender.PathRefused);

        // WW362's third message and its five answers, on the same seam and for the same reason.
        Assert.Equal(Renders.RegisteredWhy, OwnRender.RegisteredWhy);
        Assert.Equal(3, new[] { Renders.Registered, Renders.RegisteredPopup, Renders.RegisteredWhy }.Distinct(StringComparer.Ordinal).Count());

        Assert.Equal((int)RenderRefusal.WouldDraw, OwnRender.Refusals.WouldDraw);
        Assert.Equal((int)RenderRefusal.ToldNowhere, OwnRender.Refusals.ToldNowhere);
        Assert.Equal((int)RenderRefusal.PathRefused, OwnRender.Refusals.PathRefused);
        Assert.Equal((int)RenderRefusal.NotOurWindow, OwnRender.Refusals.NotOurWindow);
        Assert.Equal((int)RenderRefusal.NothingToDraw, OwnRender.Refusals.NothingToDraw);

        // Zero is the answer a window that does not take the message gives, so no refusal may be it.
        Assert.DoesNotContain(
            0,
            new[]
            {
                OwnRender.Drawn, OwnRender.NoSuchPopup, OwnRender.MoreThanOnePopup,
                OwnRender.PopupHoldsNothing, OwnRender.PathRefused,
            });
    }

    [Fact]
    public void Why_a_render_did_not_happen_is_the_first_check_it_would_have_stopped_at()
    {
        // WW362. The same checks Drawn makes, in the same order, and the order is what makes the
        // answer the reason: a process told nowhere to write would also refuse the path, and being
        // told the path is wrong sends somebody to fix a file that is fine.
        var path = Path.Combine(root, "page.png");
        var beside = root.TrimEnd(Path.DirectorySeparatorChar) + "-elsewhere";

        var answers = Apartment.Run(() =>
        {
            var window = Shown();
            var handle = Handle(window);

            return new[]
            {
                Renders.Refusing("", handle, path),
                Renders.Refusing(root, handle, Path.Combine(beside, "page.png")),
                Renders.Refusing(root, 0x1234, path),
                Renders.Refusing(root, handle, path),
            };
        });

        Assert.Equal(RenderRefusal.ToldNowhere, answers[0]);
        Assert.Equal(RenderRefusal.PathRefused, answers[1]);
        Assert.Equal(RenderRefusal.NotOurWindow, answers[2]);
        Assert.Equal(RenderRefusal.WouldDraw, answers[3]);
    }

    [Fact]
    public void Asking_why_draws_nothing()
    {
        // The property that makes this safe to ask. A caller wanting to know why a picture did not
        // happen must not be the thing that makes one happen — so the last check reads the layout
        // rather than rendering it.
        var path = Path.Combine(root, "unwritten.png");

        var answer = Apartment.Run(() => Renders.Refusing(root, Handle(Shown()), path));

        Assert.Equal(RenderRefusal.WouldDraw, answer);
        Assert.False(File.Exists(path), "asking why a render did not happen wrote one");
    }

    [Fact]
    public void A_named_popup_is_photographed_through_the_tree_it_holds_although_it_is_closed()
    {
        // WW359, and the surface WW347 is about. Closed, so there is no window anywhere for a copy
        // of the screen to reach — and the child is an ordinary element in a tree this process owns,
        // which is the whole reason this ask exists.
        var path = Path.Combine(root, "flyout.png");

        var answer = Apartment.Run(() =>
        {
            var window = Holding(new Popup
            {
                Name = "details",
                Child = new Border { Width = 90, Height = 40, Background = new SolidColorBrush(Colors.Firebrick) },
            });

            return Renders.PopupDrawn(root, Handle(window), "details", path);
        });

        Assert.Equal(PopupRendered.Drawn, answer);
        Assert.True(File.Exists(path));

        // Which surface, and not merely that there was one. The child is 90x40 and the window behind
        // it is 240x160, so the count of pixels in the file says which tree was drawn — a render is
        // one pixel per unit at the default resolution, so this is exact rather than approximate.
        var picture = Pictures.Of(path);
        Assert.Equal(90 * 40, picture.Pixels);
        Assert.True(picture.HasInk, picture.Sentence());
    }

    [Fact]
    public void A_name_no_popup_under_that_window_carries_is_said_rather_than_guessed_at()
    {
        var path = Path.Combine(root, "missing.png");

        var answer = Apartment.Run(() =>
        {
            var window = Holding(new Popup { Name = "details", Child = new Border { Width = 20, Height = 20 } });
            return Renders.PopupDrawn(root, Handle(window), "summary", path);
        });

        Assert.Equal(PopupRendered.NoSuchPopup, answer);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void A_name_two_popups_share_is_refused_rather_than_resolved_to_either_one()
    {
        // The failure this ask was designed around. A name is not unique across a tree, and a
        // picture of whichever came first in the walk is a picture no run could prove was the
        // surface it meant — which is what this block refuses everywhere else.
        var path = Path.Combine(root, "ambiguous.png");

        var answer = Apartment.Run(() =>
        {
            var window = Holding(
                new Popup { Name = "row", Child = new Border { Width = 20, Height = 20 } },
                new Popup { Name = "row", Child = new Border { Width = 30, Height = 30 } });

            return Renders.PopupDrawn(root, Handle(window), "row", path);
        });

        Assert.Equal(PopupRendered.MoreThanOnePopup, answer);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void A_popup_holding_nothing_is_refused_rather_than_written_as_an_empty_file()
    {
        // An empty file is a successful render to everything that only checks one exists, which is
        // exactly what the harness checks.
        var path = Path.Combine(root, "empty.png");

        var answer = Apartment.Run(() =>
        {
            var window = Holding(new Popup { Name = "hollow" });
            return Renders.PopupDrawn(root, Handle(window), "hollow", path);
        });

        Assert.Equal(PopupRendered.PopupHoldsNothing, answer);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void A_popup_may_not_be_written_outside_the_directory_the_run_named()
    {
        // The same guard the window ask has, and the application's to make rather than the
        // harness's. Said apart from the other refusals because it is the one about the sender.
        var elsewhere = Path.Combine(Path.GetTempPath(), "winwright-popup-not-asked-for.png");

        var answer = Apartment.Run(() =>
        {
            var window = Holding(new Popup { Name = "details", Child = new Border { Width = 20, Height = 20 } });
            return Renders.PopupDrawn(root, Handle(window), "details", elsewhere);
        });

        Assert.Equal(PopupRendered.PathRefused, answer);
        Assert.False(File.Exists(elsewhere));
    }

    [Fact]
    public void A_popup_ask_an_application_was_told_nowhere_to_write_for_answers_nothing()
    {
        var answer = Apartment.Run(() =>
        {
            var window = Holding(new Popup { Name = "details", Child = new Border { Width = 20, Height = 20 } });
            return Renders.PopupDrawn("", Handle(window), "details", Path.Combine(root, "page.png"));
        });

        Assert.Equal(PopupRendered.NotAnswered, answer);
    }

    [Fact]
    public void A_window_this_presentation_stack_does_not_own_holds_no_popup_to_ask_about()
    {
        // Answered rather than thrown, for the reason the window ask is: this rule runs inside a
        // window procedure. And NotAnswered rather than NoSuchPopup, because there is no tree to
        // have looked in — saying a popup is absent would be a claim about a window this never read.
        var answer = Apartment.Run(
            () => Renders.PopupDrawn(root, 0x1234, "details", Path.Combine(root, "page.png")));

        Assert.Equal(PopupRendered.NotAnswered, answer);
    }

    [Fact]
    public void A_window_renders_its_own_tree_into_the_file_it_was_given()
    {
        var path = Path.Combine(root, "page.png");

        var drawn = Apartment.Run(() =>
        {
            var window = Shown();
            return Renders.Drawn(root, Handle(window), path);
        });

        Assert.True(drawn, "the window did not draw its own tree");
        Assert.True(File.Exists(path));

        // A picture of the window rather than a rectangle, which is the reading that tells the two
        // apart and the one a render is still subject to.
        Assert.False(Colours.In(path).IsFlat);
    }

    [Fact]
    public void A_file_outside_the_directory_the_run_named_is_refused_by_the_application()
    {
        // The whole of the guard, and it is the application's refusal rather than the harness's. A
        // window that answered this without one would write a picture of itself wherever a sender
        // named, which is a thing a shipped product must not do.
        var elsewhere = Path.Combine(Path.GetTempPath(), "winwright-not-asked-for.png");

        var drawn = Apartment.Run(() =>
        {
            var window = Shown();
            return Renders.Drawn(root, Handle(window), elsewhere);
        });

        Assert.False(drawn);
        Assert.False(File.Exists(elsewhere));
    }

    [Fact]
    public void A_directory_whose_name_this_one_merely_starts_with_is_outside_it()
    {
        // The hole a prefix test has, shaped exactly like the thing the guard refuses: without the
        // separator, 'pictures-elsewhere' is inside 'pictures'.
        var beside = root.TrimEnd(Path.DirectorySeparatorChar) + "-elsewhere";
        var path = Path.Combine(beside, "page.png");

        var drawn = Apartment.Run(() =>
        {
            var window = Shown();
            return Renders.Drawn(root, Handle(window), path);
        });

        Assert.False(drawn);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void An_application_that_was_told_nowhere_to_write_answers_nothing()
    {
        // Unset means answer nothing, which is what makes this safe to leave in a release: it is
        // the same promise a reported surface and a geometry dump make about a build shipped to
        // its users.
        var drawn = Apartment.Run(() =>
        {
            var window = Shown();
            return Renders.Drawn("", Handle(window), Path.Combine(root, "page.png"));
        });

        Assert.False(drawn);
    }

    [Fact]
    public void A_window_this_presentation_stack_does_not_own_is_answered_and_not_drawn()
    {
        // A handle that is no window of this application's. Answered rather than thrown, because the
        // rule this runs inside is a window procedure: raising out of one takes down the application
        // the harness was only supposed to photograph.
        var drawn = Apartment.Run(() => Renders.Drawn(root, 0x1234, Path.Combine(root, "page.png")));

        Assert.False(drawn);
    }

    [Fact]
    public void Answering_is_hooked_and_unhooked_and_says_which_it_is()
    {
        // The disposal is the property worth stating: an application that left the hook on after
        // being asked to stop would keep answering a harness that had finished with it.
        var (during, after, said) = Apartment.Run(() =>
        {
            var window = Shown();
            var source = System.Windows.Interop.HwndSource.FromHwnd(Handle(window))!;

            var answering = Renders.Answer(source);
            var held = answering.Answering;
            var sentence = answering.Sentence();
            answering.Dispose();

            return (held, answering.Answering, sentence);
        });

        // Answering turns on what the environment says, and this suite's own process is not run
        // with the variable set — so what is asserted is the pair moving together, whichever way
        // the desk this runs on happens to have it.
        Assert.Equal(Renders.Where() is not null, during);
        Assert.False(after, "it went on answering after it was told to stop");
        Assert.Contains(during ? "answering renders" : "answering no renders", said, StringComparison.Ordinal);
    }

    [Fact]
    public void A_window_that_was_never_shown_is_refused_for_having_nothing_to_hook()
    {
        Assert.Throws<InvalidOperationException>(
            () => Apartment.Run(() => Renders.Answer(new Window { Width = 100, Height = 80 })));
    }

    [Fact]
    public void Nothing_may_be_answered_for_by_passing_nothing()
    {
        Assert.Throws<ArgumentNullException>(() => Renders.Answer((Window)null!));
        Assert.Throws<ArgumentNullException>(() => Renders.Answer((System.Windows.Interop.HwndSource)null!));
    }

    /// <summary>A window that is up and laid out, which is the state a harness would ask about.</summary>
    private static Window Shown()
    {
        var window = new Window
        {
            Width = 240,
            Height = 160,
            Left = 40,
            Top = 40,
            Background = new SolidColorBrush(Colors.White),
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "the report", Margin = new Thickness(12) },
                    new Border { Width = 120, Height = 40, Background = new SolidColorBrush(Colors.CornflowerBlue) },
                },
            },
        };

        window.Show();
        window.UpdateLayout();
        return window;
    }

    /// <summary>
    /// A shown window whose tree carries these popups, which is what a harness would ask about.
    /// WW359.
    /// <para>
    /// They go in closed and are left that way. Closed is the state that matters — it is the one a
    /// copy of the screen has nothing at all to reach, and the one a case photographing a flyout
    /// nobody has clicked would be in.
    /// </para>
    /// </summary>
    /// <param name="popups">The popups to hang under the window.</param>
    private static Window Holding(params Popup[] popups)
    {
        var page = new StackPanel();
        page.Children.Add(new TextBlock { Text = "the report", Margin = new Thickness(12) });
        foreach (var popup in popups)
            page.Children.Add(popup);

        var window = new Window
        {
            Width = 240,
            Height = 160,
            Left = 40,
            Top = 40,
            Background = new SolidColorBrush(Colors.White),
            Content = page,
        };

        window.Show();
        window.UpdateLayout();
        return window;
    }

    private static nint Handle(Window window) => new System.Windows.Interop.WindowInteropHelper(window).Handle;
}
