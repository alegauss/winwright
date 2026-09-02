using System.Windows;
using System.Windows.Controls;
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

    private static nint Handle(Window window) => new System.Windows.Interop.WindowInteropHelper(window).Handle;
}
