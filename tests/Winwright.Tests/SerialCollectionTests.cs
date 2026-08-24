using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW125. Some classes that create windows or launch processes declared the serial collection and
/// some did not, so xUnit ran the ones that did not beside the ones that did.
/// <para>
/// Measured across one session: the suite went red three times on runs where nothing about the
/// code under test had changed — twenty-one failures once, seven another, five on the run that
/// provoked this — and green on the next run every time. Every failure was in a class that needs
/// the foreground. A window taking it in one thread is exactly the condition the other thread is
/// trying to measure, and the run that loses reports a failure about the code.
/// </para>
/// <para>
/// This is not the same as running the fixtures off-screen or on a desktop of their own, both of
/// which are filed and both of which are larger. It is the cheap half, and it is a check rather
/// than a convention because a convention is what the seven classes below were already breaking.
/// </para>
/// </summary>
public sealed class SerialCollectionTests
{
    /// <summary>
    /// What makes a class one that touches the desktop. Each one puts a real window or a real
    /// process in front of the machine: a raw Win32 window, a WPF-hosted one, either fixture that
    /// pumps its own, or anything that starts a process of its own.
    /// </summary>
    private static readonly string[] Touches =
    [
        "CreateWindowExW",
        "HwndSource",
        "PumpedDialog",
        "TrayIconFixture",
        "ProcessStartInfo",
    ];

    [Fact]
    public void Every_class_that_touches_the_desktop_declares_the_serial_collection()
    {
        var outside = new List<string>();

        foreach (var file in Directory.EnumerateFiles(Sources(), "*Tests.cs"))
        {
            // This file names every marker it looks for, so it would always match itself.
            if (Path.GetFileName(file) == $"{nameof(SerialCollectionTests)}.cs")
                continue;

            // WW198. Code and never prose. A case that named one of these fixtures in a comment to
            // explain a catalogue entry was reported as a class that puts a window in front of the
            // machine — which is the third sweep in this suite to read what somebody wrote about a
            // call as the call itself, and the reason Checkout carries the answer.
            var text = string.Join('\n', File.ReadLines(file).Select(Checkout.Code));

            if (Touches.Any(one => text.Contains(one, StringComparison.Ordinal))
                && !text.Contains(nameof(WindowFixture.Serial), StringComparison.Ordinal))
            {
                outside.Add(Path.GetFileName(file));
            }
        }

        Assert.True(
            outside.Count == 0,
            "these classes put a window or a process in front of the machine and run beside the checks that "
                + $"need the foreground: {string.Join(", ", outside.Order(StringComparer.Ordinal))}");
    }

    [Fact]
    public void The_collection_it_names_is_the_one_that_disables_parallelism()
    {
        // Naming a collection nobody defined would serialise nothing at all, silently.
        var definition = typeof(WindowFixture)
            .GetCustomAttributes(typeof(CollectionDefinitionAttribute), inherit: false)
            .Cast<CollectionDefinitionAttribute>()
            .Single();

        Assert.True(definition.DisableParallelization);
        Assert.Equal("windows in this process", WindowFixture.Serial);
    }

    private static string Sources() => Checkout.At("tests", "Winwright.Tests");
}
