using System.Collections.ObjectModel;
using System.Reflection;

namespace Winwright.Tests;

/// <summary>Why a rendering is not read back by any case, where none does.</summary>
internal enum Unread
{
    /// <summary>Nothing asserts the text yet. Not a decision — a debt, and this is where it is counted.</summary>
    NotYet,
}

/// <summary>
/// One reader-facing rendering, paired with the case that asserts what a reader gets.
/// </summary>
/// <param name="Named">The rendering, as <c>Type.Method</c>.</param>
/// <param name="Case">The case asserting its text, as <c>TypeTests.Method_name</c>. Empty where none does.</param>
/// <param name="Why">Why none does, where none does. Null where a case is named.</param>
/// <param name="Because">What a reader gets out of it, in the words the pairing is read in.</param>
internal sealed record Rendering(string Named, string Case, Unread? Why = null, string Because = "")
{
    /// <summary>Whether a case reads this back. The count this whole catalogue exists to make arithmetic.</summary>
    public bool ReadBack => Case.Length > 0;

    public override string ToString() => ReadBack
        ? $"{Named,-30} {Because} [{Case}]"
        : $"{Named,-30} (nothing reads it back, {Phrase(Why!.Value)}): {Because}";

    private static string Phrase(Unread why) => why switch
    {
        Unread.NotYet => "not yet",
        _ => why.ToString(),
    };
}

/// <summary>
/// WW167. Every rendering the engine answers, paired with the case that asserts what a reader gets.
/// <para>
/// Two of these shipped unasserted and both were found by accident: WW149 found a roll call whose
/// every word was unchecked, and WW153 an agreement report printing a version no file in the tree
/// held. The obvious way of asking was worthless — every type carrying a rendering is referenced by
/// at least one test file, and Agreement was referenced by two while its report was asserted by
/// none. So the pairing is by rendering rather than by type, and it is checked in both directions
/// against the engine assembly: a rendering added later starts unpaired and the suite says so.
/// </para>
/// <para>
/// This is the shape WW132 used for the refusals and WW145 for the flags that drive them. What is
/// new is <see cref="Unread" />: a rendering nothing reads back is counted here rather than left
/// off, because a catalogue that only lists the checked ones is the promise it replaced.
/// </para>
/// </summary>
internal static class Rendered
{
    /// <summary>The engine, anchored on a type in it rather than by name.</summary>
    public static Assembly Engine { get; } = typeof(Winwright.Locating.Subject).Assembly;

    /// <summary>What a rendering is called. Both are reader-facing; nothing else in the engine is.</summary>
    public static IReadOnlyList<string> Spellings { get; } =
        new ReadOnlyCollection<string>(["Render", "Sentence"]);

