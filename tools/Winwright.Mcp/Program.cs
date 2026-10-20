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

        // WW563. The channel is held here and nowhere else: anything in this process that writes to
        // the console by habit lands on the error stream, which is not the protocol. A launched child
        // is the register's to keep off it, since it would inherit the handle and not this writer.
        Channel = Console.Out;
        Console.SetOut(Console.Error);

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
        Channel.WriteLine(reply.ToJsonString());
        Channel.Flush();
    }

    /// <summary>The stream replies go on, which nothing else in this process writes to. WW563.</summary>
    private static TextWriter Channel { get; set; } = Console.Out;
}
