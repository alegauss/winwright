using System.Runtime.InteropServices;
using System.Windows.Automation;

using Winwright.Acting;
using Winwright.Locating;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW141. This block's criterion says every verb needing no cooperation runs against an application
/// that references nothing, which is what keeps this usable on a product nobody here owns. Hundreds
/// of cases do exactly that, so it held by accident of how the fixtures were written and no case
/// anywhere stated it â€” and a rule met by whoever remembers is met by nobody.
/// <para>
/// The window here is bare Win32: a frame and four controls made with CreateWindowExW, no
/// presentation stack, no package, nothing written down for a harness to find. It is the closest
/// this suite can get to somebody else's product, and the one caveat is stated below rather than
/// left for a reader to discover.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class NoCooperationTests : IDisposable
{
    private const uint WsPopup = 0x80000000;
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint BsAutoCheckBox = 0x0003;
    private const uint CbsDropDownList = 0x0003;
    private const uint CbAddString = 0x0143;

    private readonly List<nint> created = [];

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SendMessageW(nint window, uint message, nint wParam, string lParam);

    public void Dispose()
    {
        for (var index = created.Count - 1; index >= 0; index--)
            DestroyWindow(created[index]);
    }

    private nint Create(string className, string? title, uint style, int w, int h, nint parent = 0)
    {
        var window = CreateWindowExW(0, className, title, style, 20, 20, w, h, parent, 0, 0, 0);
        Assert.NotEqual(0, window);
        created.Add(window);
        return window;
    }

    /// <summary>
    /// An application that references nothing: bare Win32, no presentation stack, no package.
    /// <para>
    /// One caveat, stated rather than hidden. It runs inside this process, which does take the
    /// in-app half â€” so the process-wide display awareness the half declares is in force here and
    /// would not be in a product that never took it. That is not an API these verbs call; it is a
    /// condition the desk reading already measures and names, and it is the reason that condition
    /// exists rather than a gap in this case.
    /// </para>
    /// </summary>
    private nint Application()
    {
        var frame = Create("Static", "somebody else's window", WsPopup | WsVisible, 480, 320);
        Create("Button", "Wrap lines", WsChild | WsVisible | BsAutoCheckBox, 160, 30, frame);
        Create("Edit", "alpha", WsChild | WsVisible, 200, 24, frame);
        Create("Button", "Save", WsChild | WsVisible, 90, 28, frame);

        var combo = Create("ComboBox", null, WsChild | WsVisible | CbsDropDownList, 200, 200, frame);
        SendMessageW(combo, CbAddString, 0, "Alpha");
        SendMessageW(combo, CbAddString, 0, "Beta");
        return frame;
    }

    private static Subject On(nint frame, string locator) =>
        Subject.Unguarded(AutomationElement.FromHandle(frame), Locator.Parse(locator), 2000, pollMs: 20);

    [Fact]
    public void Every_verb_the_engine_offers_is_in_the_catalogue()
    {
        // The check the criterion was missing: a verb added later fails here until somebody says
        // what it needs from the application before it can answer.
        var offered = Cooperating.Named();
        var listed = Cooperating.Known.Select(one => one.Named).ToList();

        Assert.Empty(offered.Except(listed, StringComparer.Ordinal));
    }

    [Fact]
    public void Nothing_is_catalogued_that_the_engine_no_longer_offers()
    {
        var offered = Cooperating.Named();

        Assert.Empty(Cooperating.Known.Select(one => one.Named).Except(offered, StringComparer.Ordinal));
    }

    [Fact]
    public void No_verb_is_catalogued_twice()
    {
        var listed = Cooperating.Known.Select(one => one.Named).ToList();

        Assert.Equal(listed.Count, listed.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void The_scope_is_derived_and_the_namespaces_it_leaves_out_are_measured()
    {
        // WW209. The scope was two namespaces typed into this file, and the case above claims every
        // verb the engine offers â€” of ten namespaces and about a hundred and fifty public statics
        // outside those two. It now reaches anything anywhere that touches the application, which
        // added eighteen verbs nobody had ever been asked about.
        var driving = Cooperating.Driving();

        Assert.Contains("TopLevelWindows", driving, StringComparer.Ordinal);
        Assert.Contains("AppTarget", driving, StringComparer.Ordinal);
        Assert.Contains("Obstruction", driving, StringComparer.Ordinal);

        // And the four that compose rather than drive contribute nothing â€” measured on every run
        // rather than promised once. A verdict assembled from results reaches no application, and
        // the day one does, it arrives here rather than staying outside the question.
        var composing = Checkout.SourcesIn(Checkout.Engine)
            .Where(one => Composing.Any(where =>
                one.Contains($"{Path.DirectorySeparatorChar}{where}{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .ToList();

        Assert.NotEmpty(composing);

        var reaching = composing.Where(one => driving.Contains(one, StringComparer.Ordinal)).ToList();

        Assert.True(
            reaching.Count == 0,
            $"{reaching.Count} file(s) in a namespace this catalogue does not sweep now reach the "
                + $"application: {string.Join(", ", reaching)}");
    }

    /// <summary>
    /// The namespaces that compose values rather than drive anything.
    /// <para>
    /// WW57 took <c>Scenarios</c> off this list, and the measurement above is what noticed. The
    /// declarations in it still compose — a step, a case, a vocabulary, a format — but the engine
    /// that runs one resolves locators under a root element, which is reaching the application by
    /// any reading. So it is swept as a driving namespace and <c>CaseRun.Of</c> is classified in the
    /// catalogue like every other verb, which is what this measurement exists to force.
    /// </para>
    /// </summary>
    private static readonly string[] Composing = ["Verdicts", "Tracing", "Projects"];

    [Fact]
    public void No_reading_or_pattern_act_needs_the_in_app_half()
    {
        // True by construction and worth stating: the engine assembly carries no reference to the
        // in-app half at all, so no verb here can call it. What a verb could need is an artefact
        // the application wrote down first, and none of these reads one.
        Assert.Empty(Cooperating.NeedingTheHalf());
        Assert.Contains("0 need the in-app half", Cooperating.Render()[0]);
    }

    [Fact]
    public void The_readings_answer_against_an_application_that_references_nothing()
    {
        var frame = Application();
        var root = AutomationElement.FromHandle(frame);

        Assert.NotNull(ElementFacts.Of(root));
        Assert.NotNull(Inspect.Window(frame));
        Assert.NotEmpty(Inspect.Render(Inspect.Under(root)!));
        Assert.True(Resolve.Once(root, Locator.Parse("Edit")).Found);
        Assert.True(Resolve.Until(root, Locator.Parse("""Button[name="Save"]"""), 2000, 20).Found);
        Assert.NotEmpty(Resolve.Matching(root, Locator.Parse("Button").Steps[0]));
        Assert.NotNull(Resolve.Beneath(root, Locator.Parse("Button")));
        Assert.NotEmpty(Preflight.Offers(root, Locator.Parse("""Button[name="Save"]"""))!);
    }

    [Fact]
    public void The_pattern_acts_land_against_an_application_that_references_nothing()
    {
        var frame = Application();

        // Every one of these asks the control through its own accessibility peer. A bare Win32
        // control has those because Windows gives them to it, not because anybody cooperated.
        Assert.Equal("On", Act.Toggle(On(frame, """CheckBox[name="Wrap lines"]""")).After.Toggle);
        Assert.Equal("beta", Act.SetValue(On(frame, "Edit"), "beta").After.Value);
        Assert.Equal("Expanded", Act.Expand(On(frame, "ComboBox")).After.ExpandCollapse);
        Assert.Equal("Collapsed", Act.Collapse(On(frame, "ComboBox")).After.ExpandCollapse);
        Assert.Equal("invoke", Act.Invoke(On(frame, "ComboBox > Button#DropDown")).Verb);
        Assert.Equal(["Alpha", "Beta"], Pick.Values(On(frame, "ComboBox")));
    }

    [Fact]
    public void The_judgement_and_the_door_answer_there_too()
    {
        var frame = Application();
        var save = On(frame, """Button[name="Save"]""");

        Assert.True(ActionabilityCheck.Of(save.ReadOnce().Facts, "Invoke").CanAct);
        Assert.Equal("Save", Admitted.To(save, "Invoke").Facts.Name);
        Assert.Equal("Save", Admitted.Of(save, save.Read(), "Invoke").Facts.Name);
        Assert.NotEqual(0, Admitted.To(save).Window);
    }

    [Fact]
    public void Every_verb_a_bare_window_is_enough_for_is_driven_by_a_case_here()
    {
        // The coverage claim, said as arithmetic rather than as a promise. What is driven above is
        // the subset a single window can take; the rest of the no-cooperation verbs need a tray, a
        // menu bar or a foreground, and each of those is somebody else's fixture.
        var here = new[]
        {
            "ElementFacts.Of", "Inspect.Window", "Inspect.Under", "Inspect.Render", "Resolve.Once",
            "Resolve.Until", "Resolve.Matching", "Preflight.Offers", "Act.Toggle", "Act.SetValue",
            "Act.Expand", "Act.Collapse", "Act.Invoke", "Pick.Values", "ActionabilityCheck.Of",
            "Admitted.To", "Admitted.Of", "Subject.Unguarded", "Locator.Parse",
        };

        Assert.All(here, one => Assert.Contains(Cooperating.Known, verb => verb.Named == one));
        Assert.All(
            here,
            one => Assert.True(
                Cooperating.Known.Single(verb => verb.Named == one).RunsAgainstAnything,
                $"{one} is catalogued as needing more than a bare window, and is driven against one here"));
    }

    [Fact]
    public void The_catalogue_reads_as_counts_and_then_a_line_each()
    {
        var rendered = Cooperating.Render();

        Assert.Equal(Cooperating.Known.Count + 1, rendered.Count);
        Assert.Contains("run against any application", rendered[0]);
        Assert.Contains("also need a desk", rendered[0]);
        Assert.All(rendered.Skip(1), one => Assert.StartsWith("  ", one));
    }
}

