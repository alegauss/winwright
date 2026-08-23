using System.Globalization;
using System.Runtime.InteropServices;

using Winwright.Asserting;
using Winwright.Capturing;
using Winwright.InApp;
using Winwright.Locating;
using Winwright.Processes;
using Winwright.Projects;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW145. The pairing compares two lists in both directions — the refusals the assemblies name
/// against the entries, and the entries against the flags the built fixture prints. The claim in
/// the middle was checked in neither direction: an entry saying <c>--language</c> reaches the
/// unusable-label refusal was held up by somebody having written it down.
/// <para>
/// Renaming a flag was caught. Changing what a flag draws was not, and neither was an entry that
/// was wrong when it was written — three of the four were provoked in the suite by building the
/// situation directly, so the sentence and the evidence were about different things.
/// </para>
/// <para>
/// So: one case per reachable entry, each launching the fixture with that flag, driving the thing
/// the refusal is about, and insisting on that refusal by type. Never a case catching any
/// exception and calling the pairing proved — the type is named in the entry, so a refusal
/// arriving for another reason is a red rather than a pass.
/// </para>
/// <para>
/// WW146 brought three more, and they are the ones whose refusal fires inside the fixture rather
/// than in this process. A type does not cross a process boundary, so the fixture prints its name
/// beside the sentence and these match on <c>nameof</c> — which is the type this suite references,
/// not a word somebody copied out of a message.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class ProvokedByFlagTests : IDisposable
{
    /// <summary>The flags the cases below launch the fixture with, one each.</summary>
    private static readonly string[] Driven =
        ["title", "language", "mutate", "pump", "sizeless", "blank", "unbacked"];

    /// <summary>
    /// What the fixture exits with where that shape was refused, read off its own catalogue.
    /// <para>
    /// WW161. This was a private 3 copied out of the fixture, which is a second transcription of
    /// the same fact — so a fixture that changed its code would have gone on agreeing with the copy.
    /// </para>
    /// </summary>
    private static int Refused(string flag) => Fixture.ExitFor(flag);

    private readonly ProcessRegister register = new();
    private readonly string root = Directory.CreateTempSubdirectory("winwright-provoked-").FullName;

    public void Dispose()
    {
        Attachable.StopAndSettle(register);
        register.Dispose();
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Every_flag_the_pairing_names_is_driven_by_a_case_here()
    {
        // The durable half, and arithmetic rather than a promise in the summary above: a fifth
        // reachable entry fails here until somebody writes the case that provokes it.
        Assert.Equal(
            Provocation.Reachable().Select(one => one.Flag).OrderBy(one => one, StringComparer.Ordinal),
            Driven.OrderBy(one => one, StringComparer.Ordinal));
    }

    [Fact]
    public void A_second_window_under_another_title_is_what_the_instance_refusal_counts()
    {
        // --title, and the flag is the point: the entry is about a second window of the same
        // application wearing a different title, which is the shape a person checking by hand
        // never reproduces the same way twice.
        Launched();
        var second = Launched("--title=winwright statistics");

        Assert.Equal("winwright statistics", second.Title);

        var check = InstanceCheck.Of(Fixture.Executable());
        Assert.True(check.Windowed.Count >= 2, check.Sentence());

        var refused = Assert.Throws<AnotherInstanceException>(check.RequireSole);
        Assert.Contains("Winwright.Fixture", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_window_in_the_fixtures_own_language_asks_for_a_label_that_can_never_match()
    {
        // --language. The window really is in pt-BR, read off the window rather than assumed from
        // the flag, and the key it labels itself with is the pathological one: a scenario asking
        // for it is asking for something no exact read could pass, and skipping that in silence is
        // this rule's whole reason for existing.
        var window = Launched("--language=pt-BR");
        var tree = Inspect.Window(window.Handle, depth: 12);
        Assert.NotNull(tree);
        Assert.Contains(tree.Walk(), one => one.Facts.Name == "Relatório");

        var refused = Assert.Throws<UnusableLabelException>(
            () => Labels.For("labels.profileName", TheFixturesStrings(), Speaking("pt-BR")));

        Assert.Contains("{name}", refused.Message, StringComparison.Ordinal);
        Assert.Contains("could ever pass", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_run_that_leaves_the_store_changed_is_caught_by_the_fingerprint()
    {
        // --mutate, which needs --store. The settled run goes first so the fingerprint is taken of
        // a store the fixture itself wrote. Nothing here spells what either run writes: the suite
        // cannot see the fixture's types, and a constant copied out of them would be this check
        // agreeing with a second transcription rather than with the fixture.
        var store = Path.Combine(root, "store");
        var settings = Path.Combine(store, "settings.json");
        var profiles = Path.Combine(store, "profiles.json");

        var settled = Ran($"--store={store}", settings, was: "");

        // Waited on for what it becomes and not merely for existing, which is the mistake this
        // made first: the file was already there from the settled run, so a wait for it to exist
        // came back before the mutating write had happened and the fingerprint saw nothing move.
        var refused = Assert.Throws<StoreTouchedException>(() => Untouched.Insist(
            [settings, profiles],
            () => Ran($"--store={store}", settings, was: settled, "--mutate")));

        Assert.Contains("settings.json", refused.Message, StringComparison.Ordinal);
        Assert.Contains("changed the machine of whoever ran it", refused.Message, StringComparison.Ordinal);

        // The accident the fingerprint exists for, said as the measurement: the same number of
        // bytes and a different machine, so a comparison by size calls this unchanged.
        var mutated = File.ReadAllText(settings);
        Assert.NotEqual(settled, mutated);
        Assert.Equal(settled.Length, mutated.Length);
    }

    [Fact]
    public void Work_handed_to_a_window_that_never_dispatches_is_a_named_timeout()
    {
        // --pump=none. The window is drawn and composed, so every picture of it looks perfect, and
        // its thread never takes a message up. A plain SendMessage to it does not come back, which
        // is the work the bounded runner exists to put a name and a deadline on.
        var window = Launched("--pump=none");
        Assert.True(window.OnScreen, "the unpumped window is not on screen, so this is not that window");

        var refused = Assert.Throws<ApartmentTimeoutException>(
            () => Apartment.Run(
                () => SendMessageW(window.Handle, 0x0000, 0, 0),
                TimeSpan.FromMilliseconds(750),
                "a message to the unpumped window"));

        Assert.Contains("did not finish within 0.75s", refused.Message, StringComparison.Ordinal);
        Assert.Contains("still running and cannot safely be stopped", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_page_whose_every_row_is_collapsed_lays_out_to_nothing_and_is_not_written()
    {
        // --sizeless, which needs --render. The refusal fires inside the fixture, so what crosses
        // back is a string — and the type is named in it, so this case matches the refusal that
        // fired rather than a sentence somebody transcribed.
        var path = Path.Combine(root, "sizeless.png");
        var (code, said) = Rendered(path, "--sizeless");

        Assert.Equal(Refused("sizeless"), code);
        Assert.Contains(nameof(UnrenderableException), said, StringComparison.Ordinal);
        Assert.Contains("laid out to 0x0", said, StringComparison.Ordinal);

        // The half that makes it worth refusing: an empty file passes everything that only checks
        // a file exists, so the refusal has to happen before anything is written.
        Assert.False(File.Exists(path), "the refused render wrote a file anyway");
    }

    [Fact]
    public void A_render_with_no_application_above_it_has_nothing_saying_what_to_draw_it_on()
    {
        // --unbacked. The shape is the withheld application: the capture background is declared in
        // its resources, and the element is in no window either, so neither source answers.
        var path = Path.Combine(root, "unbacked.png");
        var (code, said) = Rendered(path, "--unbacked");

        Assert.Equal(Refused("unbacked"), code);
        Assert.Contains(nameof(NoBackgroundException), said, StringComparison.Ordinal);
        Assert.Contains(Backgrounds.DefaultKey, said, StringComparison.Ordinal);
        Assert.False(File.Exists(path), "the refused render wrote a file anyway");
    }

    [Fact]
    public void A_page_that_paints_nothing_writes_a_picture_nothing_drew()
    {
        // --blank. This one the fixture completes: the refusal belongs to whoever reads the
        // picture back, which is the harness, so the file is the evidence and the type is asserted
        // here rather than matched in a string.
        var path = Path.Combine(root, "blank.png");
        var (code, said) = Rendered(path, "--blank");

        Assert.Equal(0, code);
        Assert.Contains("nothing, so it is transparent", said, StringComparison.Ordinal);
        Assert.True(File.Exists(path), $"the fixture wrote no picture to {path}");

        var refused = Assert.Throws<BlankPictureException>(() => Pictures.Insist(path));
        Assert.Contains("is a blank", refused.Message, StringComparison.Ordinal);

        // Read rather than inferred from the refusal: the picture is the right size and empty,
        // which is the defect this shape stands in for and not a render that never happened.
        var scanned = Pictures.Of(path);
        Assert.Equal(0, scanned.Drawn);
        Assert.True(scanned.Pixels > 0, scanned.Sentence());
    }

    /// <summary>
    /// Render with those flags and hand back what the fixture exited with and said. Both streams
    /// are read, because a shape here either writes a picture and says so or refuses and says why.
    /// </summary>
    /// <param name="path">Where the picture goes.</param>
    /// <param name="flags">The shape the render takes.</param>
    private static (int Code, string Said) Rendered(string path, params string[] flags)
    {
        var start = Fixture.Started([$"--render={path}", .. flags]);
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.UseShellExecute = false;

        using var running = System.Diagnostics.Process.Start(start)!;

        // Both before the wait: a process filling a redirected pipe nobody is reading blocks on
        // the write, and this would then time out on a fixture that had already finished its work.
        var output = running.StandardOutput.ReadToEnd();
        var error = running.StandardError.ReadToEnd();
        Assert.True(running.WaitForExit(30_000), $"the render did not finish for {string.Join(' ', flags)}");

        return (running.ExitCode, output + error);
    }

    /// <summary>Launch the fixture with those flags and wait for the window it draws.</summary>
    private TopLevelWindow Launched(params string[] flags)
    {
        var launched = Attachable.Launch(register, Fixture.Started(flags));

        return Waits.Until(
            "draw",
            $"the fixture drew no window for {string.Join(' ', flags)} (pid {launched.Pid})",
            () =>
            {
                var window = TopLevelWindows.Largest(launched.Pid);
                return window is not null && window.Title.Length > 0 ? window : null;
            });
    }

    /// <summary>
    /// Run the fixture until that file holds something other than <paramref name="was"/>, then stop
    /// it and hand back what it holds. The store is written before any window, so what is waited
    /// for is the write and never the drawing.
    /// </summary>
    /// <param name="store">The <c>--store</c> flag, which every run here carries.</param>
    /// <param name="file">The file inside it this run is about.</param>
    /// <param name="was">What it held before this run, so a wait cannot pass on the old content.</param>
    /// <param name="flags">Whatever else this run carries.</param>
    private static string Ran(string store, string file, string was, params string[] flags)
    {
        using var running = new ProcessRegister();
        var launched = Attachable.Launch(running, Fixture.Started([store, .. flags]));

        var written = "";
        Waits.Until(
            "wrote",
            $"pid {launched.Pid} never wrote {Path.GetFileName(file)} as anything but what it already held",
            () =>
            {
                if (!File.Exists(file))
                    return false;

                // Read through the write rather than around it: a file that exists and is empty is
                // the half of the write that happened to finish first.
                written = Read(file);
                return written.Length > 0 && !string.Equals(written, was, StringComparison.Ordinal);
            });

        Attachable.StopAndSettle(running);
        return written;
    }

    /// <summary>The file, or nothing where it is still being written.</summary>
    private static string Read(string file)
    {
        try
        {
            return File.ReadAllText(file);
        }
        catch (IOException)
        {
            return "";
        }
    }

    /// <summary>A declaration over the fixture's own strings, copied beside a project file.</summary>
    private ProjectDeclaration TheFixturesStrings()
    {
        var into = Directory.CreateDirectory(Path.Combine(root, "project")).FullName;
        var names = new List<string>();

        foreach (var file in Directory.EnumerateFiles(Fixture.StringsDirectory(), "strings.*.json"))
        {
            File.Copy(file, Path.Combine(into, Path.GetFileName(file)), overwrite: true);
            names.Add(Path.GetFileName(file));
        }

        Assert.NotEmpty(names);
        File.WriteAllText(
            Path.Combine(into, ProjectDeclaration.FileName),
            $$"""{ "languageFiles": [{{string.Join(", ", names.Select(one => $"\"{one}\""))}}] }""");

        return ProjectDeclaration.Find(into);
    }

    private static ResolvedLanguage Speaking(string tag) =>
        ResolvedLanguage.Resolve(null, null, CultureInfo.GetCultureInfo(tag));

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SendMessageW(nint window, uint message, nint wParam, nint lParam);
}

