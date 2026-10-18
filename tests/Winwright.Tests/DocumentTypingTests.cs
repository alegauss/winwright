using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW545. Typing into a control that reads as a document — a terminal, whose Text pattern is its
/// whole screen — arrives where the screen holds what was sent, and is repaired where the screen
/// carries WW249's substitution of it.
/// <para>
/// Found by quickshell's tab case, which typed <c>title first-tab-marker</c> into a terminal and read
/// <c>first-tabrmarker</c>, then <c>first-tab-markrr</c>, in two runs out of twenty. The value loop
/// compares a whole reading with the string expected, and a screen is never that string, so the
/// fault reached the case as a defect in the application. These are the two readings the document
/// half rests on, asked without a desk because they are arithmetic on strings.
/// </para>
/// </summary>
public sealed class DocumentTypingTests
{
    private const string Prompt = "PS C:\\Users\\oobe> ";
    private const string Sent = "title first-tab-marker";

    [Fact]
    public void A_screen_holding_what_was_sent_is_where_it_arrived()
    {
        Assert.True(Substitution.Holds($"Windows PowerShell\r\n\r\n{Prompt}{Sent}   \r\n", Sent));
        Assert.False(Substitution.Holds($"{Prompt}title first-tabrmarker", Sent));
        Assert.False(Substitution.Holds(null, Sent));
    }

    [Theory]
    [InlineData("title first-tabrmarker")]
    [InlineData("title first-tab-markrr")]
    public void Both_readings_quickshell_saw_are_the_substitution(string read)
    {
        Assert.True(Substitution.InDocument($"Windows PowerShell\r\n{Prompt}{read}\r\n", Prompt, Sent));
    }

    [Fact]
    public void Two_positions_taking_the_same_intruder_are_still_the_substitution()
    {
        // §WW249's own example, WW246-5 read as W5245-5, on a screen instead of a box.
        Assert.True(Substitution.InDocument($"{Prompt}W5245-5", Prompt, "WW246-5"));
    }

    [Fact]
    public void A_screen_missing_a_key_is_not_repaired_into_a_pass()
    {
        // Short by one and wrong by a character that is not the last sent: the two ways a window
        // that drops or garbles keys reads, and neither is this engine's fault to take back.
        Assert.False(Substitution.InDocument($"{Prompt}title first-tab-marke", Prompt, Sent));
        Assert.False(Substitution.InDocument($"{Prompt}title first-tabxmarker", Prompt, Sent));
        Assert.False(Substitution.InDocument(Prompt, Prompt, Sent));
    }

    [Fact]
    public void A_run_already_on_the_screen_before_the_send_is_not_the_fault()
    {
        // The same substituted run, printed by the shell before anything was typed: a line the
        // screen carried all along says nothing about the send, and neither hides a new one.
        var earlier = $"{Prompt}echo title first-tabrmarker\r\ntitle first-tabrmarker\r\n";

        Assert.False(Substitution.InDocument($"{earlier}{Prompt}", earlier, Sent));
        Assert.True(Substitution.InDocument($"{earlier}{Prompt}title first-tabrmarker", earlier, Sent));
    }

    [Fact]
    public void Text_carrying_a_control_character_is_never_repaired()
    {
        // Enter runs the line and Tab completes it as they arrive, so by the time the screen is read
        // the shell has acted, and Backspace takes back neither.
        Assert.False(Substitution.InDocument($"{Prompt}dir\r\r\n", Prompt, "dir\r"));
        Assert.False(Substitution.InDocument($"{Prompt}cd\t\t", Prompt, "cd\tx"));
    }

    [Fact]
    public void One_code_unit_has_no_earlier_position_to_take()
    {
        Assert.False(Substitution.InDocument($"{Prompt}x", Prompt, "y"));
    }
}
