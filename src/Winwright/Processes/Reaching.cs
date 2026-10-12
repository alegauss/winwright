using System.Globalization;
using System.Net.Sockets;

using Winwright.Verdicts;

namespace Winwright.Processes;

/// <summary>
/// Whether something a fixture says its application connects to is there to connect to. WW522.
/// <para>
/// Found by quickshell's QS269. Its session bar exists only while a pane holds a live SSH session,
/// and a fixture could stage files and pass arguments but not say the case needs a server — so the
/// bar was proven by a unit test against the project's own sshd instead of by a case.
/// </para>
/// <para>
/// A probe and never a service. Starting the server is the project's job, and the engine keeps no
/// daemon: the fixture says what it needs, and this says whether it was there. An answer of no is a
/// precondition that is absent, so every check on that fixture is a hole naming the address rather
/// than a red about an application that was never given its server.
/// </para>
/// </summary>
public static class Reaching
{
    /// <summary>What a run calls the reading.</summary>
    public const string PreconditionName = "what the fixture reaches is listening";

    /// <summary>How long a probe waits where the project declares no <c>reach</c> timeout.</summary>
    public const int DefaultTimeoutMs = 2000;

    /// <summary>
    /// The host and port an address names, or null where it names neither.
    /// </summary>
    /// <param name="address">An address as a fixture writes it, <c>host:port</c>.</param>
    public static (string Host, int Port)? Parsed(string address)
    {
        ArgumentNullException.ThrowIfNull(address);

        var written = address.Trim();
        var colon = written.LastIndexOf(':');
        if (colon <= 0 || colon == written.Length - 1)
            return null;

        var host = written[..colon].Trim('[', ']');
        return int.TryParse(written[(colon + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var port)
            && port is > 0 and <= 65535
            && host.Length > 0
                ? (host, port)
                : null;
    }

    /// <summary>
    /// Try each address once with a TCP connect, and answer met where all of them took it.
    /// </summary>
    /// <param name="addresses">The addresses, already expanded.</param>
    /// <param name="timeoutMs">How long each connect may take.</param>
    public static Precondition Probe(IReadOnlyList<string> addresses, int timeoutMs)
    {
        ArgumentNullException.ThrowIfNull(addresses);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timeoutMs);

        foreach (var address in addresses)
        {
            if (Parsed(address) is not { } named)
                return Precondition.Absent(PreconditionName, $"'{address}' is not a host:port this can connect to");

            if (Refused(named.Host, named.Port, timeoutMs) is { } why)
                return Precondition.Absent(PreconditionName, $"nothing took a connection at {address}: {why}");
        }

        return Precondition.Met(PreconditionName, $"{string.Join(", ", addresses)} took a connection");
    }

    /// <summary>Why a connect to that address did not complete, or null where it did.</summary>
    private static string? Refused(string host, int port, int timeoutMs)
    {
        using var client = new TcpClient();
        try
        {
            return client.ConnectAsync(host, port).Wait(timeoutMs)
                ? null
                : $"no answer inside {timeoutMs}ms";
        }
        catch (AggregateException failed) when (failed.InnerException is SocketException socket)
        {
            return socket.Message;
        }
        catch (SocketException socket)
        {
            return socket.Message;
        }
    }
}
