using System.Collections.ObjectModel;

using Xunit;

namespace Winwright.Tests;

/// <summary>Why nothing demonstrates a criterion, where nothing does.</summary>
internal enum Unshown
{
    /// <summary>The capability it is about is not built. Not a gap in the suite — a gap in the tool.</summary>
    NotBuilt,

    /// <summary>The capability is built and nothing reads the claim back. This one is a debt.</summary>
    NotYet,
}

/// <summary>
/// One criterion that decides whether a block is finished, and what demonstrates it.
/// </summary>
/// <param name="Block">The block it binds, as the roadmap labels it.</param>
/// <param name="Lead">The criterion's lead, exactly as the roadmap spells it.</param>
/// <param name="Shown">The case that demonstrates it, as <c>TypeTests.Method_name</c>. Empty where none does.</param>
/// <param name="Why">Why none does, where none does. Null where a case is named.</param>
/// <param name="Because">What the named case actually establishes, or what is missing.</param>
internal sealed record Criterion(string Block, string Lead, string Shown, Unshown? Why = null, string Because = "")
{
    /// <summary>Whether anything in this suite reads the claim back.</summary>
    public bool Demonstrated => Shown.Length > 0;

    public override string ToString() => Demonstrated
        ? $"{Block} {Lead,-58} {Because} [{Shown}]"
        : $"{Block} {Lead,-58} (nothing shows it, {Phrase(Why!.Value)}): {Because}";

    private static string Phrase(Unshown why) => why switch
    {
        Unshown.NotBuilt => "not built",
        Unshown.NotYet => "not yet",
        _ => why.ToString(),
    };
}

/// <summary>
/// WW176. Every criterion the roadmap declares, paired with the case that demonstrates it.
/// <para>
/// This project decides a block is finished by reading its criteria. roadkeep asserts the list
/// exists and not that anything satisfies it — presence, not enforcement, and it says so — so the
/// reading was a judgement with nothing underneath it. Thirty-three criteria rested on whoever last
/// looked.
/// </para>
/// <para>
/// WW169 is what that cost. It was filed with a full design section arguing that the fixture's
/// justification field was read back by nobody; WW106 had shipped both read-backs two weeks earlier,
/// and its own ledger line said so. Running the cases before building anything was what settled it.
/// Since then the same reading has found a real gap in three blocks running — WW178 in K, WW184 in
/// C, WW177 in A — which is the argument for doing it with a list rather than from memory.
/// </para>
/// <para>
/// The shape is the fourth of its kind here, after <see cref="Provocation" />,
/// <see cref="Cooperating" /> and <see cref="Rendered" />: checked against the source of truth in
/// both directions, with an honest bucket for what nothing shows, counted rather than left off. The
/// bucket is large and most of it is <see cref="Unshown.NotBuilt" /> — a criterion about scenario
/// files cannot be demonstrated before scenario files exist, and saying so is not the same admission
/// as a capability that shipped with nobody reading it back.
/// </para>
/// </summary>
internal static class Criteria
{
    /// <summary>The heading a block's criteria live under, as the roadmap writes it.</summary>
    internal const string Heading = "## Done when — ";

