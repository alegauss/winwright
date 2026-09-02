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

    private AnsweringWindow(string into, bool answers)
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
                        hooked = Renders.Answer(built);
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

    /// <summary>The window's handle, which is what a harness sends to.</summary>
    internal nint Handle { get; }

    /// <summary>What it says it is answering, read on its own thread.</summary>
    internal string Sentence() => answering is null ? "answering nothing at all." : dispatcher.Invoke(answering.Sentence);

    /// <summary>Open one that answers renders into <paramref name="into"/>.</summary>
    /// <param name="into">The directory it may write into.</param>
    internal static AnsweringWindow Open(string into) => new(into, answers: true);

    /// <summary>
    /// Open one that does not answer at all, which is every application that has not called
    /// <c>Renders.Answer</c> — and is what the harness has to report rather than hang on.
    /// </summary>
    internal static AnsweringWindow Silent() => new("", answers: false);

    /// <summary>Close it and let its thread go.</summary>
    public void Dispose()
    {
        try
        {
            dispatcher.Invoke(() =>
            {
                answering?.Dispose();
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
