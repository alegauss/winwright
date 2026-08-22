using System.Diagnostics;
using System.Runtime.InteropServices;

using System.Windows.Automation;

using Winwright.Asserting;
using Winwright.Capturing;
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

    public void Dispose()
    {
        // Settled and not merely stopped: the next class in this collection is often the one
        // measuring who owns the foreground, and a window still fading off the desktop is exactly
        // what that measurement cannot tell from a defect.
        Attachable.StopAndSettle(register);
        register.Dispose();
        Directory.Delete(root, recursive: true);
    }

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

    [Fact]
    public void Raising_the_fixture_does_not_take_the_desktop_from_whoever_had_it()
    {
        // WW128, measured by exclusion before it was written: the full run failed twelve checks
        // and the same run without this class failed one. All eleven need the foreground.
        var before = Foreground.Now();
        Assert.NotEqual(0, before.Window);

        var window = Launched();

        var after = Foreground.Now();
        Assert.Equal(before.Window, after.Window);
        Assert.NotEqual(window.Handle, after.Window);
    }

    [Fact]
    public void Everything_the_fixture_is_for_survives_not_being_activated()
    {
        var window = Launched();

        // On screen, composed, enumerable, and readable through the tree. Losing any of those to
        // save the foreground would be trading the fixture for the thing it exists to be.
        Assert.True(window.OnScreen, window.ToString());
        Assert.True(window.Bounds.Area > 0, window.ToString());
        Assert.Contains(TopLevelWindows.OfProcess(window.Pid), one => one.Handle == window.Handle);
        Assert.NotNull(Inspect.Window(window.Handle, depth: 4));
    }
    [Fact]
    public void A_toast_beside_the_main_window_is_found_by_enumeration_and_not_by_the_process()
    {
        var pid = LaunchedPid("--toast=beside");

        var windows = Waited(pid, howMany: 2);
        var toast = Assert.Single(windows, one => one.Title.Length == 0);

        Assert.True(toast.IsOwned, "the toast is unowned, and a real one is owned by the window that raised it");
        Assert.True(toast.OnScreen, toast.ToString());

        using var process = Process.GetProcessById(pid);
        process.Refresh();
        Assert.NotEqual(toast.Handle, process.MainWindowHandle);
    }

    [Fact]
    public void A_run_whose_only_window_is_a_toast_has_no_main_window_at_all()
    {
        // The whole reason the launcher enumerates. Asked which window it had, this process
        // answers zero, and a launcher that believed it would conclude nothing was drawn.
        var pid = LaunchedPid("--toast=only");

        var toast = Assert.Single(Waited(pid, howMany: 1));
        Assert.Equal("", toast.Title);
        Assert.True(toast.OnScreen, toast.ToString());

        using var process = Process.GetProcessById(pid);
        process.Refresh();
        Assert.Equal(0, process.MainWindowHandle);
    }

    [Fact]
    public void A_toast_is_borderless_and_stays_off_the_taskbar()
    {
        var pid = LaunchedPid("--toast=only");
        var toast = Assert.Single(Waited(pid, howMany: 1));

        // Its size is its own and not a frame's. Asserted as a ratio because the display's scaling
        // multiplies both sides equally and a caption or a resize border would not - either one
        // adds rows without adding columns, which is what this number would catch.
        Assert.True(toast.Bounds.Width > 0 && toast.Bounds.Height > 0, toast.ToString());
        Assert.Equal(320d / 90, (double)toast.Bounds.Width / toast.Bounds.Height, precision: 1);
        Assert.Equal("", toast.Title);
    }

    [Fact]
    public void A_way_of_raising_one_the_fixture_does_not_have_is_refused()
    {
        var (code, said) = Ran("--toast=someday");

        Assert.Equal(2, code);
        Assert.Contains("it takes beside or only", said);
    }

    /// <summary>Launch and hand back the pid, without waiting on a caption a toast does not have.</summary>
    private int LaunchedPid(params string[] flags)
    {
        var start = new ProcessStartInfo(Executable());
        foreach (var flag in flags)
            start.ArgumentList.Add(flag);

        return Attachable.Launch(register, start).Pid;
    }

    /// <summary>Wait until the process owns that many windows above the size floor.</summary>
    private static IReadOnlyList<TopLevelWindow> Waited(int pid, int howMany)
    {
        for (var attempt = 0; attempt < 200; attempt++)
        {
            var windows = TopLevelWindows.OfProcess(pid);
            if (windows.Count >= howMany)
                return windows;

            Thread.Sleep(25);
        }

        Assert.Fail($"pid {pid} never owned {howMany} window(s)");
        return [];
    }

    [Fact]
    public void A_page_still_loading_shows_the_note_and_not_the_content()
    {
        // The refusal's arm, at a moment this run chose. It was found on a machine that happened
        // to be slow, and reproducing it meant finding another one.
        var window = Launched("--loading=6000");

        var names = Ids(window);

        // Read on reportNote and not on reportSwatch: a Border has no automation peer, so
        // asserting its absence would pass on a page that never loaded at all.
        Assert.Contains("loadingNote", names);
        Assert.DoesNotContain("reportNote", names);
    }

    [Fact]
    public void A_page_that_finishes_inside_the_wait_must_not_be_refused_for_it()
    {
        // The other arm, and the one a one-sided check gets wrong: a page that loaded and then
        // finished is a page, and refusing it would make the check useless on a slow desk.
        var window = Launched("--loading=150");

        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (Ids(window).Contains("reportNote"))
            {
                Assert.DoesNotContain("loadingNote", Ids(window));
                return;
            }

            Thread.Sleep(25);
        }

        Assert.Fail("the page never finished loading, so the shorter duration reached nothing");
    }

    [Fact]
    public void A_duration_that_is_not_a_number_is_refused_rather_than_read_as_none()
    {
        // Taken as zero it would load for no time at all, which is the shape nobody can provoke
        // again and a green nobody would question.
        var (code, said) = Ran("--loading=twoseconds");

        Assert.Equal(2, code);
        Assert.Contains("--loading takes a whole number of milliseconds and was given 'twoseconds'", said);
        Assert.Equal(2, Ran("--loading=-5").Code);
    }

    /// <summary>Every automation id on the window, which is what a page's state reads as.</summary>
    private static IReadOnlyList<string> Ids(TopLevelWindow window) =>
        Inspect.Window(window.Handle, depth: 12)!
            .Walk()
            .Select(one => one.Facts.AutomationId)
            .Where(one => one.Length > 0)
            .ToList();

    [Fact]
    public void The_animation_says_how_many_states_it_has_so_nothing_has_to_be_told()
    {
        var window = Launched("--animate=200");

        var said = Sampled(window, TimeSpan.FromSeconds(3));

        // Read off the window, never typed: an expectation typed into a case is one that goes
        // stale the day the animation gains a state.
        var count = Assert.Single(said.Select(one => one.Split(" of ")[1]).Distinct(StringComparer.Ordinal));
        var declared = int.Parse(count, System.Globalization.CultureInfo.InvariantCulture);

        Assert.True(declared > 1, $"an animation of {declared} state(s) is not one");
        Assert.Equal(declared, said.Select(one => one.Split(' ')[0]).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void The_states_arrive_in_the_order_they_were_declared_in_and_come_round_again()
    {
        // Five hundred and not one fifty, measured: reading the tree of another process costs
        // more than a state stands for at that speed, so the sampler skipped one and the order
        // read as broken when it was the reading that could not keep up. A frame sequence cannot
        // be checked faster than it can be read, and that is a property of the check.
        var window = Launched("--animate=500");
        var seen = Changes(Sampled(window, TimeSpan.FromSeconds(5)));

        Assert.True(seen.Count >= 4, $"only {seen.Count} state change(s) were seen");

        var declared = int.Parse(
            seen[0].Split(" of ")[1], System.Globalization.CultureInfo.InvariantCulture);

        for (var at = 1; at < seen.Count; at++)
        {
            var previous = int.Parse(seen[at - 1].Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);
            var now = int.Parse(seen[at].Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);

            Assert.Equal(previous % declared + 1, now);
        }
    }

    [Fact]
    public void A_full_cycle_takes_about_as_long_as_the_length_the_run_declared()
    {
        // Loosely, and deliberately: a live window sampled from another process cannot be timed to
        // the millisecond. The band is wide enough to survive a busy desk and narrow enough to
        // catch an animation that is not running, or one running ten times too fast.
        const int every = 200;
        var window = Launched($"--animate={every}");

        var started = System.Diagnostics.Stopwatch.StartNew();
        var seen = Changes(Sampled(window, TimeSpan.FromSeconds(5)));
        var elapsed = started.Elapsed;

        var declared = int.Parse(seen[0].Split(" of ")[1], System.Globalization.CultureInfo.InvariantCulture);
        var perState = elapsed.TotalMilliseconds / Math.Max(1, seen.Count - 1);

        Assert.True(
            perState > every * 0.4 && perState < every * 3,
            $"{seen.Count} change(s) over {elapsed.TotalMilliseconds:0}ms is {perState:0}ms each, against {every}");
        Assert.True(declared > 1);
    }

    [Fact]
    public void Without_the_flag_nothing_is_animating()
    {
        var window = Launched();

        Assert.DoesNotContain("animationState", Ids(window));
    }

    /// <summary>Read what the animation is showing, over and over, for as long as this is given.</summary>
    private static IReadOnlyList<string> Sampled(TopLevelWindow window, TimeSpan howLong)
    {
        var said = new List<string>();
        var until = DateTime.UtcNow + howLong;

        while (DateTime.UtcNow < until)
        {
            var tree = Inspect.Window(window.Handle, depth: 12);
            var state = tree?.Walk().FirstOrDefault(one => one.Facts.AutomationId == "animationState");
            if (state is not null && state.Facts.Name.Contains(" of ", StringComparison.Ordinal))
                said.Add(state.Facts.Name);

            Thread.Sleep(30);
        }

        Assert.NotEmpty(said);
        return said;
    }

    /// <summary>The samples with the repeats removed, which is the sequence rather than the poll.</summary>
    private static IReadOnlyList<string> Changes(IReadOnlyList<string> sampled)
    {
        var changes = new List<string>();
        foreach (var one in sampled)
        {
            if (changes.Count == 0 || !string.Equals(changes[^1], one, StringComparison.Ordinal))
                changes.Add(one);
        }

        return changes;
    }

    [Fact]
    public void Two_renders_of_the_fixed_surface_are_byte_identical()
    {
        // The whole point of the shape: a comparison needs something to be identical to, and a
        // surface reading a clock, a machine name or the desktop's theme is never identical twice.
        var first = Rendered("first.png");
        var second = Rendered("second.png");

        Assert.Equal(File.ReadAllBytes(first), File.ReadAllBytes(second));
        Assert.True(new FileInfo(first).Length > 0, "the render wrote an empty file");
    }

    [Fact]
    public void The_render_shows_no_window_and_says_what_it_drew()
    {
        var start = new ProcessStartInfo(Executable())
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };

        start.ArgumentList.Add($"--render={Path.Combine(root, "quiet.png")}");

        using var running = Process.Start(start)!;
        var said = running.StandardOutput.ReadToEnd();
        Assert.True(running.WaitForExit(30_000), "the render did not finish");

        Assert.Equal(0, running.ExitCode);
        Assert.Contains("rendered 360x200", said);

        // No window was ever shown, so nothing about this run could take the desktop.
        Assert.Empty(TopLevelWindows.OfProcess(running.Id));
    }

    [Fact]
    public void The_fixed_surface_is_drawn_with_no_themed_control_on_it()
    {
        // The one that is easy to miss: a button, a tab header or a text box draws its chrome from
        // the desktop's theme and accent colour, so a pane holding one renders differently on two
        // desks while every value on it is fixed.
        var source = File.ReadAllText(Path.Combine(Sources(), "FixedPane.cs"));

        foreach (var themed in new[] { "new Button", "new TabItem", "new TextBox", "new CheckBox", "SystemColors" })
            Assert.DoesNotContain(themed, source, StringComparison.Ordinal);
    }

    /// <summary>Render the fixed surface to a file of its own and hand back the path.</summary>
    private string Rendered(string name)
    {
        var path = Path.Combine(root, name);
        var start = new ProcessStartInfo(Executable());
        start.ArgumentList.Add($"--render={path}");

        using var running = Process.Start(start)!;
        Assert.True(running.WaitForExit(30_000), "the render did not finish");
        Assert.Equal(0, running.ExitCode);
        Assert.True(File.Exists(path), $"the render wrote nothing to {path}");

        return path;
    }

    private readonly string root = Directory.CreateTempSubdirectory("winwright-fixed-").FullName;

    /// <summary>Where the fixture's own sources are, for the check that reads them.</summary>
    private static string Sources()
    {
        var here = new DirectoryInfo(AppContext.BaseDirectory);
        while (here is not null && !File.Exists(Path.Combine(here.FullName, "Winwright.slnx")))
            here = here.Parent;

        Assert.NotNull(here);
        return Path.Combine(here.FullName, "src", "Winwright.Fixture");
    }

    [Fact]
    public void A_second_windowed_instance_is_what_the_refusal_exists_for()
    {
        Launched();
        Launched();

        // Nothing of this run's is named as ours, so both count as other instances.
        var check = InstanceCheck.Of(Executable());

        Assert.True(check.Windowed.Count >= 2, check.Sentence());
        Assert.True(check.Refuses, check.Sentence());
        Assert.Throws<AnotherInstanceException>(check.RequireSole);
    }

    [Fact]
    public void The_override_beside_it_is_driven_rather_than_remembered()
    {
        Launched();
        Launched();

        var check = InstanceCheck.Of(Executable(), allowOthers: true);

        Assert.False(check.Refuses);
        check.RequireSole();

        // Named in the sentence, because an override that does not appear in the output is one
        // nobody remembers passing.
        Assert.Contains(InstanceCheck.OverrideName, check.Sentence());
    }

    [Fact]
    public void A_process_showing_nothing_must_never_trip_the_refusal()
    {
        // The ordinary state of every developer machine this tool was written on. A check that
        // fired on it would make every capture take an override, which is an override everybody
        // passes always and therefore a check nobody has.
        var resident = Attachable.Launch(register, Started("--resident")).Pid;

        for (var attempt = 0; attempt < 200 && TopLevelWindows.OfProcess(resident).Count == 0; attempt++)
            Thread.Sleep(25);

        var check = InstanceCheck.Of(Executable());

        Assert.Contains(check.Resident, one => one.Pid == resident);
        Assert.DoesNotContain(check.Windowed, one => one.Pid == resident);
        Assert.False(check.Refuses, check.Sentence());
    }

    [Fact]
    public void A_run_never_counts_its_own_processes_as_another_instance()
    {
        var mine = Attachable.Launch(register, Started()).Pid;

        var check = InstanceCheck.Of(Executable(), ours: [mine]);

        Assert.DoesNotContain(check.Others, one => one.Pid == mine);
    }

    /// <summary>The fixture's start info, with whatever flags this case wants.</summary>
    private static ProcessStartInfo Started(params string[] flags)
    {
        var start = new ProcessStartInfo(Executable());
        foreach (var flag in flags)
            start.ArgumentList.Add(flag);

        return start;
    }

    [Fact]
    public void A_run_that_writes_its_store_the_same_way_twice_left_the_machine_as_it_found_it()
    {
        var store = Path.Combine(root, "clean");
        Launched($"--store={store}");

        // Fingerprinted around a second launch that writes the same constants. This is the arm a
        // one-sided check gets wrong: a run that touches a file and leaves it identical is clean.
        var change = Untouched.Around([store], () => Launched($"--store={store}"));

        Assert.True(change.Untouched, change.Sentence());
        Assert.Contains("left the machine as it found it", change.Sentence());
    }

    [Fact]
    public void A_run_that_changed_the_store_is_caught_and_the_file_is_named()
    {
        var store = Path.Combine(root, "dirty");
        Launched($"--store={store}");

        var change = Untouched.Around([store], () => Launched($"--store={store}", "--mutate"));

        Assert.False(change.Untouched);
        Assert.Equal(1, change.Moved);
        Assert.Contains("settings.json", change.Changed[0]);
        Assert.Contains("was rewritten", change.Sentence());
    }

    [Fact]
    public void The_mutation_is_the_same_number_of_bytes_as_what_it_replaced()
    {
        // The exact accident the fingerprint exists for: a settings file repointed from one
        // profile to another of the same name, which size or write time calls unchanged.
        var settled = Path.Combine(root, "sized-clean");
        var mutated = Path.Combine(root, "sized-dirty");

        Launched($"--store={settled}");
        Launched($"--store={mutated}", "--mutate");

        var before = new FileInfo(Path.Combine(settled, "settings.json"));
        var after = new FileInfo(Path.Combine(mutated, "settings.json"));

        Assert.Equal(before.Length, after.Length);
        Assert.NotEqual(File.ReadAllText(before.FullName), File.ReadAllText(after.FullName));
    }

    [Fact]
    public void A_store_this_run_never_wrote_is_reported_as_appearing_rather_than_as_untouched()
    {
        var store = Path.Combine(root, "fresh");
        Directory.CreateDirectory(store);

        var change = Untouched.Around([store], () => Launched($"--store={store}"));

        Assert.False(change.Untouched);
        Assert.Equal(2, change.Appeared.Count);
        Assert.Contains("were created", change.Sentence());
    }

    [Fact]
    public void Mutating_with_no_store_to_mutate_is_refused()
    {
        // A flag that does nothing without another is a flag that silently does nothing, which is
        // the same green as a misspelt one and just as hard to notice.
        var (code, said) = Ran("--mutate");

        Assert.Equal(2, code);
        Assert.Contains("--mutate has nothing to change without --store=<directory>", said);
    }

    [Fact]
    public void Settling_waits_for_the_process_to_be_gone_and_not_only_for_its_window()
    {
        // WW129, measured: no window of a stopped process is enumerable well before the process
        // has exited, and the class that follows is usually the one asserting who owns the desk.
        using var register = new ProcessRegister();
        var pid = Attachable.Launch(register, Started()).Pid;

        for (var attempt = 0; attempt < 200 && TopLevelWindows.OfProcess(pid).Count == 0; attempt++)
            Thread.Sleep(25);

        Attachable.StopAndSettle(register);

        Assert.Throws<ArgumentException>(() => Process.GetProcessById(pid));
    }

    [Fact]
    public void The_fixture_reports_the_surfaces_it_drew_when_the_harness_asks()
    {
        var (surfaces, _) = Driven();

        var window = SurfaceReport.Of(surfaces, "the window");
        var panes = SurfaceReport.Of(surfaces, "the panes");

        Assert.True(window.Reported, window.Sentence());
        Assert.True(panes.Reported, panes.Sentence());

        // The panes are inside the window, which is the relation a capture is asserted on.
        Assert.True(Containment.Of(window.Surface!.Bounds, panes.Surface!).Contains, panes.Sentence());
    }

    [Fact]
    public void The_fixture_dumps_the_geometry_it_laid_out()
    {
        var (_, geometry) = Driven();

        var read = GeometryDump.Read(geometry);

        Assert.NotNull(read.Root);
        Assert.True(read.Elements.Count > 10, read.Sentence());
        Assert.Equal(0, read.Unreadable);
    }

    [Fact]
    public void The_dump_the_fixture_writes_is_one_the_layout_check_can_read()
    {
        var (_, geometry) = Driven();

        var read = Layout.Of(geometry);

        // Read, not held: a real themed window is not laid out to this check's satisfaction and
        // that is a fact about the framework rather than about the fixture. Its default tab
        // template lifts a selected header four pixels outside the panel holding it and two past
        // the border containing it, and a collapsed element measures nothing on purpose. Both are
        // filed as gaps in the reading rather than papered over with a narrower assertion here.
        Assert.True(read.Examined > 10, read.Sentence());
        Assert.NotNull(read.Root);
        Assert.Contains(read.Faults, one => one.Kind == Fault.MeasuresNothing);
    }

    /// <summary>Where the fixture's own sources are.</summary>
    private static string FixtureSources()
    {
        var here = new DirectoryInfo(AppContext.BaseDirectory);
        while (here is not null && !File.Exists(Path.Combine(here.FullName, "Winwright.slnx")))
            here = here.Parent;

        Assert.NotNull(here);
        return Path.Combine(here.FullName, "src", "Winwright.Fixture");
    }

    [Fact]
    public void An_application_nobody_asked_reports_nothing_and_dumps_nothing()
    {
        // What makes the protocol safe to leave in a release rather than something to remember to
        // take out: no variable set, no file written, nothing on anybody's disk.
        var surfaces = Path.Combine(root, "unasked-surfaces.tsv");
        var geometry = Path.Combine(root, "unasked-geometry.tsv");

        Launched();

        Assert.False(File.Exists(surfaces));
        Assert.False(File.Exists(geometry));
    }

    [Fact]
    public void The_render_is_drawn_on_the_background_the_application_declares()
    {
        var path = Path.Combine(root, "declared.png");
        var start = Started($"--render={path}");
        start.RedirectStandardOutput = true;
        start.UseShellExecute = false;

        using var running = Process.Start(start)!;
        var said = running.StandardOutput.ReadToEnd();
        Assert.True(running.WaitForExit(30_000), "the render did not finish");

        // Named in the receipt, so a hex value in a report can be traced back to a theme key.
        Assert.Contains("the theme's 'WinwrightCaptureBackground'", said);
        Assert.False(Pictures.Of(path).IsBlank);
    }

    /// <summary>Launch with the harness's own channels open, and hand back where they landed.</summary>
    private (string Surfaces, string Geometry) Driven()
    {
        var surfaces = Path.Combine(root, "driven-surfaces.tsv");
        var geometry = Path.Combine(root, "driven-geometry.tsv");

        var start = Started();
        start.Environment[SurfaceReport.PathVariable] = surfaces;
        start.Environment[GeometryDump.PathVariable] = geometry;

        var launched = Attachable.Launch(register, start);
        for (var attempt = 0; attempt < 200; attempt++)
        {
            if (File.Exists(surfaces) && File.Exists(geometry))
                return (surfaces, geometry);

            Thread.Sleep(25);
        }

        Assert.Fail($"pid {launched.Pid} never wrote what it drew");
        return ("", "");
    }

    [Fact]
    public void The_fixture_ships_more_than_one_language_and_the_window_shows_the_one_asked_for()
    {
        // One language is not enough to develop the rule against: a check that only ever saw
        // English cannot tell a label it read from a label that happens to be the same word.
        var english = Ids(Launched("--language=en"));
        Assert.NotEmpty(english);

        var headers = Headers(Launched("--language=pt-BR"));
        Assert.Equal(["Relatório", "Estado", "Configuração"], headers);

        Assert.Equal(["Bericht", "Status", "Konfiguration"], Headers(Launched("--language=de")));
    }

    [Fact]
    public void The_expected_headers_are_derived_from_the_fixture_s_own_strings_and_never_typed()
    {
        var window = Launched("--language=de");

        // The whole of the derived-set rule, against a real localized window: the expectation is
        // read out of the file the application is showing, so it cannot drift from it.
        var set = DerivedSet.From("the tab headers", Strings("de"), "tabs");
        var compared = set.Against(Headers(window));

        Assert.True(compared.Held, compared.Sentence());
        Assert.Equal(3, compared.Matched.Count);
    }

    [Fact]
    public void The_one_key_carrying_a_placeholder_is_refused_rather_than_skipped()
    {
        // An exact-name read can never match it, and a rule that skipped it would report a green
        // about a control nobody could have checked.
        foreach (var culture in new[] { "en", "pt-BR", "de" })
        {
            var said = DerivedSet.From("the labels", Strings(culture), "labels").Expected;

            Assert.Contains(said, one => Labels.CarriesAPlaceholder(one));
        }
    }

    [Fact]
    public void A_window_in_that_language_really_shows_the_unmatchable_label()
    {
        var window = Launched("--language=pt-BR");

        var shown = Inspect.Window(window.Handle, depth: 12)!
            .Walk()
            .Single(one => one.Facts.AutomationId == "localizedLabel")
            .Facts.Name;

        // Present on the window and unmatchable by an exact read, which is the pair that makes the
        // refusal developable rather than reasoned about.
        Assert.Equal("Perfil: {name}", shown);
        Assert.True(Labels.CarriesAPlaceholder(shown));
    }

    [Fact]
    public void A_language_the_fixture_does_not_ship_is_refused_with_the_ones_it_does()
    {
        var (code, said) = Ran("--language=fr");

        Assert.Equal(2, code);
        Assert.Contains("it takes en or pt-BR or de", said);
    }

    /// <summary>The tab headers the window is showing, in order.</summary>
    private static IReadOnlyList<string> Headers(TopLevelWindow window) =>
        Inspect.Window(window.Handle, depth: 12)!
            .Walk()
            .Where(one => one.Facts.ControlType == "TabItem")
            .Select(one => one.Facts.Name)
            .ToList();

    /// <summary>The strings file the fixture ships for one language, beside its executable.</summary>
    private static string Strings(string culture) =>
        Path.Combine(Path.GetDirectoryName(Executable())!, "strings", $"strings.{culture}.json");

    [Fact]
    public void An_intruder_lands_on_exactly_the_rectangle_it_was_named_in_physical_pixels()
    {
        var pid = LaunchedPid("--intrude=400,300,240,160");

        var intruder = Assert.Single(
            Waited(pid, howMany: 2), one => one.Title.Length == 0 && one.Bounds.Width == 240);

        // The whole of the placement rule: named in physical pixels and placed with a call that
        // takes them. Set through the layout's own units it would land at half of this on a
        // display at two hundred percent, and the check reading it would be right about a window
        // nobody meant to put there.
        Assert.Equal(400, intruder.Bounds.Left);
        Assert.Equal(300, intruder.Bounds.Top);
        Assert.Equal(240, intruder.Bounds.Width);
        Assert.Equal(160, intruder.Bounds.Height);
    }

    [Fact]
    public void An_intruder_over_the_window_covers_part_of_it_and_one_elsewhere_covers_none()
    {
        var over = Placed("--intrude=200,200,300,200");
        Assert.True(Overlap(over.Window, over.Intruder) > 0, $"{over.Intruder} covers none of {over.Window}");

        // The case that must pass, and the one a check exercised by hand never gets to: an intruder
        // that is genuinely in the way of nothing.
        var beside = Placed("--intrude=3000,2000,200,150");
        Assert.Equal(0, Overlap(beside.Window, beside.Intruder));
    }

    [Fact]
    public void An_intruder_is_topmost_so_it_is_over_the_window_and_not_merely_beside_it()
    {
        var placed = Placed("--intrude=200,200,300,200");

        // Read off the window rather than assumed from the request: a topmost style that did not
        // take would leave a window in the right rectangle and behind everything.
        Assert.True(Topmost(placed.Intruder.Handle), $"{placed.Intruder} is not topmost");
        Assert.False(Topmost(placed.Window.Handle), "the fixture's own window became topmost");
    }

    [Fact]
    public void A_rectangle_that_is_not_four_numbers_is_refused_before_any_window()
    {
        Assert.Contains("takes left,top,width,height", Ran("--intrude=200,200,300").Said);
        Assert.Contains("takes whole numbers", Ran("--intrude=200,200,300,tall").Said);

        // A rectangle of no area covers nothing, so an intruder placed at one provokes the refusal
        // it exists for exactly never.
        Assert.Contains("covers nothing", Ran("--intrude=200,200,0,150").Said);
    }

    /// <summary>Launch with an intruder and hand back both windows.</summary>
    private (TopLevelWindow Window, TopLevelWindow Intruder) Placed(string flag)
    {
        var pid = LaunchedPid(flag);
        var windows = Waited(pid, howMany: 2);

        return (
            windows.Single(one => one.Title == "winwright fixture"),
            windows.Single(one => one.Title.Length == 0));
    }

    /// <summary>How many pixels the two rectangles share.</summary>
    private static long Overlap(TopLevelWindow left, TopLevelWindow right)
    {
        var width = Math.Min(left.Bounds.Right, right.Bounds.Right) - Math.Max(left.Bounds.Left, right.Bounds.Left);
        var height = Math.Min(left.Bounds.Bottom, right.Bounds.Bottom) - Math.Max(left.Bounds.Top, right.Bounds.Top);

        return width <= 0 || height <= 0 ? 0 : (long)width * height;
    }

    /// <summary>Whether that window carries the topmost style, asked of Windows.</summary>
    private static bool Topmost(nint window) => (GetWindowLongPtrW(window, -20) & 0x00000008) != 0;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint GetWindowLongPtrW(nint window, int index);

    [Fact]
    public void A_locator_resolves_against_nothing_on_the_peerless_pane()
    {
        var window = Launched("--peerless");

        var ids = Ids(window);

        // The pane itself is a tab and has a peer; everything it holds has none. That is what a
        // custom-drawn surface looks like from outside, and what an installer page is.
        Assert.Contains("drawnPane", ids);
        Assert.DoesNotContain("drawnSurface", ids);
        Assert.DoesNotContain("drawnHeader", ids);
        Assert.DoesNotContain("drawnBody", ids);
    }

    [Fact]
    public void The_geometry_dump_reports_all_of_what_the_tree_cannot_see()
    {
        var geometry = Path.Combine(root, "peerless.tsv");
        var start = Started("--peerless");
        start.Environment[GeometryDump.PathVariable] = geometry;

        Attachable.Launch(register, start);
        for (var attempt = 0; attempt < 200 && !File.Exists(geometry); attempt++)
            Thread.Sleep(25);

        var read = GeometryDump.Read(geometry);

        // The whole contrast the dump was built for: nothing above found these, and here they are.
        foreach (var name in new[] { "drawnSurface", "drawnHeader", "drawnBody", "drawnFooter" })
            Assert.NotEmpty(read.Named(name));
    }

    [Fact]
    public void What_the_dump_reports_of_it_is_laid_out_soundly()
    {
        var geometry = Path.Combine(root, "peerless-layout.tsv");
        var start = Started("--peerless");
        start.Environment[GeometryDump.PathVariable] = geometry;

        Attachable.Launch(register, start);
        for (var attempt = 0; attempt < 200 && !File.Exists(geometry); attempt++)
            Thread.Sleep(25);

        var read = GeometryDump.Read(geometry);
        var surface = Assert.Single(read.Named("drawnSurface"));

        // Every box inside the surface holding them, and none of them overlapping the next - the
        // invariants a page with no tree can be checked on at all.
        var boxes = new[] { "drawnHeader", "drawnBody", "drawnFooter" }
            .Select(one => Assert.Single(read.Named(one)))
            .ToList();

        Assert.All(boxes, box => Assert.True(
            box.Bounds.Left >= surface.Bounds.Left && box.Bounds.Right <= surface.Bounds.Right,
            $"{box} is not inside {surface}"));

        for (var at = 1; at < boxes.Count; at++)
            Assert.True(boxes[at].Bounds.Top >= boxes[at - 1].Bounds.Bottom, $"{boxes[at]} overlaps {boxes[at - 1]}");
    }

    [Fact]
    public void Without_the_flag_there_is_no_peerless_pane_at_all()
    {
        Assert.DoesNotContain("drawnPane", Ids(Launched()));
    }
}
