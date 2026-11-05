using System.Text;
using System.Text.Json.Nodes;

namespace Winwright.Guarding;

/// <summary>
/// The pipe, and nothing else.
/// <para>
/// WW67. Everything that decides anything is in <see cref="Guard"/>, where the suite calls it. What
/// is left here is reading the call off stdin, writing the decision to stdout, and the rule that
/// cannot be tested anywhere else: <em>never break a turn</em>. Anything unreadable, any exception,
/// any missing field exits zero and says nothing, because a hook that fails loudly on a call it did
/// not understand is a hook somebody removes — and a removed guard guards nothing.
/// </para>
/// </summary>
public static class Program
{
    /// <summary>Read one call, answer it, and exit.</summary>
    public static int Main()
    {
        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        try
        {
            if (JsonNode.Parse(Console.In.ReadToEnd()) is not JsonObject call)
                return 0;

            var verdict = Guard.On(call, Guard.Nearest);
            if (!verdict.Denied)
                return 0;

            Console.Out.WriteLine(Denying(verdict.Because).ToJsonString());
            Console.Out.Flush();
            return 0;
        }
        catch (Exception anything) when (anything is not (OutOfMemoryException or StackOverflowException))
        {
            // Deliberately silent. Stderr on a PreToolUse hook is shown to the session, and a guard
            // that narrates its own failure on every write is one that gets turned off by lunchtime.
            return 0;
        }
    }

    /// <summary>The deny, in the shape the harness reads it.</summary>
    private static JsonObject Denying(string because) => new()
    {
        ["hookSpecificOutput"] = new JsonObject
        {
            ["hookEventName"] = "PreToolUse",
            ["permissionDecision"] = "deny",
            ["permissionDecisionReason"] = because,
        },
    };
}
