using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

using Winwright.InApp;

namespace Winwright.Tests;

/// <summary>
/// A WPF window on a thread that pumps its own dispatcher, answering the render a harness asks for.
/// WW349.
/// <para>
/// It is the application half of the message standing up. The engine's ask is a <c>WM_COPYDATA</c>
/// send that waits for the window's own thread to draw the picture and answer, so the thing on the
/// other end has to be a real window with a real message loop — a window built on the test thread
/// would never take the message, and the send would time out against a fixture rather than against
/// the defect.
/// </para>
/// <para>
/// The variable is set around the hook and put back. <c>Renders.Answer</c> reads it once, when the
/// hook goes on, so the window keeps answering into the directory it was given while the process it
/// runs in is left as this suite found it — which matters, because the same variable would otherwise
/// be read by every other case in the run.
/// </para>
/// </summary>
internal sealed class AnsweringWindow : IDisposable
{
    private readonly Thread thread;
    private readonly Dispatcher dispatcher;
    private readonly Window window;
    private readonly RendersAnswered? answering;

    /// <summary>Every window <see cref="AlsoOpen"/> put up, so disposal closes them too. WW361.</summary>
    private readonly List<Window> opened = [];

    private AnsweringWindow(string into, bool answers, bool everywhere = false)
    {
        using var ready = new ManualResetEventSlim();
        Window? built = null;
        Dispatcher? pumping = null;
        RendersAnswered? hooked = null;
        Exception? broke = null;

        thread = new Thread(() =>
        {
            try
            {
                pumping = Dispatcher.CurrentDispatcher;
                built = new Window
                {
                    Title = "winwright answering",
                    Width = 240,
                    Height = 160,

                    // Clear of where this collection's other windows go, for the reason a flyout is:
                    // nothing here reads the screen, but a window over the one a capture case is
                    // photographing is a red in somebody else's file.
                    Left = 620,
                    Top = 300,
                    Background = new SolidColorBrush(Colors.White),
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = "the report", Margin = new Thickness(12) },
                            new Border
                            {
                                Width = 120,
                                Height = 40,
                                Background = new SolidColorBrush(Colors.CornflowerBlue),
                            },

                            // WW359. Closed, and they stay that way: a closed popup draws nothing
                            // into the window, so every case here that photographs the window sees
                            // what it always saw — and closed is the state the popup ask exists for,
                            // because it is the one where there is no window anywhere to copy.
                            new System.Windows.Controls.Primitives.Popup
                            {
                                Name = PopupNamed,
                                Child = new Border
                                {
                                    Width = 90,
                                    Height = 40,
                                    Background = new SolidColorBrush(Colors.Firebrick),
                                },
                            },
                            new System.Windows.Controls.Primitives.Popup { Name = EmptyPopupNamed },
                        },
                    },
                };

                built.Show();
                built.UpdateLayout();

                if (answers)
                {
                    var was = Environment.GetEnvironmentVariable(Renders.PathVariable);
                    Environment.SetEnvironmentVariable(Renders.PathVariable, into);
                    try
                    {
                        // WW361. The two lines an adopter can write, so a case can drive either:
                        // one names this window, and one says the application answers.
                        hooked = everywhere ? Renders.Everywhere() : Renders.Answer(built);
                    }
                    finally
                    {
                        Environment.SetEnvironmentVariable(Renders.PathVariable, was);
                    }
                }
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
            Name = "winwright: answering window",
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (!ready.Wait(TimeSpan.FromSeconds(20)))
            throw new TimeoutException("the answering window never opened");

        if (broke is not null)
            throw new InvalidOperationException("the answering window would not open", broke);

        window = built!;
        dispatcher = pumping!;
        answering = hooked;
        Handle = dispatcher.Invoke(() => new WindowInteropHelper(window).Handle);
    }

    /// <summary>The popup this window holds a drawable tree in. WW359.</summary>
    internal const string PopupNamed = "details";

    /// <summary>The one it holds nothing in, which is a refusal of its own.</summary>
    internal const string EmptyPopupNamed = "hollow";

    /// <summary>The window's handle, which is what a harness sends to.</summary>
    internal nint Handle { get; }

    /// <summary>What it says it is answering, read on its own thread.</summary>
    internal string Sentence() => answering is null ? "answering nothing at all." : dispatcher.Invoke(answering.Sentence);

    /// <summary>How many windows are answering under it, which is nothing where none are. WW361.</summary>
    internal int Windows => answering?.Windows ?? 0;

    /// <summary>Open one that answers renders into <paramref name="into"/>.</summary>
    /// <param name="into">The directory it may write into.</param>
    internal static AnsweringWindow Open(string into) => new(into, answers: true);

    /// <summary>
    /// Open one that does not answer at all, which is every application that has not called
    /// <c>Renders.Answer</c> — and is what the harness has to report rather than hang on.
    /// </summary>
    internal static AnsweringWindow Silent() => new("", answers: false);

    /// <summary>
    /// Open one that answers for the whole application rather than for its first window. WW361.
    /// </summary>
    /// <param name="into">The directory it may write into.</param>
    internal static AnsweringWindow Everywhere(string into) => new(into, answers: true, everywhere: true);

    /// <summary>
    /// Show a second window on the same thread, after the answering was arranged. WW361.
    /// <para>
    /// This is the window the defect was about, and the reason it is opened here rather than in the
    /// constructor: hooked per window it is a window nobody named, and a harness asking it for a
    /// render is told the application does not take the message. Opened afterwards, it is also the
    /// only thing that proves the class handler and not merely the enumeration of what was already
    /// up — those are two different halves of the fix and a window shown first exercises one of them.
    /// </para>
    /// </summary>
    /// <returns>The second window's handle, which is what a harness would send to.</returns>
    internal nint AlsoOpen() => dispatcher.Invoke(() =>
    {
        var second = new Window
        {
            Title = "winwright answering, the second",
            Width = 160,
            Height = 120,

            // Clear of the first, and of where this collection's other windows go.
            Left = 900,
            Top = 300,

            // WW128's rule, and this window has no reason to break it: nothing here reads the
            // screen, and a window that took the desk would decide the foreground for every check
            // running after it.
            ShowActivated = false,
            Background = new SolidColorBrush(Colors.White),
            Content = new Border
            {
                Width = 100,
                Height = 60,
                Background = new SolidColorBrush(Colors.SeaGreen),
            },
        };

        second.Show();
        second.UpdateLayout();
        opened.Add(second);

        return new WindowInteropHelper(second).Handle;
    });

    /// <summary>Close it and let its thread go.</summary>
    public void Dispose()
    {
        try
        {
            dispatcher.Invoke(() =>
            {
                answering?.Dispose();
                foreach (var also in opened)
                    also.Close();

                window.Close();
            });
        }
        catch (TaskCanceledException)
        {
            // Its dispatcher went first. Nothing left to close.
        }

        dispatcher.InvokeShutdown();

        if (!thread.Join(TimeSpan.FromSeconds(5)))
            throw new InvalidOperationException($"an answering window would not close: 0x{Handle:X} is still up");
    }
}
