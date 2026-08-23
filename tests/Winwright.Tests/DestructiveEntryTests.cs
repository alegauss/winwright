using System.Runtime.InteropServices;
using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Projects;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW116. This block's criterion says destructive entries are named in the scenario and reached
/// only by traversal. The second half shipped — walking a menu cannot invoke anything — and the
/// route beside it was wide open: the general invoke pressed a menu item called Quit exactly as
/// willingly as one called Open, and nothing anywhere knew the difference.
/// <para>
/// The list is a fact about the application, so it is declared once beside the executable; the
/// refusal is at the door every act already passes through, so a click cannot press what an invoke
/// may not; and the permission is one sentence on the subject, where a reviewer reads it.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class DestructiveEntryTests : IDisposable
{
    private const uint WsPopup = 0x80000000;
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;

    private readonly List<nint> created = [];
    private readonly string root = Directory.CreateTempSubdirectory("winwright-destructive-").FullName;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    public void Dispose()
    {
        for (var index = created.Count - 1; index >= 0; index--)
            DestroyWindow(created[index]);

        Directory.Delete(root, recursive: true);
    }

    private nint Create(string className, string? title, uint style, nint parent = 0)
    {
        // On the desk: half of these press a real button, and an offscreen element is one this
        // framework correctly refuses to act on for a different reason entirely.
        var window = CreateWindowExW(0, className, title, style, 20, 20, 320, 200, parent, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
        return window;
    }

    /// <summary>A frame with one harmless button and one that would end the run.</summary>
    private nint Dialog()
    {
        var frame = Create("Static", "winwright statistics", WsPopup | WsVisible);
        Create("Button", "Open", WsChild | WsVisible, frame);
        Create("Button", "Quit", WsChild | WsVisible, frame);
        return frame;
    }

    /// <summary>A project naming what ends the run, which is where that fact belongs.</summary>
    private ProjectDeclaration Declared(params string[] destructive)
    {
        var path = Path.Combine(root, "winwright.json");
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Environment.ProcessPath)}},
              "destructive": {{System.Text.Json.JsonSerializer.Serialize(destructive)}}
            }
            """);

        return ProjectDeclaration.Load(path);
    }

    private Subject On(nint frame, string locator, ProjectDeclaration declaration) =>
        new(AutomationElement.FromHandle(frame), Locator.Parse(locator), declaration);

    [Fact]
    public void A_project_names_what_ends_the_run_beside_the_executable()
    {
        var declaration = Declared("Quit", "Delete account");

        Assert.True(declaration.Declares("destructive"));
        Assert.Equal(["Quit", "Delete account"], declaration.Destructive.Entries.Select(one => one.Declared));
        Assert.Contains("2 entries are declared destructive", declaration.Destructive.Sentence());
    }

    [Fact]
    public void A_project_that_names_none_refuses_nothing_and_says_that()
    {
        var declaration = Declared();

        Assert.False(declaration.Declares("destructive"));
        Assert.False(declaration.Destructive.Any);
        Assert.Contains("no destructive entry", declaration.Destructive.Sentence());
    }

    [Fact]
    public void Invoke_refuses_a_declared_entry_and_the_refusal_names_it()
    {
        var frame = Dialog();
        var quit = On(frame, """Button[name="Quit"]""", Declared("Quit"));

        var refused = Assert.Throws<DestructiveEntryException>(() => Act.Invoke(quit));

        Assert.Equal("Quit", refused.Entry);
        Assert.Contains("Quit", refused.Element);
        Assert.Contains("MeaningIt()", refused.Message);
    }

    [Fact]
    public void The_entry_beside_it_is_pressed_exactly_as_before()
    {
        var frame = Dialog();
        var open = On(frame, """Button[name="Open"]""", Declared("Quit"));

        // Nothing about the guard slows down the ordinary case, which is the whole point of
        // naming the dangerous one rather than making every act ask permission.
        Assert.Equal("invoke", Act.Invoke(open).Verb);
    }

    [Fact]
    public void A_scenario_that_means_the_quit_path_says_so_once_and_is_allowed()
    {
        var frame = Dialog();
        var quit = On(frame, """Button[name="Quit"]""", Declared("Quit"));

        Assert.False(quit.MeansIt);
        Assert.True(quit.MeaningIt().MeansIt);
        Assert.Equal("invoke", Act.Invoke(quit.MeaningIt()).Verb);
    }

    [Fact]
    public void Saying_it_once_does_not_say_it_for_the_subject_it_was_said_about()
    {
        var frame = Dialog();
        var quit = On(frame, """Button[name="Quit"]""", Declared("Quit"));

        Act.Invoke(quit.MeaningIt());

        // The permission is on the copy, so it cannot leak into the next act through the subject
        // a scenario keeps in a field.
        Assert.Throws<DestructiveEntryException>(() => Act.Invoke(quit));
    }

    [Fact]
    public void Every_verb_is_refused_because_the_guard_is_the_door_and_not_the_verb()
    {
        // The lesson the admission already carries: a guard on invoke alone leaves the click
        // beside it pressing Quit as willingly as Open.
        var frame = Dialog();
        var quit = On(frame, """Button[name="Quit"]""", Declared("Quit"));

        Assert.Throws<DestructiveEntryException>(() => Act.Invoke(quit));
        Assert.Throws<DestructiveEntryException>(() => Acting.Pointer.Click(quit, PointerReason.PointerIsTheAct));
        Assert.Throws<DestructiveEntryException>(() => Keyboard.Type(quit, "x"));
        Assert.Throws<DestructiveEntryException>(() => Admitted.To(quit));
    }

    [Fact]
    public void The_name_is_matched_without_case_because_neither_author_is_wrong()
    {
        var frame = Dialog();
        var quit = On(frame, """Button[name="Quit"]""", Declared("quit"));

        Assert.Equal("quit", Assert.Throws<DestructiveEntryException>(() => Act.Invoke(quit)).Entry);
    }

    [Fact]
    public void The_automation_id_is_matched_first_because_it_is_the_field_the_application_owns()
    {
        var declared = Destructive.Of(["quitCommand"]);

        Assert.Equal("quitCommand", declared.Matched("Sair", "quitCommand"));
        Assert.Null(declared.Matched("Quit", "openCommand"));
    }

    [Fact]
    public void A_subject_built_without_a_declaration_refuses_nothing()
    {
        // The engine has no opinion about which entry quits; a project that never said is a
        // project where nothing is refused, and that is stated rather than assumed. WW135: the
        // shape that gives the guard up is not a constructor and says so in its name.
        var frame = Dialog();
        var quit = Subject.Unguarded(AutomationElement.FromHandle(frame), Locator.Parse("""Button[name="Quit"]"""), 2000);

        Assert.False(quit.Destructive.Any);
        Assert.Equal("invoke", Act.Invoke(quit).Verb);
    }

    [Fact]
    public void The_only_way_to_make_a_subject_is_with_a_project_behind_it()
    {
        // WW135. There used to be a constructor taking a bare Timeouts, and it was the one a
        // scenario author would have reached for: with a project in hand you write the timeouts out
        // of it. A subject made that way refused nothing, and no line anywhere said so.
        var constructors = typeof(Subject).GetConstructors();

        var only = Assert.Single(constructors);
        Assert.Equal(
            [typeof(AutomationElement), typeof(Locator), typeof(ProjectDeclaration)],
            only.GetParameters().Select(one => one.ParameterType));
    }

    [Fact]
    public void A_blank_in_a_hand_written_list_is_not_an_entry_that_matches_everything()
    {
        var declared = Destructive.Of(["Quit", "  ", "", "quit"]);

        Assert.Equal(["Quit"], declared.Entries.Select(one => one.Declared));
        Assert.Null(declared.Matched("Open", ""));
        Assert.Null(declared.Matched("", ""));
    }
}
