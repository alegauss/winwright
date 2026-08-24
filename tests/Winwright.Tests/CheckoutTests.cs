using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW193. The walk every source-reading case shares. What is asserted here is the part that went
/// wrong once: <c>Deadlines</c> shipped recursing into <c>bin</c> and <c>obj</c>, and the guest went
/// red twice on timing before anybody looked at the enumeration.
/// </summary>
public sealed class CheckoutTests
{
    [Fact]
    public void The_root_is_the_directory_holding_the_solution()
    {
        Assert.True(Directory.Exists(Checkout.Root), Checkout.Root);
        Assert.True(File.Exists(Path.Combine(Checkout.Root, Checkout.Marker)), Checkout.Root);
        Assert.True(Directory.Exists(Checkout.Engine));
        Assert.True(Directory.Exists(Checkout.Suite));
    }

    [Fact]
    public void A_path_inside_it_is_joined_the_way_this_platform_spells_one()
    {
        var roadmap = Checkout.At("docs", "ROADMAP.md");

        Assert.True(File.Exists(roadmap), roadmap);
        Assert.StartsWith(Checkout.Root, roadmap, StringComparison.Ordinal);
        Assert.Equal(Checkout.Root, Checkout.At());
    }

    [Fact]
    public void The_walk_finds_the_sources_and_leaves_out_what_a_build_wrote()
    {
        var sources = Checkout.Sources(Checkout.Everything).ToList();

        Assert.NotEmpty(sources);
        Assert.All(sources, one => Assert.EndsWith(".cs", one, StringComparison.Ordinal));
        Assert.DoesNotContain(sources, one => !Checkout.Written(one));
        Assert.Contains(sources, one => Path.GetFileName(one) == $"{nameof(Checkout)}.cs");
    }

    [Fact]
    public void The_exclusion_is_doing_something_rather_than_being_believed()
    {
        // The measurement WW193 exists for, and the only way to state it: walk the same trees
        // without the rule and count what it kept out. A machine that has built has thousands of
        // copies under bin and obj, and this is the check that fails the day the filter stops
        // matching — which is what happened, silently, and cost two guest runs to find.
        var everything = Checkout.Everything
            .SelectMany(one => Directory.EnumerateFiles(one, "*.cs", SearchOption.AllDirectories))
            .ToList();

        var written = Checkout.Sources(Checkout.Everything).ToList();

        Assert.True(
            everything.Count > written.Count,
            $"the walk kept {written.Count} of {everything.Count} files, so nothing was excluded — "
                + "either this tree has never been built, or the exclusion has stopped matching");

        Assert.All(everything.Except(written, StringComparer.Ordinal), one => Assert.False(Checkout.Written(one)));
    }

    [Fact]
    public void A_directory_that_merely_starts_with_bin_is_not_build_output()
    {
        // Matched on the separators either side. "binding" and "objects" are ordinary names, and a
        // filter that dropped them would take real sources out of every catalogue at once.
        var separator = Path.DirectorySeparatorChar;

        Assert.False(Checkout.Written($"C:{separator}src{separator}bin{separator}Thing.cs"));
        Assert.False(Checkout.Written($"C:{separator}src{separator}obj{separator}Thing.cs"));
        Assert.True(Checkout.Written($"C:{separator}src{separator}binding{separator}Thing.cs"));
        Assert.True(Checkout.Written($"C:{separator}src{separator}objects{separator}Thing.cs"));
        Assert.True(Checkout.Written($"C:{separator}src{separator}Thing.cs"));
    }

    [Fact]
    public void A_catalogue_can_leave_its_own_file_out()
    {
        // Every catalogue built on this spells the thing it searches for, so scanning itself finds
        // the naming and reports it as a use. Passed in rather than guessed at, because which file
        // that is belongs to the catalogue and not to the walk.
        var without = Checkout.Sources(Checkout.Everything, except: $"{nameof(Checkout)}.cs").ToList();

        Assert.DoesNotContain(without, one => Path.GetFileName(one) == $"{nameof(Checkout)}.cs");
        Assert.Contains(without, one => Path.GetFileName(one) == $"{nameof(CheckoutTests)}.cs");
    }

