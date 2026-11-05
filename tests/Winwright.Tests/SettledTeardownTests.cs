using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW201. A class that copies a binary somewhere of its own and starts it has to wait for that
/// process to leave the machine before it deletes the directory, because Windows will not delete a
/// running image — and the throw comes out of <c>Dispose</c>, where it reads as a broken harness
/// rather than as the file handle it is.
/// <para>
/// Found by a guest run and fixed in four classes. This is the part that makes the fifth somebody
/// else's red instead of a repeat: the rule is read out of the sources, which is what WW190 did for
/// the desk and for the same reason — applying a rule everywhere it is needed today does nothing
/// about the class written tomorrow.
/// </para>
/// </summary>
public sealed class SettledTeardownTests
{
    /// <summary>
    /// The three things a class has to do to be able to commit this, matched as code.
    /// <para>
    /// The combination and never one of them. Which file a class copies and which it starts are the
    /// same file only through a variable, and following that would be reading the program rather
    /// than the sources — so the reading asks the cheaper question: does this class put a file
    /// somewhere, build something to start, and delete a directory. A class doing all three is one that can
    /// delete an image something is running; a class copying strings into a temp directory is not,
    /// and is left out because it starts nothing.
    /// </para>
    /// </summary>
    private static readonly string[] Marks = ["File.Copy(", "ProcessStartInfo", "Directory.Delete("];

    /// <summary>Either door that waits for the process rather than only stopping it.</summary>
    private static readonly string[] Settles = ["StopAndSettle", "Attachable.Settling("];

    /// <summary>Every case file that does all three, read as code.</summary>
    private static IReadOnlyList<string> CopyRunAndDelete() => Checkout
        .SourcesIn(Checkout.Suite, except: $"{nameof(SettledTeardownTests)}.cs")
        .Select(one => (Name: Path.GetFileName(one), Lines: File.ReadLines(one).Select(Checkout.Code).ToList()))
        .Where(one => Marks.All(mark => one.Lines.Any(line => line.Contains(mark, StringComparison.Ordinal))))
        .Select(one => one.Name)
        .OrderBy(one => one, StringComparer.Ordinal)
        .ToList();

    private static bool Settled(string named) => File
        .ReadLines(Path.Combine(Checkout.Suite, "Winwright.Tests", named))
        .Select(Checkout.Code)
        .Any(line => Settles.Any(one => line.Contains(one, StringComparison.Ordinal)));

    [Fact]
    public void Every_class_that_deletes_a_binary_it_ran_waits_for_it_to_be_gone()
    {
        var stopping = CopyRunAndDelete().Where(one => !Settled(one)).ToList();

        Assert.True(
            stopping.Count == 0,
            $"{stopping.Count} class(es) copy an executable, start it and delete the directory it is "
                + $"in without waiting for it to leave the machine: {string.Join(", ", stopping)}");
    }

    [Fact]
    public void The_reading_finds_the_four_it_was_written_for_and_not_the_ones_that_copy_data()
    {
        // A sweep that found nothing would pass the rule above by arithmetic. Both ends are named:
        // the class the guest run actually caught, and one that copies strings and is none of this.
        var found = CopyRunAndDelete();

        Assert.Contains("CaptureReceiptTests.cs", found, StringComparer.Ordinal);
        Assert.Contains("AppTargetTests.cs", found, StringComparer.Ordinal);
        Assert.Contains("InstanceCheckTests.cs", found, StringComparer.Ordinal);
        Assert.Contains("RunningBinaryTests.cs", found, StringComparer.Ordinal);

        Assert.DoesNotContain("DerivedSetTests.cs", found, StringComparer.Ordinal);
        Assert.DoesNotContain("LoadingTests.cs", found, StringComparer.Ordinal);
    }

    [Fact]
    public void Stopping_and_settling_are_different_things_and_the_door_is_the_second()
    {
        // Why a door and not a habit. The register's own Dispose stops what it started, which is
        // right for it — an adopter's run should not wait on a process it is finished with. What a
        // case deleting that process's image needs is the stronger promise, and this is where it is.
        using var settling = Attachable.Settling();

        Assert.NotNull(settling.Register);
        Assert.Empty(settling.Register.Launched);
    }
}
