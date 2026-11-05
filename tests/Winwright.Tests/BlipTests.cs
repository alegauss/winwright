using System.Runtime.InteropServices;

using Winwright.Windowing;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW140. Caught in a full-suite run while WW119 was being verified. The desk reading touches
/// UI Automation's root to find out whether it is usable at all, and caught COMException among the
/// ways it can be unusable. Once in a loaded run it came back with "Unexpected HRESULT has been
/// returned from a call to a COM component", the reading said this desk cannot observe, and the
/// case asserting the conditions a running suite proves went red. The class passed six of six on
/// its own.
/// <para>
/// Both halves of that catch are right and they are not the same fact. A machine with no automation
/// assemblies fails the call every time; a machine under load fails it once and answers the next
/// moment. Reporting them identically is this block's own criterion pointed the wrong way — nothing
/// about the desk should be reported as a defect in the code.
/// </para>
/// </summary>
public sealed class BlipTests
{
    [Theory]
    [InlineData(typeof(COMException))]
    [InlineData(typeof(InvalidOperationException))]
    public void A_call_that_failed_is_looked_at_again(Type failure)
    {
        // The blip: a machine under load answers the next moment, so one look is not an answer.
        var thrown = (Exception)Activator.CreateInstance(failure)!;

        Assert.True(Desk.Caught(thrown));
        Assert.True(Desk.WorthAnotherLook(thrown));
    }

    [Theory]
    [InlineData(typeof(FileNotFoundException))]
    [InlineData(typeof(TypeLoadException))]
    [InlineData(typeof(BadImageFormatException))]
    public void An_assembly_that_will_not_load_is_answered_on_the_first_look(Type failure)
    {
        // The half that must not spend a deadline: the file is not there, or the type is not in it,
        // or the image is the wrong architecture, and no amount of waiting changes any of those.
        var thrown = (Exception)Activator.CreateInstance(failure)!;

        Assert.True(Desk.Caught(thrown), "a failure this reading answers for");
        Assert.False(Desk.WorthAnotherLook(thrown));
    }

    [Theory]
    [InlineData(typeof(ArgumentException))]
    [InlineData(typeof(OutOfMemoryException))]
    [InlineData(typeof(NotSupportedException))]
    public void Anything_else_is_not_caught_at_all(Type failure)
    {
        // Three outcomes and not two. Asserting only that it is not worth another look would say
        // nothing, since a settled failure answers that way too — what makes this case real is
        // that the reading does not answer for it at all, so the failure leaves.
        var thrown = (Exception)Activator.CreateInstance(failure)!;

        Assert.False(Desk.Caught(thrown));
        Assert.False(Desk.WorthAnotherLook(thrown));
    }

    [Fact]
    public void The_reading_is_refused_nothing_to_judge()
    {
        Assert.Throws<ArgumentNullException>(() => Desk.WorthAnotherLook(null!));
        Assert.Throws<ArgumentNullException>(() => Desk.Caught(null!));
    }

    [Fact]
    public void This_machine_reaches_ui_automation_on_the_first_look()
    {
        // The live half. A suite that is running has UI Automation by definition — it has been
        // reading trees for a minute — so one look is the answer, and more than one here would be
        // this machine telling us something worth knowing rather than this case being wrong.
        var desk = Desk.Read();

        Assert.True(desk.AutomationLooks >= 1);
        Assert.Contains(desk.Conditions, one => one.Name == Desk.AutomationAssemblies && one.Satisfied);
    }

    [Fact]
    public void A_reading_that_settled_first_time_says_nothing_about_settling()
    {
        // The other side of saying it out loud: an ordinary reading is not decorated with a fact
        // that did not happen.
        var desk = Desk.Read();

        if (desk.AutomationSettled)
        {
            Assert.Contains("UI Automation answered on look", desk.Sentence());
            return;
        }

        Assert.DoesNotContain("UI Automation answered on look", desk.Sentence());
        Assert.Equal(1, desk.AutomationLooks);
    }

    [Fact]
    public void The_whole_reading_still_names_every_condition_it_took()
    {
        // The count is carried beside the conditions rather than as one of them: a look that had to
        // be repeated is not a thing an assertion may be excused by.
        var desk = Desk.Read();

        Assert.Equal(6, desk.Conditions.Count);
        Assert.DoesNotContain(desk.Conditions, one => one.Name.Contains("look", StringComparison.Ordinal));
    }
}
