using Microsoft.Win32;

using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW619, its first slice. A fixture that starts the application through a file's association, by a
/// ProgID, the way Open with does.
/// <para>
/// Found by Modelado (MOD75, MOD78): a packaged app's association could only be proven by a script
/// beside the case, which started the executable with the path as an argument. Here a type and a
/// ProgID are registered for this user only, under HKCU\Software\Classes, with a command that starts
/// this repository's fixture; the project names an executable that is not there, so the window that
/// opens can only be the one the association started.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ActivatesTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-activates-").FullName;
    private readonly string tag = Guid.NewGuid().ToString("N")[..10];

    private string Extension => $".wwtest{tag}";

    private string ProgId => $"Winwright.Test.{tag}";

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{Extension}", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ProgId}", throwOnMissingSubKey: false);
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_fixture_activating_a_file_says_so_and_resolves_it_against_the_root()
    {
        var fixture = FixtureDeclaration.Of("opened", activates: @"models\cube.glb", activatesThrough: "Modelado.glb");

        Assert.Equal(Path.Combine(root, "models", "cube.glb"), fixture.Activating(root));
        Assert.Contains(
            @"activating models\cube.glb through Modelado.glb, holding the process the shell started and not what it starts",
            fixture.Sentence(),
            StringComparison.Ordinal);
        Assert.Null(FixtureDeclaration.Of("plain").Activating(root));
    }

    [Theory]
    [Trait(NoDesk.Key, NoDesk.Free)]
    [InlineData("cube.glb", null, "names no ProgID in 'through'")]
    [InlineData(null, "Modelado.glb", "no file in 'activates'")]
    [InlineData("cube.glb", "Modelado_abc!App", "user model ID")]
    public void An_activation_missing_its_half_or_naming_a_packaged_id_is_refused(string? file, string? through, string said)
    {
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => FixtureDeclaration.Of("opened", activates: file, activatesThrough: through));

        Assert.Contains(said, refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void An_activation_beside_what_it_never_carries_is_refused_naming_each()
    {
        var refused = Assert.Throws<ScenarioRefusedException>(() => FixtureDeclaration.Of(
            "opened", arguments: ["--status"], application: "app.exe", activates: "cube.glb", activatesThrough: "Modelado.glb"));

        Assert.Contains("'arguments' and 'application'", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_case_file_reads_the_two_fields()
    {
        var fixtures = ScenarioFile.Read(
            "activates.cases.json",
            """
            {
              "fixtures": [ { "name": "opened", "activates": "{files}/cube.glb", "through": "Modelado.glb" } ],
              "cases": [ { "name": "c", "fixture": "opened", "steps": [ { "locator": "Text#heading", "act": "read", "reads": "name", "expect": "x", "named": "s" } ] } ]
            }
            """);

        var fixture = Assert.Single(fixtures).Fixture;
        Assert.Equal("{files}/cube.glb", fixture.Activates);
        Assert.Equal("Modelado.glb", fixture.Through);
        Assert.True(fixture.NeedsStaging);
    }

    [Fact]
    [Trait(NoDesk.Key, NoDesk.Free)]
    public void A_progid_is_read_against_the_type_it_has_to_claim()
    {
        var file = Path.Combine(root, $"cube{Extension}");

        // No type at all, then a type and no ProgID, then a ProgID the type does not list.
        Assert.Contains("has no association", Activation.Unclaimed(file, ProgId), StringComparison.Ordinal);

        Registry.CurrentUser.CreateSubKey($@"Software\Classes\{Extension}").Dispose();
        Assert.Contains("is not a registered ProgID", Activation.Unclaimed(file, ProgId), StringComparison.Ordinal);

        Register(@"C:\nowhere\app.exe", claimed: false);
        Assert.Contains("does not claim", Activation.Unclaimed(file, ProgId), StringComparison.Ordinal);

        // The control: listed under OpenWithProgids, it claims the type.
        Register(@"C:\nowhere\app.exe", claimed: true);
        Assert.Null(Activation.Unclaimed(file, ProgId));
    }

    [Fact]
    public void A_file_activated_through_its_progid_opens_the_application_the_association_names()
    {
        var verdict = Run(Fixture.Executable(), claimed: true);
        if (verdict is null || BusyDesk.Excused(verdict))
            return;

        Assert.True(verdict.Outcome == RunOutcome.Passed, Said(verdict));
    }

    [Fact]
    public void A_progid_that_does_not_claim_the_type_is_refused_before_anything_starts()
    {
        if (!Desk.Read().CanObserve)
            return;

        var refused = Assert.Throws<ScenarioRefusedException>(() => Run(Fixture.Executable(), claimed: false));

        Assert.Contains($"'{ProgId}' does not claim '{Extension}'", refused.Because, StringComparison.Ordinal);
    }

    [Fact]
    public void An_activation_whose_process_leaves_at_once_is_said_and_never_attached_to()
    {
        if (!Desk.Read().CanObserve)
            return;

        // What a single-instance application does with a file it hands to its running copy: the
        // process the shell started is gone before a window could be asked of it.
        var refused = Assert.Throws<ScenarioRefusedException>(
            () => Run(Path.Combine(Environment.SystemDirectory, "cmd.exe"), claimed: true, command: "/c exit"));

        // Two arms, and which one a run meets is a race the shell decides: gone before it could be
        // held, or held and gone without a window. Both say the process left, and neither attaches.
        Assert.True(
            refused.Because.Contains("exited before it could be held", StringComparison.Ordinal)
                || refused.Because.Contains("its process exited without drawing one", StringComparison.Ordinal),
            refused.Because);
    }

    /// <summary>The type and the ProgID, for this user only, the ProgID listed under the type or not.</summary>
    private void Register(string executable, bool claimed, string? command = null)
    {
        using (var verb = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\shell\open\command"))
            verb.SetValue(null, $"\"{executable}\" {command ?? "--status \"--title=%1\""}");

        using var type = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{Extension}\OpenWithProgids");
        if (claimed)
            type.SetValue(ProgId, Array.Empty<byte>(), RegistryValueKind.None);
        else
            type.DeleteValue(ProgId, throwOnMissingValue: false);
    }

    /// <summary>Everything the run said, so a red here carries its own explanation.</summary>
    private static string Said(SuiteVerdict verdict) => string.Join(
        Environment.NewLine,
        verdict.Render()
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Results.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Verdict.Broke.Select(each => each.ToString())))
            .Concat(verdict.Ran.SelectMany(one => one.Trace.Select(each => each.ToString()))));

    /// <summary>
    /// Read the fixture's heading in the window an activation of a file of the registered type opens,
    /// under a project whose own executable is not there; or null where this desk cannot observe.
    /// </summary>
    private SuiteVerdict? Run(string executable, bool claimed, string? command = null)
    {
        if (!Desk.Read().CanObserve)
            return null;

        Register(executable, claimed, command);
        File.WriteAllText(Path.Combine(root, $"cube{Extension}"), "a model");

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            """
            {
              "executable": "not-built.exe",
              "timeouts": { "launch": 30000, "resolve": 4000, "act": 4000, "poll": 25 }
            }
            """);

        var cases = $$"""
            {
              "fixtures": [ { "name": "opened through its type", "activates": "cube{{Extension}}", "through": "{{ProgId}}" } ],
              "cases": [
                {
                  "name": "a model opened the way Open with does",
                  "catches": "a file association no case could activate, so the activation itself stayed unread",
                  "fixture": "opened through its type",
                  "steps": [
                    { "locator": "Text#heading", "act": "read", "reads": "name", "expect": "winwright fixture", "named": "the window is the fixture's" }
                  ]
                }
              ]
            }
            """;

        var project = ProjectDeclaration.Load(declaration);
        using var register = ProcessRegister.For(project);

        SuiteVerdict verdict;
        try
        {
            verdict = Suite.Launch(ScenarioFile.Read("activates.cases.json", cases), Selection.All, register, project);
        }
        finally
        {
            register.StopAll();
        }

        return verdict;
    }
}