    public static IReadOnlyList<Rendering> Known { get; } = new ReadOnlyCollection<Rendering>(
    [
        // --- lines a reader is handed -----------------------------------------------------------
        new("Agreement.Render", "AgreementTests.The_build_a_copy_was_made_from_is_moved_rather_than_thrown_away",
            null, "a line per copy of the engine, with the build each was made from"),
        new("Desk.Render", "DeskTests.The_report_carries_one_line_per_condition",
            null, "a line per condition this desk was read against"),
        new("Diagnosis.Render", "DiagnosisTests.The_diagnosis_opens_with_the_line_the_summary_would_have_printed_alone",
            null, "the failure's own line, then the control view under it"),
        new("Glass.Sentence", "FixtureTests.A_window_that_never_asked_is_told_apart_from_one_that_asked_for_nothing",
            null, "which backdrop a window opted into, and whether its glass carries what is behind it"),
        new("Inspect.Render", "InspectTests.A_walk_that_stops_at_the_depth_says_how_many_it_did_not_walk",
            null, "the control tree as lines, with what the walk did not reach counted"),
        new("Preamble.Render", "PreambleTests.The_preamble_renders_one_line_per_measurement_with_the_reading_first",
            null, "a line per measurement the run took before any assertion"),
        new("ReadGeometry.Render", "GeometryDumpTests.The_tree_renders_indented_by_the_depth_it_carried",
            null, "the dumped tree, indented by the depth each element carried"),
        new("SweepSummary.Render", "SweepTests.A_clean_sweep_is_zero",
            null, "the headline a sweep across environments exits on"),
        new("VerdictSummary.Render", "VerdictSummaryTests.A_clean_run_has_nothing_under_the_headline",
            null, "the headline a run exits on, and the failures under it"),

        // --- one sentence -------------------------------------------------------------------------
        new("ActionabilityCheck.Sentence", "ActionabilityTests.Everything_wrong_is_kept_and_the_first_leads",
            null, "why an element cannot take an act, leading with the first thing wrong"),
        new("Agreement.Sentence", "AgreementTests.Copies_naming_one_version_agree_and_the_gate_exits_zero",
            null, "whether every copy of the engine agrees on a version"),
        new("AppTarget.Sentence", "AppTargetTests.A_launch_knows_what_it_passed",
            null, "what the run is driving, declared by the target rather than by its caller"),
        new("AttachedTarget.Sentence", "AppTargetTests.An_attach_says_which_binary_it_reached",
            null, "which binary an attach reached, and that its arguments cannot be known"),
        new("CaptureReceipt.Sentence", "CaptureReceiptTests.A_launched_run_prints_what_it_passed",
            null, "what a picture photographed, and under what the run was driving"),
        new("CaptureRoute.Sentence", "CaptureRouteTests.A_receipt_that_did_route_says_which_way_and_why",
            null, "which way a capture went and why, or that it did not route"),
        new("ColourCheck.Sentence", "ColoursTests.A_picture_of_exactly_one_colour_is_read_as_one_colour",
            null, "how many distinct colours a picture carries, and whether it is one flat rectangle"),
        new("Containment.Sentence", "ContainmentTests.A_surface_the_copy_clipped_says_which_sides_and_by_how_much",
            null, "whether a surface sat inside the copy, and which sides clipped it"),
        new("Desk.Sentence", "DeskTests.The_sentence_never_says_the_desk_is_fine_while_anything_is_absent",
            null, "whether this desk can be observed at all, naming what is absent"),
        new("Destructive.Sentence", "DestructiveEntryTests.A_project_that_names_none_refuses_nothing_and_says_that",
            null, "what a project declared destructive, or that it declared none"),
        new("Expectation.Sentence", "ExpectationTests.A_value_that_arrives_late_passes_and_says_how_long_it_took",
            null, "what was expected, what was read, and how long it took to arrive"),
        new("Falsification.Sentence", "FalsifiableTests.A_check_that_stays_green_with_the_defect_in_place_is_the_finding",
            null, "whether a check was watched go red on every defect declared for it"),
        new("FocusReading.Sentence", "FocusScopeTests.The_sentence_says_which_way_the_reading_went_and_never_only_that_it_went",
            null, "what holds the focus and whether it is this application's, or why it is not"),
        new("Foreground.Sentence", "ForegroundTests.A_window_that_cannot_hold_the_foreground_reads_as_not_holding_it",
            null, "what holds the foreground, and whether this run's window could take it"),
        new("ForeignInput.Sentence", "ForeignInputTests.The_sentence_and_the_flag_never_disagree",
            null, "whether input this run did not synthesise arrived during it"),
        new("FrameSequence.Sentence", "FrameRunTests.The_run_that_fell_behind_says_so_instead_of_reporting_a_count",
            null, "whether the capture kept up with the frames, rather than a count that implies it did"),
        new("InstanceCheck.Sentence", "InstanceCheckTests.Nothing_running_at_all_says_so_rather_than_saying_nothing",
            null, "what else is running the application, and whether this run owns what it drives"),
        new("Label.Sentence", "LabelTests.A_declared_fallback_is_used_and_the_label_says_it_fell_back",
            null, "which declared string a check was made against, and whether it fell back"),
        new("LaunchedTarget.Sentence", "AppTargetTests.A_launch_that_passed_nothing_says_so_rather_than_printing_an_empty_tail",
            null, "what a launch started and what it passed, or that it passed nothing"),
        new("LayoutReading.Sentence", "LayoutTests.A_dump_with_nothing_in_it_is_unchecked_and_never_a_pass",
            null, "whether a page laid out as declared, or was never read at all"),
        new("LoadingCheck.Sentence", "LoadingTests.A_page_still_computing_says_so_rather_than_being_photographed",
            null, "whether a page is still showing one of the strings its project declares as loading text"),
        new("LocatorMiss.Sentence", "LocatorMissTests.A_step_under_something_shut_that_is_nowhere_else_names_the_route",
            null, "why a locator matched nothing, and the route that would reach it"),
        new("NameCheck.Sentence", "NameCheckTests.A_template_nobody_filled_in_is_reported_as_one",
            null, "what a control is called, with a glyph or an unfilled template said plainly"),
        new("Obstruction.Sentence", "FixtureTests.The_region_check_names_the_intruder_rather_than_answering_whether",
            null, "which windows stand over the region a capture is about, and how much of it they take"),
        new("PaintedFrame.Sentence", "PaintedFrameTests.A_window_with_no_border_trims_nothing_and_says_so",
            null, "what a capture trimmed off the window rectangle, and on which sides"),
        new("PictureCheck.Sentence", "PicturesTests.A_render_that_drew_nothing_is_a_blank",
            null, "whether a picture carries ink, is blank, or could not be checked"),
        new("Preamble.Sentence", "PreambleTests.A_run_with_no_store_declared_says_it_took_no_fingerprint_rather_than_nothing",
            null, "what the run read about the machine, counting what it could not read"),
        new("Preflighted.Sentence", "PreflightTests.The_sentence_never_says_every_act_was_checked_while_one_was_not",
            null, "which declared acts the tree can take, never claiming an unchecked one"),
        new("ProcessSummary.Sentence", "ProcessRegisterTests.The_summary_counts_what_stopped_apart_from_what_would_not",
            null, "what the run had to stop, apart from what would not stop"),
        new("ReadGeometry.Sentence", "GeometryDumpTests.A_walk_that_ran_out_says_how_much_it_did_not_reach",
            null, "what a dump held, and how much of the tree the walk did not reach"),
        new("ReasonsChecked.Sentence", "JoinedReasonsTests.A_reason_the_tree_agrees_with_prints_as_agreeing",
            null, "whether the tree agrees with each declared reason an act needs a pointer"),
        new("ResolvedLanguage.Sentence", "ResolvedLanguageTests.A_preference_file_that_is_not_there_says_so_rather_than_saying_nothing",
            null, "which language the run resolved, and what it read to get there"),
        new("Restorable.Sentence", "SurfaceTests.A_toggle_that_comes_round_the_long_way_says_how_many_presses_it_took",
            null, "what a case moved and what putting it back cost"),
        new("RunningBinary.Sentence", "RunningBinaryTests.A_pid_nothing_is_running_as_is_reported_rather_than_thrown",
            null, "which binary a pid is running, or that nothing is running as it"),
        new("SetComparison.Sentence", "DerivedSetTests.Both_directions_are_reported_in_one_sentence",
            null, "what the window shows the strings do not declare, and the other way round"),
        new("Staleness.Sentence", "StalenessTests.The_run_says_which_binary_it_drove_whatever_the_reading",
            null, "which binary the run drove, and whether it is older than the source"),
        new("StoreChange.Sentence", "StoreFingerprintTests.A_file_the_case_created_is_reported_as_created",
            null, "what the run left changed in the store it was told to protect"),
        new("SurfaceReading.Sentence", "SurfaceReportTests.A_surface_nobody_reported_is_a_hole_and_never_a_failure",
            null, "which surfaces the application reported, and which are holes"),
        new("SweepSummary.Sentence", "CoverageTests.A_sweep_earns_the_word_only_where_every_environment_did",
            null, "what a sweep concluded across environments, and what it could not check"),
        new("TimedOutRead.Sentence", "TimedOutReadTests.The_blank_one_is_pointed_at_the_locator_and_never_at_the_deadline",
            null, "why a read ran out, pointed at the locator or at the deadline"),
        new("TraySearch.Sentence", "NotificationAreaTests.The_search_says_what_it_found_and_where_rather_than_only_whether",
            null, "which icon answered a name, or why none did and how far the looking got"),
        new("VerdictSummary.Sentence", "CoverageTests.A_run_where_everything_ran_and_passed_earns_the_word",
            null, "what the run concluded, and what it never got to check"),
    ]);

