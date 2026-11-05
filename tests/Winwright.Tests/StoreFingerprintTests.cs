using Winwright.Asserting;
using Winwright.Verdicts;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW53. A run mutates the store of the user who launched it.
/// <para>
/// The test that decides the design is the one about a rewrite of the same length. A picker
/// repointed from one profile to another of the same name changes a settings file without
/// changing its size, and on a tool that preserves the write time it changes neither — so a
/// fingerprint built on size and date would call the machine untouched while the user's real
/// profile now points somewhere else.
/// </para>
/// </summary>
public sealed class StoreFingerprintTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("winwright-store-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private string Settings(string content = """{ "profile": "alpha" }""")
    {
        var path = Path.Combine(root, "settings.json");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void A_rewrite_of_the_same_length_and_the_same_date_is_still_caught()
    {
        var file = Settings();
        var written = File.GetLastWriteTimeUtc(file);

        var change = Untouched.Around([file], () =>
        {
            // Same length to the byte, and the write time put back the way a careful tool does.
            File.WriteAllText(file, """{ "profile": "bravo" }""");
            File.SetLastWriteTimeUtc(file, written);
        });

        // The premise, measured rather than assumed: after the case the file is the same length
        // and carries the same write time, so a fingerprint built on either would see nothing at
        // all here and report a machine nobody had touched.
        Assert.Equal("""{ "profile": "alpha" }""".Length, new FileInfo(file).Length);
        Assert.Equal(written, File.GetLastWriteTimeUtc(file));

        Assert.False(change.Untouched);
        Assert.Equal([file], change.Changed);
        Assert.Contains("was rewritten", change.Sentence());
        Assert.Contains("settings.json", change.Sentence());
    }

    [Fact]
    public void A_case_that_only_reads_leaves_the_machine_as_it_found_it()
    {
        var file = Settings();

        var change = Untouched.Around([file], () => File.ReadAllText(file));

        Assert.True(change.Untouched);
        Assert.Equal(0, change.Moved);
        Assert.Equal("the run left the machine as it found it.", change.Sentence());
    }

    [Fact]
    public void A_file_the_case_created_is_reported_as_created()
    {
        var made = Path.Combine(root, "made.json");

        var change = Untouched.Around([root], () => File.WriteAllText(made, "{}"));

        Assert.Equal([made], change.Appeared);
        Assert.Contains("was created", change.Sentence());
    }

    [Fact]
    public void A_file_the_case_deleted_is_reported_as_removed()
    {
        var file = Settings();

        var change = Untouched.Around([file], () => File.Delete(file));

        Assert.Equal([file], change.Gone);
        Assert.Contains("was removed", change.Sentence());
    }

    [Fact]
    public void A_whole_directory_is_watched_to_its_leaves()
    {
        var deep = Path.Combine(root, "profiles", "alpha");
        Directory.CreateDirectory(deep);
        var buried = Path.Combine(deep, "state.json");
        File.WriteAllText(buried, "{}");

        var change = Untouched.Around([root], () => File.WriteAllText(buried, """{"a":1}"""));

        Assert.Equal([buried], change.Changed);
    }

    [Fact]
    public void An_environment_variable_the_case_set_is_caught_too()
    {
        var name = $"WINWRIGHT_TEST_{Guid.NewGuid():N}";

        try
        {
            var change = Untouched.Around([], [name], () => Environment.SetEnvironmentVariable(name, "on"));

            Assert.Equal([$"%{name}%"], change.Appeared);
            Assert.Contains("was created", change.Sentence());
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    [Fact]
    public void Insisting_refuses_and_names_what_moved()
    {
        var file = Settings();

        var refused = Assert.Throws<StoreTouchedException>(
            () => Untouched.Insist([file], () => File.WriteAllText(file, "changed")));

        Assert.Contains("changed the machine of whoever ran it", refused.Message);
        Assert.Contains("settings.json", refused.Message);
    }

    [Fact]
    public void A_case_that_throws_keeps_its_own_exception_and_reports_no_tidiness()
    {
        var file = Settings();

        // A comparison living in a disposer would replace this with a complaint about the file
        // it dirtied on the way down, and the failure that actually matters would be gone.
        var thrown = Assert.Throws<InvalidOperationException>(
            () => Untouched.Insist([file], () =>
            {
                File.WriteAllText(file, "half done");
                throw new InvalidOperationException("the act itself failed");
            }));

        Assert.Equal("the act itself failed", thrown.Message);
        Assert.IsNotType<StoreTouchedException>(thrown);
    }

    [Fact]
    public void Watching_nothing_is_refused_rather_than_met_by_any_machine_at_all()
    {
        // A fingerprint of nothing equals a fingerprint of nothing, so a run watching nothing
        // would report that it left the machine alone. That green is the whole defect.
        var refused = Assert.Throws<ArgumentException>(() => StoreFingerprint.Of());

        Assert.Contains("met by any machine at all", refused.Message);
        Assert.Throws<ArgumentException>(() => Untouched.Around([], () => { }));
    }

    [Fact]
    public void A_path_that_is_not_there_is_absent_rather_than_a_refusal()
    {
        var never = Path.Combine(root, "never.json");

        var before = StoreFingerprint.Of(never);

        Assert.Equal(1, before.Count);
        Assert.Null(before.Entries[never]);

        // And its arrival is a change, which is exactly what a run creating somebody's settings
        // file for them looks like.
        File.WriteAllText(never, "{}");
        Assert.Equal([never], before.Against(StoreFingerprint.Of(never)).Appeared);
    }

    [Fact]
    public void The_result_a_verdict_counts_carries_the_same_sentence()
    {
        var file = Settings();

        var moved = Untouched.Around([file], () => File.WriteAllText(file, "x"));
        var dirty = moved.AsAssertion();
        var clean = Untouched.Around([file], () => { }).AsAssertion();

        Assert.Equal(AssertionOutcome.Failed, dirty.Outcome);
        Assert.Contains("settings.json", dirty.Detail);
        Assert.Equal(AssertionOutcome.Passed, clean.Outcome);
        Assert.Equal(StoreChange.Named, clean.Name);

        // WW163: and so does the step a trace records, so a reader holding the verdict can reach
        // the file rather than re-running to find out which one it was.
        var step = moved.AsTraceStep();

        Assert.Equal(Winwright.Tracing.StepVerdict.Failed, step.Verdict);
        Assert.Equal("fingerprint", step.Verb);
        Assert.Contains("settings.json", step.Detail);
        Assert.Contains("\"verb\":\"fingerprint\"", Winwright.Tracing.TraceFormat.Line(step));
    }

    [Fact]
    public void A_reading_can_be_taken_again_of_exactly_what_it_read()
    {
        // WW151. Every caller spelling both readings out is one edit away from comparing a reading
        // of one list against a reading of another, and what comes out of that is files appearing
        // and going — a report of a run dirtying a machine it never touched.
        var file = Settings();
        var before = StoreFingerprint.Of([file], ["PATH"]);

        var again = before.Again();

        Assert.Equal(before.Watched, again.Watched);
        Assert.True(before.Against(again).Untouched, before.Against(again).Sentence());

        // And it is a reading and not a copy: what the file says now is what it reports now.
        File.WriteAllText(file, "{ \"profile\": \"gamma\" }");
        Assert.Equal([file], before.Against(before.Again()).Changed);
    }

    [Fact]
    public void An_environment_variable_survives_being_read_again_as_a_variable()
    {
        // The one the merged list could not have carried: %PATH% read back as a path would be a
        // file nobody has, so the second reading would call it gone.
        var before = StoreFingerprint.Of([], ["PATH"]);

        Assert.True(before.Against(before.Again()).Untouched);
        Assert.Equal(["%PATH%"], before.Again().Watched);
    }
}
