using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

using Winwright.Processes;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW563. A child started without its own streams inherits its runner's, and under the MCP server
/// those are the protocol channel. Found driving polyweave's Electron window: a line the application
/// printed arrived on the server's stdout before the result, and a strict JSON-RPC reader stopped
/// there. These drive real processes, for <c>ProcessRegisterTests</c>' reason: what is asserted is
/// where a real child's bytes went.
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class LaunchStreamsTests : IDisposable
{
    private const string Heard = "winwright heard this";

    private readonly string root = Directory.CreateTempSubdirectory("winwright-streams-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void What_a_launch_prints_is_kept_on_the_launch_rather_than_inherited()
    {
        using var register = new ProcessRegister();
        var launched = register.Launch(Quiet("cmd.exe", "/c", $"echo {Heard}& echo {Heard} too 1>&2"));

        Assert.True(launched.WaitForExit(30_000));
        Settled(launched, 2);

        // Both streams, in whichever order the two pipes delivered them.
        Assert.Contains(Heard, launched.Wrote);
        Assert.Contains($"{Heard} too ", launched.Wrote);
        Assert.Equal(2, launched.Lines);
    }

    [Fact]
    public void Only_the_last_lines_are_kept_and_the_count_says_how_many_there_were()
    {
        using var register = new ProcessRegister();
        var launched = register.Launch(Quiet("cmd.exe", "/c", "for /l %i in (1,1,50) do @echo line %i"));

        Assert.True(launched.WaitForExit(30_000));
        Settled(launched, 50);

        Assert.Equal(LaunchedProcess.Kept, launched.Wrote.Count);
        Assert.Equal("line 50", launched.Wrote[^1]);
    }

    [Fact]
    public void A_launch_that_reads_its_input_meets_the_end_of_it_rather_than_the_runners_input()
    {
        // Under the server the runner's input is the protocol too, so a child reading it would take
        // the next request. Closed, it reads nothing and goes on.
        using var register = new ProcessRegister();
        var launched = register.Launch(Quiet("findstr.exe", "anything"));

        Assert.True(launched.WaitForExit(10_000), "findstr was still waiting on an input nobody closed");
    }

    [Fact]
    public void A_launched_application_that_prints_leaves_the_server_stream_all_replies()
    {
        // The whole claim, over the real pipe: the server launches an application that prints, among
        // other things, a line beginning with a brace, and every line the server writes still parses
        // as a reply. The red case's answer then names what the application wrote.
        if (!Desk.Read().CanObserve)
            return;

        var project = Path.Combine(root, Winwright.Projects.ProjectDeclaration.FileName);
        File.WriteAllText(
            project,
            $$"""
            {
              "executable": {{JsonSerializer.Serialize(Path.Combine(Environment.SystemDirectory, "cmd.exe"))}},
              "timeouts": { "resolve": 300, "act": 300, "poll": 25 }
            }
            """);

        File.WriteAllText(
            Path.Combine(root, $"printing{Winwright.Scenarios.ScenarioFile.Extension}"),
            $$"""
            {
              "fixtures": [
                {
                  "name": "an application that prints",
                  "arguments": ["/c", "echo {not a reply& echo {{Heard}}& ping -n 5 127.0.0.1 >nul"],
                  "resident": true
                }
              ],
              "cases": [
                {
                  "name": "the printing application is driven",
                  "catches": "an application whose output reached the protocol channel",
                  "fixture": "an application that prints",
                  "steps": [
                    {
                      "locator": "Button#nothingIsCalledThisOnAnyDesktop",
                      "act": "read",
                      "reads": "name",
                      "answers": true,
                      "named": "the run reached this step at all"
                    }
                  ]
                }
              ]
            }
            """);

        var server = Path.Combine(AppContext.BaseDirectory, "Winwright.Mcp.exe");
        using var running = Process.Start(new ProcessStartInfo(server)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
        })!;

        try
        {
            running.StandardInput.WriteLine(
                """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}""");
            running.StandardInput.WriteLine(new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = 2,
                ["method"] = "tools/call",
                ["params"] = new JsonObject
                {
                    ["name"] = "winwright_run",
                    ["arguments"] = new JsonObject { ["project"] = project, ["cases"] = root },
                },
            }.ToJsonString());
            running.StandardInput.Close();

            var lines = new List<string>();
            while (Next(running) is { } line)
                lines.Add(line);

            // Every line a reply, which is what a strict reader needs and what it did not get.
            Assert.Equal(2, lines.Count);
            foreach (var line in lines)
                Assert.Equal("2.0", JsonNode.Parse(line)!["jsonrpc"]!.GetValue<string>());

            var said = JsonNode.Parse(lines[1])!["result"]!["content"]![0]!["text"]!.GetValue<string>();
            Assert.Contains("the application wrote", said, StringComparison.Ordinal);
            Assert.Contains(Heard, said, StringComparison.Ordinal);
            Assert.Contains("{not a reply", said, StringComparison.Ordinal);
        }
        finally
        {
            if (!running.HasExited)
                running.Kill(entireProcessTree: true);
        }
    }

    /// <summary>A start with no console window and no stream of its own, which the register takes.</summary>
    private static ProcessStartInfo Quiet(string executable, params string[] arguments)
    {
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);

        return start;
    }

    /// <summary>The server's next line, or null where it closed or went quiet for a minute.</summary>
    private static string? Next(Process running)
    {
        var reading = running.StandardOutput.ReadLineAsync();
        return reading.Wait(TimeSpan.FromSeconds(60)) ? reading.Result : null;
    }

    /// <summary>Wait for the drained lines to arrive, which they do after the exit and not with it.</summary>
    private static void Settled(LaunchedProcess launched, int lines)
    {
        var clock = Stopwatch.StartNew();
        while (launched.Lines < lines && clock.ElapsedMilliseconds < 10_000)
            Thread.Sleep(25);

        Assert.True(launched.Lines >= lines, $"{launched.Lines} of {lines} line(s) arrived");
    }
}
