using System.Text.Json;

using Winwright.Asserting;
using Winwright.Projects;
using Winwright.Tracing;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW64. An answer an agent cannot audit gets audited by reading the file the command already
/// read, which is the cost the verb existed to remove.
/// <para>
/// The line is the whole point: a strings file with four hundred keys names the file for every one
/// of them, and a reader still has to open it and search.
/// </para>
/// </summary>
public sealed class ProvenanceTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-provenance-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private string Write(string name, string json)
    {
        var path = Path.Combine(root, name);
        File.WriteAllText(path, json);
        return path;
    }

    private string Nested() => Write(
        "strings.en.json",
        """
        {
          "tabs": {
            "panes": "Panes",
            "status": "Status",
            "config": "Config",
            "logs": "Logs"
          },
          "buttons": { "close": "Close" }
        }
        """);

    [Fact]
    public void A_key_is_answered_with_the_line_it_is_declared_on()
    {
        var file = Nested();

        Assert.Equal(2, JsonSource.LineOf(file, "tabs"));
        Assert.Equal(3, JsonSource.LineOf(file, "tabs.panes"));
        Assert.Equal(6, JsonSource.LineOf(file, "tabs.logs"));
        Assert.Equal(8, JsonSource.LineOf(file, "buttons"));
    }

    [Fact]
    public void A_flat_file_spelling_its_keys_with_dots_is_read_the_way_it_spells_them()
    {
        var file = Write(
            "flat.json",
            """
            {
              "tabs.panes": "Panes",
              "tabs.logs": "Logs"
            }
            """);

        Assert.Equal(3, JsonSource.LineOf(file, "tabs.logs"));
    }

    [Fact]
    public void Every_key_is_answered_in_one_pass_over_the_file()
    {
        var file = Nested();

        var lines = JsonSource.LinesOf(file, ["tabs.panes", "tabs.logs", "buttons.close"]);

        Assert.Equal(3, lines["tabs.panes"]);
        Assert.Equal(6, lines["tabs.logs"]);
        Assert.Equal(8, lines["buttons.close"]);
    }

    [Fact]
    public void A_key_the_file_does_not_declare_is_left_out_rather_than_reported_as_line_zero()
    {
        var lines = JsonSource.LinesOf(Nested(), ["tabs.logs", "tabs.extras"]);

        Assert.True(lines.ContainsKey("tabs.logs"));
        Assert.False(lines.ContainsKey("tabs.extras"));
        Assert.Equal(0, JsonSource.LineOf(Nested(), "tabs.extras"));
    }

    [Fact]
    public void A_file_that_is_not_there_answers_nothing_rather_than_throwing()
    {
        Assert.Equal(0, JsonSource.LineOf(Path.Combine(root, "absent.json"), "tabs"));
    }

    [Fact]
    public void A_file_that_stops_parsing_before_the_key_answers_nothing()
    {
        // The message about a file that does not parse belongs to whoever refuses on it —
        // DerivedSet raises the parser's own — and a line number invented from a half-read token
        // stream would be worse than no answer.
        var broken = Write("broken.json", "{ \"first\" \"no colon\", \"tabs\": { \"logs\": \"Logs\" } }");

        Assert.Equal(0, JsonSource.LineOf(broken, "tabs.logs"));
    }

    [Fact]
    public void What_was_read_before_a_file_gave_out_still_stands()
    {
        var broken = Write("half.json", "{\n  \"tabs\": {\n    \"logs\": \"Logs\",\n    \"panes\" ");

        Assert.Equal(3, JsonSource.LineOf(broken, "tabs.logs"));
    }

    [Fact]
    public void Crlf_line_endings_are_counted_the_same_as_lf()
    {
        var file = Write("crlf.json", "{\r\n  \"tabs\": {\r\n    \"logs\": \"Logs\"\r\n  }\r\n}\r\n");

        Assert.Equal(3, JsonSource.LineOf(file, "tabs.logs"));
    }

    [Fact]
    public void A_derived_set_carries_the_line_behind_every_value_it_expects()
    {
        var set = DerivedSet.From("the tab headers", Nested(), "tabs");

        Assert.Equal(2, set.Origin.Line);
        Assert.Equal("tabs", set.Origin.Key);
        Assert.Equal(4, set.Origins.Count);
        Assert.Equal(6, set.Whence("Logs").Line);
        Assert.Equal("strings.en.json:6 'tabs.logs'", set.Whence("Logs").ToString());
    }

    [Fact]
    public void A_value_the_set_never_held_has_no_provenance_to_offer()
    {
        var set = DerivedSet.From("the tab headers", Nested(), "tabs");

        Assert.False(set.Whence("Extras").Known);
        Assert.Equal("(source unrecorded)", set.Whence("Extras").ToString());
    }

    [Fact]
    public void A_missing_header_is_reported_with_the_line_that_declares_it()
    {
        var compared = DerivedSet.From("the tab headers", Nested(), "tabs")
            .Against(["Panes", "Status", "Config"]);

        Assert.False(compared.Held);

        // The whole payoff: the red says where to look, so nobody opens the strings file to
        // find out whether 'Logs' was ever declared or the expectation invented it.
        Assert.Contains("'Logs' (strings.en.json:6 'tabs.logs') is declared and was not read", compared.Sentence());
    }

    [Fact]
    public void A_value_read_off_the_window_names_the_element_and_the_pattern()
    {
        var from = Provenance.OnElement("""Button[@id="save"]""", "Invoke");

        Assert.True(from.Known);
        Assert.Equal("""Button[@id="save"] via Invoke""", from.ToString());
        Assert.Null(from.File);
    }

    [Fact]
    public void A_provenance_survives_the_trace_it_is_written_into()
    {
        var step = new TraceStep
        {
            Verb = "assert",
            Locator = "TabItem",
            Verdict = StepVerdict.Failed,
            From = Provenance.InFile(Nested(), 6, "tabs.logs"),
        };

        var read = TraceFormat.Parse(TraceFormat.Line(step));

        Assert.NotNull(read.From);
        Assert.Equal(6, read.From.Line);
        Assert.Equal("tabs.logs", read.From.Key);
        Assert.Equal(step.From.File, read.From.File);
    }

    [Fact]
    public void A_step_that_compared_against_nothing_writes_no_provenance_at_all()
    {
        var step = new TraceStep { Verb = "click", Locator = "Button", Verdict = StepVerdict.Ok };

        var line = TraceFormat.Line(step);

        Assert.DoesNotContain("from", line, StringComparison.Ordinal);
        Assert.Null(TraceFormat.Parse(line).From);
    }

    [Fact]
    public void The_machine_readable_form_leaves_out_what_it_does_not_know()
    {
        var json = Provenance.OnElement("Button", "Invoke").Json();

        using var read = JsonDocument.Parse(json);
        Assert.False(read.RootElement.TryGetProperty("file", out _));
        Assert.Equal("Button", read.RootElement.GetProperty("element").GetString());
        Assert.Equal("Invoke", read.RootElement.GetProperty("pattern").GetString());
    }

    [Fact]
    public void An_unknown_provenance_says_so_rather_than_reading_as_a_file_nobody_named()
    {
        Assert.False(Provenance.Unknown.Known);
        Assert.Null(Provenance.Unknown.File);
        Assert.Null(Provenance.Unknown.Element);
        Assert.Equal("(source unrecorded)", Provenance.Unknown.ToString());
    }
}
