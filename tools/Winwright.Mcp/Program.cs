using System.Text;
using System.Text.Json.Nodes;

namespace Winwright.Mcp;

/// <summary>
/// The pipe, and nothing else.
/// <para>
/// WW66. Everything that decides anything is in <see cref="Served"/>, where the suite can call it —
/// so what is left here is reading a line, writing a line, and the one judgement that cannot be made
/// anywhere else: text that is not a message gets an error back and the loop goes on, because a
/// server that exits on the first bad line takes the session's tools with it.
/// </para>
/// </summary>
public static class Program
{
    /// <summary>Answer messages until the pipe closes.</summary>
    public static void Main()
    {
        // UTF-8 without a byte-order mark, because the first thing this writes is a JSON-RPC reply
        // and a mark in front of it is a parse error at the other end.
        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        while (Console.ReadLine() is { } line)
        {
            if (line.Trim().Length == 0)
                continue;

            var request = Served.Parsed(line, out var complaint);
            if (request is null)
            {
                Write(Served.Wrong(null, Served.Unparseable, complaint));
                continue;
            }

            if (Served.To(request) is { } reply)
                Write(reply);
        }
    }

    /// <summary>One message, on one line, flushed — a reply sitting in a buffer never arrived.</summary>
    private static void Write(JsonNode reply)
    {
        Console.Out.WriteLine(reply.ToJsonString());
        Console.Out.Flush();
    }
}