    /// <summary>
    /// Every rendering the engine answers, read off the assembly rather than off this list.
    /// <para>
    /// Declared only, so an override is its own entry: <c>AppTarget.Sentence</c> and the two that
    /// override it are three renderings a reader can be handed, and pairing the base alone would
    /// leave two texts unchecked behind one entry that looked covered.
    /// </para>
    /// </summary>
    public static IReadOnlyList<string> Named() => new ReadOnlyCollection<string>(
        Engine
            .GetExportedTypes()
            .SelectMany(one => one
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName && Spellings.Contains(method.Name))
                .Select(method => $"{one.Name}.{method.Name}"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(one => one, StringComparer.Ordinal)
            .ToList());

    /// <summary>The ones nothing reads back, counted rather than left off.</summary>
    public static IReadOnlyList<Rendering> Unpaired() =>
        new ReadOnlyCollection<Rendering>(Known.Where(one => !one.ReadBack).ToList());

    /// <summary>
    /// The catalogue as a reader gets it: the counts first, because the arithmetic is the point, and
    /// then a line each. This rendering is itself in the catalogue's own shape, and asserted like the
    /// rest of them.
    /// </summary>
    public static IReadOnlyList<string> Render()
    {
        var unpaired = Unpaired().Count;
        return new ReadOnlyCollection<string>(
        [
            $"{Known.Count} rendering(s): {Known.Count - unpaired} read back by a case, {unpaired} not yet",
            .. Known.Select(one => $"  {one}"),
        ]);
    }
}
