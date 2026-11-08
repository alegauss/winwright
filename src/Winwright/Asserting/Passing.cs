using System.Windows.Automation;

namespace Winwright.Asserting;

/// <summary>
/// A watch for one declared string showing anywhere in a window, armed before an act so that a line
/// the act's handler shows and takes away again is still seen. WW608.
/// <para>
/// Found by claude-tray (T435): a rescan's invoke returns before its handler runs, the page shows
/// <c>Scanning…</c> while the handler works, and a read after the invoke could not tell a rescan that
/// kept the selection from one that had not started. The line is the evidence the rescan happened,
/// and it is gone by the time anything reads.
/// </para>
/// <para>
/// Two ears and a look. A name changing anywhere under the window is heard as it happens, which is
/// what catches a flash shorter than a poll; an element arriving is heard too, and its name read as it
/// arrives, because a new element carries its name from the start and changes nothing. The look on
/// every poll is the third, for a provider that raises no events at all.
/// </para>
/// </summary>
internal sealed class Passing : IDisposable
{
    private readonly AutomationElement root;
    private readonly Label watched;
    private readonly AutomationPropertyChangedEventHandler named;
    private readonly StructureChangedEventHandler arrived;
    private readonly bool listening;
    private string? seen;

    private Passing(AutomationElement root, Label watched)
    {
        this.root = root;
        this.watched = watched;

        // Read before anything listens: a line already showing says nothing about the act to come.
        Before = Loading.Sighted(root, watched).Text;

        named = (_, changed) => Heard(changed.NewValue as string);

        // An element that arrives carries its name from the start and raises no name change, so its
        // name is read here, as it arrives: a line added and taken away between two polls is still heard.
        arrived = (sender, _) =>
        {
            try
            {
                if (sender is AutomationElement element)
                    Heard(element.Current.Name);
            }
            catch (Exception gone) when (gone is ElementNotAvailableException or InvalidOperationException
                or System.Runtime.InteropServices.COMException)
            {
                // Gone before it could be read, which is the flash this cannot catch.
            }
        };

        try
        {
            Automation.AddAutomationPropertyChangedEventHandler(root, TreeScope.Subtree, named, AutomationElement.NameProperty);
            Automation.AddStructureChangedEventHandler(root, TreeScope.Subtree, arrived);
            listening = true;
        }
        catch (Exception deaf) when (deaf is ElementNotAvailableException or InvalidOperationException
            or System.Runtime.InteropServices.COMException)
        {
            // A window that will not take a listener is still looked at on every poll; the claim
            // is weaker for it, and the looks are what is left.
            listening = false;
        }
    }

    /// <summary>Where the string was already showing before the act, or null where it was not.</summary>
    public string? Before { get; }

    /// <summary>Whether the window took the listeners, or only the looks are watching.</summary>
    public bool Listening => listening;

    /// <summary>Keep the first name heard that carries the string.</summary>
    private void Heard(string? name)
    {
        if (name is not null && name.Contains(watched.Text, StringComparison.Ordinal))
            Interlocked.CompareExchange(ref seen, $"'{name}'", null);
    }

    /// <summary>Arm a watch for <paramref name="watched"/> on <paramref name="root"/>.</summary>
    /// <param name="root">The window the string may show anywhere in.</param>
    /// <param name="watched">The string, resolved for the language the run is in.</param>
    public static Passing Arm(AutomationElement root, Label watched)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(watched);
        return new Passing(root, watched);
    }

    /// <summary>
    /// What carried the string since the watch was armed, looking once more first; or null where
    /// nothing has yet.
    /// </summary>
    public string? Seen()
    {
        if (Volatile.Read(ref seen) is { } heard)
            return heard;

        if (Loading.Sighted(root, watched).Text is { } looked)
            Interlocked.CompareExchange(ref seen, looked, null);

        return Volatile.Read(ref seen);
    }

    /// <summary>The string being watched for, as the run reads it.</summary>
    public string Text => watched.Text;

    /// <summary>The key it was declared under.</summary>
    public string Key => watched.Key;

    /// <inheritdoc />
    public void Dispose()
    {
        if (!listening)
            return;

        try
        {
            Automation.RemoveAutomationPropertyChangedEventHandler(root, named);
            Automation.RemoveStructureChangedEventHandler(root, arrived);
        }
        catch (Exception gone) when (gone is ElementNotAvailableException or InvalidOperationException
            or System.Runtime.InteropServices.COMException)
        {
            // The window went with its listeners; there is nothing left to take them off.
        }
    }
}
