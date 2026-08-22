using System.Runtime.InteropServices;

namespace Winwright.InApp;

/// <summary>
/// Per-monitor awareness: asked for as this package loads, and reported whatever the answer is.
/// <para>
/// Whoever touches the presentation stack first fixes the process awareness, and it is set once —
/// every later call is refused. Measured on this machine, a plain windows application is already
/// system-aware before any code of its own runs, and constructing one WPF element fixes it where
/// it is not. A library loaded afterwards therefore cannot change it, and this one is always
/// loaded afterwards: an application references it, so its own startup got there first.
/// </para>
/// <para>
/// So this asks, because loading early is the one case it can win, and then it <em>reports</em>,
/// which is the part that matters. A render in a system-aware process on a scaled display produces
/// a picture whose size does not mean what it says, and a receipt that says nothing about which
/// space it was drawn in is a receipt nobody can disbelieve.
/// </para>
/// <para>
/// Declaring it is the application's own obligation and cannot be delegated to a package it
/// references: an app manifest, or the first line of its entry point, before any window exists.
/// The sequence below is the engine's, written twice on purpose — one package referencing the
/// other to share thirty lines would cost the separation that makes this half safe to ship inside
/// somebody's product.
/// </para>
/// </summary>
public static class Coordinates
{
    private const int PerMonitorAwareV2 = -4;
    private const int PerMonitorAware = 2;

    private static readonly Lock Gate = new();
    private static string? settled;

    // CA2255 says a module initialiser belongs in application code. Here the side effect is the
    // purpose: a process that references this assembly is about to render something whose size
    // has to mean what it says, and asking at the first render would already be too late.
#pragma warning disable CA2255
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void OnLoad() => Ensure();
#pragma warning restore CA2255

    /// <summary>
    /// Ask for per-monitor awareness, falling back through the older calls. The answer is what
    /// this process has now, read back rather than assumed from whichever call returned true — a
    /// host that declared its awareness in a manifest owns that decision and keeps it.
    /// </summary>
    public static string Ensure()
    {
        lock (Gate)
        {
            settled ??= Ask();
            return settled;
        }
    }

    /// <summary>Whether a rectangle read now is in the space the window actually lives in.</summary>
    public static bool Trustworthy => Current() == PerMonitorAware;

    /// <summary>
    /// What this process can see, in the words a receipt carries. Said either way, because the
    /// reading that matters is the one nobody would have gone looking for.
    /// </summary>
    public static string Sentence() => Current() switch
    {
        PerMonitorAware => "per-monitor aware",
        1 => "system-aware, which is right on one display and wrong on the others",
        0 => "unaware of the display's scaling, so this size is virtualised",
        _ => "of an awareness Windows would not name",
    };

    private static string Ask()
    {
        if (Current() == PerMonitorAware)
            return "it was already set";

        var how = "nothing took";
        if (Try(() => SetProcessDpiAwarenessContext(PerMonitorAwareV2)))
            how = "SetProcessDpiAwarenessContext";
        else if (Try(() => SetProcessDpiAwareness(PerMonitorAware) == 0))
            how = "SetProcessDpiAwareness";
        else if (Try(SetProcessDPIAware))
            how = "SetProcessDPIAware";

        return Current() == PerMonitorAware ? how : $"{how}, and this process is not per-monitor aware";
    }

    private static int Current()
    {
        try
        {
            // PROCESS_DPI_AWARENESS counts from zero, and reading it as one-based is a mistake
            // this project has already made once: a per-monitor process came back as "system".
            return GetProcessDpiAwareness(0, out var awareness) == 0 ? awareness : -1;
        }
        catch (Exception missing) when (missing is EntryPointNotFoundException or DllNotFoundException)
        {
            return -1;
        }
    }

    private static bool Try(Func<bool> call)
    {
        try
        {
            return call();
        }
        catch (Exception missing) when (missing is EntryPointNotFoundException or DllNotFoundException)
        {
            return false;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessDpiAwarenessContext(nint context);

    [DllImport("shcore.dll")]
    private static extern int SetProcessDpiAwareness(int awareness);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessDPIAware();

    [DllImport("shcore.dll")]
    private static extern int GetProcessDpiAwareness(nint process, out int awareness);
}
