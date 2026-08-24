using System.Collections.ObjectModel;

namespace Winwright.Tests;

/// <summary>
/// One place this suite asks the desk something and throws the answer away.
/// </summary>
/// <param name="Where">The member it happens in, as <c>Type.Member</c>.</param>
/// <param name="Call">The call, as the sources spell it.</param>
/// <param name="Because">Why throwing that answer away loses nothing.</param>
internal sealed record DeskDiscard(string Where, string Call, string Because)
{
    /// <summary>How the pairing addresses it, which is the pair and never either half.</summary>
    public string Named => $"{Where}: {Call}";

    public override string ToString() => $"{Where,-46} {Call,-32} {Because}";
}

/// <summary>
/// WW204, and it is WW197's rule read from the other end. That task established that a reading whose
/// answer is thrown away is not a case <em>asking</em> for a verdict — which is right, and is why
/// <c>DeskAsks</c> passes a discarded call over rather than demanding an excuse for it.
/// <para>
/// The hazard it leaves is the mirror image. A discarded desk reading has not been asked either, and
/// the cost does not land on the line that discarded it: it lands on whoever asserts afterwards.
/// Measured while WW200 was being built — <c>TrayIconFixture</c> shut the overflow it had opened and
/// threw away what shutting it answered, a shell that would not shut it left it standing silently,
/// and the case asserting the fixture leaves the overflow as it found it went red about the fixture.
/// </para>
/// <para>
/// So the same both-ways catalogue, over the calls <c>DeskAsks</c> passes over. Every discard is
/// paired with why nothing downstream can be wronged by it, and a discard added later is red until
/// somebody has answered that question — at the moment they write it rather than on the guest run
/// that meets it.
/// </para>
/// <para>
/// Nothing is listed that reads the answer. <c>TrayIconFixture</c> is absent because WW200 repaired
/// it, which is the shape of the whole thing: the entry that matters most is the one not here.
/// </para>
/// </summary>
internal static class DeskDiscards
{
    /// <summary>Every discard, paired with why it loses nothing.</summary>
    internal static IReadOnlyList<DeskDiscard> Known { get; } = new ReadOnlyCollection<DeskDiscard>(
    [
        new("ForeignInputTests.Input_this_run_synthesised_does_not_read_as_somebody_else",
            "Keyboard.Type(",
            "the typing is there to advance the machine's last-input time and its verdict is not "
                + "this case's subject. A desk that refused it is answered two lines later, where "
                + "the case returns on a reading that synthesised nothing"),
        new("ForeignInputTests.Watching_again_forgets_what_came_before",
            "Keyboard.Type(",
            "the same typing, to leave something for the next watch to forget. What is asserted is "
                + "that watching again reports nothing synthesised, and a type the desk refused "
                + "leaves nothing to forget — so the claim holds either way and proves less on the "
                + "run where it was refused, which is honest rather than wrong"),
        new("NotificationAreaTests.Dispose", "NotificationArea.CloseOverflow(",
            "the class tidies up on the way out and asserts nothing after it. A shell that will not "
                + "shut the flyout leaves it standing for the next class, and the one case that "
                + "would notice reads the flyout before and after its own work rather than assuming "
                + "it started shut — which WW197 made it do for exactly this reason"),
        new("NotificationAreaTests.A_shell_that_will_not_work_the_flyout_is_a_hole_naming_what_it_was",
            "NotificationArea.CloseOverflow(",
            "shutting what this case opened, on the arm where opening worked. The case has already "
                + "asserted everything it is about by then, and the class's own Dispose shuts it "
                + "again — so a refused close here is answered one line later"),
        new("TrayGhosts.Showing", "NotificationArea.CloseOverflow(",
            "the census opens the flyout to read what is hiding in it and shuts it again. The "
                + "reading it answers is about ghosts and carries its own third state for a flyout "
                + "that would not open; whether it shut again afterwards changes nothing it claims"),
        new("TrayPlacementTests.Adding_one_and_finding_it_holds_every_time_rather_than_most_times",
            "NotificationArea.CloseOverflow(",
            "shutting between rounds so each round looks at a taskbar in the same state. A round "
                + "that found the flyout still open would still find the icon, since the search "
                + "opens it anyway — the close is tidiness and never a precondition"),
        new("TrayPlacementTests.Two_icons_from_the_same_run_are_each_placed_before_their_own_add_returns",
            "NotificationArea.CloseOverflow(",
            "the same tidiness at the end of the case, after both icons have been found and "
                + "asserted on"),
    ]);

