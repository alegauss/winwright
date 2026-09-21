using Winwright.Acting;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// Which mouse button the notification area is ever pressed with. WW31, and WW483 for why it is a class
/// of its own.
/// <para>
/// It stood in <c>NotificationAreaTests</c> as a check that no method carried "Click" in its name, which
/// was WW31's finding put the only way reflection could put it: a synthesised right-click opens nothing
/// on this shell, so the menu route is focus and the application key. WW483 added the one click the
/// class makes on purpose, the primary button, and the check became a read of the source. That class
/// puts an icon up before any of its cases runs, and a case that only reads the checkout there is one
/// the host gate can never answer, which is what <c>NoDeskTests</c> exists to say.
/// </para>
/// </summary>
public sealed class TrayButtonTests
{
    [Fact]
    public void Nothing_here_reaches_for_a_synthesised_right_click()
    {
        // About the button and no longer about the word. No method is named for a right or context
        // click, and nothing in the source sends the right button.
        var named = typeof(NotificationArea).GetMethods()
            .Select(method => method.Name)
            .Where(name => name.Contains("RightClick", StringComparison.OrdinalIgnoreCase)
                || name.Contains("ContextClick", StringComparison.OrdinalIgnoreCase));

        Assert.Empty(named);

        var source = File.ReadAllText(Checkout.At("src", "Winwright", "Acting", "NotificationArea.cs"));
        Assert.DoesNotContain("MouseButton.Right", source, StringComparison.Ordinal);
    }

    [Fact]
    public void The_one_click_here_is_the_primary_button()
    {
        // WW483's click, and the other half of the guard above: a guard that only forbids the right
        // button passes over a file that sends none at all, which says nothing about the one it does.
        var source = File.ReadAllText(Checkout.At("src", "Winwright", "Acting", "NotificationArea.cs"));

        Assert.Contains("Pointer.Send(x, y, MouseButton.Left, 1)", source, StringComparison.Ordinal);
    }
}
