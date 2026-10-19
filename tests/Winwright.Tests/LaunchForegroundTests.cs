using Winwright.Processes;
using Winwright.Projects;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW549. A window a launch starts may come forward, whatever ran before it.
/// <para>
/// Found by quickshell (QS368): one typing case soaked alone on the guest was Degraded every run,
/// the foreground held by Program Manager, while the same case passed after other cases had sent
/// input. The fixture's <c>--show</c> activates its own window the way an application does at start,
/// so the claim is the adopter's: launched with nothing before it, it holds the desk.
/// </para>
/// <para>
/// Only a person's hand excuses it. A desk held by the shell is exactly the defect, so it is a red
/// here and never an excuse; run alone, this is the launch first in its run that QS368 was.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class LaunchForegroundTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-launch-fg-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void A_window_launched_first_is_handed_the_foreground_and_takes_it()
    {
        if (!Desk.Read().CanObserve)
            return;

        var declaration = Path.Combine(root, ProjectDeclaration.FileName);
        File.WriteAllText(
            declaration,
            $$"""{ "executable": {{System.Text.Json.JsonSerializer.Serialize(Fixture.Executable())}} }""");

        using var register = ProcessRegister.For(ProjectDeclaration.Load(declaration));
        ForeignInput.Watch();

        var launched = register.Launch(Fixture.Started("--show"), "a case first in its run");
        var window = Fixture.Drew(launched.Pid);
        var held = Foreground.Waited(window.Handle, DeskWait.Of(5000, 25));

        // Said while the launch is running, because the sentence asks the register about it and a
        // stopped process is one it no longer answers for.
        var said = held.Sentence();
        register.StopAll();

        if (BusyDesk.Excused(ForeignInput.Read().AsPrecondition()))
            return;

        Assert.True(held.Ours, said);
        Assert.True(launched.Handed, "the window came forward and the launch says it could not hand it the right");
    }
}
