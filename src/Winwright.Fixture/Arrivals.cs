using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace Winwright.Fixture;

/// <summary>
/// Every character Windows delivers to this window, written where a check can read it.
/// <para>
/// WW249. The case proving that typing reaches a WPF box fails intermittently, and five reds fit one
/// rule: a single character is overwritten by the last one sent, length for length, never lost. The
/// typing path was read for a defect that would produce that and has none — one input pair per UTF-16
/// code unit, each a fresh struct, one <c>SendInput</c>, and the union written explicitly so its size
/// is right on x64.
/// </para>
/// <para>
/// So what was owed was never another hypothesis. It is a reading of what actually reaches the box,
/// and this is it: the window's own <c>WM_CHAR</c> messages, recorded in order, below WPF entirely.
/// With it a red separates a send that went wrong — the characters arrived already substituted —
/// from a text box that dropped one under load, and those are opposite repairs in opposite
/// repositories.
/// </para>
/// <para>
/// The pump and not <c>HwndSource.AddHook</c>, which is measured rather than preferred. Hooked that
/// way, a run that typed <c>WW246-1</c> into the box recorded <c>WM_KEYUP</c> for End, seven
/// <c>WM_KEYUP</c> for Backspace and seven VK_PACKET pairs — and not one <c>WM_CHAR</c>, while the
/// box itself read the text correctly. WPF's input filter answers the character messages and the
/// real virtual keys before the public hooks run, and <c>HwndWrapper</c> stops the chain at the
/// first hook that answers. So <c>AddHook</c> sits above WPF and reads what WPF did not want;
/// <see cref="ComponentDispatcher.ThreadFilterMessage" /> sees every message the thread pulls off
/// its queue, before any of it, which is what "below WPF entirely" has to mean here.
/// </para>
/// <para>
/// Everything is recorded, control characters included: a Backspace arrives as <c>WM_CHAR</c> too,
/// and the erase that precedes every send is exactly the kind of thing a reading that quietly left
/// it out could not have been used to rule out. What a console cannot draw is escaped rather than
/// dropped, which is the rule this project already applies to a name.
/// </para>
/// </summary>
internal sealed class Arrivals
{
    private const int WmChar = 0x0102;

    private readonly StringBuilder said = new();
    private readonly TextBlock into;
    private readonly nint window;

    private Arrivals(TextBlock into, nint window)
    {
        this.into = into;
        this.window = window;
    }

    /// <summary>
    /// Start recording into a caption, once the window has a handle whose messages can be told from
    /// every other window's on the same thread.
    /// </summary>
    /// <param name="window">The window whose messages are read.</param>
    /// <param name="into">The caption the record is written to, as its own text.</param>
    /// <returns>The recorder, held by the caller so it is not collected while it is still reading.</returns>
    public static Arrivals On(Window window, TextBlock into)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var recorder = new Arrivals(into, handle);

        // Said and not skipped. The first guest run of this recorder read the caption's original text
        // back, which reads exactly like a window that was sent nothing — because a recorder that
        // could not attach reported it by doing nothing at all. An instrument that is not running
        // says so, in the one place its reading is ever taken from.
        recorder.Say(handle == 0 ? "<no window to read>" : "<reading, nothing has arrived>");

        if (handle != 0)
            ComponentDispatcher.ThreadFilterMessage += recorder.Heard;

        return recorder;
    }

    /// <summary>
    /// One message, looked at and never answered: this reads what arrives and changes nothing about
    /// what the window then does with it, which is the whole of what makes it a reading.
    /// </summary>
    /// <param name="message">The message the thread pulled off its queue.</param>
    /// <param name="answered">Left as it was found, always.</param>
    private void Heard(ref MSG message, ref bool answered)
    {
        // Every window on this thread comes through here, the fixture's popups included, so the
        // handle is checked rather than assumed: a record mixing two windows is a record that
        // cannot be used to rule anything out.
        if (message.hwnd != window || message.message != WmChar)
            return;

        var character = (char)(message.wParam & 0xFFFF);
        said.Append(char.IsControl(character) || character >= 0xE000
            ? $"\\u{(int)character:X4}"
            : character.ToString());

        Say(said.ToString());
    }

    /// <summary>
    /// Write one reading where a case can take it.
    /// <para>
    /// Both the text and the name explicitly: a caption's words are in its name and in no pattern,
    /// and a reading taken through the name is the one a case can make — WW238 measured that.
    /// </para>
    /// </summary>
    /// <param name="reading">What the caption now says.</param>
    private void Say(string reading)
    {
        into.Text = reading;
        System.Windows.Automation.AutomationProperties.SetName(into, reading);
    }
}