    /// <summary>Every member of this suite that throws a desk reading away, read out of the sources.</summary>
    internal static IReadOnlyList<string> Found() => found.Value.Select(one => one.Where).ToList();

    /// <summary>The same, with the call kept, which is what a red here has to print.</summary>
    internal static IReadOnlyList<DeskDiscard> Sites() => found.Value;

    /// <summary>The reading a person gets: the count first, then a line each.</summary>
    internal static IReadOnlyList<string> Render() => new ReadOnlyCollection<string>(
    [
        $"{Found().Count} desk reading(s) this suite throws away, each paired with why it loses nothing",
        .. Known.Select(one => $"  {one}"),
    ]);

    private static readonly Lazy<IReadOnlyList<DeskDiscard>> found = new(Scan);

    /// <summary>
    /// Every member — and not only every case, which is where this differs from <c>DeskAsks</c>. The
    /// one that cost a red was a fixture helper, and a reading that looked only at cases would have
    /// been looking away from it.
    /// </summary>
    private static IReadOnlyList<DeskDiscard> Scan()
    {
        var found = new List<DeskDiscard>();
        foreach (var file in Checkout.SourcesIn(Checkout.Suite, except: $"{nameof(DeskDiscards)}.cs"))
        {
            var owner = Path.GetFileNameWithoutExtension(file);
            var member = "";

            foreach (var line in File.ReadLines(file).Select(Checkout.Code))
            {
                if (Declares(line) is { } next)
                {
                    member = next;
                    continue;
                }

                if (member.Length == 0)
                    continue;

                if (Statement(line) is not { } trimmed)
                    continue;

                var discarded = DeskAsks.Calls
                    .FirstOrDefault(one => trimmed.StartsWith(one.Call, StringComparison.Ordinal));

                if (discarded is not null)
                    found.Add(new DeskDiscard($"{owner}.{member}", discarded.Call, ""));
            }
        }

        return found;
    }

    /// <summary>
    /// The line as a whole statement, or null where it is part of one.
    /// <para>
    /// A wrapped argument begins with the call exactly as a discarded statement does — this reported
    /// <c>Assert.DoesNotContain(</c> newline <c>NotificationArea.Showing(), …)</c> as a reading
    /// thrown away, when the assertion three characters later is what it is for. What separates them
    /// is that a statement closes what it opened: the brackets balance and a semicolon ends it.
    /// </para>
    /// </summary>
    private static string? Statement(string line)
    {
        var trimmed = line.Trim();
        if (!trimmed.EndsWith(';'))
            return null;

        return trimmed.Count(one => one == '(') == trimmed.Count(one => one == ')') ? trimmed : null;
    }

    /// <summary>The member a line declares, at the one indentation a member of a class sits at.</summary>
    private static string? Declares(string line)
    {
        if (!line.StartsWith("    private ", StringComparison.Ordinal)
            && !line.StartsWith("    internal ", StringComparison.Ordinal)
            && !line.StartsWith("    public ", StringComparison.Ordinal))
            return null;

        var arrow = line.IndexOf("=>", StringComparison.Ordinal);
        var signature = arrow < 0 ? line : line[..arrow];

        // The last bracket an identifier opens, for the reason WaitedForTests gives about its own:
        // a member returning a tuple opens one before its own name.
        var named = "";
        for (var at = 1; at < signature.Length; at++)
        {
            if (signature[at] != '(')
                continue;

            var began = at;
            while (began > 0 && (char.IsLetterOrDigit(signature[began - 1]) || signature[began - 1] == '_'))
                began--;

            if (began < at)
                named = signature[began..at];
        }

        return named.Length == 0 ? null : named;
    }
}
