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
    private readonly List<(HwndSource Source, HwndSourceHook Hook)> hooked = [];
    private bool released;

    internal RendersAnswered(HwndSource source, HwndSourceHook hook, string into)
        : this(into) => Also(source, hook);

    /// <summary>
    /// One that starts empty and is given windows as they arrive. WW361.
    /// </summary>
    /// <param name="into">The directory those windows may write into.</param>
    internal RendersAnswered(string into) => Into = into;

    /// <summary>The directory these windows may write pictures into. Empty where they answer nothing.</summary>
    public string Into { get; }

    /// <summary>Whether this is answering at all.</summary>
    public bool Answering => !released && Into.Length > 0;

    /// <summary>
    /// How many windows are answering under this. WW361.
    /// <para>
    /// A reading and not a detail: an adopter who hooked one window and meant the application has
    /// no other way to find that out, and being told the count is what turns a silent gap into a
    /// number somebody can disagree with.
    /// </para>
    /// </summary>
    public int Windows
    {
        get
        {
            lock (hooked)
                return released ? 0 : hooked.Count;
        }
    }

    /// <summary>The one line a report prints, said either way.</summary>
    public string Sentence()
    {
        if (!Answering)
            return "answering no renders.";

        lock (hooked)
        {
            var handles = string.Join(", ", hooked.Select(one => $"0x{one.Source.Handle:X}"));
            return $"answering renders for {hooked.Count} window(s) — {handles} — into {Into}.";
        }
    }

    /// <summary>
    /// Take one more window under this answer. WW361.
    /// <para>
    /// Locked, because the windows arrive on their own threads: a WPF application may run more than
    /// one dispatcher, and the class handler that finds a new window runs on whichever thread showed
    /// it. Ignored once released, so a window shown after an application stopped answering does not
    /// quietly start.
    /// </para>
    /// </summary>
    /// <param name="source">The window's presentation source.</param>
    /// <param name="hook">What to run for its messages.</param>
    internal void Also(HwndSource source, HwndSourceHook hook)
    {
        lock (hooked)
        {
            if (released || hooked.Any(one => ReferenceEquals(one.Source, source)))
                return;

            hooked.Add((source, hook));
        }

        source.AddHook(hook);
    }

    /// <summary>Stop answering, and leave every window as it was found.</summary>
    public void Dispose()
    {
        List<(HwndSource Source, HwndSourceHook Hook)> letting;
        lock (hooked)
        {
            if (released)
                return;

            released = true;
            letting = [.. hooked];
            hooked.Clear();
        }

        foreach (var (source, hook) in letting)
        {
            // A source whose window has already gone is one where unhooking is the one thing that no
            // longer needs doing.
            if (source.IsDisposed)
                continue;

            // Never onto another thread. Everything under one answer belongs to one dispatcher by
            // construction, so a caller disposing from elsewhere is a caller doing something this
            // cannot do safely — and blocking on a dispatcher that may not be pumping is how that
            // goes wrong: it wedges, holding the windows open, which is worse than saying so.
            if (!source.CheckAccess())
            {
                throw new InvalidOperationException(
                    $"this answer belongs to another thread, so 0x{source.Handle:X} cannot be put "
                        + "back from here — dispose it on the thread that took it");
            }

            source.RemoveHook(hook);
        }
    }
}

/// <summary>
/// What came of a harness asking for one popup's tree. WW359.
/// <para>
/// The numbers are the wire and are held to by a case: the engine holds no reference to this half,
/// so it spells the same five on <c>OwnRender</c> and a test reads both. They are distinct rather
/// than one bit because the refusals want different things done about them — a name matching
/// nothing is a case naming a popup that is not there, and a name matching two is an ambiguity in
/// the application only its author can settle.
/// </para>
/// </summary>
public enum PopupRendered
{
    /// <summary>Nothing to answer with: no directory was named, or the window is not this stack's.</summary>
    NotAnswered = 0,

    /// <summary>The picture is written.</summary>
    Drawn = 1,

    /// <summary>No popup under that window carries the name asked for.</summary>
    NoSuchPopup = 2,

    /// <summary>More than one does, so which was meant is not this half's to guess.</summary>
    MoreThanOnePopup = 3,

    /// <summary>The popup is holding nothing that can be drawn, or nothing at all.</summary>
    PopupHoldsNothing = 4,

    /// <summary>The file is not inside the directory this application was told it may write into.</summary>
    PathRefused = 5,
}

