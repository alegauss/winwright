using System.Collections.ObjectModel;
using System.Text;

namespace Winwright.Locating;

/// <summary>
/// How this project addresses an element, in one grammar written once and read the same way by
/// every verb. The same three automation conditions were being rebuilt at every call site — in
/// PowerShell in one project and in C# in another — so this is also what a scenario file writes,
/// and an agent learns one thing rather than one per language.
/// </summary>
/// <remarks>
/// <para>The shape, which is deliberately small enough to hold in the head:</para>
/// <code>
/// #saveButton                                   the automation id
/// Button                                        the control type
/// Button#saveButton                             both
/// Button[name="Save as..."]                     the name
/// Pane[class=Chrome_WidgetWin_1]                the window class
/// Button[pattern=Invoke]                        it must carry that pattern
/// Text[name="Statistics"][order=left]           the leftmost of the ones that match
/// MenuItem[order=top][index=2]                   the second from the top
/// Window#main &gt; Pane &gt; Button#save        a descendant of, at any depth
/// </code>
/// <para>
/// <c>&gt;</c> is <em>a descendant of</em> and not a direct child, and that is a decision rather
/// than a shorthand: UI Automation wraps controls in panes that differ between frameworks, between
/// versions of one framework, and between a maximised window and a restored one. A direct-child
/// locator is the one that breaks on somebody else's machine. A child operator can be added the
/// day a scenario genuinely needs one, and it will be its own task.
/// </para>
/// </remarks>
public sealed record Locator
{
    private const string Keys = "name, class, pattern, order, index";

    private Locator(string text, IReadOnlyList<LocatorStep> steps)
    {
        Text = text;
        Steps = steps;
    }

    /// <summary>The locator as it was written, which is what a trace records.</summary>
    public string Text { get; }

    /// <summary>Its steps, outermost first. Never empty.</summary>
    public IReadOnlyList<LocatorStep> Steps { get; }

    /// <summary>Parse one, or refuse with the position and the reason.</summary>
    /// <exception cref="LocatorSyntaxException">Where it does not parse.</exception>
    public static Locator Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var steps = new List<LocatorStep>();
        var at = 0;
        while (true)
        {
            steps.Add(Step(text, ref at));
            SkipSpace(text, ref at);
            if (at >= text.Length)
                break;

            if (text[at] != '>')
                throw new LocatorSyntaxException(LocatorFault.StepNotSeparated, text, at, $"expected '>' or the end, and found '{text[at]}'");

            at++;
        }

