using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace Winwright.InApp;

/// <summary>
/// Whether a tree is still fading: an element whose opacity an animation is carrying, and which has
/// moved since the last time a render looked. WW583.
/// <para>
/// Found in claude-tray (T390): a page fades in when it is shown, and an in-app render asked for in
/// that moment drew the page at about a tenth of its opacity — every picture washed out and every
/// case green, because a render answers what the tree is and the tree was, at that instant, faint.
/// The same page drawn a moment later was crisp.
/// </para>
/// <para>
/// An animated opacity is not enough to say so. A fade-out that finished and holds 0, or a dim that
/// holds 0.4, sits there for good, and refusing it would refuse that window forever. What tells a
/// fade under way from one that holds is that it moves, which takes two looks: the first look at an
/// animated element remembers its opacity and refuses, and a later look draws once the value has
/// stopped changing. The harness asks again a moment later for exactly this, so a held state costs
/// one more ask and a fade costs as long as it runs.
/// </para>
/// <para>
/// The first look refuses whatever the value reads, and that is measured rather than cautious: an
/// animation begun and not yet ticked reads as the value it settles at, and the guest drew a fade-in
/// at full opacity two milliseconds before it started.
/// </para>
/// </summary>
public static class Fading
{
    /// <summary>What each animated element's opacity was the last time a render looked.</summary>
    private static readonly ConditionalWeakTable<UIElement, StrongBox<double>> Seen = [];

    /// <summary>The trees whose last render was refused for something in them moving.</summary>
    private static readonly ConditionalWeakTable<DependencyObject, object> Refused = [];

    /// <summary>
    /// The first element under <paramref name="root"/> whose animated opacity has moved since it was
    /// last looked at, or has not been looked at yet, said as a sentence names it; null where nothing
    /// is fading. Remembers what it read, so the next look can tell a fade from a held value.
    /// </summary>
    /// <param name="root">The tree a render is about to draw.</param>
    public static string? Moving(DependencyObject root)
    {
        ArgumentNullException.ThrowIfNull(root);

        string? first = null;
        foreach (var (element, now, settles) in Animated(root))
        {
            // Any change at all and not a tolerance: a sixty-second fade moves about a sixth of a
            // percent between two asks, and a value an animation holds is the same double each look.
            var moved = !Seen.TryGetValue(element, out var was) || was.Value != now;
            Seen.AddOrUpdate(element, new StrongBox<double>(now));

            if (moved && first is null)
                first = Said(element, now, settles);
        }

        if (first is null)
            Refused.Remove(root);
        else
            Refused.AddOrUpdate(root, first);

        return first;
    }

    /// <summary>
    /// What the last render of <paramref name="root"/> was refused for, where it was refused for
    /// something moving and something in it is still animated; null otherwise. Remembers nothing:
    /// this is the answer to why a render did not happen, and asking why must not change what the
    /// next render sees — so it reads the refusal the render made rather than looking again.
    /// </summary>
    /// <param name="root">The tree a render would draw.</param>
    public static string? Away(DependencyObject root)
    {
        ArgumentNullException.ThrowIfNull(root);

        return Refused.TryGetValue(root, out var said) && Animated(root).Any() ? (string)said : null;
    }

    /// <summary>Every element whose opacity an animation supplies.</summary>
    private static IEnumerable<(UIElement Element, double Now, double Settles)> Animated(DependencyObject root)
    {
        var pending = new Stack<DependencyObject>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var here = pending.Pop();
            if (here is UIElement element
                && DependencyPropertyHelper.GetValueSource(element, UIElement.OpacityProperty).IsAnimated)
            {
                yield return (element, element.Opacity, (double)element.GetAnimationBaseValue(UIElement.OpacityProperty));
            }

            var children = VisualTreeHelper.GetChildrenCount(here);
            for (var at = 0; at < children; at++)
                pending.Push(VisualTreeHelper.GetChild(here, at));
        }
    }

    private static string Said(UIElement element, double now, double settles)
    {
        var named = element is FrameworkElement { Name.Length: > 0 } framed
            ? $"{element.GetType().Name} '{framed.Name}'"
            : element.GetType().Name;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{named} is fading: its opacity is {now:0.00} and settles at {settles:0.00}");
    }
}