/// <summary>
/// Why a render did not happen, asked of the application after it did not happen. WW362.
/// <para>
/// The render ask answers one bit, and a run that reads zero cannot tell an application without the
/// in-app half from one that has it and was started without <c>WINWRIGHT_RENDERS</c>. Those want
/// opposite things done: the first is a line somebody adds to the application, the second is the
/// environment it was launched in — and at the attach door, where the run did not launch it, the
/// second is the only one that ever applies and the harness was printing the first.
/// </para>
/// <para>
/// A second ask rather than more answers on the first, and that is deliberate. Widening the render
/// message would make a harness older than the half it is driving read a refusal code as a drawing
/// and then report the file missing, which is a worse sentence about an already-failing step. Asked
/// separately, and only where the render already came back zero, the wire nobody changed stays
/// exactly what it was: an older half answers nothing to a message it has never heard of, which is
/// the sentence that was right about it all along.
/// </para>
/// </summary>
public enum RenderRefusal
{
    /// <summary>Nothing answered, which is an application carrying no in-app half at all.</summary>
    NotAnswered = 0,

    /// <summary>Nothing is wrong here, which after a failed render is a race and not an answer.</summary>
    WouldDraw = 1,

    /// <summary>The half is here and the process was started without a directory it may write into.</summary>
    ToldNowhere = 2,

    /// <summary>It has one, and the file asked for is not inside it.</summary>
    PathRefused = 3,

    /// <summary>The window asked about is not one this presentation stack owns.</summary>
    NotOurWindow = 4,

    /// <summary>It is, and it has laid out to nothing, so there is no picture to take.</summary>
    NothingToDraw = 5,
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

    /// <summary>
    /// The name for the ask that names one popup rather than the window's own tree. WW359.
    /// <para>
    /// A message of its own and not a field added to the one above, so that an application shipping
    /// a half older than the harness driving it simply does not answer this — rather than reading a
    /// two-field payload as a path. Which way that skew runs is not hypothetical: this half is what
    /// an adopter ships, and it reaches them by a release.
    /// </para>
    /// </summary>
    public const string RegisteredPopup = "Winwright.OwnRender.Popup";

    /// <summary>
    /// The name for the ask that answers why a render did not happen. WW362.
    /// <para>
    /// Its own message for the reason the popup ask is one: an application shipping a half older
    /// than the harness driving it has never heard of this, leaves it unhandled and answers nothing
    /// — which is exactly the reading a harness should get about a half that cannot explain itself.
    /// </para>
    /// </summary>
    public const string RegisteredWhy = "Winwright.OwnRender.Why";

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

