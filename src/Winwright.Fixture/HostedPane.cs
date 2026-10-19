using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;

namespace Winwright.Fixture;

/// <summary>
/// A page holding a region drawn by a window of its own: a WinForms panel in a host. WW552.
/// <para>
/// Found by PhotoBoost (PB27, PB40), whose image area is Direct3D in a child window with a DXGI
/// swapchain, hosted in a WPF page. A render draws the visual tree, and a hosted window is not in
/// it, so the picture came out the right size with a hole where the image was and nothing saying so.
/// The render refuses that now, naming the region, and this is the page that makes it refuse.
/// </para>
/// <para>
/// A WinForms panel and not a swapchain, because the property that matters is the one they share:
/// both are an <see cref="System.Windows.Interop.HwndHost"/>, whose pixels belong to a window the
/// visual tree only reserves room for. Direct3D would add a device this fixture does not need to
/// make the same refusal fire.
/// </para>
/// </summary>
public static class HostedPane
{
    /// <summary>The size it is rendered at.</summary>
    public static Size Size { get; } = new(240, 140);

    /// <summary>Build it: a title, and under it the hosted region.</summary>
    public static FrameworkElement Build()
    {
        var rows = new StackPanel { Name = "hostedRows" };
        rows.Children.Add(new TextBlock { Name = "hostedTitle", Text = "A comparator", Height = 24 });
        rows.Children.Add(new WindowsFormsHost
        {
            Name = "hostedImage",
            Width = 200,
            Height = 100,
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = new System.Windows.Forms.Panel { BackColor = System.Drawing.Color.DarkSlateBlue },
        });

        return new Border { Name = "hostedPane", Child = rows };
    }
}