    internal static IReadOnlyList<Criterion> Known { get; } = new ReadOnlyCollection<Criterion>(
    [
        // --- A, the verdict -------------------------------------------------------------------
        new("A", "A degraded run is legible without reading the log",
            "VerdictSummaryTests.Each_assertion_that_did_not_run_is_named_with_the_precondition_that_was_absent",
            null, "the summary names every hole and the precondition behind it, rather than counting them, "
                + "and since WW177 the reading that measured those preconditions is on the same page"),
        new("A", "Nothing about this machine is typed into a scenario",
            "FixtureDeclarationTests.An_environment_that_reaches_the_launch_nowhere_is_refused",
            null, "a scenario file names an environment and never a desk: what a case is launched with is "
                + "declared, refused where it would reach the launch nowhere, and the same field is what "
                + "the expectations are read against"),
        new("A", "A failure is diagnosed from the record and not from a re-run",
            "TraceTests.A_step_carries_the_locator_what_it_resolved_to_and_what_was_read_back",
            null, "a step carries the locator, what it resolved to, what was read back and its verdict"),

        // --- B, attach and leave nothing behind -------------------------------------------------
        new("B", "No process outlives the run that started it",
            "ProcessRegisterTests.What_outlived_the_case_is_named_rather_than_cleaned_up_in_silence",
            null, "the register stops what it launched and the summary names whatever would not stop"),
        new("B", "A run says which binary it drove",
            "RunningBinaryTests.Both_keys_agreeing_is_the_binary_the_run_named",
            null, "the version and the write time are both read, and a difference in either is reported"),
        new("B", "Nothing about the desk is reported as a defect in the code",
            "DeskGateTests.The_refusal_is_degraded_rather_than_failed",
            null, "a desk that cannot observe ends the run as degraded, and BusyDesk carries the same "
                + "rule through every desk-dependent case in this suite"),

        // --- C, locate ---------------------------------------------------------------------------
        new("C", "An element is addressed without reading the markup", "", Unshown.NotBuilt,
            "the claim is about migrated scenarios and there are none; Inspect is what they would be "
                + "written from, and InspectTests reads that back"),
        new("C", "No scenario carries a sleep", "SleepTests.A_sleep_that_is_still_a_wait_is_counted_rather_than_argued_away",
            null, "every file that calls it is paired with why it is not a scenario waiting, checked "
                + "against the sources both ways — and the one that is still a wait is counted"),
        new("C", "An act never runs against an element that cannot take it",
            "ActionabilityTests.All_four_holding_is_the_only_reading_that_lets_an_act_run",
            null, "present, on screen, enabled and carrying the pattern, with the refusal naming which failed"),

        // --- D, act -------------------------------------------------------------------------------
        new("D", "The default act needs no foreground",
            "NoCooperationTests.No_reading_or_pattern_act_needs_the_in_app_half",
            null, "the verb catalogue is checked against the engine both ways and says which need the desk"),
        new("D", "A retry is bounded and said out loud",
            "RetryTests.There_is_no_form_of_this_without_a_cap",
            null, "every spelling of the retry takes a cap, and the attempt count reaches the trace"),
        new("D", "A destructive entry is never invoked by accident",
            "DestructiveEntryTests.Invoke_refuses_a_declared_entry_and_the_refusal_names_it",
            null, "a declared entry is refused by name, and the one beside it still presses"),

        // --- E, capture ----------------------------------------------------------------------------
        new("E", "A capture proves what it photographed",
            "CaptureReceiptTests.The_line_names_the_window_the_process_and_the_arguments_behind_it",
            null, "the receipt names the window, the process and what it was launched with"),
        new("E", "An off-screen render is the default",
            "CaptureRouteTests.The_application_window_is_rendered_and_not_photographed",
            null, "the render is what runs, and the receipt says which of the two produced the file"),
        new("E", "Every arm of a capture refusal has something that provokes it",
            "CaptureArmTests.Every_arm_the_engine_declares_is_paired_with_something",
            null, "WW199: at the arm, and 'something' rather than 'a fixture' — the check under the "
                + "old wording asserted that named flags exist, never that every refusal names one, "
                + "and three of the six name a defect no proving ground can be"),

        // --- F, assert ------------------------------------------------------------------------------
        new("F", "No expectation is typed twice",
            "DerivedSetTests.Both_directions_are_reported_in_one_sentence",
            null, "the set is derived from the project's own strings and compared both ways"),
        new("F", "A red step carries its diagnosis",
            "DiagnosedRedTests.A_red_carries_the_control_view_it_failed_to_read",
            null, "a failing expectation carries the control view, marked at the element it was reading"),
        new("F", "A run leaves the machine as it found it",
            "FixtureTests.The_runs_own_reading_closes_the_fingerprint_it_opened_against_a_real_application",
            null, "the run's own reading closes the fingerprint it opened, against a launched application"),

        // --- G, the scenario ---------------------------------------------------------------------------
        new("G", "A case is data", "", Unshown.NotBuilt,
            "the format exists, a case declared in it runs end to end, and a launch is declared rather "
                + "than written — but the criterion is about the adopting project's line count, and "
                + "nothing is migrated yet"),
        new("G", "The format refuses before the prose exists",
            "ScenarioFileTests.A_key_the_format_does_not_have_is_refused_with_the_keys_it_does",
            null, "a key the format does not have is refused at its own address in the file, with the keys "
                + "there are, before the rest of it is read"),
        new("G", "One case runs alone",
            "SuiteRunTests.A_filtered_run_qualifies_its_pass_before_it_states_it",
            null, "a case runs by name and the run's own sentence names what it did not run, which is the "
                + "half the criterion is actually about — an unqualified pass over a filtered run"),

        // --- H, the Claude Code surface -----------------------------------------------------------------
        new("H", "The plugin is the whole installation",
            "PluginTests.Everything_the_plugin_carries_is_declared_by_it_and_points_at_this_tree",
            null, "every surface the plugin carries — the server, the hook, the skill, the version it "
                + "declares — is declared by the manifest and points at something this tree produces, "
                + "each paired with the case reading its wiring back; the criterion's own sentence also "
                + "names commands, and there are none"),
        new("H", "An answer costs no context",
            "McpTests.The_format_and_the_vocabulary_are_answers_rather_than_prose_somebody_loads",
            null, "the format and the vocabulary are tool answers rather than prose a turn loads, and the "
                + "one tool that takes a case carries the loader's own schema, so the key a caller cannot "
                + "spell is one they cannot send"),
        new("H", "The skill fits its budget",
            "SkillTests.The_skill_fits_its_budget_on_both_of_the_costs_it_has",
            null, "both costs are measured apart — the description, which is in context on every turn "
                + "whether a window is in play or not, and the body, which is paid only when one is"),

        // --- I, the in-app half -------------------------------------------------------------------------
        new("I", "The in-app half is one package reference", "", Unshown.NotBuilt,
            "no project has adopted it, so there is no deletion to report; SeparationTests holds the "
                + "half of this that can be checked, which is that the reference is one and separate"),
        new("I", "A project that cannot take the package still works",
            "NoCooperationTests.The_readings_answer_against_an_application_that_references_nothing",
            null, "every verb needing no cooperation is driven against an application referencing nothing"),

        // --- J, adoption ---------------------------------------------------------------------------------
        new("J", "The proof is a deletion", "", Unshown.NotBuilt, "no project has adopted it yet"),
        new("J", "Nothing was lost in the move", "", Unshown.NotBuilt, "there has been no move"),
        new("J", "The migrated suite is not slower than what it replaced", "", Unshown.NotBuilt,
            "there is nothing migrated to time against"),

        // --- K, the proving ground -----------------------------------------------------------------------
        new("K", "Every refusal has something that provokes it",
            "RefusalArmTests.Every_arm_the_engine_declares_is_provoked_by_something",
            null, "WW196: at the arm and not the type, which is the unit a reader meets — a type "
                + "thrown thirteen times was one entry and this criterion counted it as covered. Two "
                + "sweeps run beside this one: the type-level pairing, which catches a type with no "
                + "entry at all, and FixtureArmTests, which asks the same question of the proving "
                + "ground's own refusal by running it (WW200)"),
        new("K", "The fixture needs nothing from the machine",
            "FixtureNeedsTests.The_fixture_asks_the_machine_for_nothing_the_criterion_says_it_does_not",
            null, "its sources name no network, account, second display or well-known folder, and its "
                + "project declares one reference and no package — WW178, the first debt this list found"),
        new("K", "A shape exists because a defect existed",
            "FixtureTests.No_reason_is_a_restatement_of_what_the_shape_does",
            null, "every justification clears a length bar and is not a restatement of the row above it"),

        // --- WW158, a criterion bound to one task -----------------------------------------------------------
        new("WW158", "Proven on a desk that draws nothing, not on a mock", "", Unshown.NotYet,
            "the condition is asserted against an ordinary desk by DeskGateTests, and the desk that "
                + "reports everything present and renders nothing has never been driven here"),
    ]);

