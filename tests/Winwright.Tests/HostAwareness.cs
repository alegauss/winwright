using System.Runtime.CompilerServices;

using Winwright.InApp;

namespace Winwright.Tests;

/// <summary>
/// WW121. The test host declares its own display awareness, before anything in it draws.
/// <para>
/// This is not a workaround for the suite: it is the obligation an adopting application carries
/// and cannot delegate to a package it references. Whoever touches the presentation stack first
/// fixes the process awareness, it is set once, and a library is always loaded afterwards — so an
/// application that wants per-monitor coordinates declares them in its manifest or in the first
/// line of its entry point, and this module initializer is that first line.
/// </para>
/// <para>
/// Left undeclared, this suite is green or red depending on which assembly a run happens to load
/// first: selecting the render tests beside the awareness tests turned five of them red on a tree
/// with nothing wrong with it, and every earlier full run passed on an ordering that loaded the
/// engine before anything touched a window.
/// </para>
/// </summary>
internal static class HostAwareness
{
    // CA2255 says a module initialiser belongs in application code, and this is application code:
    // the test host is the application here, and it is the only thing that can be first.
#pragma warning disable CA2255
    [ModuleInitializer]
    internal static void Declare() => Coordinates.Ensure();
#pragma warning restore CA2255
}
