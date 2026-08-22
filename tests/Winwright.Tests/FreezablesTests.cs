using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Winwright.InApp;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW73. A brush is a freezable, and an unfrozen one belongs to the thread that made it — so a
/// static brush belongs to whichever thread reached the class first and every capture thread after
/// that is refused.
/// <para>
/// It was reproduced twice in this repository's own tests before the rule was written: a
/// <c>SolidColorBrush</c> built on the test thread and composed on the render thread, and a brush
/// read back from the wrong side of a render. Both arrived as "cannot use a DependencyObject that
/// belongs to a different thread than its parent Freezable", which names neither the brush nor
/// either thread.
/// </para>
/// </summary>
public sealed class FreezablesTests
{
    [Fact]
    public void A_frozen_brush_belongs_to_nobody_so_every_thread_reaches_it()
    {
        var shared = Apartment.Run(() => Freezables.Shared(new SolidColorBrush(Colors.Red)));

        Assert.True(shared.IsFrozen);

        // Read here, on the thread that did not make it. Without the freeze this is the throw.
        Assert.Equal(Colors.Red, ((SolidColorBrush)shared).Color);
        Assert.True(Freezables.Reaches(shared));
    }

    [Fact]
    public void The_caller_s_own_brush_is_left_alone_because_freezing_it_would_break_them_later()
    {
        var (sharedIsFrozen, theirsIsFrozen) = Apartment.Run(() =>
        {
            var theirs = new SolidColorBrush(Colors.Red);
            return (Freezables.Shared(theirs).IsFrozen, theirs.IsFrozen);
        });

        Assert.True(sharedIsFrozen);
        Assert.False(theirsIsFrozen);
    }

    [Fact]
    public void A_brush_from_another_thread_is_refused_for_a_reason_about_threading()
    {
        var theirs = Apartment.Run(() => new SolidColorBrush(Colors.Red));

        var refused = Assert.Throws<ThreadBoundException>(() => Freezables.Shared(theirs));

        // Both threads named, which is the whole of what the exception it replaces does not say.
        Assert.Contains("SolidColorBrush that is not frozen", refused.Message);
        Assert.Contains("belongs to thread", refused.Message);
        Assert.Contains("make one per capture thread", refused.Message);
    }

    [Fact]
    public void A_brush_that_cannot_be_frozen_stays_bound_however_it_is_passed_around()
    {
        // A VisualBrush painting a live tree is the case: cloning it does not free it, so the
        // honest answer is that it belongs to one thread and always will.
        var refused = Assert.Throws<ThreadBoundException>(
            () => Apartment.Run(() => Freezables.Shared(new VisualBrush(new Border { Width = 4, Height = 4 }))));

        Assert.Contains("cannot be frozen", refused.Message);
        Assert.Contains("make one per capture thread", refused.Message);
    }

    [Fact]
    public void Asking_whether_a_thread_reaches_something_never_throws_the_thing_it_is_asking_about()
    {
        var theirs = Apartment.Run(() => new SolidColorBrush(Colors.Red));

        // The point of the whole helper: IsFrozen would throw here, and a check that throws the
        // exception it exists to prevent is no check at all.
        Assert.False(Freezables.Reaches(theirs));
    }

    [Fact]
    public void Shareable_answers_null_rather_than_refusing_where_a_caller_has_a_fallback()
    {
        var theirs = Apartment.Run(() => new SolidColorBrush(Colors.Red));

        Assert.Null(Freezables.Shareable(theirs));
        Assert.Null(Freezables.Shareable(null));
        Assert.NotNull(Freezables.Shareable(Apartment.Run(() => Freezables.Shared(new SolidColorBrush(Colors.Red)))));
    }

    [Fact]
    public void Insisting_says_nothing_about_a_null_so_an_optional_brush_stays_optional()
    {
        var refused = Record.Exception(() => Freezables.Insist(null, "the background brush"));

        Assert.Null(refused);
    }

    [Fact]
    public void A_render_handed_a_brush_from_another_thread_is_refused_before_it_draws()
    {
        // The reproduction, exactly as it happened: a brush built on this thread and composed on
        // the render thread. The refusal now names the brush and both threads.
        var made = Apartment.Run(() => new SolidColorBrush(Colors.Blue));

        var refused = Assert.Throws<ThreadBoundException>(
            () => Apartment.Run(() => Render.ToBitmap(new Border { Width = 10, Height = 10 }, background: made)));

        Assert.Contains("the background brush is a SolidColorBrush that is not frozen", refused.Message);
    }

    [Fact]
    public void A_render_handed_an_element_from_another_thread_is_refused_the_same_way()
    {
        var made = Apartment.Run<FrameworkElement>(() => new Border { Width = 10, Height = 10 });

        var refused = Assert.Throws<ThreadBoundException>(() => Apartment.Run(() => Render.ToBitmap(made)));

        Assert.Contains("the element being rendered is a Border", refused.Message);
    }

    [Fact]
    public void A_frozen_brush_crosses_into_a_render_on_another_thread_without_complaint()
    {
        var shared = Apartment.Run(() => Freezables.Shared(new SolidColorBrush(Colors.Blue)));

        var picture = Apartment.Run(
            () => Render.ToBitmap(new Border { Width = 10, Height = 10 }, background: shared));

        Assert.Equal(10, picture.PixelWidth);
    }

    [Fact]
    public void The_background_a_capture_chose_is_shareable_by_construction()
    {
        // Backgrounds hands back what Freezables says may be shared, so the two cannot drift into
        // one freezing and the other not.
        var chosen = Apartment.Run(() =>
        {
            var page = new Border { Width = 40, Height = 20 };
            page.Resources[Backgrounds.DefaultKey] = new SolidColorBrush(Colors.Black);
            return Backgrounds.Choose(page);
        });

        Assert.NotNull(chosen.Brush);
        Assert.True(Freezables.Reaches(chosen.Brush));
    }
}