    /// <summary>
    /// Every criterion the roadmap declares, read out of the governed file rather than out of the
    /// list above. The roadmap is the source of truth and roadkeep is its writer; this only reads.
    /// </summary>
    internal static IReadOnlyList<(string Block, string Lead)> Declared()
    {
        var declared = new List<(string, string)>();
        var block = "";

        foreach (var raw in File.ReadAllLines(Roadmap()))
        {
            var line = raw.TrimEnd();
            if (line.StartsWith(Heading, StringComparison.Ordinal))
            {
                block = line[Heading.Length..].Trim().Replace("Block ", "", StringComparison.Ordinal);
                continue;
            }

            // Any other heading ends the region, so a bullet under Non-goals is never read as one.
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                block = "";
                continue;
            }

            if (block.Length == 0 || !line.StartsWith("- **", StringComparison.Ordinal))
                continue;

            var shut = line.IndexOf("**", 4, StringComparison.Ordinal);
            if (shut > 4)
                declared.Add((block, line[4..shut].Trim()));
        }

        return declared;
    }

    /// <summary>The ones nothing in this suite reads back, counted rather than left off.</summary>
    internal static IReadOnlyList<Criterion> Unproven() =>
        new ReadOnlyCollection<Criterion>(Known.Where(one => !one.Demonstrated).ToList());

    /// <summary>The reading a person gets: the counts first, then a line each.</summary>
    internal static IReadOnlyList<string> Render()
    {
        var unbuilt = Known.Count(one => one.Why == Unshown.NotBuilt);
        var owed = Known.Count(one => one.Why == Unshown.NotYet);
        return new ReadOnlyCollection<string>(
        [
            $"{Known.Count} criterion(s): {Known.Count - unbuilt - owed} shown by a case, "
                + $"{unbuilt} not built yet, {owed} built and not read back",
            .. Known.Select(one => $"  {one}"),
        ]);
    }

    /// <summary>The governed file, found from the solution beside it.</summary>
    private static string Roadmap() => Checkout.At("docs", "ROADMAP.md");
}
