using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Winwright.Capturing;
using Winwright.InApp;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW72. The classic system palette was the obvious source and is measurably wrong: it answers
/// white on a machine whose application window is dark, so the first capture taken that way came
/// back as pale text on nothing — correct in every respect and unreadable.
/// <para>
/// Every case here is built on the render thread, because a brush belongs to the thread that made
/// it and a background read on one thread and composed on another is the failure of a different
/// task entirely.
/// </para>
/// </summary>
public sealed class BackgroundsTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-background-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private static T OnStaThread<T>(Func<T> work)
    {
        T? answer = default;
        Exception? threw = null;

        var thread = new Thread(() =>
        {
            try
            {
                answer = work();
            }
            catch (Exception broke)
            {
                threw = broke;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "the render thread did not finish");

        if (threw is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(threw).Throw();

        return answer!;
    }

    /// <summary>The colour a dark application paints its window with, which is the whole case.</summary>
    private static readonly Color Dark = Color.FromRgb(0x1e, 0x1e, 0x1e);

    [Fact]
    public void The_theme_key_the_application_declares_is_asked_first()
    {
        var chosen = OnStaThread(() =>
        {
            var page = new Border { Width = 40, Height = 20 };
            page.Resources[Backgrounds.DefaultKey] = new SolidColorBrush(Dark);
            return Backgrounds.Choose(page);
        });

        Assert.Equal(Backdrop.Theme, chosen.From);
        Assert.Equal(Dark, chosen.Colour);
        Assert.Contains("the theme's own 'WinwrightCaptureBackground'", chosen.Sentence());
    }

    [Fact]
    public void A_theme_that_declares_its_palette_as_colours_is_a_theme_and_not_a_mistake()
    {
        var chosen = OnStaThread(() =>
        {
            var page = new Border { Width = 40, Height = 20 };
            page.Resources[Backgrounds.DefaultKey] = Dark;
            return Backgrounds.Choose(page);
        });

        Assert.Equal(Backdrop.Theme, chosen.From);
        Assert.Equal(Dark, chosen.Colour);
    }

    [Fact]
    public void With_no_key_declared_the_window_colour_that_was_observed_answers()
    {
        var chosen = OnStaThread(() =>
        {
            var page = new Border { Width = 40, Height = 20 };
            _ = new Window { Background = new SolidColorBrush(Dark), Content = page };
            return Backgrounds.Choose(page);
        });

        Assert.Equal(Backdrop.ObservedWindow, chosen.From);
        Assert.Equal(Dark, chosen.Colour);
        Assert.Contains("read off what the window is painted with", chosen.Sentence());
    }

    [Fact]
    public void The_key_wins_over_the_window_because_the_application_knows_what_it_is_painting_on()
    {
        var chosen = OnStaThread(() =>
        {
            var page = new Border { Width = 40, Height = 20 };
            page.Resources[Backgrounds.DefaultKey] = new SolidColorBrush(Colors.Green);
            _ = new Window { Background = new SolidColorBrush(Dark), Content = page };
            return Backgrounds.Choose(page);
        });

        Assert.Equal(Backdrop.Theme, chosen.From);
        Assert.Equal(Colors.Green, chosen.Colour);
    }

    [Fact]
    public void Neither_source_answering_is_stated_and_never_quietly_turned_into_white()
    {
        // The whole of the defect. A palette consulted here would answer white on the dark machine
        // above, and the capture would come back correct in every respect and unreadable.
        var chosen = OnStaThread(() => Backgrounds.Choose(new Border { Width = 40, Height = 20 }));

        Assert.False(chosen.Answered);
        Assert.Equal(Backdrop.Unanswered, chosen.From);
        Assert.Null(chosen.Colour);
        Assert.Contains("nothing says what to draw the capture on", chosen.Sentence());
        Assert.Equal("nothing", chosen.ToString());
    }

    [Fact]
    public void A_window_painted_with_no_brush_of_its_own_is_no_answer_either()
    {
        var chosen = OnStaThread(() =>
        {
            var page = new Border { Width = 40, Height = 20 };
            _ = new Window { Background = null, Content = page };
            return Backgrounds.Choose(page);
        });

        Assert.False(chosen.Answered);
        Assert.Contains("painted with no brush of its own", chosen.Because);
    }

    [Fact]
    public void A_fully_transparent_brush_is_not_a_background()
    {
        // It composes to exactly the capture that has no background at all, which is the picture
        // this reading exists to stop being written.
        var chosen = OnStaThread(() =>
        {
            var page = new Border { Width = 40, Height = 20 };
            page.Resources[Backgrounds.DefaultKey] = new SolidColorBrush(Colors.Transparent);
            return Backgrounds.Choose(page);
        });

        Assert.False(chosen.Answered);
    }

    [Fact]
    public void Insisting_refuses_where_nothing_answered_and_says_what_to_declare()
    {
        var refused = Assert.Throws<NoBackgroundException>(
            () => OnStaThread(() => Backgrounds.Insist(new Border { Width = 40, Height = 20 })));

        Assert.Contains("Declare a brush under 'WinwrightCaptureBackground'", refused.Message);
        Assert.Contains("or give the window a background of its own", refused.Message);
    }

    [Fact]
    public void An_application_may_name_its_own_key()
    {
        var chosen = OnStaThread(() =>
        {
            var page = new Border { Width = 40, Height = 20 };
            page.Resources["CaptureBackdrop"] = new SolidColorBrush(Dark);
            return Backgrounds.Choose(page, "CaptureBackdrop");
        });

        Assert.Equal(Backdrop.Theme, chosen.From);
        Assert.Equal("CaptureBackdrop", chosen.Key);
    }

    [Fact]
    public void The_brush_handed_back_is_a_frozen_copy_so_the_theme_can_still_switch()
    {
        // Both read on the thread that owns them. The unfrozen original cannot be read from here
        // at all, which is the same rule this whole file is careful about.
        var (handedBack, theApplications) = OnStaThread(() =>
        {
            var page = new Border { Width = 40, Height = 20 };
            var brush = new SolidColorBrush(Dark);
            page.Resources[Backgrounds.DefaultKey] = brush;
            return (Backgrounds.Choose(page).Brush!.IsFrozen, brush.IsFrozen);
        });

        Assert.True(handedBack);

        // The application's own brush is untouched: freezing that one would make its next theme
        // switch throw, from inside a capture nobody would think to blame.
        Assert.False(theApplications);
    }

    [Fact]
    public void A_capture_on_the_chosen_background_names_the_source_in_its_receipt()
    {
        var path = Path.Combine(root, "themed.png");

        var picture = OnStaThread(() =>
        {
            var page = new Border { Width = 40, Height = 20 };
            page.Resources[Backgrounds.DefaultKey] = new SolidColorBrush(Dark);
            return Render.ToFile(page, path, Backgrounds.Choose(page));
        });

        Assert.Contains("#FF1E1E1E (the theme's 'WinwrightCaptureBackground')", picture.Background);
        Assert.Contains("the theme's", picture.Sentence());
    }

    [Fact]
    public void A_page_that_drew_nothing_on_the_chosen_background_is_still_a_written_picture()
    {
        // The two readings are separate on purpose: the background makes the file readable, and
        // whether the tree drew anything is the alpha scan, which this can no longer answer.
        var path = Path.Combine(root, "blankonbackground.png");

        OnStaThread(() =>
        {
            var page = new Border { Width = 40, Height = 20 };
            page.Resources[Backgrounds.DefaultKey] = new SolidColorBrush(Dark);
            return Render.ToFile(page, path, Backgrounds.Choose(page));
        });

        Assert.False(Pictures.Of(path).IsBlank);
    }
}