    [Fact]
    public void A_line_is_read_as_code_and_never_as_what_somebody_wrote_about_it()
    {
        // WW191 and WW197, in one place because two scanners kept finding the same thing: a call in
        // a string and a call in a comment are both a call being talked about rather than made.
        Assert.Equal("var found = ;", Checkout.Code("""var found = "NotificationArea.Tray(";""").Trim());
        Assert.Equal("", Checkout.Code("        // NotificationArea.Tray() is the taskbar"));
        Assert.Equal("", Checkout.Code("    /// <summary>Reads NotificationArea.Tray().</summary>"));
        Assert.Equal("var tray = NotificationArea.Tray();", Checkout.Code("        var tray = NotificationArea.Tray(); // the shell's").Trim());

        // And a line whose quotes do not pair is left whole, because deleting real code would turn
        // a call that was made into one that appears not to be — which no sweep would report.
        Assert.Equal(
            """var text = "NotificationArea.Tray(""",
            Checkout.Code("""        var text = "NotificationArea.Tray(""").Trim());
    }

    [Fact]
    public void The_member_a_line_declares_is_found_even_where_it_returns_a_tuple()
    {
        // WW207, and this is the difference the four copies had split over. Two of them took the
        // first bracket on the line, which for a member returning a tuple is the tuple — so the
        // member had no name and was invisible to the sweep pointed straight at it.
        Assert.Equal("Driven", Checkout.Member("    private (string Surfaces, string Geometry) Driven()"));
        Assert.Equal("Ran", Checkout.Member("    private static string Ran(string store, string file)"));
        Assert.Equal("Settling", Checkout.Member("    internal static Settling Settling() => new();"));
        Assert.Equal("Dispose", Checkout.Member("    public void Dispose()"));

        // And nothing that is not a member at that indentation.
        Assert.Null(Checkout.Member("        NotificationArea.CloseOverflow();"));
        Assert.Null(Checkout.Member("    private readonly string root = Temp();"));
        Assert.Null(Checkout.Member("public sealed class DeskAsks"));
    }

    [Fact]
    public void The_type_a_line_declares_is_a_declaration_and_never_a_mention()
    {
        Assert.Equal("DeskAsks", Checkout.Owner("public sealed class DeskAsks"));
        Assert.Equal("Settling", Checkout.Owner("internal sealed class Settling : IDisposable"));
        Assert.Null(Checkout.Owner("    /// The taskbar's own window class."));
        Assert.Null(Checkout.Owner("        var named = one.ClassName;"));
    }

    [Fact]
    public void The_root_is_walked_once_and_not_once_per_case()
    {
        // Cached, which is the third thing the copies each did for themselves. Same instance both
        // times: a walk repeated per case is disk work in a suite full of deadlines.
        Assert.Same(Checkout.Root, Checkout.Root);
    }

    [Fact]
    public void Nothing_walks_up_to_the_solution_on_its_own_any_more()
    {
        // The claim this task makes, asserted the way this repository asserts everything: read out
        // of the sources. Eighteen copies across sixteen files, spelled with three different
        // variable names, and the next one is what this exists to stop.
        var naming = Checkout
            .Sources(Checkout.Everything, except: $"{nameof(Checkout)}.cs")
            .Where(one => Path.GetFileName(one) != $"{nameof(CheckoutTests)}.cs")
            // WW206. Read as code, which the finer unit caught: this looks for the solution's name,
            // and every file that walks up to it explains why in a comment. Spoken rather than Code,
            // because the name it looks for is a string and the stricter reading would delete it.
            .Where(one => File.ReadLines(one).Select(Checkout.Spoken)
                .Any(line => line.Contains(Checkout.Marker, StringComparison.Ordinal)))
            .Select(Path.GetFileName)
            .ToList();

        Assert.True(
            naming.Count == 0,
            $"{naming.Count} file(s) walk up to the solution themselves rather than asking Checkout: "
                + string.Join(", ", naming));
    }
}
