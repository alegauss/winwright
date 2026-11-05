using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

using Winwright.InApp;

namespace Winwright.Tests;

/// <summary>
/// An open WPF popup on a thread that pumps its own dispatcher, which is the surface WW347 is about
/// and the one this suite had never put on a desk.
/// <para>
/// It is the whole of the defect standing up: a popup a framework drew is its own top-level window,
/// and the framework draws the drop shadow behind it itself. Measured here and in the design that
/// filed it — <c>style=0x96000000 ex=0x08080088</c>, which is WS_POPUP with no caption, layered with
/// an alpha per pixel, owned by nothing and visible. So the route calls it a popup, correctly, and
/// sends it to the screen copy; and WW334 refuses that copy, correctly as well.
/// </para>
/// <para>
/// A whole dispatcher rather than <see cref="Apartment" />, and the difference is measured rather
/// than assumed: the popup's window exists the moment <c>IsOpen</c> is set, with nothing pumping at
/// all, but the thread that owns it has to outlive the call for anything to read it — and a thread
/// parked on a wait handle is not what an application is.
/// </para>
/// </summary>
internal sealed class PumpedFlyout : IDisposable
{
    private readonly Thread thread;
    private readonly Popup flyout;
    private readonly Dispatcher dispatcher;

    private PumpedFlyout(string named, int width, int height)
    {
        using var ready = new ManualResetEventSlim();
        Popup? built = null;
        Dispatcher? pumping = null;
        Exception? broke = null;

        thread = new Thread(() =>
        {
            try
            {
                pumping = Dispatcher.CurrentDispatcher;
                built = new Popup
                {
                    Name = named,
                    AllowsTransparency = true,
                    StaysOpen = true,

                    // Absolute, and clear of where this suite's other windows go: a popup with no
                    // placement target is placed at the origin, and this one is topmost by the style
                    // bits WPF gives it — so leaving it at 60,60 would stand it over the pumped
                    // dialog every capture case in this collection photographs. Nothing here depends
                    // on the position beyond it being somewhere a desk can show it.
                    Placement = PlacementMode.Absolute,
                    HorizontalOffset = 620,
                    VerticalOffset = 60,
                    Child = new Border
                    {
                        Width = width,
                        Height = height,
                        Background = new SolidColorBrush(Colors.CornflowerBlue),
                        BorderBrush = new SolidColorBrush(Colors.DarkSlateGray),
                        BorderThickness = new Thickness(2),
                        Child = new TextBlock { Text = named, Margin = new Thickness(8) },
                    },
                };

                built.IsOpen = true;
            }
            catch (Exception raised)
            {
                broke = raised;
            }
            finally
            {
                ready.Set();
            }

            if (broke is null)
                Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = $"winwright: {named}",
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (!ready.Wait(TimeSpan.FromSeconds(20)))
            throw new TimeoutException($"the flyout '{named}' never opened");

        if (broke is not null)
            throw new InvalidOperationException($"the flyout '{named}' would not open", broke);

        flyout = built!;
        dispatcher = pumping!;

        // Read on the popup's own thread, because a presentation source is as thread-bound as
        // everything else behind it. Zero where the popup put up no window, which is a state this
        // fixture reports rather than hides: a case asserting against handle zero is a case that
        // would otherwise assert against whatever window came back next.
        Handle = dispatcher.Invoke(() =>
            PresentationSource.FromVisual(flyout.Child) is HwndSource source ? source.Handle : 0);
    }

    /// <summary>The popup's own top-level window, or zero where it put none up.</summary>
    internal nint Handle { get; }

    /// <summary>
    /// Open one, blocking until its thread has built it and is pumping.
    /// <para>
    /// The name has to be an identifier, and that is WPF's rule rather than this fixture's:
    /// <c>Popup.Name</c> is the XAML name, so a phrase with a space in it is refused by the property
    /// setter with a sentence about a value not being valid. Found by the first guest run, which is
    /// the only place it could be found — the refusal is raised on the popup's own thread.
    /// </para>
    /// </summary>
    /// <param name="named">What to call it, which is also what it draws. An identifier.</param>
    /// <param name="width">The child's width in device-independent units.</param>
    /// <param name="height">Its height.</param>
    internal static PumpedFlyout Open(string named, int width = 160, int height = 90) =>
        new(named, width, height);

    /// <summary>
    /// Photograph it through the door WW347 opened, on the thread that owns it. The marshalling is
    /// the fixture's business rather than a case's: every element behind this belongs to that
    /// thread, and a case that forgot would be told about threading instead of about the picture.
    /// </summary>
    /// <param name="path">Where to write the PNG.</param>
    internal RenderedPicture Picture(string path) => dispatcher.Invoke(() => Popups.Picture(flyout, path));

    /// <summary>What the popup's child settled on, in device-independent units.</summary>
    internal Size Laid() => dispatcher.Invoke(() => ((FrameworkElement)flyout.Child).RenderSize);

    /// <summary>Shut the popup and let its thread go.</summary>
    public void Dispose()
    {
        // The popup first and the dispatcher second: shutting the dispatcher down leaves the window
        // to be torn down by the thread ending, and a run whose next class counts this process's
        // windows would find one on its way out.
        try
        {
            dispatcher.Invoke(() => flyout.IsOpen = false);
        }
        catch (TaskCanceledException)
        {
            // Its dispatcher went first. Nothing left to close, and nothing worth raising over.
        }

        dispatcher.InvokeShutdown();

        if (!thread.Join(TimeSpan.FromSeconds(5)))
            throw new InvalidOperationException($"a flyout would not close: 0x{Handle:X} is still up");
    }
}