        // Hooked either way, and answering only where a directory was named. The hook that answers
        // nothing costs one comparison per message and keeps the disposal symmetrical, which is
        // worth more than the branch it saves: a caller holding an answer that never installed
        // anything would still have to put something back.
        return new RendersAnswered(source, Hooking(into), into ?? "");
    }

    /// <summary>
    /// Answer for every window this application shows, including the ones it has not shown yet.
    /// WW361.
    /// <para>
    /// <see cref="Answer(Window)" /> hooks one window and the harness sends to the window it wants a
    /// picture of. Both are right, and together they left the second window an application draws
    /// answering nothing — a dialog, a wizard page, a tool window a run opened on purpose is exactly
    /// the surface somebody reaches for a capture of, and it was the one nobody remembered.
    /// </para>
    /// <para>
    /// Per window stays available and stays correct; what was wrong is that remembering was the
    /// adopter's, with no reading anywhere saying which windows were covered. This is the other
    /// line, and it is the one the README should have been describing: an application says it
    /// answers, once, and every window it ever shows is covered.
    /// </para>
    /// <para>
    /// The design named a second candidate — let the answer say a window is not hooked, rather than
    /// say the application does not take the message, which are the same sentence today and two
    /// different faults. It is not available on its own. The harness sends <c>WM_COPYDATA</c> to one
    /// window, so where nothing is hooked on that window no code of this half runs at all and there
    /// is nobody left to say anything. Telling the two apart needs the process-wide hook first,
    /// which is this; with it, an unhooked window is a window this application does not own.
    /// </para>
    /// <para>
    /// New windows are found by a class handler on <c>Loaded</c>, which fires after the handle
    /// exists. A class handler cannot be unregistered, so disposal makes this one inert rather than
    /// removing it: the answer it belongs to stops answering, and every window it had hooked is put
    /// back.
    /// </para>
    /// <para>
    /// One UI thread's windows, and that bound is deliberate. A class handler is registered for the
    /// whole AppDomain, so without it this takes windows belonging to threads it has no business
    /// touching — and hooking is the owning thread's call to make, which means putting them back is
    /// a blocking <c>Invoke</c> onto a dispatcher that may not be pumping. Measured, not feared:
    /// unbounded, this wedged a suite solid with two windows on the desk and a disposal waiting on a
    /// thread that never answers. An application with a second UI thread calls this on each one,
    /// which is the same rule everything else touching those windows already follows.
    /// </para>
    /// </summary>
    /// <returns>What it is answering, which says nothing is where the variable is unset.</returns>
    public static RendersAnswered Everywhere()
    {
        var into = Where();
        var mine = System.Windows.Threading.Dispatcher.CurrentDispatcher;
        var answering = new RendersAnswered(into ?? "");

        // The windows already up on this thread. The dispatcher is compared rather than trusted:
        // CurrentSources reads as this thread's and hands back other threads' as well, which is how
        // a suite ended up disposing an answer holding a source it could not touch.
        foreach (var source in PresentationSource.CurrentSources.OfType<HwndSource>())
        {
            if (!source.IsDisposed && source.Dispatcher == mine)
                answering.Also(source, Hooking(into));
        }

        // And every window this thread shows after it. Guarded on the answer rather than on a flag
        // of its own, so a disposed answer takes its class handler out of service with it — which is
        // the closest a handler that cannot be removed gets to being removed.
        EventManager.RegisterClassHandler(
            typeof(Window),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) =>
            {
                if (!answering.Answering || sender is not Window shown || shown.Dispatcher != mine)
                    return;

                if (HwndSource.FromHwnd(new WindowInteropHelper(shown).Handle) is { IsDisposed: false } source)
                    answering.Also(source, Hooking(into));
            }));

        return answering;
    }

    /// <summary>
    /// The hook one window answers through. WW361 made it a factory: several windows answer the same
    /// way, and each needs its own delegate to be removed by.
    /// </summary>
    /// <param name="into">The directory renders may be written into, or null where none was named.</param>
    private static HwndSourceHook Hooking(string? into)
    {
        // Registered once per hook rather than once per message: the numbers are the same in every
        // process for the same string, and asking Windows for them inside a window procedure would
        // be a call on every message an application receives.
        var wanted = RegisterWindowMessageW(Registered);
        var wantedPopup = RegisterWindowMessageW(RegisteredPopup);
        var wantedWhy = RegisterWindowMessageW(RegisteredWhy);

        return (nint window, int message, nint wParam, nint lParam, ref bool handled) =>
            Handle(into, wanted, wantedPopup, wantedWhy, window, message, lParam, ref handled);
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
    /// Why a render of this window into this file would not happen. WW362.
    /// <para>
    /// The same checks <see cref="Drawn" /> makes, in the same order, reporting the first that fails
    /// instead of going on to draw. In that order because that is what makes the answer the reason:
    /// a process told nowhere to write would also refuse the path, and saying the path is wrong to
    /// somebody whose real problem is the environment sends them to fix a file that is fine.
    /// </para>
    /// <para>
    /// It draws nothing, which is the point — a caller asking why a picture did not happen must not
    /// be the thing that makes one happen. So the last check is the layout rather than the render:
    /// a tree that has laid out to nothing is what <c>Render</c> refuses, and reading its size
    /// answers that without writing a file this ask was never asked for.
    /// </para>
    /// </summary>
    /// <param name="into">The directory renders may be written into.</param>
    /// <param name="window">The window that was asked about.</param>
    /// <param name="path">The file that was asked for.</param>
    /// <returns>Which check a render would have stopped at, or that none of them would.</returns>
    public static RenderRefusal Refusing(string? into, nint window, string path)
    {
        if (string.IsNullOrWhiteSpace(into))
            return RenderRefusal.ToldNowhere;

        if (string.IsNullOrWhiteSpace(path) || !Beneath(Path.GetFullPath(into.Trim()), path))
            return RenderRefusal.PathRefused;

        if (HwndSource.FromHwnd(window)?.RootVisual is not FrameworkElement tree)
            return RenderRefusal.NotOurWindow;

        return tree.RenderSize is { Width: > 0, Height: > 0 }
            ? RenderRefusal.WouldDraw
            : RenderRefusal.NothingToDraw;
    }

    /// <summary>
    /// Render the tree one named popup is holding into a file, refusing where the name reaches
    /// nothing, reaches more than one thing, or names a file this application may not write. WW359.
    /// <para>
    /// Its own verb for the reason <see cref="Drawn" /> is one: a message handler nothing can call
    /// directly is a rule with no case behind it. This is what the hook runs and what the suite
    /// drives, and every answer below is reachable from a test that never sends a message.
    /// </para>
    /// <para>
    /// The popup is looked up under the window that was asked, not across the application. A harness
    /// sends to the window it drove to the state it means to photograph, and a walk that left that
    /// window would make a name that is unambiguous inside one dialog ambiguous because some other
    /// window happens to spell a popup the same.
    /// </para>
    /// </summary>
    /// <param name="into">The directory renders may be written into.</param>
    /// <param name="window">The window whose tree holds the popup.</param>
    /// <param name="named">The popup's name.</param>
    /// <param name="path">Where the picture goes.</param>
    /// <returns>Which of the five answers this ask came to.</returns>
    public static PopupRendered PopupDrawn(string into, nint window, string named, string path)
    {
        if (string.IsNullOrWhiteSpace(into) || string.IsNullOrWhiteSpace(named) || string.IsNullOrWhiteSpace(path))
            return PopupRendered.NotAnswered;

        if (!Beneath(Path.GetFullPath(into.Trim()), path))
            return PopupRendered.PathRefused;

        if (HwndSource.FromHwnd(window)?.RootVisual is not FrameworkElement tree)
            return PopupRendered.NotAnswered;

        // Ordinal and case-sensitive, which is how the author spelled it and how XAML resolves it.
        // A looser match would make two popups one name reaches, which is the answer below.
        var matching = Popups.Under(tree)
            .Where(one => string.Equals(one.Name, named, StringComparison.Ordinal))
            .ToList();

        if (matching.Count == 0)
            return PopupRendered.NoSuchPopup;

        if (matching.Count > 1)
            return PopupRendered.MoreThanOnePopup;

        try
        {
            Popups.Picture(matching[0], path);
            return PopupRendered.Drawn;
        }
        catch (UnrenderableException)
        {
            // Holding nothing, or holding something with no layout. Said rather than written as an
            // empty file, and never raised: this runs inside a window procedure, and throwing out of
            // one would take down the application this is only supposed to photograph.
            return PopupRendered.PopupHoldsNothing;
        }
        catch (IOException)
        {
            // Somewhere this application may write and still cannot — a locked file, a full disk.
            // Not one of the four refusals, because every one of those is a thing the case or the
            // application could be changed to fix and this is not: it is the same answer the window
            // ask gives, which the harness reports as the application having drawn nothing.
            return PopupRendered.NotAnswered;
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
        string? into,
        uint wanted,
        uint wantedPopup,
        uint wantedWhy,
        nint window,
        int message,
        nint lParam,
        ref bool handled)
    {
        if (message != WmCopyData)
            return 0;

        var carried = Marshal.PtrToStructure<CopyData>(lParam);
        var id = (uint)carried.Data;
        if (id != wanted && id != wantedPopup && id != wantedWhy)
            return 0;

        // Handled from here on, whatever the answer: the message was addressed to this and an
        // application that left it unhandled would be passing a harness's request on to its own
        // window procedure, which knows nothing about it.
        handled = true;

        // Read whole and split, rather than trimmed to the first NUL. WW359's ask carries two fields
        // and WW349's carries one, so the count is what says which arrived — and a window ask read
        // by this parse is still the one field it always was.
        var said = carried.Buffer == 0 || carried.Size <= 0
            ? null
            : Marshal.PtrToStringUni(carried.Buffer, carried.Size / 2);

        var fields = said?.Split('\0') ?? [];
        var path = fields.Length > 0 ? fields[0] : null;
        if (string.IsNullOrWhiteSpace(path))
            return 0;

        // WW362, and above the guard below on purpose: this ask exists to answer for the case where
        // nowhere was named, so it is the one message a process told nothing still speaks about.
        if (id == wantedWhy)
            return (nint)Refusing(into, window, path);

        // And the two that draw answer nothing at all where this process was told nowhere to write,
        // which is the promise the protocol makes about a build shipped to its users.
        if (into is null)
            return 0;

        if (id == wanted)
            return Drawn(into, window, path) ? 1 : 0;

        var named = fields.Length > 1 ? fields[1] : null;
        return string.IsNullOrWhiteSpace(named)
            ? (nint)PopupRendered.NotAnswered
            : (nint)PopupDrawn(into, window, named, path);
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
