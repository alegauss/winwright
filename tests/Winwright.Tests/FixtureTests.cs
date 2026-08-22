using System.Diagnostics;
using System.Runtime.InteropServices;

using System.Windows.Automation;

using Winwright.Asserting;
using Winwright.Locating;
using Winwright.Processes;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW89. Every loop in this framework was developed against somebody's shipping product, which
/// means a real account, a real transcript directory, a real controller and a machine somebody set
/// up by hand.
/// <para>
/// These tests drive the fixture as a real application: launched through the register, found by
/// enumeration, read through UI Automation. Nothing is stubbed, because a fixture proved against a
/// stub proves the stub.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class FixtureTests : IDisposable
{
    private readonly ProcessRegister register = new();

    public void Dispose() => register.Dispose();

    /// <summary>
    /// The fixture's executable, in its own output directory. Not copied beside the suite: the
    /// copy brought the apphost without its assembly, and every launch died at CLR startup with a
    /// number that says nothing about which file was missing.
    /// </summary>
    private static string Executable()
    {
        var here = new DirectoryInfo(AppContext.BaseDirectory);
        var framework = here.Name;
        var configuration = here.Parent!.Name;

        var repository = here;
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "Winwright.slnx")))
            repository = repository.Parent;

        Assert.NotNull(repository);
        var path = Path.Combine(
            repository.FullName, "src", "Winwright.Fixture", "bin", configuration, framework, "Winwright.Fixture.exe");

        Assert.True(File.Exists(path), $"the fixture was not built: {path}");
        return path;
    }

    /// <summary>Launch it and wait for the window it draws, which is the only signal worth waiting on.</summary>
    private TopLevelWindow Launched(params string[] flags)
    {
        var start = new ProcessStartInfo(Executable());
        foreach (var flag in flags)
            start.ArgumentList.Add(flag);

        var launched = Attachable.Launch(register, start);

        for (var attempt = 0; attempt < 200; attempt++)
        {
            var window = TopLevelWindows.Largest(launched.Pid);
            if (window is not null && window.Title.Length > 0)
                return window;

            Thread.Sleep(25);
        }

        Assert.Fail($"the fixture never drew a window (pid {launched.Pid})");
        return null!;
    }

    [Fact]
    public void The_fixture_launches_and_draws_a_window_of_its_own()
    {
        var window = Launched();

        Assert.Equal("winwright fixture", window.Title);
        Assert.True(window.OnScreen, window.ToString());
        Assert.False(window.IsOwned, "the main window is owned by something, which a frame is not");
    }

    [Fact]
    public void It_needs_nothing_from_the_machine_it_runs_on()
    {
        // Launched with no arguments, no working directory of its own, and an environment carrying
        // nothing this repository set. A fixture needing any of those is one more thing to set up
        // by hand, which is the cost it exists to remove.
        var start = new ProcessStartInfo(Executable()) { WorkingDirectory = Path.GetTempPath() };
        start.Environment.Remove("WINWRIGHT_SURFACES");
        start.Environment.Remove("WINWRIGHT_GEOMETRY");

        var launched = Attachable.Launch(register, start);

        TopLevelWindow? window = null;
        for (var attempt = 0; attempt < 200 && window is null; attempt++)
        {
            window = TopLevelWindows.Largest(launched.Pid);
            if (window is null)
                Thread.Sleep(25);
        }

        Assert.NotNull(window);
    }

    [Fact]
    public void Every_control_on_it_carries_a_name_a_locator_can_be_written_against()
    {
        var tree = Inspect.Window(Launched().Handle, depth: 12);

        Assert.NotNull(tree);
        var ids = tree.Walk().Select(one => one.Facts.AutomationId).Where(one => one.Length > 0).ToList();

        // A surface addressed only by its position is one every layout change breaks.
        Assert.Contains("heading", ids);
        Assert.Contains("subheading", ids);
        Assert.Contains("panes", ids);
        Assert.Contains("save", ids);
        Assert.Contains("close", ids);

        // Not 'page': a layout panel has no automation peer, so a Grid is in no control view
        // however it is named. And not 'profile': a tab's content is realised when the tab is
        // selected, so the Config pane is in no tree until something picks it. Both are facts
        // about Windows that this fixture exists to make reproducible, not naming failures.
        Assert.DoesNotContain("page", ids);
        Assert.DoesNotContain("profile", ids);
    }

    [Fact]
    public void Every_line_of_the_fixture_s_own_tree_is_a_locator_the_grammar_accepts()
    {
        var tree = Inspect.Window(Launched().Handle, depth: 12)!;

        // The fixture's own controls, not the window chrome around them. The chrome's system menu
        // carries an automation id with a space in it - 'Item 1' - which inspect renders into a
        // step the grammar refuses. That is a defect in the rendering rather than in the fixture,
        // and it is filed under its own id rather than worked around here.
        var mine = tree.Children
            .Where(one => one.Facts.AutomationId != "TitleBar")
            .SelectMany(one => one.Walk())
            .ToList();

        Assert.NotEmpty(mine);
        foreach (var element in mine)
        {
            var step = element.Facts.AsLocatorStep().ToString();
            Assert.True(
                Locator.TryParse(step, out _, out var because),
                $"the fixture rendered '{step}', which the grammar refuses: {because}");
        }
    }

    [Fact]
    public void The_three_panes_it_shows_are_the_three_it_always_shows()
    {
        var tree = Inspect.Window(Launched().Handle, depth: 12)!;

        var headers = tree.Walk()
            .Where(one => one.Facts.ControlType == "TabItem")
            .Select(one => one.Facts.Name)
            .ToList();

        // Fixed by construction: two runs on two desks show the same three, which is what makes a
        // derived expectation and a byte-identical render mean anything at all.
        Assert.Equal(["Report", "Status", "Config"], headers);
    }

    [Fact]
    public void Nothing_it_draws_is_read_off_the_machine()
    {
        var tree = Inspect.Window(Launched().Handle, depth: 12)!;
        var texts = tree.Walk().Select(one => one.Facts.Name).Where(one => one.Length > 0).ToList();

        // The three things a fixture accidentally leaks: who is running it, where, and when.
        Assert.DoesNotContain(texts, one => one.Contains(Environment.MachineName, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(texts, one => one.Contains(Environment.UserName, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(texts, one => one.Contains(DateTime.Now.Year.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public void Stopping_it_leaves_no_process_running()
    {
        var window = Launched();
        Win32Pid(window, out var pid);

        register.StopAll();

        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (!Alive(pid))
                return;

            Thread.Sleep(20);
        }

        Assert.Fail($"pid {pid} was still running after the register stopped everything");
    }

    private static void Win32Pid(TopLevelWindow window, out int pid) => pid = window.Pid;

    private static bool Alive(int pid)
    {
        try
        {
            using var running = Process.GetProcessById(pid);
            return !running.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    [Fact]
    public void A_flag_it_knows_reaches_the_window()
    {
        var window = Launched("--title=winwright fixture, second");

        Assert.Equal("winwright fixture, second", window.Title);
    }

    [Fact]
    public void A_flag_it_does_not_know_is_refused_rather_than_ignored()
    {
        // The worst possible green: a misspelt flag silently does nothing, the shape is never
        // taken, the refusal never fires, and the case passes having asserted nothing.
        var (code, said) = Ran("--backdrp");

        Assert.Equal(2, code);
        Assert.Contains("--backdrp is not a shape this fixture has", said);
        Assert.Contains("This fixture knows:", said);
        Assert.Contains("--title=<text>", said);
    }

    [Fact]
    public void A_flag_given_no_value_and_one_given_too_much_are_both_refused()
    {
        Assert.Contains("takes a value", Ran("--title").Said);
        Assert.Contains("every argument begins with --", Ran("title=x").Said);
    }

    [Fact]
    public void A_refused_run_draws_no_window_at_all()
    {
        var (code, _) = Ran("--nope");

        // Not a window somebody could photograph and mistake for the shape they asked for.
        Assert.Equal(2, code);
    }

    /// <summary>Run it to completion, reading what it refused with.</summary>
    private (int Code, string Said) Ran(params string[] flags)
    {
        var start = new ProcessStartInfo(Executable())
        {
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var flag in flags)
            start.ArgumentList.Add(flag);

        using var running = Process.Start(start)!;
        var said = running.StandardError.ReadToEnd();
        Assert.True(running.WaitForExit(20_000), "the fixture did not exit after being refused");

        return (running.ExitCode, said);
    }

    [Fact]
    public void The_same_window_under_a_dispatcher_that_runs_answers_a_message()
    {
        var window = Launched("--pump=dispatcher");

        Assert.True(Answers(window.Handle), "the pumped window did not answer a message");
    }

    [Fact]
    public void The_same_window_under_a_dispatcher_that_never_runs_looks_perfect_and_answers_nothing()
    {
        // The difference no picture can see. One product shipped windows that took no keystrokes
        // while every screenshot of them looked perfect, and this is that window on demand.
        var window = Launched("--pump=none");

        Assert.Equal("winwright fixture", window.Title);
        Assert.True(window.OnScreen, "the unpumped window is not on screen, so no picture of it would look right");
        Assert.True(window.Bounds.Area > 0, window.ToString());

        Assert.False(Answers(window.Handle), "the unpumped window answered a message, so it is pumping something");
    }

    [Fact]
    public void A_value_outside_what_a_flag_takes_is_refused_with_the_ones_it_does()
    {
        var (code, said) = Ran("--pump=maybe");

        Assert.Equal(2, code);
        Assert.Contains("--pump does not take 'maybe': it takes dispatcher or none", said);
        Assert.Contains("--pump=dispatcher|none", said);
    }

    /// <summary>
    /// Whether the window's thread is dispatching at all, asked without needing the foreground:
    /// a message it must answer, with a bound on how long it may take. This is the one reading
    /// that tells a live window from a composed picture of a dead one.
    /// </summary>
    private static bool Answers(nint window) =>
        SendMessageTimeoutW(window, 0x0000, 0, 0, 0x0002, 1500, out _) != 0;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SendMessageTimeoutW(
        nint window, uint message, nint wParam, nint lParam, uint flags, uint timeoutMs, out nint result);

    [Fact]
    public void The_names_pane_carries_both_branches_of_the_rule_at_once()
    {
        var facts = NamesPane();

        // The four that must fail, each for its own reason, and the one that must not.
        Assert.Equal(Named.Missing, Names.Of(facts["unnamed"]).Verdict);
        Assert.Equal(Named.Glyph, Names.Of(facts["glyphOnly"]).Verdict);
        Assert.Equal(Named.EchoesTheId, Names.Of(facts["echoesTheId"]).Verdict);
        Assert.Equal(Named.Missing, Names.Of(facts["profileBox"]).Verdict);
        Assert.Equal(Named.Spoken, Names.Of(facts["spoken"]).Verdict);
    }

    [Fact]
    public void The_glyph_control_is_the_worst_case_and_prints_as_an_escape()
    {
        var check = Names.Of(NamesPane()["glyphOnly"]);

        // A control announcing a codepoint and nothing else arrives in a report looking exactly
        // like the empty case it is not, unless the report writes it out.
        Assert.False(check.IsALabel);
        Assert.Equal(@"\uE711", check.Printable);
        Assert.Contains("which is a font glyph and not a label", check.Sentence("the cancel button"));
    }

    [Fact]
    public void The_box_whose_label_is_a_neighbour_reads_as_unnamed_and_its_label_reads_fine()
    {
        var facts = NamesPane();

        // The shape one real window shipped: two controls reading as unnamed while every
        // neighbouring button read fine, because a control takes its name from its own content.
        Assert.False(Names.Of(facts["profileBox"]).IsALabel);
        Assert.Equal("Profile", facts["profileLabel"].Name);
        Assert.Contains("its label is likely a separate text block beside it", Names.Of(facts["profileBox"]).Sentence("the profile box"));
    }

    [Fact]
    public void Without_the_flag_the_page_carries_none_of_them()
    {
        var tree = Inspect.Window(Launched().Handle, depth: 12)!;
        var ids = tree.Walk().Select(one => one.Facts.AutomationId).ToList();

        Assert.DoesNotContain("namesPane", ids);
        Assert.DoesNotContain("unnamed", ids);
    }

    /// <summary>Launch with the naming pane, and read every control on it by its automation id.</summary>
    private Dictionary<string, ElementFacts> NamesPane()
    {
        var tree = Inspect.Window(Launched("--names").Handle, depth: 12)!;

        return tree.Walk()
            .Select(one => one.Facts)
            .Where(one => one.AutomationId.Length > 0)
            .GroupBy(one => one.AutomationId, StringComparer.Ordinal)
            .ToDictionary(one => one.Key, one => one.First(), StringComparer.Ordinal);
    }

    [Fact]
    public void A_collapsed_pane_is_a_door_that_says_how_it_opens()
    {
        var root = Absences();

        var miss = Resolve.Once(root, Locator.Parse("""Group#collapsedPane > Button[name="Inside the pane"]""")).Miss!;

        Assert.Equal(MissKind.NavigationNeeded, miss.Kind);
        Assert.Contains("is expanded", miss.Route);
        Assert.Contains("it will not be until", miss.Sentence());
    }

    [Fact]
    public void An_unopened_submenu_is_the_same_kind_of_absence_and_a_different_door()
    {
        var root = Absences();

        var miss = Resolve.Once(root, Locator.Parse("""MenuItem#fileMenu > MenuItem[name="Recent"]""")).Miss!;

        Assert.Equal(MissKind.NavigationNeeded, miss.Kind);
        Assert.Contains("is expanded", miss.Route);
    }

    [Fact]
    public void A_closed_popup_leaves_nothing_behind_at_all_not_even_itself()
    {
        // The one that reads differently from the other two. A collapsed pane and an unopened
        // submenu are both in the tree announcing that they are shut; a closed popup is not there,
        // so nothing in the window says what is missing or how to reach it.
        var root = Absences();

        Assert.Null(Resolve.Once(root, Locator.Parse("#closedFlyout")).Facts);

        var miss = Resolve.Once(root, Locator.Parse("""Button[name="Inside the flyout"]""")).Miss!;

        Assert.Equal(MissKind.Absent, miss.Kind);

        // Nothing in the window is a door onto it. The collapsed pane and the file menu are both
        // offered as leads and neither is where it went, which is the honest shape of this miss.
        Assert.DoesNotContain(miss.ClosedDoors, one => one.What.Contains("flyout", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void What_is_shut_in_the_window_is_offered_as_leads_on_a_miss_nothing_explains()
    {
        var root = Absences();

        var miss = Resolve.Once(root, Locator.Parse("""Button[name="Inside the flyout"]""")).Miss!;

        // Leads rather than an answer: each is a true statement about the window, and none of them
        // claims to hold what was looked for.
        var doors = miss.ClosedDoors.Select(one => one.ToString()).ToList();
        Assert.Contains(doors, one => one.Contains("collapsedPane (expanded)", StringComparison.Ordinal));
        Assert.Contains(doors, one => one.Contains("fileMenu (expanded)", StringComparison.Ordinal));

        // The panes nobody picked are doors too, and they open a different way. Two kinds of shut
        // in one list is exactly why the lead says how rather than only what.
        Assert.Contains(doors, one => one.Contains("statusPane (selected)", StringComparison.Ordinal));
    }

    [Fact]
    public void What_is_present_beside_them_still_resolves()
    {
        // A classification that has never seen a hit cannot be trusted about a miss.
        var resolution = Resolve.Once(Absences(), Locator.Parse("""Button[name="Showing"]"""));

        Assert.True(resolution.Found);
        Assert.Null(resolution.Miss);
    }

    /// <summary>Launch with the three absences and hand back the window to resolve against.</summary>
    private AutomationElement Absences() => AutomationElement.FromHandle(Launched("--absences").Handle);

    [Fact]
    public void A_window_that_opted_into_a_backdrop_says_so_to_the_compositor()
    {
        var window = Launched("--backdrop=mica");

        // Read from the compositor and not from the fixture: what a window asked for and what it
        // has are different claims, and only the second one decides what a copy of it contains.
        Assert.Equal(2, SystemBackdrop(window.Handle));
    }

    [Fact]
    public void A_window_that_never_asked_is_told_apart_from_one_that_asked_for_nothing()
    {
        // The arm a one-sided check gets wrong. Auto is the compositor deciding; none is the
        // window having decided. A refusal that read them as the same would let one through.
        Assert.Equal(0, SystemBackdrop(Launched().Handle));
        Assert.Equal(1, SystemBackdrop(Launched("--backdrop=none").Handle));
    }

    [Fact]
    public void Every_backdrop_the_catalogue_offers_is_one_the_compositor_takes()
    {
        // A name in the catalogue that the compositor refuses is a shape nobody can provoke, which
        // is the whole failure this block exists to stop.
        Assert.Equal(3, SystemBackdrop(Launched("--backdrop=acrylic").Handle));
        Assert.Equal(4, SystemBackdrop(Launched("--backdrop=tabbed").Handle));
    }

    [Fact]
    public void A_backdrop_the_fixture_does_not_have_is_refused_with_the_ones_it_does()
    {
        var (code, said) = Ran("--backdrop=frosted");

        Assert.Equal(2, code);
        Assert.Contains("it takes none or mica or acrylic or tabbed", said);
    }

    /// <summary>DWMWA_SYSTEMBACKDROP_TYPE, asked of the compositor rather than of the window.</summary>
    private static int SystemBackdrop(nint window) =>
        DwmGetWindowAttribute(window, 38, out var read, sizeof(int)) == 0 ? read : -1;

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);
}
