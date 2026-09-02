using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Winwright.InApp;

/// <summary>
/// What one window is answering with, and how to stop answering.
/// </summary>
public sealed class RendersAnswered : IDisposable
{
    private readonly HwndSource source;
    private readonly HwndSourceHook hook;
    private bool released;

    internal RendersAnswered(HwndSource source, HwndSourceHook hook, string into)
    {
        this.source = source;
        this.hook = hook;
        Into = into;
        source.AddHook(hook);
    }

    /// <summary>The directory this window may write pictures into. Empty where it answers nothing.</summary>
    public string Into { get; }

    /// <summary>Whether this window is answering at all.</summary>
    public bool Answering => !released && Into.Length > 0;

    /// <summary>The one line a report prints, said either way.</summary>
    public string Sentence() => Answering
        ? $"answering renders for 0x{source.Handle:X}, into {Into}."
        : "answering no renders.";

    /// <summary>Stop answering, and leave the window as it was found.</summary>
    public void Dispose()
    {
        if (released)
            return;

        released = true;
        source.RemoveHook(hook);
    }
}

/// <summary>
/// Rendering this application's own tree when a harness asks for it. WW349.
/// <para>
/// The off-screen render is the harness's default route and the one it cannot take: a render draws a
/// visual tree, and nothing outside this process has one. So the harness asks, over
/// <c>WM_COPYDATA</c>, and this is the half that answers — it looks the window up, renders what that
/// window is showing, writes the file it was given and says it did.
/// </para>
/// <para>
/// The message rather than a file this polls, and it costs nothing to leave in a release. An
/// application shipped to its users runs no thread for this, watches no directory and holds no
/// timer: the work happens on the message loop it already runs, only when somebody sends the
/// message, and only where <see cref="PathVariable" /> named somewhere to write.
/// </para>
/// <para>
/// That variable is the whole of the guard and it is a directory rather than a file. A window
/// answering this without one would write a picture of the application anywhere a sender named,
/// which is a thing a shipped product must not do — so a path outside the named directory is refused
/// the same way, and the refusal is the application's rather than the harness's to make.
/// </para>
/// </summary>
public static class Renders
{
    /// <summary>
    /// The variable naming the directory renders may be written into. Unset means answer nothing,
    /// for the reason <see cref="Surfaces.PathVariable" /> means report nothing.
    /// </summary>
    public const string PathVariable = "WINWRIGHT_RENDERS";

    /// <summary>The name both halves register, which is how this message is told from any other.</summary>
    public const string Registered = "Winwright.OwnRender";

    private const uint WmCopyData = 0x004A;

    /// <summary>Where renders may be written, or null where nothing asked for any.</summary>
    public static string? Where()
    {
        var named = Environment.GetEnvironmentVariable(PathVariable);
        return string.IsNullOrWhiteSpace(named) ? null : Path.GetFullPath(named.Trim());
    }

    /// <summary>
    /// Answer renders for this window, for as long as the answer lives.
    /// <para>
    /// Per window rather than once per application, because the harness sends to the window it wants
    /// a picture of: a case that drove a dialog open means the dialog, and an application that
    /// answered only for its main window would hand back a picture of the wrong surface with nothing
    /// in the file saying so.
    /// </para>
    /// </summary>
    /// <param name="window">The window to answer for. It must have been shown, so it has a handle.</param>
    /// <returns>What it is answering, which says nothing is where the variable is unset.</returns>
    /// <exception cref="InvalidOperationException">Where the window has no handle to hook.</exception>
    public static RendersAnswered Answer(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        Freezables.Insist(window, "the window being answered for");

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == 0)
        {
            throw new InvalidOperationException(
                "this window has no handle yet, so there is nothing to hook — show it first, which is "
                    + "also when it becomes a window a harness could ask about");
        }

