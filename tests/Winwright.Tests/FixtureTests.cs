using System.Diagnostics;
using System.Runtime.InteropServices;

using System.Windows.Automation;

using Winwright.Asserting;
using Winwright.Capturing;
using Winwright.Locating;
using Winwright.Processes;
using Winwright.Projects;
using Winwright.Verdicts;
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
    private static string Executable() => Fixture.Executable();

    /// <summary>Launch it and wait for the window it draws, which is the only signal worth waiting on.</summary>
    private TopLevelWindow Launched(params string[] flags)
    {
        var start = new ProcessStartInfo(Executable());
        foreach (var flag in flags)
            start.ArgumentList.Add(flag);

        var launched = Attachable.Launch(register, start);

        return Waits.Until(
            "draw",
            $"the fixture never drew a window (pid {launched.Pid})",
            () =>
            {
                var window = TopLevelWindows.Largest(launched.Pid);
                return window is not null && window.Title.Length > 0 ? window : null;
            });
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

        Waits.Until(
            "draw",
            $"the fixture drew nothing from a bare environment (pid {launched.Pid})",
            () => TopLevelWindows.Largest(launched.Pid));
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

        Waits.Until("gone", $"pid {pid} was still running after the register stopped everything", () => !Alive(pid));
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
        //
        // WW41: through the engine's own reading, which is where this attribute is now asked. A
        // second P/Invoke of the same attribute in the suite is two things that can disagree.
        var glass = Glass.Of(window.Handle);

        Assert.Equal(SystemBackdrop.Mica, glass.Backdrop);
        Assert.True(glass.Transmits, glass.Sentence());
        Assert.Contains("mica", glass.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_window_that_never_asked_is_told_apart_from_one_that_asked_for_nothing()
    {
        // The arm a one-sided check gets wrong. Auto is the compositor deciding; none is the
        // window having decided. A refusal that read them as the same would let one through.
        var never = Glass.Of(Launched().Handle);
        var asked = Glass.Of(Launched("--backdrop=none").Handle);

        Assert.Equal(SystemBackdrop.Auto, never.Backdrop);
        Assert.Equal(SystemBackdrop.None, asked.Backdrop);

        // And neither carries anything through, which is the half that decides a capture.
        Assert.False(never.Transmits, never.Sentence());
        Assert.False(asked.Transmits, asked.Sentence());
        Assert.Contains("never asked", never.Sentence(), StringComparison.Ordinal);
        Assert.Contains("asked for no backdrop", asked.Sentence(), StringComparison.Ordinal);
    }

    [Fact]
    public void Every_backdrop_the_catalogue_offers_is_one_the_compositor_takes()
    {
        // A name in the catalogue that the compositor refuses is a shape nobody can provoke, which
        // is the whole failure this block exists to stop.
        Assert.Equal(SystemBackdrop.Acrylic, Glass.Of(Launched("--backdrop=acrylic").Handle).Backdrop);
        Assert.Equal(SystemBackdrop.Tabbed, Glass.Of(Launched("--backdrop=tabbed").Handle).Backdrop);
    }

    [Fact]
    public void A_backdrop_the_fixture_does_not_have_is_refused_with_the_ones_it_does()
    {
        var (code, said) = Ran("--backdrop=frosted");

        Assert.Equal(2, code);
        Assert.Contains("it takes none or mica or acrylic or tabbed", said);
    }

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
        return Waits.Until(
            "draw",
            $"pid {pid} never owned {howMany} window(s)",
            () =>
            {
                var windows = TopLevelWindows.OfProcess(pid);
                return windows.Count >= howMany ? windows : null;
            });
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

        Waits.Until(
            "loaded",
            "the page never finished loading, so the shorter duration reached nothing",
            () => Ids(window).Contains("reportNote"));

        Assert.DoesNotContain("loadingNote", Ids(window));
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

    /// <summary>
    /// How long a state must last for this desk's reader to keep up with it, with room to spare.
    /// <para>
    /// WW171. A property of the reading and not a preference about the fixture, and one number
    /// because three cases here used to carry three — 200, 500 and 200 — of which exactly one had
    /// been measured. Reading the tree of another process through UI Automation is what costs; a
    /// guest run measured 251ms a look and a case asking for 200 went red about a fixture that was
    /// cycling exactly as asked, which teaches whoever reads it to distrust the guard that fired.
    /// </para>
    /// <para>
    /// Six hundred, which is the 200ms the old guard compared against times
    /// <see cref="LooksPerState" /> — so this asks no more of a desk than it used to and gets three
    /// looks a state instead of one. Five states at this length is a three-second cycle, against the
    /// eight seconds this suite declares for 'cycle'.
    /// </para>
    /// </summary>
    private const int ReadableMs = 600;

    /// <summary>
    /// How many looks a state has to get before a sampler may claim it observed a sequence. One is
    /// sampling at the edge of it: any jitter drops a member, and a count that lost members is how
    /// a missed sample becomes a confident number about the application.
    /// </summary>
    private const int LooksPerState = 3;

    [Fact]
    public void The_animation_says_how_many_states_it_has_so_nothing_has_to_be_told()
    {
        var window = Launched($"--animate={ReadableMs}");

        // WW159: sampled until the cycle has shown everything rather than for a fixed three
        // seconds. The count is still read off the window and never typed — an expectation typed
        // into a case is one that goes stale the day the animation gains a state.
        var (said, declared) = Cycled(window);

        Assert.True(declared > 1, $"an animation of {declared} state(s) is not one");
        Assert.Single(said.Select(one => one.Split(" of ")[1]).Distinct(StringComparer.Ordinal));
        Assert.Equal(declared, said.Select(Ordinal).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void The_states_arrive_in_the_order_they_were_declared_in_and_come_round_again()
    {
        // Not a hundred and fifty, measured: reading the tree of another process costs more than a
        // state stands for at that speed, so the sampler skipped one and the order read as broken
        // when it was the reading that could not keep up. A frame sequence cannot be checked faster
        // than it can be read, and that is a property of the check — which is why the number is
        // ReadableMs and not a fourth constant written here.
        var window = Launched($"--animate={ReadableMs}");
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
        //
        // WW171. The declared length is a property of the reading and not a preference about the
        // fixture, which is why it is written as one. This case asked for 200ms and its neighbour
        // for 500 — two numbers in one file disagreeing about what this harness can read, and only
        // one of them measured. A guest run read the window every 251ms and went red about a
        // fixture that was cycling exactly as asked.
        //
        // The desk this demands is the same desk as before — ReadableMs over LooksPerState is the 200
        // the old guard compared against — so nothing that passed this stops passing, and the
        // sequence underneath is now measured off three looks a state instead of one.
        const int every = ReadableMs;
        var window = Launched($"--animate={every}");

        var watched = Watched(window, TimeSpan.FromSeconds(5));
        var perLook = PerLook(watched);

        // WW162, and read before anything is concluded about the animation.
        Assert.True(
            perLook * LooksPerState < every,
            $"this run looked every {perLook:0}ms at a state that lasts {every}ms, which is fewer than "
                + $"{LooksPerState} looks each, so it could not have observed the sequence — that is the "
                + "reading being too slow and not the animation");

        var seen = ChangesIn(watched);
        Assert.True(seen.Count > 1, $"only {seen.Count} change(s) over {watched[^1].AtMs:0}ms of watching");

        // Measured between the first change and the last rather than across the whole watch: the
        // time before the first and after the last belongs to no state, and counting it in is what
        // made a five-second window read as a slower animation than it was.
        var over = seen[^1].AtMs - seen[0].AtMs;
        var perState = over / (seen.Count - 1);

        Assert.True(
            perState > every * 0.4 && perState < every * 3,
            $"{seen.Count} change(s) over {over:0}ms is {perState:0}ms each, against {every}, "
                + $"and this run looked every {perLook:0}ms");

        var declared = int.Parse(seen[0].Said.Split(" of ")[1], System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(declared > 1);
    }

    [Fact]
    public void Without_the_flag_nothing_is_animating()
    {
        var window = Launched();

        Assert.DoesNotContain("animationState", Ids(window));
    }

    /// <summary>
    /// Read what the animation is showing, over and over, for as long as this is given.
    /// <para>
    /// WW143 left this one alone, and said so rather than leaving the exemption to be re-argued.
    /// It is not a wait: nothing here is waiting for a condition, and there is no early answer to
    /// return on. It is a sampler, and the whole of what it measures is how many different things
    /// were showing over a fixed span — so the interval is the resolution of the measurement and
    /// converting it to a deadline would delete the observation.
    /// </para>
    /// </summary>
    /// <summary>
    /// What the animation state reads right now, or null where it does not read as one.
    /// <para>
    /// WW159. This used to be a walk of the whole tree to depth twelve, per sample, and reading
    /// another process's tree costs more than a state stands for at two hundred milliseconds — so a
    /// loaded machine sampled every second or third state and the case went red about an animation
    /// that was cycling exactly as asked. One locator resolved by automation id is a find and a
    /// property read, which is what a sampler is entitled to cost.
    /// </para>
    /// </summary>
    private static string? AnimationState(AutomationElement root)
    {
        var name = Resolve.Once(root, Locator.Parse("#animationState")).Facts?.Name;
        return name is not null && name.Contains(" of ", StringComparison.Ordinal) ? name : null;
    }

    /// <summary>One reading of the animation, and when this run took it.</summary>
    /// <param name="Said">What the window announced itself as.</param>
    /// <param name="AtMs">How far into the watch it was read, in milliseconds.</param>
    private sealed record Shown(string Said, double AtMs);

    /// <summary>
    /// Watch the animation, keeping when each reading was taken.
    /// <para>
    /// WW162. The times are the half that was missing. A check that divides elapsed time by the
    /// number of changes it saw cannot tell a slow animation from a slow reader — a skipped state
    /// does not read as a gap, it reads as a state that lasted twice as long.
    /// </para>
    /// </summary>
    private static IReadOnlyList<Shown> Watched(TopLevelWindow window, TimeSpan howLong)
    {
        var root = AutomationElement.FromHandle(window.Handle);
        var said = new List<Shown>();
        var clock = System.Diagnostics.Stopwatch.StartNew();

        while (clock.Elapsed < howLong)
        {
            if (AnimationState(root) is { } state)
                said.Add(new Shown(state, clock.Elapsed.TotalMilliseconds));

            Thread.Sleep(30);
        }

        Assert.NotEmpty(said);
        return said;
    }

    private static IReadOnlyList<string> Sampled(TopLevelWindow window, TimeSpan howLong) =>
        Watched(window, howLong).Select(one => one.Said).ToList();

    /// <summary>
    /// How often this run actually got a look, which is what decides whether it could have observed
    /// the sequence at all. A reader slower than a state has not measured the animation.
    /// </summary>
    private static double PerLook(IReadOnlyList<Shown> watched) =>
        watched.Count < 2 ? double.MaxValue : (watched[^1].AtMs - watched[0].AtMs) / (watched.Count - 1);

    /// <summary>The watch with the repeats removed, which is the sequence rather than the poll.</summary>
    private static IReadOnlyList<Shown> ChangesIn(IReadOnlyList<Shown> watched)
    {
        var changes = new List<Shown>();
        foreach (var one in watched)
        {
            if (changes.Count == 0 || !string.Equals(changes[^1].Said, one.Said, StringComparison.Ordinal))
                changes.Add(one);
        }

        return changes;
    }

    /// <summary>
    /// Every state the animation showed, sampled until it has shown all of them.
    /// <para>
    /// WW159. A fixed window is a race the reader loses on a busy machine, and lengthening it only
    /// moves which machine loses. This stops when the cycle has shown everything it declares, so
    /// the fast case costs one cycle and the slow one gets the looks it needs — and where the
    /// deadline wins it says which states were never seen, which is the difference between a red
    /// about the fixture and a red about the desk it ran on.
    /// </para>
    /// </summary>
    /// <param name="window">The fixture's window, drawn and animating.</param>
    private static (IReadOnlyList<string> Said, int Declared) Cycled(TopLevelWindow window)
    {
        var root = AutomationElement.FromHandle(window.Handle);
        var first = Waits.Until("draw", "the fixture never showed an animation state", () => AnimationState(root));
        var declared = int.Parse(first.Split(" of ")[1], System.Globalization.CultureInfo.InvariantCulture);

        var said = new List<string> { first };
        var seen = new HashSet<string>(StringComparer.Ordinal) { Ordinal(first) };

        var waited = Waits.Trying("cycle", () =>
        {
            if (AnimationState(root) is { } now)
            {
                said.Add(now);
                seen.Add(Ordinal(now));
            }

            return seen.Count >= declared;
        });

        var never = Enumerable.Range(1, declared)
            .Select(one => one.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Where(one => !seen.Contains(one))
            .ToList();

        Assert.True(
            never.Count == 0,
            $"the animation showed {seen.Count} of {declared} state(s) over {waited.Polls} look(s) in "
                + $"{waited.WaitedMs}ms, and never showed: {string.Join(", ", never)}");

        return (said, declared);
    }

    /// <summary>Which state a reading is, out of the "n of m" the window announces itself with.</summary>
    private static string Ordinal(string said) => said.Split(' ')[0];

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

        // WW143: the loop here waited for a window from the one flag that draws none, so it ran
        // its cap out every time - five seconds of sleep spelled as a condition that could not come
        // true. What the case actually needs is the process visible to the check it is about.
        Waits.Until(
            "readable",
            $"pid {resident} never showed up as an instance of the fixture at all",
            () => InstanceCheck.Of(Executable()).Resident.Any(one => one.Pid == resident));

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
    public void The_runs_own_reading_closes_the_fingerprint_it_opened_against_a_real_application()
    {
        // WW170, and against the fixture rather than against a temp directory: the criterion says a
        // run leaves the machine as it found it, so what has to close the reading is a run. Untouched
        // .Around already answered the coarse question and hands back a StoreChange the caller must
        // render itself; this is the store reaching the run's own reading, beside the desk and the
        // binary and the language, where every other thing this run measured is already reported.
        var store = Path.Combine(root, "read-back");
        Launched($"--store={store}");

        var declared = Path.Combine(root, "winwright.read-back.json");
        File.WriteAllText(
            declared,
            $$"""
            {
              "executable": {{System.Text.Json.JsonSerializer.Serialize(Executable())}},
              "sourceRoot": {{System.Text.Json.JsonSerializer.Serialize(root)}},
              "fingerprintStore": {{System.Text.Json.JsonSerializer.Serialize(store)}}
            }
            """);

        var closed = Preamble.Around(
            AppTarget.AttachTo(Environment.ProcessId),
            () => Launched($"--store={store}", "--mutate"),
            ProjectDeclaration.Load(declared));

        var finding = Assert.Single(closed.Findings, one => one.Named == StoreChange.Named);

        Assert.False(finding.Holds, finding.Sentence);
        Assert.Contains("settings.json", finding.Sentence, StringComparison.Ordinal);

        // And it is in the page a reader is handed, not only on the object.
        Assert.Contains(
            closed.Render(), one => one.StartsWith("  differs " + StoreChange.Named, StringComparison.Ordinal));
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

        Waits.Until("draw", $"pid {pid} drew nothing to be stopped", () => TopLevelWindows.OfProcess(pid).Count > 0);

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

        Assert.True(read.Examined > 10, read.Sentence());
        Assert.NotNull(read.Root);

        // WW130, against the window it was found on: every element this used to report as laid out
        // to no size was one the application had collapsed on purpose. They are left alone now, and
        // counted rather than dropped — a page hiding a note is not a page with a defect on it.
        Assert.DoesNotContain(read.Faults, one => one.Kind == Fault.MeasuresNothing);
        Assert.NotEmpty(read.Concealed);
        Assert.Contains("the application is not showing left alone", read.Sentence());

        // WW131, on the same window: the default tab template lifts a selected header four pixels
        // outside the panel holding it and two past the border containing it. Those are true
        // statements about what was drawn and no adopter can fix any of them, so they are the
        // framework's rather than nobody's.
        Assert.DoesNotContain(read.Faults, one => one.Kind is Fault.StartsOutside or Fault.EndsOutside);
        Assert.Contains(read.Chrome, one => one.Kind is Fault.StartsOutside or Fault.EndsOutside);
        Assert.Contains("left to the framework's own template", read.Sentence());
        Assert.Contains(read.WithChrome().Faults, one => one.Kind == Fault.EndsOutside);

        // What is left is one thing about elements the fixture itself declared: two tab items
        // overlapping by the four pixels a selected one is lifted. Overlap is the fault a case
        // narrows away where a window legitimately has one thing over another, which is what Only
        // is for — and it is reported rather than assumed away here.
        var overlap = Assert.Single(read.Faults);
        Assert.Equal(Fault.Overlaps, overlap.Kind);
        Assert.True(overlap.What.IsOwn && overlap.Against!.IsOwn, overlap.Detail);
        Assert.Empty(read.Only(Fault.StartsOutside, Fault.EndsOutside, Fault.MeasuresNothing).Faults);
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

        // WW164. Waited for what is about to be read and not for the names. Existence is the first
        // thing a write produces and the last thing that means anything: the file appears empty,
        // the wait comes back, and the reader gets a dump with nothing in it — which comes out as a
        // fault about the application, on a run where the application was fine. Measured on a
        // loaded guest, where the layout case read "there was no geometry to check" against a
        // fixture that had drawn its window and was still writing the file.
        //
        // The same repair WW145 made for the store: read through the write rather than around it.
        Waits.Until(
            "wrote",
            $"pid {launched.Pid} never wrote what it drew, or wrote it and it read as nothing",
            () => Readable(surfaces) && Drawn(geometry));

        return (surfaces, geometry);
    }

    [Fact]
    public void A_dump_that_exists_and_holds_nothing_is_not_one_this_run_may_read()
    {
        // WW164, and the confusion provoked directly rather than waited for. A file that exists and
        // is empty is the half of the write that happened to finish first, and it used to satisfy
        // the wait — so the reader got a dump with nothing in it and reported a fault about the
        // application on a run where the application was fine.
        var empty = Path.Combine(root, "empty-geometry.tsv");
        File.WriteAllText(empty, "");

        Assert.True(File.Exists(empty), "the empty dump was not written, so this proves nothing");
        Assert.False(Drawn(empty));
        Assert.False(Readable(Path.Combine(root, "never-surfaces.tsv")));

        // A line the writer had not finished is answered rather than thrown on: a reader meeting
        // one would otherwise turn the wait into the failure, which is the same misattribution one
        // step earlier.
        var half = Path.Combine(root, "half-geometry.tsv");
        File.WriteAllText(half, "0\tWindow\twinwright fixt");
        Assert.False(Drawn(half));

        // And the real ones read as written, so the guard is not simply refusing everything.
        var (surfaces, geometry) = Driven();

        Assert.True(Readable(surfaces), "the fixture's surface report read as empty");
        Assert.True(Drawn(geometry), "the fixture's geometry dump read as empty");
    }

    /// <summary>Whether the surface report holds a surface, rather than merely being there.</summary>
    private static bool Readable(string surfaces) => Written(surfaces, () => SurfaceReport.Read(surfaces).Count > 0);

    /// <summary>Whether the geometry dump holds a tree, rather than merely being there.</summary>
    private static bool Drawn(string geometry) =>
        Written(geometry, () =>
        {
            var read = GeometryDump.Read(geometry);
            return read.Root is not null && read.Elements.Count > 0;
        });

    /// <summary>
    /// Read a file the fixture is still writing, answering false rather than throwing. A reader
    /// that met a half-written line would otherwise turn the wait itself into the failure, which
    /// is the same misattribution one step earlier.
    /// </summary>
    private static bool Written(string path, Func<bool> holds)
    {
        if (!File.Exists(path))
            return false;

        try
        {
            return holds();
        }
        catch (Exception unfinished) when (unfinished is IOException or FormatException)
        {
            return false;
        }
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
    public void The_one_key_carrying_a_placeholder_is_left_out_and_said_out_loud()
    {
        // WW118, in all three languages the fixture ships. An exact-name read can never match it,
        // so it is no member of the expectation — and a rule that dropped it silently would be the
        // green about a control nobody could have checked, which is what the recording prevents.
        foreach (var culture in new[] { "en", "pt-BR", "de" })
        {
            var set = DerivedSet.From("the labels", Strings(culture), "labels");

            Assert.DoesNotContain(set.Expected, Labels.CarriesAPlaceholder);
            Assert.Equal("labels.profileName", Assert.Single(set.Templated).Key);
            Assert.True(Labels.CarriesAPlaceholder(set.Templated[0].Value));
            Assert.Contains("less 1 carrying a placeholder", set.Source);

            // WW139: only the English file carries the notes, which is the ordinary way a strings
            // file ends up — the comment is written once beside the key it explains and nobody
            // translates it. Both are left out, and the source says how many of each.
            var notes = culture == "en" ? 2 : 0;
            Assert.Equal(notes, set.Notes.Count);
            Assert.DoesNotContain(set.Expected, one => one.StartsWith("The pathological key", StringComparison.Ordinal));

            if (notes > 0)
                Assert.Contains("2 a note and not a string", set.Source);
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

    [Fact]
    public void The_region_check_names_the_intruder_rather_than_answering_whether()
    {
        // WW38, driven against the flag WW103 built for it. Nine sampled points cannot cover a
        // window: the capture taken to verify one task passed all nine while carrying two windows
        // of another process across its lower-right corner, so the question is asked about the area.
        var placed = Placed("--intrude=200,200,300,200");

        var read = Obstruction.Reading(placed.Window.Handle, placed.Window.Bounds);

        Assert.True(read.Was, read.Sentence());
        Assert.False(read.Clear, read.Sentence());
        Assert.Contains(read.Over, one => one.Window.Handle == placed.Intruder.Handle);

        // Named, which is the half a boolean loses: a reader handed a covered capture needs to
        // know which window to move, and "covered" sends them to a screenshot to find out.
        Assert.Contains($"pid {placed.Intruder.Pid}", read.Sentence(), StringComparison.Ordinal);
        Assert.Contains($"of its {placed.Window.Bounds.Area} pixel(s)", read.Sentence(), StringComparison.Ordinal);

        // A fact about the desk and never a defect in the code: the picture was not taken, so
        // nothing about the application was observed.
        Assert.Equal(
            Winwright.Verdicts.AssertionOutcome.Unchecked,
            read.AsAssertion("the window is not covered").Outcome);
    }

    [Fact]
    public void The_area_it_answers_with_is_the_area_the_intruder_actually_takes()
    {
        // The number, against the one the case works out for itself off two rectangles. A region
        // check that answered a different number from the arithmetic beside it would be a reading
        // nobody could check, which is what this whole task is about replacing.
        var placed = Placed("--intrude=200,200,300,200");

        var read = Obstruction.Reading(placed.Window.Handle, placed.Window.Bounds);
        var intruder = Assert.Single(read.Over, one => one.Window.Handle == placed.Intruder.Handle);

        Assert.Equal(Overlap(placed.Window, placed.Intruder), intruder.Overlap.Area);
        Assert.True(read.Covered >= intruder.Overlap.Area, read.Sentence());
        Assert.True(read.Covered <= placed.Window.Bounds.Area, $"{read.Covered} is more than the window has");
    }

    [Fact]
    public void An_intruder_in_the_way_of_nothing_takes_none_of_the_region()
    {
        // The case that must pass, and the one a check exercised by hand never gets to. This is
        // also the arm that fails loudly if the reading ever starts counting the desktop.
        var beside = Placed("--intrude=3000,2000,200,150");

        var read = Obstruction.Reading(beside.Window.Handle, beside.Window.Bounds);

        Assert.True(read.Was, read.Sentence());
        Assert.DoesNotContain(read.Over, one => one.Window.Handle == beside.Intruder.Handle);
    }

    [Fact]
    public void A_region_with_no_area_and_a_window_that_is_nothing_are_readings_not_taken()
    {
        // The two absences, told apart from a clear region. A reading answering "nothing stands
        // over it" to either would be claiming an emptiness it never looked for, which is the
        // green this project exists to withdraw.
        var placed = Placed("--intrude=3000,2000,200,150");

        var nowhere = Obstruction.Reading(placed.Window.Handle, new WindowBounds(10, 10, 10, 10));

        Assert.False(nowhere.Was);
        Assert.False(nowhere.Clear);
        Assert.Contains("no area", nowhere.Sentence(), StringComparison.Ordinal);

        var nothing = Obstruction.Reading(0, placed.Window.Bounds);

        Assert.False(nothing.Was);
        Assert.False(nothing.Clear);
        Assert.Contains("no window was named", nothing.Sentence(), StringComparison.Ordinal);

        Assert.Equal(
            Winwright.Verdicts.AssertionOutcome.Unchecked,
            nowhere.AsAssertion("the region is clear").Outcome);
    }

    [Fact]
    public void A_capture_whose_region_was_stood_over_is_refused_and_names_the_intruder()
    {
        // WW40. An overlap fails rather than crops. The copied rectangle is the painted frame, so
        // there is no invisible border left for a foreign window to hide in — an overlap is inside
        // real content, and a file trimmed to dodge one is a picture of something nobody asked for.
        var placed = Placed("--intrude=200,200,300,200");
        var read = Obstruction.Reading(placed.Window.Handle, placed.Window.Bounds);

        Assert.False(read.Clear, read.Sentence());

        var refused = Assert.Throws<WrongCaptureException>(
            () => CaptureReceipt.Of(
                Path.Combine(root, "covered.png"),
                placed.Window,
                AppTarget.AttachTo(placed.Window.Pid),
                over: read));

        // Actionable and not mysterious: "something else was in the way" sends a reader nowhere,
        // and a title with a pid and a rectangle sends them to the window to move.
        Assert.Contains($"pid {placed.Intruder.Pid}", refused.Message, StringComparison.Ordinal);
        Assert.Contains("stand over", refused.Message, StringComparison.Ordinal);
        Assert.Contains("pixel(s)", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_capture_whose_region_was_clear_carries_the_reading_that_says_so()
    {
        // The arm the refusal must not catch, and the reason the reading is carried rather than
        // consumed: a receipt that refused on an overlap and said nothing on a clear region would
        // leave a reader unable to tell a checked capture from an unchecked one.
        var beside = Placed("--intrude=3000,2000,200,150");
        var read = Obstruction.Reading(beside.Window.Handle, beside.Window.Bounds);

        if (!read.Clear)
            return;

        var receipt = CaptureReceipt.Of(
            Path.Combine(root, "clear.png"),
            beside.Window,
            AppTarget.AttachTo(beside.Window.Pid),
            over: read);

        Assert.NotNull(receipt.Over);
        Assert.True(receipt.Over.Clear);
    }

    [Fact]
    public void A_capture_nobody_read_the_region_for_says_nothing_rather_than_that_it_was_clear()
    {
        // Null is not "nothing stood over it". A caller that never looked and a caller that looked
        // and found nothing are two different facts, and a receipt answering both from one value
        // would be the green this project exists to withdraw.
        var placed = Placed("--intrude=200,200,300,200");

        var receipt = CaptureReceipt.Of(
            Path.Combine(root, "unread.png"), placed.Window, AppTarget.AttachTo(placed.Window.Pid));

        Assert.Null(receipt.Over);
    }

    [Fact]
    public void A_reading_that_was_never_taken_does_not_refuse_a_capture_either()
    {
        // The third state carried through. A region with no area is a reading not taken, and
        // refusing on it would report an intruder nobody saw.
        var placed = Placed("--intrude=200,200,300,200");
        var unread = Obstruction.Reading(placed.Window.Handle, new WindowBounds(10, 10, 10, 10));

        Assert.False(unread.Was);

        var receipt = CaptureReceipt.Of(
            Path.Combine(root, "not-read.png"),
            placed.Window,
            AppTarget.AttachTo(placed.Window.Pid),
            over: unread);

        Assert.NotNull(receipt.Over);
        Assert.False(receipt.Over.Was);
    }

    [Fact]
    public void A_capture_of_a_window_with_a_backdrop_is_refused_rather_than_warned_about()
    {
        // A warning is not a refusal and the file gets written either way, which is what the first
        // response to this got wrong. The refusal names the backdrop, so the reader knows the
        // picture is carrying the desktop rather than being merely suspect.
        var window = Launched("--backdrop=acrylic");
        var glass = Glass.Of(window.Handle);

        if (!glass.Transmits)
            return;

        var refused = Assert.Throws<WrongCaptureException>(
            () => CaptureReceipt.Of(
                Path.Combine(root, "glassy.png"),
                window,
                AppTarget.AttachTo(window.Pid),
                glass: glass));

        Assert.Contains("acrylic", refused.Message, StringComparison.Ordinal);
        Assert.Contains("through the glass", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_popup_is_not_refused_by_a_backdrop_because_it_is_what_the_copy_route_exists_for()
    {
        // The arm that must not be refused. A menu on this shell has acrylic by design, so a
        // refusal that fired on every backdrop would refuse every capture the copy route was built
        // to take — and the copy route is the only way to photograph a menu at all.
        var window = Launched("--backdrop=acrylic");
        var glass = Glass.Of(window.Handle);

        if (!glass.Transmits)
            return;

        // A menu window of the same process: its own top-level window, drawn by the system and in
        // nobody's visual tree, which is exactly why the copy route exists for it.
        var menu = window with { Handle = window.Handle + 1, ClassName = "#32768" };
        var route = CaptureRoute.For(menu, window);

        Assert.Equal(OutOfReach.Menu, route.Reach);

        var receipt = CaptureReceipt.Of(
            Path.Combine(root, "menu.png"),
            menu,
            AppTarget.AttachTo(window.Pid),
            route: route,
            glass: glass);

        // Carried and not refused: the reader is told the glass transmits, and the capture the
        // copy route exists to take is still taken.
        Assert.NotNull(receipt.Glass);
        Assert.True(receipt.Glass.Transmits);
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
        var read = Dumped(geometry);

        // The whole contrast the dump was built for: nothing above found these, and here they are.
        foreach (var name in new[] { "drawnSurface", "drawnHeader", "drawnBody", "drawnFooter" })
            Assert.NotEmpty(read.Named(name));
    }

    /// <summary>
    /// The dump the fixture wrote, waited for by what it says rather than by its file existing.
    /// <para>
    /// A file is created before it is written, so waiting on the name alone reads an empty one and
    /// answers with no elements — and the loop that did it gave up silently after five seconds,
    /// which turned a slow launch into an assertion about the fixture drawing nothing. Measured: it
    /// went red once in a full run and passed on either side of it.
    /// </para>
    /// </summary>
    private static ReadGeometry Dumped(string geometry)
    {
        var found = Attempt.Until(
            () =>
            {
                if (!File.Exists(geometry))
                    return null;

                var read = GeometryDump.Read(geometry);
                return read.Elements.Count > 0 ? read : null;
            },
            deadlineMs: 15000,
            pollMs: 25);

        Assert.True(found.Found, $"the fixture never wrote a readable dump to {geometry} within 15s");
        return found.Value!;
    }

    [Fact]
    public void What_the_dump_reports_of_it_is_laid_out_soundly()
    {
        var geometry = Path.Combine(root, "peerless-layout.tsv");
        var start = Started("--peerless");
        start.Environment[GeometryDump.PathVariable] = geometry;

        Attachable.Launch(register, start);
        var read = Dumped(geometry);
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

    [Fact]
    public void The_fixture_prints_its_catalogue_without_having_to_be_misspelt_at()
    {
        var start = Started("--flags");
        start.RedirectStandardOutput = true;
        start.UseShellExecute = false;

        using var running = Process.Start(start)!;
        var said = running.StandardOutput.ReadToEnd();
        Assert.True(running.WaitForExit(30_000), "the catalogue never finished printing");

        // An answer and not a refusal: exit zero, on the output stream, without a window.
        Assert.Equal(0, running.ExitCode);
        Assert.StartsWith("This fixture knows:", said);
        Assert.Contains("--flags", said);
    }

    [Fact]
    public void Every_flag_the_fixture_reads_is_one_the_catalogue_lists()
    {
        var (catalogue, read) = Catalogued();

        // A flag acted on and not listed is a shape nobody can find, which is the whole failure
        // this task exists over.
        var unlisted = read.Except(catalogue, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

        Assert.True(unlisted.Count == 0, $"read but not listed: {string.Join(", ", unlisted)}");
    }

    [Fact]
    public void Every_flag_the_catalogue_lists_is_one_the_fixture_reads()
    {
        var (catalogue, read) = Catalogued();

        // The other direction, and the one a catalogue kept by hand always breaks first: a row
        // nobody reads is a shape a run can ask for and never get.
        var dead = catalogue.Except(read, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

        Assert.True(dead.Count == 0, $"listed but never read: {string.Join(", ", dead)}");
    }

    [Fact]
    public void Every_row_in_the_catalogue_says_what_it_is_for()
    {
        var start = Started("--flags");
        start.RedirectStandardOutput = true;
        start.UseShellExecute = false;

        using var running = Process.Start(start)!;
        var said = running.StandardOutput.ReadToEnd();
        running.WaitForExit(30_000);

        // A name with no sentence beside it is a row that tells a reader nothing they could not
        // have guessed from the name.
        foreach (var line in Lines(said).Where(one => one.TrimStart().StartsWith("--", StringComparison.Ordinal)))
            Assert.True(line.Trim().Length > 40, $"'{line.Trim()}' says nothing about what it is for");
    }

    /// <summary>
    /// What the fixture lists against what it reads, both taken from the fixture itself: the
    /// catalogue from the running application, and the reads from its own sources.
    /// </summary>
    private static (IReadOnlySet<string> Catalogue, IReadOnlySet<string> Read) Catalogued()
    {
        var start = Started("--flags");
        start.RedirectStandardOutput = true;
        start.UseShellExecute = false;

        using var running = Process.Start(start)!;
        var said = running.StandardOutput.ReadToEnd();
        Assert.True(running.WaitForExit(30_000), "the catalogue never finished printing");

        var catalogue = System.Text.RegularExpressions.Regex
            .Matches(said, "^ *--([A-Za-z]+)", System.Text.RegularExpressions.RegexOptions.Multiline)
            .Select(one => one.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        var read = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(FixtureSources(), "*.cs"))
        {
            foreach (System.Text.RegularExpressions.Match one in System.Text.RegularExpressions.Regex
                .Matches(File.ReadAllText(file), "hapes\\.(?:Has|Value)\\(\"([A-Za-z]+)\""))
            {
                read.Add(one.Groups[1].Value);
            }
        }

        Assert.NotEmpty(catalogue);
        Assert.NotEmpty(read);
        return (catalogue, read);
    }

    /// <summary>The lines of a block of text, whatever the machine wrote its newlines as.</summary>
    private static IEnumerable<string> Lines(string said) =>
        said.Split('\n').Select(one => one.TrimEnd('\r'));

    [Fact]
    public void Every_shape_the_fixture_carries_names_the_defect_it_reproduces()
    {
        var said = Printed("--flags");

        // A fixture that grows shapes nobody can justify becomes a second product to maintain,
        // drifts from the things it stands in for, and starts producing false confidence.
        var flags = Lines(said).Count(one => one.TrimStart().StartsWith("--", StringComparison.Ordinal));
        var reasons = Lines(said).Where(one => one.TrimStart().StartsWith("because ", StringComparison.Ordinal)).ToList();

        Assert.True(flags > 10, $"only {flags} shape(s) were listed");
        Assert.Equal(flags, reasons.Count);
    }

    [Fact]
    public void No_reason_is_a_restatement_of_what_the_shape_does()
    {
        // The failure this guards is a justification that says the flag's own sentence again,
        // which is a shape nobody has actually justified. Whether a sentence names a real defect
        // is a judgement no test can make; whether it repeats the line above it is not.
        var lines = Lines(Printed("--flags")).Select(one => one.Trim()).ToList();

        for (var at = 1; at < lines.Count; at++)
        {
            if (!lines[at].StartsWith("because ", StringComparison.Ordinal))
                continue;

            var reason = lines[at]["because ".Length..];
            var does = lines[at - 1];

            Assert.True(reason.Length > 60, $"'{reason}' is too short to have said what happened");
            Assert.DoesNotContain(reason, does, StringComparison.Ordinal);
            Assert.True(Shared(reason, does) < 25, $"'{reason}' repeats its own description");
        }
    }

    /// <summary>The longest run of characters two sentences have in common.</summary>
    private static int Shared(string left, string right)
    {
        var longest = 0;
        for (var at = 0; at < left.Length; at++)
        {
            for (var length = longest + 1; at + length <= left.Length; length++)
            {
                if (!right.Contains(left.AsSpan(at, length), StringComparison.Ordinal))
                    break;

                longest = length;
            }
        }

        return longest;
    }

    [Fact]
    public void The_catalogue_says_what_a_run_of_this_fixture_exits_with()
    {
        // WW161. The rows said what each shape provokes, what it needs and whether it draws, and
        // nothing said what any of it exits with — so both codes were learnt by reading the host,
        // and the suite carried its own copy of the number.
        var said = Printed("--flags");

        Assert.Contains("It exits:", said, StringComparison.Ordinal);
        foreach (var code in new[] { "  0  ", "  2  ", "  3  " })
            Assert.Contains(code, said, StringComparison.Ordinal);

        // The third is the one an adopter has never met: a shape that did what it was asked and a
        // fixture that was driven wrong are different runs, and the numbers are where that is said.
        Assert.Contains("does not have", said, StringComparison.Ordinal);
        Assert.Contains("the refusal it exists to provoke", said, StringComparison.Ordinal);
    }

    [Fact]
    public void A_shape_that_ends_in_a_refusal_says_so_on_its_own_row()
    {
        // Read off the article rather than typed, which is the whole repair: the number lives in
        // one place and a case wanting it asks the fixture the way a person would.
        var said = Printed("--flags");
        var marked = Lines(said)
            .Select(one => one.Trim())
            .Where(one => one.StartsWith("--", StringComparison.Ordinal))
            .Where(one => one.Contains("[exits ", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(marked);
        Assert.All(marked, one => Assert.DoesNotContain("[exits 0]", one, StringComparison.Ordinal));

        // And the two that do are the two the suite drives for their refusal.
        Assert.Equal(3, Fixture.ExitFor("sizeless"));
        Assert.Equal(3, Fixture.ExitFor("unbacked"));

        // A shape that ends the ordinary way says nothing about it, so the marker means something
        // rather than being on every row. --blank writes its picture and exits clean: the refusal
        // it exists for belongs to whoever reads the picture back.
        var asked = Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Fixture.ExitFor("blank"));
        Assert.Contains("--blank says nothing about what it exits with", asked.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_codes_reach_somebody_who_was_just_refused_as_well()
    {
        // The person holding a 2 is exactly the person who needs to know it means the fixture was
        // driven wrong rather than that it did something.
        var refused = Ran("--nope").Said;

        Assert.Contains("It exits:", refused, StringComparison.Ordinal);
    }

    [Fact]
    public void A_refusal_stays_scannable_and_leaves_the_reasons_out()
    {
        // Two audiences: somebody who misspelt a flag wants the list, and somebody who asked for
        // the catalogue wants the whole story. Printing the story at a refusal buries the list.
        var refused = Ran("--nope").Said;

        Assert.Contains("This fixture knows:", refused);
        Assert.DoesNotContain("      because ", refused);
    }

    /// <summary>Run the fixture to completion and hand back what it wrote to the output stream.</summary>
    private static string Printed(params string[] flags)
    {
        var start = Started(flags);
        start.RedirectStandardOutput = true;
        start.UseShellExecute = false;

        using var running = Process.Start(start)!;
        var said = running.StandardOutput.ReadToEnd();
        Assert.True(running.WaitForExit(30_000), "the fixture did not finish printing");
        Assert.Equal(0, running.ExitCode);

        return said;
    }

    [Fact]
    public void Every_shape_that_draws_opens_a_window_somebody_can_look_at()
    {
        // Driven for every flag the catalogue lists, not for the ones somebody remembered. When a
        // case fails the fastest way to understand it is to look at the thing it is talking about,
        // and a shape that opens nothing costs somebody a minute finding that out.
        var listed = Lines(Printed("--flags"))
            .Select(one => one.Trim())
            .Where(one => one.StartsWith("--", StringComparison.Ordinal))
            .ToList();

        Assert.True(listed.Count > 10, $"only {listed.Count} shape(s) were listed");

        foreach (var row in listed)
        {
            var name = row[2..].Split(['=', ' '])[0];
            if (row.Contains("[draws nothing]", StringComparison.Ordinal))
                continue;

            // A shape that says it needs a companion gets one, read off the catalogue rather than
            // remembered here: launching it alone would be refused and read as drawing nothing.
            var needs = System.Text.RegularExpressions.Regex.Match(row, @"\[needs --([a-z]+)\]");
            var arguments = needs.Success
                ? new[] { $"--{needs.Groups[1].Value}{Value(needs.Groups[1].Value)}", $"--{name}{Value(name)}" }
                : [$"--{name}{Value(name)}"];

            using var register = new ProcessRegister();
            var pid = Attachable.Launch(register, Started(arguments)).Pid;

            // Carried rather than asserted here, because the process has to be stopped either way:
            // a failure that leaves the shape on the desk fails the next case as well.
            var drew = Waits.Trying("draw", () => TopLevelWindows.OfProcess(pid).Count > 0);

            Attachable.StopAndSettle(register);
            Assert.True(drew.Happened, Waits.Missed("draw", $"--{name} opened nothing a person could look at", drew));
        }
    }

    [Fact]
    public void The_shapes_that_show_nothing_say_so_where_a_person_reads_them()
    {
        var said = Printed("--flags");

        // Said out loud rather than left to be discovered by launching one and waiting. Named here
        // rather than derived from the catalogue's own marker: a list read off the thing it is
        // checking would agree with itself whatever the fixture did.
        foreach (var quiet in new[] { "--flags", "--render", "--resident", "--sizeless", "--blank", "--unbacked" })
        {
            var row = Assert.Single(
                Lines(said), one => one.TrimStart().StartsWith(quiet + " ", StringComparison.Ordinal)
                    || one.TrimStart().StartsWith(quiet + "=", StringComparison.Ordinal));

            Assert.Contains("[draws nothing]", row);
        }
    }

    [Fact]
    public void A_person_can_ask_for_the_window_to_come_forward()
    {
        // What is asserted here is that the window asks and is there to be looked at. Whether
        // Windows grants the foreground is not this fixture's to promise: a process that does not
        // already own it is refused, which is a policy measured rather than argued - this test
        // passed alone and failed after eighty others had run, on the same code.
        var window = Launched("--show");

        Assert.True(window.OnScreen, window.ToString());
        Assert.False(Minimised(window.Handle), $"{window} came up minimised, which nobody can look at");

        // And the default is still the one the suite needs: unactivated, so raising the fixture
        // thirty times a run does not decide the foreground for the checks that follow.
        var quiet = Launched();
        Assert.NotEqual(quiet.Handle, Foreground.Now().Window);
    }

    /// <summary>Whether that window is minimised, which is the one state nobody can look at.</summary>
    private static bool Minimised(nint window) => (GetWindowLongPtrW(window, -16) & 0x20000000) != 0;

    /// <summary>A value one shape needs, or nothing where it takes none.</summary>
    private string Value(string name) => name switch
    {
        "title" => "=by hand",
        "pump" => "=dispatcher",
        "backdrop" => "=mica",
        "toast" => "=beside",
        "loading" => "=300",
        "animate" => "=300",
        "language" => "=en",
        "store" => $"={Path.Combine(root, "by-hand")}",
        "intrude" => "=900,700,200,150",
        _ => "",
    };
}
