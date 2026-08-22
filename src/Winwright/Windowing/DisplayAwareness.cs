using System.Runtime.InteropServices;

namespace Winwright.Windowing;

/// <summary>How much of the display this process is allowed to see, in its own words.</summary>
public enum DpiAwareness
{
    /// <summary>Windows would not say.</summary>
    Unknown,

    /// <summary>Everything is virtualised: a rectangle here is not where the window is.</summary>
    Unaware,

    /// <summary>Right on the display the process started on, and wrong on any other.</summary>
    System,

    /// <summary>Right on every display, which is the only answer a capture can use.</summary>
    PerMonitor,
}

/// <summary>What asking for per-monitor awareness did.</summary>
/// <param name="Sees">What the process has now.</param>
/// <param name="How">Which call answered, or that it was already set.</param>
/// <param name="Because">Why it is not per-monitor, where it is not.</param>
public sealed record Awareness(DpiAwareness Sees, string How, string? Because)
{
    /// <summary>Whether a rectangle read now is in the space the window actually lives in.</summary>
    public bool Trustworthy => Sees == DpiAwareness.PerMonitor;

    /// <summary>What this process can see, said out loud.</summary>
    public override string ToString()
    {
        var word = Sees switch
        {
            DpiAwareness.Unaware => "unaware of the display's scaling",
            DpiAwareness.System => "system-aware, which is right on one display and wrong on the others",
            DpiAwareness.PerMonitor => "per-monitor aware",
            _ => "of an awareness Windows would not name",
        };

        return Because is null ? $"this process is {word} ({How})." : $"this process is {word} ({How}): {Because}.";
    }
}

/// <summary>
/// Per-monitor awareness, set once and never remembered by an author.
/// <para>
/// Without it the rectangle and the copy sit in different coordinate spaces, and a capture is
/// offset or scaled on any display between 125 and 200 percent. It matters as much for
/// synthesised input: a bounding rectangle is reported in physical pixels and the cursor call
/// takes them, so every click lands somewhere else on a scaled display.
/// </para>
/// <para>
/// It is asked for when this assembly loads, and that placement is the finding rather than the
/// convenience. The first draft asked at the doors that read a rectangle instead, which sounds
/// politer and is wrong: awareness changed partway through a process leaves the windows made
/// before it in one coordinate space and every read after it in another, and a suite that had
/// been green went nine tests red on exactly that. Both harnesses this replaces set it as the
/// first thing they do, and now so does this.
/// </para>
/// <para>
/// A host that declared its own awareness in a manifest keeps it: every call here refuses once
/// the process has one, and the answer is read back rather than assumed from what was asked.
/// </para>
/// </summary>
public static class DisplayAwareness
{
    private const int PerMonitorAwareV2 = -4;
    private const int PerMonitorAware = 2;

    private static readonly Lock Gate = new();
    private static Awareness? settled;

    // CA2255 says a module initialiser belongs in application code, and it is right about the
    // general case: a library with a side effect at load is a library that surprises its host.
    // Here the side effect is the purpose. A process that references this assembly is driving a
    // desktop, and the alternative was measured rather than argued — see the note above the type.
#pragma warning disable CA2255
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void OnLoad() => Ensure();
#pragma warning restore CA2255

    /// <summary>
    /// Ask for per-monitor awareness, falling back through the older calls before giving up. The
    /// answer is what this process has now, which is not always what this call asked for: a host
    /// that declared its awareness in a manifest owns that decision and keeps it.
    /// </summary>
    public static Awareness Ensure()
    {
        lock (Gate)
        {
            if (settled is not null)
                return settled;

            settled = Ask();
            return settled;
        }
    }

    /// <summary>
    /// What this process has right now, read rather than remembered — and read of the
    /// <em>process</em> and not of the calling thread. A thread carries its own context, so a
    /// worker started before the awareness was set answers for itself and not for the run.
    /// </summary>
    public static DpiAwareness Current()
    {
        try
        {
            // PROCESS_DPI_AWARENESS counts from zero, which the first draft of this read as
            // one-based: a per-monitor process came back as "system", so the engine asked for
            // something it already had and then reported having failed to get it.
            return GetProcessDpiAwareness(0, out var awareness) == 0
                ? awareness switch
                {
                    0 => DpiAwareness.Unaware,
                    1 => DpiAwareness.System,
                    2 => DpiAwareness.PerMonitor,
                    _ => DpiAwareness.Unknown,
                }
                : DpiAwareness.Unknown;
        }
        catch (Exception missing)
            when (missing is EntryPointNotFoundException or DllNotFoundException)
        {
            return DpiAwareness.Unknown;
        }
    }

    private static Awareness Ask()
    {
        var before = Current();
        if (before == DpiAwareness.PerMonitor)
            return new Awareness(before, "it was already set", null);

        // The newest call first, then the one before it, then the one that only reaches system
        // awareness. Each refuses once the process has an awareness, which is why the answer is
        // read back rather than inferred from whichever call returned true.
        var how = "nothing took";
        if (Try(() => SetProcessDpiAwarenessContext(PerMonitorAwareV2)))
            how = "SetProcessDpiAwarenessContext";
        else if (Try(() => SetProcessDpiAwareness(PerMonitorAware) == 0))
            how = "SetProcessDpiAwareness";
        else if (Try(SetProcessDPIAware))
            how = "SetProcessDPIAware";

        var after = Current();
        return after == DpiAwareness.PerMonitor
            ? new Awareness(after, how, null)
            : new Awareness(
                after,
                how,
                before == after
                    ? "the awareness was already fixed before this ran, and it cannot be changed twice"
                    : "no call on this build reached per-monitor");
    }

    private static bool Try(Func<bool> call)
    {
        try
        {
            return call();
        }
        catch (Exception missing)
            when (missing is EntryPointNotFoundException or DllNotFoundException)
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
