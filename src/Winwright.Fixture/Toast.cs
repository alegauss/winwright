using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Winwright.Fixture;

/// <summary>
/// A borderless top-level window the process object never names.
/// <para>
/// A toast, a balloon or a menu is a top-level window with no caption and no taskbar button, and
/// <c>MainWindowHandle</c> reports none of them — it looks for a visible, unowned window that has
/// a title, and a toast is not one. A launcher that asked the process which window it had would be
/// handed zero and conclude the application drew nothing.
/// </para>
/// <para>
/// That shape exists here in exactly one real product, and only when its notification happens to
/// fire. Raised on request it makes the enumerating launcher and the frame sequence both
/// developable without waiting on somebody else's schedule.
/// </para>
/// </summary>
public static class Toast
{
    /// <summary>The two ways a run can be asked for one.</summary>
    public static IReadOnlyList<string> Ways { get; } = ["beside", "only"];

    /// <summary>
    /// Raise one.
    /// </summary>
    /// <param name="owner">The window that owns it, or null where the toast is the only window.</param>
    /// <returns>The toast, shown.</returns>
    public static Window Raise(Window? owner)
    {
        var toast = new Window
        {
            // No caption, no taskbar button, no resize border. Every one of those is what stops
            // the process object naming it, and taking any of them away would make it findable
            // the easy way — which is the case this shape exists to be the opposite of.
            Title = "",
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ResizeMode = ResizeMode.NoResize,
            Width = 320,
            Height = 90,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = 200,
            Top = 500,
            Background = new SolidColorBrush(Color.FromRgb(0x1f, 0x23, 0x28)),
            Owner = owner,

            // WW128's rule, and a toast is the shape it matters most for: a real notification
            // appears without stealing what somebody is typing into, and one that took the desk
            // would decide the foreground for every check running after it.
            ShowActivated = false,
            Content = new Border
            {
                Name = "toastBody",
                Padding = new Thickness(16),
                Child = new TextBlock
                {
                    Name = "toastText",
                    Text = "winwright fixture: a window nothing will name",
                    Foreground = new SolidColorBrush(Color.FromRgb(0xd0, 0xd7, 0xde)),
                    TextWrapping = TextWrapping.Wrap,
                },
            },
        };

        toast.Show();
        return toast;
    }
}
