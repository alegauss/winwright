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
    private const int WmKeyDown = 0x0100;

    /// <summary>
    /// VK_PACKET, which is the virtual key a <c>KEYEVENTF_UNICODE</c> injection arrives as. The
    /// character it carries is in the scan code, which is the high word of the message's lParam.
    /// </summary>
    private const int VkPacket = 0xE7;

    /// <summary>
    /// How many code units of each record stay on its caption.
    /// <para>
    /// WW316. It used to be all of them, and that made the recorder the heaviest thing in the run:
    /// both a caption's text and its automation name were rewritten once per keystroke over a string
    /// that grew all run, so a 400-round measurement finished rewriting eighteen kilobytes per
    /// character. Measured across one run, the average round went 4600ms, 6968ms, 9135ms and 11325ms
    /// by quarter, and the substitution rate rose with it — the instrument moving what it measures,
    /// in the direction the measurement is most sensitive to.
    /// </para>
    /// <para>
    /// Four hundred, because every claim ever made against this record is about the end of it: a case
    /// asks whether what arrived ends with what it sent. Nothing has read further back than one
    /// round, and a round is nine code units.
    /// </para>
    /// </summary>
    private const int Kept = 400;

    private readonly StringBuilder said = new();
    private readonly StringBuilder injected = new();

    /// <summary>
    /// How many keys have been injected since this recorder started, counted rather than left to be
    /// counted off the caption.
    /// <para>
    /// WW316. The one claim the packet record carries is a count, and it is read as a difference
    /// across a round — so a caption that forgets its own beginning would make that difference wrong
    /// from the first trim. The count is kept here, where trimming cannot reach it, and written at
    /// the front of the caption for the case that reads it.
    /// </para>
    /// </summary>
    private long keys;
    private readonly TextBlock into;
    private readonly TextBlock packets;
    private readonly nint window;

    private Arrivals(TextBlock into, TextBlock packets, nint window)
    {
        this.into = into;
        this.packets = packets;
        this.window = window;
    }

    /// <summary>
    /// Start recording into two captions, once the window has a handle whose messages can be told
    /// from every other window's on the same thread.
    /// </summary>
    /// <param name="window">The window whose messages are read.</param>
    /// <param name="into">The caption the characters are written to, as its own text.</param>
    /// <param name="packets">The caption the injected keys are written to.</param>
    /// <returns>The recorder, held by the caller so it is not collected while it is still reading.</returns>
    public static Arrivals On(Window window, TextBlock into, TextBlock packets)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var recorder = new Arrivals(into, packets, handle);

        // Said and not skipped. The first guest run of this recorder read the caption's original text
        // back, which reads exactly like a window that was sent nothing — because a recorder that
        // could not attach reported it by doing nothing at all. An instrument that is not running
        // says so, in the one place its reading is ever taken from.
        recorder.Start(handle == 0 ? "<no window to read>" : "<reading, nothing has arrived>");

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
        if (message.hwnd != window)
            return;

        if (message.message == WmChar)
        {
            said.Append(Readable((char)(message.wParam & 0xFFFF)));
            Trim(said);
            Say(into, said.ToString());
            return;
        }

        // WW249. The injected key beside the character it becomes, because that is the one boundary
        // left. Six reds say the window is delivered text with one character overwritten by the last
        // one sent, which put the defect in the send rather than in WPF — and the send is still two
        // things: what SendInput was given, and what TranslateMessage makes of what arrived.
        //
        // The whole word and not a field of it, which is measured rather than chosen. Reading the
        // scan code at bits 16-23 answered zero for all seven of a round that typed correctly, so
        // the character is not there: `WM_KEYDOWN` gives the scan code eight bits and a UTF-16 code
        // unit does not fit in eight bits. What is recorded is therefore the message as it arrived,
        // and the first thing it settles is whether the character is anywhere in it at all.
        if (message.message == WmKeyDown && (message.wParam & 0xFFFF) == VkPacket)
        {
            keys++;
            injected.Append($"[{(uint)message.lParam:X8}]");
            Trim(injected);
            Say(packets, Counted());
        }
    }

    /// <summary>What a console can draw, escaped where it cannot.</summary>
    /// <param name="character">The code unit as it arrived.</param>
    private static string Readable(char character) =>
        char.IsControl(character) || character >= 0xE000
            ? $"\\u{(int)character:X4}"
            : character.ToString();

    /// <summary>
    /// Write one reading where a case can take it.
    /// <para>
    /// Both the text and the name explicitly: a caption's words are in its name and in no pattern,
    /// and a reading taken through the name is the one a case can make — WW238 measured that.
    /// </para>
    /// </summary>
    /// <param name="caption">Which caption is being written.</param>
    /// <param name="reading">What it now says.</param>
    private static void Say(TextBlock caption, string reading)
    {
        caption.Text = reading;
        System.Windows.Automation.AutomationProperties.SetName(caption, caption.Text);
    }

    /// <summary>
    /// Keep a record to its last <see cref="Kept"/> code units, dropping the oldest.
    /// </summary>
    /// <param name="record">The record to shorten in place.</param>
    private static void Trim(StringBuilder record)
    {
        if (record.Length > Kept)
            record.Remove(0, record.Length - Kept);
    }

    /// <summary>
    /// The packet record as its caption carries it: the count first, then as much of the tail as is
    /// kept. WW316 — the count is the claim and the tail is the evidence, and only the tail is bounded.
    /// </summary>
    private string Counted() => $"{keys} keys: {injected}";

    /// <summary>Both captions, before either has anything to say.</summary>
    /// <param name="reading">What each says until something arrives.</param>
    private void Start(string reading)
    {
        said.Append(reading);
        injected.Append(reading);
        Say(into, said.ToString());
        Say(packets, Counted());

        // Cleared so the first arrival is the first thing in the record rather than the second: the
        // caption's opening words say the recorder is running, and a transcript that kept them would
        // read as a window that was sent them.
        said.Clear();
        injected.Clear();
    }
}
