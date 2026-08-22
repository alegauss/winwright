using System.Diagnostics;

using Winwright.Capturing;
using Winwright.Processes;
using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW47. A wrong capture is caught only because a person looked at the picture.
/// <para>
/// The two refusals are the point of the task. A picture of somebody else's window and a picture
/// of a window nothing is drawing are both perfectly good files on disk, indistinguishable from a
/// correct capture by anything except opening them — so they are refused from the facts that were
/// already established rather than left for a reader to notice.
/// </para>
/// </summary>
[Collection(WindowFixture.Serial)]
public sealed class CaptureReceiptTests : IDisposable
{
    private readonly ProcessRegister register = new();
    private readonly string root = Directory.CreateTempSubdirectory("winwright-receipt-").FullName;

    public void Dispose()
    {
        register.StopAll();
        Directory.Delete(root, recursive: true);
    }

    private static TopLevelWindow Found(PumpedDialog dialog) =>
        Assert.Single(
            TopLevelWindows.OfProcess(Environment.ProcessId), window => window.Handle == dialog.Frame);

    private AppTarget Elsewhere()
    {
        var app = Path.Combine(root, "other.exe");
        if (!File.Exists(app))
            File.Copy(Path.Combine(Environment.SystemDirectory, "cmd.exe"), app);

        var start = new ProcessStartInfo(app) { UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add("/c");
        start.ArgumentList.Add("ping -n 120 127.0.0.1");

        return AppTarget.FromLaunch(Attachable.Launch(register, start), "/c", "ping");
    }

    [Fact]
    public void The_line_names_the_window_the_process_and_the_arguments_behind_it()
    {
        using var dialog = PumpedDialog.OpenFramed("winwright receipt");

        // The window is this process's, so the target has to be too for the receipt to compose.
        var target = AppTarget.AttachTo(Environment.ProcessId);
        var receipt = CaptureReceipt.Of(
            Path.Combine(root, "shot.png"), Found(dialog), target, PaintedFrame.Of(dialog.Frame));

        var said = receipt.Sentence();

        Assert.Contains("winwright receipt", said);
        Assert.Contains($"pid {Environment.ProcessId}", said);
        Assert.Contains("shot.png", said);
        Assert.Contains("the painted frame is", said);
    }

    [Fact]
    public void An_attached_run_says_it_cannot_know_the_arguments_rather_than_printing_none()
    {
        using var dialog = PumpedDialog.Open("winwright attached");

        var receipt = CaptureReceipt.Of(
            Path.Combine(root, "shot.png"), Found(dialog), AppTarget.AttachTo(Environment.ProcessId));

        // "no arguments" and "this run did not start it" are different claims, and only one of
        // them is true here. A receipt that printed an empty list would be the wrong one.
        Assert.False(receipt.Arguments.Satisfied);
        Assert.Contains("did not start it", receipt.Arguments.Absence);
        Assert.Contains("attached to pid", receipt.Sentence());
        Assert.DoesNotContain("no arguments", receipt.Sentence());
    }

    [Fact]
    public void A_launched_run_prints_what_it_passed()
    {
        var target = Elsewhere();

        var window = new TopLevelWindow(
            0x1234, target.Pid, "other", "Static", new WindowBounds(0, 0, 100, 100), true, 0);
        var receipt = CaptureReceipt.Of(Path.Combine(root, "shot.png"), window, target);

        Assert.True(receipt.Arguments.Satisfied);
        Assert.Contains("with /c ping", receipt.Sentence());
    }

    [Fact]
    public void A_picture_of_somebody_elses_window_is_refused_and_names_both_processes()
    {
        using var dialog = PumpedDialog.Open("winwright intruder");
        var target = Elsewhere();

        var refused = Assert.Throws<WrongCaptureException>(
            () => CaptureReceipt.Of(Path.Combine(root, "shot.png"), Found(dialog), target));

        Assert.Contains($"pid {Environment.ProcessId}", refused.Message);
        Assert.Contains($"driving pid {target.Pid}", refused.Message);
        Assert.Contains("winwright intruder", refused.Message);
    }

    [Fact]
    public void A_picture_of_a_window_nothing_is_drawing_is_refused_and_says_who_cloaked_it()
    {
        using var dialog = PumpedDialog.Open("winwright cloaked shot");
        var window = Found(dialog);
        dialog.Cloak();

        // Read before the cloak and used after it, which is the real shape of the mistake: a
        // window enumerated a moment ago and photographed once it had already gone.
        var refused = Assert.Throws<WrongCaptureException>(
            () => CaptureReceipt.Of(
                Path.Combine(root, "shot.png"),
                window with { Cloak = Cloaking.Of(dialog.Frame) },
                AppTarget.AttachTo(Environment.ProcessId)));

        Assert.Contains("which nothing is drawing", refused.Message);
        Assert.Contains("the application cloaked it", refused.Message);
    }

    [Fact]
    public void The_trace_step_addresses_the_window_and_keeps_the_file_as_the_read_back()
    {
        using var dialog = PumpedDialog.Open("winwright traced");
        var path = Path.Combine(root, "shot.png");

        var step = CaptureReceipt.Of(path, Found(dialog), AppTarget.AttachTo(Environment.ProcessId)).AsTraceStep();

        Assert.Equal("capture", step.Verb);
        Assert.Equal($"0x{dialog.Frame:X}", step.Locator);
        Assert.Equal(path, step.ReadBack);
        Assert.Contains("winwright traced", step.Resolved);

        // The hole travels with the step: an attached run's arguments are absent, and the step
        // carries the reason rather than being silent about a field it could not fill.
        Assert.Contains("did not start it", step.Detail);
    }

    [Fact]
    public void A_receipt_with_no_file_and_no_facts_is_refused_before_it_composes()
    {
        using var dialog = PumpedDialog.Open("winwright refused");
        var window = Found(dialog);
        var target = AppTarget.AttachTo(Environment.ProcessId);

        Assert.Throws<ArgumentException>(() => CaptureReceipt.Of(" ", window, target));
        Assert.Throws<ArgumentNullException>(() => CaptureReceipt.Of("shot.png", null!, target));
        Assert.Throws<ArgumentNullException>(() => CaptureReceipt.Of("shot.png", window, null!));
    }
}