        return new Locator(text, new ReadOnlyCollection<LocatorStep>(steps));
    }

    /// <summary>Parse one without throwing, for a caller collecting refusals rather than stopping.</summary>
    public static bool TryParse(string text, out Locator? locator, out string? because)
    {
        try
        {
            locator = Parse(text);
            because = null;
            return true;
        }
        catch (LocatorSyntaxException refused)
        {
            locator = null;
            because = refused.Because;
            return false;
        }
    }

    /// <summary>The canonical spelling, which parses back to an equal locator.</summary>
    public override string ToString() => string.Join(" > ", Steps.Select(step => step.ToString()));

    private static LocatorStep Step(string text, ref int at)
    {
        SkipSpace(text, ref at);
        var began = at;

        string? controlType = null;
        string? automationId = null;
        string? name = null;
        string? className = null;
        string? pattern = null;
        int? index = null;
        MatchOrder? order = null;

        if (at < text.Length && (char.IsLetter(text[at]) || text[at] == '_'))
        {
            var word = Word(text, ref at);
            if (!UiaVocabulary.IsControlType(word))
                throw new LocatorSyntaxException(
                    LocatorFault.UnknownControlType,
                    text,
                    at - word.Length,
                    $"'{word}' is no UI Automation control type; nearest are "
                    + string.Join(", ", UiaVocabulary.Nearest(word, UiaVocabulary.ControlTypes)));

            controlType = word;
        }

        if (at < text.Length && text[at] == '#')
        {
            at++;

            // WW124: quoted as well as bare. Windows gives a window's own system menu the id
            // "Item 1", and an id was assumed to be an identifier because nothing this project
            // builds produces one with a space in it — so the first tree walked that somebody else
            // built broke on the first line under the title bar.
            automationId = at < text.Length && text[at] == '"' ? Quoted(text, ref at) : Word(text, ref at);
            if (automationId.Length == 0)
                throw new LocatorSyntaxException(LocatorFault.EmptyAutomationId, text, at, "'#' introduces an automation id and this one is empty");
        }

        while (at < text.Length && text[at] == '[')
        {
            var opened = at;
            at++;
            var key = Word(text, ref at);
            if (at >= text.Length || text[at] != '=')
                throw new LocatorSyntaxException(LocatorFault.PredicateMalformed, text, at, $"a predicate reads [key=value], with key one of {Keys}");

            at++;
            var value = Value(text, ref at);
            if (at >= text.Length || text[at] != ']')
                throw new LocatorSyntaxException(LocatorFault.PredicateNotClosed, text, at, "this predicate is not closed");

            at++;

            switch (key)
            {
                case "name":
                    Once(text, opened, name, "name");
                    name = value;
                    break;
                case "class":
                    Once(text, opened, className, "class");
                    className = value;
                    break;
                case "pattern":
                    Once(text, opened, pattern, "pattern");
                    if (!UiaVocabulary.IsPattern(value))
                        throw new LocatorSyntaxException(
                            LocatorFault.UnknownPattern,
                            text,
                            opened,
                            $"'{value}' is no UI Automation pattern; nearest are "
                            + string.Join(", ", UiaVocabulary.Nearest(value, UiaVocabulary.Patterns)));

                    pattern = value;
                    break;
                case "order":
                    Once(text, opened, order?.ToString(), "order");
                    if (!Enum.TryParse<MatchOrder>(value, ignoreCase: true, out var sorted)
                        || sorted == MatchOrder.Tree)
                    {
                        throw new LocatorSyntaxException(
                            LocatorFault.UnknownOrder, text, opened, $"'{value}' is no order here; they are left, right, top, bottom");
                    }

                    order = sorted;
                    break;
                case "index":
                    Once(text, opened, index?.ToString(), "index");
                    if (!int.TryParse(value, out var ordinal))
                        throw new LocatorSyntaxException(LocatorFault.IndexNotANumber, text, opened, $"'{value}' is not a whole number");
                    if (ordinal < 1)
                        throw new LocatorSyntaxException(
                            LocatorFault.IndexBelowOne, text, opened, $"an index counts from one, so {ordinal} addresses nothing");

                    index = ordinal;
                    break;
                default:
                    throw new LocatorSyntaxException(
                        LocatorFault.UnknownKey, text, opened + 1, $"'{key}' is no predicate here; the keys are {Keys}");
            }
        }

        if (controlType is null && automationId is null && name is null && className is null
            && pattern is null && index is null && order is null)
        {
            throw new LocatorSyntaxException(
                LocatorFault.StepConstrainsNothing, text, began, "a step that constrains nothing addresses everything, so it is refused");
        }

        return new LocatorStep(controlType, automationId, name, className, pattern, index, order);
    }

    private static void Once(string text, int at, string? already, string key)
    {
        if (already is not null)
            throw new LocatorSyntaxException(LocatorFault.KeyClaimedTwice, text, at, $"'{key}' is claimed twice in one step, which is two claims");
    }

    private static string Word(string text, ref int at)
    {
        var began = at;
        while (at < text.Length && (char.IsLetterOrDigit(text[at]) || text[at] == '_' || text[at] == '.'
            || text[at] == '-'))
        {
            at++;
        }

        return text[began..at];
    }

    private static string Value(string text, ref int at)
    {
        if (at < text.Length && text[at] == '"')
            return Quoted(text, ref at);

        var began = at;
        while (at < text.Length && text[at] != ']')
            at++;

        return text[began..at].Trim();
    }

    private static string Quoted(string text, ref int at)
    {
        var opened = at;
        at++;
        var value = new StringBuilder();
        while (at < text.Length)
        {
            if (text[at] == '\\' && at + 1 < text.Length)
            {
                // WW124: the three that carry a line break or a tab are decoded, and anything else
                // after a backslash is still itself. A tray icon's name is a tooltip and a real one
                // runs to several lines — rendered raw, the step ran to several lines too, and a
                // verb whose whole claim is one line per element was printing three.
                value.Append(text[at + 1] switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    var other => other,
                });

                at += 2;
                continue;
            }

            if (text[at] == '"')
            {
                at++;
                return value.ToString();
            }

            value.Append(text[at]);
            at++;
        }

        throw new LocatorSyntaxException(LocatorFault.QuoteNotClosed, text, opened, "this quoted value is never closed");
    }

    private static void SkipSpace(string text, ref int at)
    {
        while (at < text.Length && char.IsWhiteSpace(text[at]))
            at++;
    }
}
