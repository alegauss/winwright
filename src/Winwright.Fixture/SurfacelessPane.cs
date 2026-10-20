using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace Winwright.Fixture;

/// <summary>
/// A page holding a <see cref="D3DImage"/> whose surface never reaches the render. WW552.
/// <para>
/// PhotoBoost's other spelling of its comparator (PB27) was a D3DImage, which is in the visual tree
/// and so is drawn — but a render is a software pass, and a D3DImage reaches one only through the
/// software fallback its back buffer was set with. Without it the region is empty, the picture is the
/// right size, and nothing said so. The render reads the region back now and refuses one that drew
/// nothing; this page is the D3DImage with no surface at all, which is the same empty region.
/// </para>
/// </summary>
public static class SurfacelessPane
{
    /// <summary>The size it is rendered at.</summary>
    public static Size Size { get; } = new(240, 140);

    /// <summary>Build it: a title, and under it the image whose surface never arrives.</summary>
    public static FrameworkElement Build()
    {
        var rows = new StackPanel { Name = "surfacelessRows" };
        rows.Children.Add(new TextBlock { Name = "surfacelessTitle", Text = "A comparator", Height = 24 });
        rows.Children.Add(new Image
        {
            Name = "surfacelessImage",
            Width = 200,
            Height = 100,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Source = new D3DImage(),
        });

        return new Border { Name = "surfacelessPane", Child = rows };
    }
}
