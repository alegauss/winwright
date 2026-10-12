using System.Net;
using System.Net.Sockets;

using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW522. A fixture that says what its application connects to.
/// <para>
/// Found by quickshell's QS269: the session bar exists only while a pane holds a live SSH session,
/// and a fixture could not say the case needed a server, so the bar was proven by a unit test. These
/// run against a listener this test opens and a port it has just closed, so both answers are real
/// connects rather than a probe that was told what to say.
/// </para>
/// </summary>
public sealed class ReachesTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-reach-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void A_server_that_is_listening_is_met_and_one_that_is_not_is_absent_naming_the_address()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var open = $"127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}";
            Assert.True(Reaching.Probe([open], 2000).Satisfied);
        }
        finally
        {
            listener.Stop();
        }

        var closed = $"127.0.0.1:{Closed()}";
        var probed = Reaching.Probe([closed], 2000);

        Assert.False(probed.Satisfied);
        Assert.Equal(Reaching.PreconditionName, probed.Name);
        Assert.Contains(closed, probed.Absence, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1:")]
    [InlineData("127.0.0.1:70000")]
    [InlineData(":2222")]
    public void An_address_that_is_not_a_host_and_a_port_is_refused_where_it_is_written(string written)
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => FixtureDeclaration.Of("connected", reaches: [written]));

        Assert.Contains("host:port", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void A_variable_naming_the_address_is_judged_when_it_is_probed_and_not_at_load()
    {
        var fixture = FixtureDeclaration.Of("connected", reaches: ["%WINWRIGHT_REACH_NOWHERE%"]);

        // Unset, it expands to itself, which names no port: absent, and saying what it read.
        var missing = Assert.IsType<Precondition>(fixture.Unreachable(500));
        Assert.Contains("WINWRIGHT_REACH_NOWHERE", missing.Absence, StringComparison.Ordinal);
    }

    [Fact]
    public void The_address_the_probe_found_is_offered_to_the_arguments_and_the_variables()
    {
        var fixture = FixtureDeclaration.Of(
            "connected",
            arguments: ["--connect", FixtureDeclaration.FirstReach],
            variables: new Dictionary<string, string> { ["QS_SERVER"] = "ssh://{reach0}" },
            reaches: ["127.0.0.1:2222"]);

        var start = fixture.Starting(@"C:\app\YourApp.exe", root);

        Assert.Equal(["--connect", "127.0.0.1:2222"], start.ArgumentList);
        Assert.Equal("ssh://127.0.0.1:2222", start.Environment["QS_SERVER"]);
        Assert.Contains("reaching 127.0.0.1:2222", fixture.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_case_on_a_fixture_whose_server_is_not_there_is_a_hole_and_the_application_never_launches()
    {
        var declaration = Path.Combine(root, ProjectDeclaration.FileName);

        // An executable that is not there: a launch would throw, so a run that reaches the end with
        // a verdict is a run that never tried.
        File.WriteAllText(declaration, """{ "executable": "not-built/YourApp.exe", "timeouts": { "reach": 500 } }""");
        var project = ProjectDeclaration.Load(declaration);

        var cases = $$"""
            {
              "fixtures": [ { "name": "connected", "reaches": ["127.0.0.1:{{Closed()}}"] } ],
              "cases": [
                {
                  "name": "the session bar says connected",
                  "fixture": "connected",
                  "steps": [
                    { "locator": "Text#sessionState", "act": "read", "reads": "name", "expect": "Connected" },
                    { "locator": "Text#forwardChip", "act": "read", "reads": "name", "answers": true }
                  ]
                }
              ]
            }
            """;

        using var register = ProcessRegister.For(project);
        var verdict = Suite.Launch(ScenarioFile.Read("reach.cases.json", cases), Selection.All, register, project);

        Assert.Equal(RunOutcome.Degraded, verdict.Outcome);
        var only = Assert.Single(verdict.Ran);
        Assert.Equal(2, only.Verdict.Results.Count);
        Assert.All(only.Verdict.Results, one =>
        {
            Assert.True(one.DidNotRun);
            Assert.Equal(Reaching.PreconditionName, one.Missing!.Name);
            Assert.Contains("nothing took a connection at 127.0.0.1:", one.Missing.Absence, StringComparison.Ordinal);
        });
    }

    /// <summary>A loopback port nothing is listening on: bound, read and let go.</summary>
    private static int Closed()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