        return Answer(HwndSource.FromHwnd(handle)
            ?? throw new InvalidOperationException($"0x{handle:X} is a window this presentation stack does not own"));
    }

    /// <summary>The same, for a caller that already has the source.</summary>
    /// <param name="source">The window's presentation source.</param>
    public static RendersAnswered Answer(HwndSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var into = Where();
        var wanted = (uint)RegisterWindowMessageW(Registered);

        // Hooked either way, and answering only where a directory was named. The hook that answers
        // nothing costs one comparison per message and keeps the disposal symmetrical, which is
        // worth more than the branch it saves: a caller holding an answer that never installed
        // anything would still have to put something back.
        return new RendersAnswered(
            source,
            (nint window, int message, nint wParam, nint lParam, ref bool handled) =>
                Handle(into, wanted, window, message, lParam, ref handled),
            into ?? "");
    }

    /// <summary>
    /// Render what one window is showing into a file, refusing where the file is not somewhere this
    /// application may write.
    /// <para>
    /// Its own verb because it is the whole of what the message does, and a message handler nobody
    /// can call directly is a rule with no case behind it: this is what the hook runs and what the
    /// suite drives.
    /// </para>
    /// </summary>
    /// <param name="into">The directory renders may be written into.</param>
    /// <param name="window">The window whose tree is wanted.</param>
    /// <param name="path">Where the picture goes.</param>
    /// <returns>Whether it drew one.</returns>
    public static bool Drawn(string into, nint window, string path)
    {
        if (string.IsNullOrWhiteSpace(into) || string.IsNullOrWhiteSpace(path))
            return false;

        if (!Beneath(Path.GetFullPath(into.Trim()), path))
            return false;

        if (HwndSource.FromHwnd(window)?.RootVisual is not FrameworkElement tree)
            return false;

        try
        {
            // The size it is already showing, and never the one it asks for. A root visual measured
            // against infinite room is a window laid out as though it had no edges, which is a
            // picture of a layout this application has never drawn.
            var settled = tree.RenderSize is { Width: > 0, Height: > 0 } shown ? shown : (Size?)null;
            Render.ToFile(tree, path, settled);
            return true;
        }
        catch (UnrenderableException)
        {
            // A window laid out to nothing. The harness is told by the file not being there, which
            // is a better answer than a picture of nothing — and raising out of a window procedure
            // would take down the application this is only supposed to photograph.
            return false;
        }
        catch (IOException)
        {
            // Somewhere it may write and cannot. Same answer, same reason.
            return false;
        }
    }

    /// <summary>
    /// Whether a path is inside a directory, compared after both are made absolute.
    /// <para>
    /// The separator is appended to the directory before the comparison, which is the whole of it:
    /// without one, <c>C:\pictures-elsewhere</c> is inside <c>C:\pictures</c> by a prefix test, and
    /// the guard this exists to be would have a hole shaped exactly like the thing it refuses.
    /// </para>
    /// </summary>
    /// <param name="directory">The directory, absolute.</param>
    /// <param name="path">The path to judge.</param>
    private static bool Beneath(string directory, string path)
    {
        var full = Path.GetFullPath(path.Trim());
        var under = directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return full.StartsWith(under, StringComparison.OrdinalIgnoreCase);
    }

    private static nint Handle(
        string? into, uint wanted, nint window, int message, nint lParam, ref bool handled)
    {
        if (message != WmCopyData || into is null)
            return 0;

        var carried = Marshal.PtrToStructure<CopyData>(lParam);
        if ((uint)carried.Data != wanted)
            return 0;

        // Handled from here on, whatever the answer: the message was addressed to this and an
        // application that left it unhandled would be passing a harness's request on to its own
        // window procedure, which knows nothing about it.
        handled = true;

        var path = carried.Buffer == 0 || carried.Size <= 0
            ? null
            : Marshal.PtrToStringUni(carried.Buffer, carried.Size / 2)?.TrimEnd('\0');

        return string.IsNullOrWhiteSpace(path) ? 0 : Drawn(into, window, path) ? 1 : 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CopyData
    {
        public nint Data;
        public int Size;
        public nint Buffer;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint RegisterWindowMessageW([MarshalAs(UnmanagedType.LPWStr)] string name);
}
