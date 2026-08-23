using System.Runtime.InteropServices;
using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Locating;
using Winwright.Projects;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW134. Found while shipping the guard itself. A declared entry was matched against the
/// automation id first and the displayed name second, and the second is the one every project will
/// reach for, because the name is what an author reading a menu can see — and it is the one field a
/// translation rewrites.
/// <para>
/// So a project declaring "Quit" was guarded on an English desk and unguarded the moment the same
/// application came up in pt-BR showing "Sair", silently, because a name that matched nothing looks
/// exactly like a name that was never dangerous. The failure mode is the worst available: the run
/// presses the entry that ends the run, on the machine where somebody was least expecting it.
/// </para>
/// <para>
/// The fixture ships three languages and one key per button, so the case is real rather than
/// constructed: <c>buttons.close</c> reads Close, Fechar and Schließen.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class TranslatedGuardTests : IDisposable
{
    private const uint WsPopup = 0x80000000;
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;

    private readonly List<nint> created = [];
    private readonly string root = Directory.CreateTempSubdirectory("winwright-translated-").FullName;

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
        var window = CreateWindowExW(0, className, title, style, 20, 20, 320, 200, parent, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
        return window;
    }

    /// <summary>A window showing the button in whichever language is asked for.</summary>
    private nint Dialog(string closeReads)
    {
        var frame = Create("Static", "winwright statistics", WsPopup | WsVisible);
        Create("Button", "Open", WsChild | WsVisible, frame);
        Create("Button", closeReads, WsChild | WsVisible, frame);
        return frame;
    }

    /// <summary>The fixture's own strings, which ship three languages of the same keys.</summary>
    private static IReadOnlyList<string> Shipped()
    {
        var walking = new DirectoryInfo(AppContext.BaseDirectory);
        while (walking is not null && !File.Exists(Path.Combine(walking.FullName, "Winwright.slnx")))
            walking = walking.Parent;

        Assert.NotNull(walking);
        var strings = Path.Combine(walking.FullName, "src", "Winwright.Fixture", "strings");
        var files = new[] { "strings.en.json", "strings.pt-BR.json", "strings.de.json" }
            .Select(one => Path.Combine(strings, one))
            .ToList();

        Assert.All(files, one => Assert.True(File.Exists(one), one));
        return files;
    }

    /// <summary>A project declaring what ends the run, and however many languages it ships.</summary>
    private ProjectDeclaration Declared(string destructive, int languages = 3)
    {
        var path = Path.Combine(root, "winwright.json");
        var files = Shipped().Take(languages).ToList();
        File.WriteAllText(
            path,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Environment.ProcessPath)}},
              "languageFiles": {{System.Text.Json.JsonSerializer.Serialize(files)}},
              "destructive": [{{destructive}}]
            }
            """);

        return ProjectDeclaration.Load(path);
    }

    private static Subject On(nint frame, string locator, ProjectDeclaration declaration) =>
        new(AutomationElement.FromHandle(frame), Locator.Parse(locator), declaration);

    [Theory]
    [InlineData("Close")]
    [InlineData("Fechar")]
    [InlineData("Schließen")]
    public void A_key_guards_the_entry_in_every_language_the_project_ships(string showing)
    {
        var declaration = Declared("""{ "key": "buttons.close" }""");
        var frame = Dialog(showing);

        var refused = Assert.Throws<DestructiveEntryException>(
            () => Act.Invoke(On(frame, $"""Button[name="{showing}"]""", declaration)));

        // The whole point: written once, and it holds on the desk of whoever is running it.
        Assert.Equal("buttons.close", refused.Entry);
    }

    [Fact]
    public void The_key_resolves_to_what_each_language_shows_and_says_so()
    {
        var entry = Assert.Single(Declared("""{ "key": "buttons.close" }""").Destructive.Entries);

        Assert.Equal(DeclaredBy.Key, entry.By);
        Assert.True(entry.SurvivesTranslation);
        Assert.Equal(["Close", "Fechar", "Schließen"], entry.Shows);
        Assert.Contains("showing \"Close\", \"Fechar\", \"Schließen\"", entry.ToString());
    }

    [Fact]
    public void The_button_beside_it_is_still_pressed_in_every_language()
    {
        var declaration = Declared("""{ "key": "buttons.close" }""");

        // The guard must not widen: a key resolving to three texts refuses those three and nothing
        // else, or a project would find its harmless buttons refused in one language only.
        Assert.Equal("invoke", Act.Invoke(On(Dialog("Fechar"), """Button[name="Open"]""", declaration)).Verb);
    }

    [Fact]
    public void An_automation_id_needs_no_language_at_all()
    {
        var entry = Assert.Single(Declared("""{ "id": "quitCommand" }""").Destructive.Entries);

        Assert.Equal(DeclaredBy.Id, entry.By);
        Assert.True(entry.SurvivesTranslation);
        Assert.Equal("#quitCommand", entry.ToString());
    }

    [Fact]
    public void A_bare_name_is_refused_where_the_project_ships_more_than_one_language()
    {
        // The refusal that closes the hole rather than papering over it: this declaration would
        // have held on an English desk and stopped holding everywhere else, saying nothing.
        var refused = Assert.Throws<ArgumentException>(() => Declared(""" "Close" """));

        Assert.Contains("ships 3 languages", refused.Message);
        Assert.Contains("stops matching in the others", refused.Message);
        Assert.Contains("""{"id": "…"}""", refused.Message);
    }

    [Fact]
    public void A_name_written_as_one_is_refused_for_the_same_reason()
    {
        var refused = Assert.Throws<ArgumentException>(() => Declared("""{ "name": "Close" }"""));

        Assert.Contains("quietly stop holding in the rest", refused.Message);
    }

    [Fact]
    public void A_project_with_one_language_may_still_name_what_it_sees()
    {
        // Not a rule for its own sake. With one language a name cannot be moved by a translation
        // that does not exist, and refusing it would cost every single-language project a guard.
        var declaration = Declared(""" "Close" """, languages: 1);

        var entry = Assert.Single(declaration.Destructive.Entries);
        Assert.Equal(DeclaredBy.Name, entry.By);
        Assert.False(entry.SurvivesTranslation);

        Assert.Throws<DestructiveEntryException>(
            () => Act.Invoke(On(Dialog("Close"), """Button[name="Close"]""", declaration)));
    }

    [Fact]
    public void A_key_the_strings_do_not_carry_is_recorded_as_matching_nothing()
    {
        // Honest rather than silent: a key nobody resolves guards nothing, and the sentence a
        // report prints says which key it was instead of leaving a reader to notice a short list.
        var entry = Assert.Single(Declared("""{ "key": "buttons.nosuchthing" }""").Destructive.Entries);

        Assert.Empty(entry.Shows);
        Assert.Contains("a key the strings do not carry", entry.ToString());
        Assert.Null(Declared("""{ "key": "buttons.nosuchthing" }""").Destructive.Matched("Close", ""));
    }

    [Fact]
    public void The_sentence_names_how_each_entry_was_declared()
    {
        var declaration = Declared("""{ "id": "quitCommand" }, { "key": "buttons.close" }""");

        var said = declaration.Destructive.Sentence();

        Assert.Contains("#quitCommand", said);
        Assert.Contains("buttons.close", said);
        Assert.Contains("2 entries are declared destructive", said);
    }
}
