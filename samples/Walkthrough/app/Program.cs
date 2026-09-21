using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace Walkthrough.App;

/// <summary>
/// The smallest application a case can be written against: a label, a button, and an automation
/// id on each so a locator can address them without reading the words on them.
/// <para>
/// WW493. What the documentation area needed was a run it could capture, and there was nothing to
/// run: this repository's other sample is a library that compiles and proves a package reference.
/// So this is the application under test, and it is deliberately dull - a captured walkthrough is
/// about the harness, and an application with anything interesting in it would be about itself.
/// </para>
/// <para>
/// No reference to either half of winwright. That is the point rather than an omission: every
/// reading and every pattern act in the capture runs against an application that has never heard
/// of the tool driving it, which is the claim the page beside it makes.
/// </para>
/// </summary>
public static class Program
{
    /// <summary>What the label says before anything is pressed.</summary>
    public const string Greeting = "Nothing pressed yet";

    /// <summary>What it says afterwards, which is what the captured case reads back.</summary>
    public const string Pressed = "The button was pressed";

    [STAThread]
    public static void Main()
    {
        var greeting = new TextBlock { Text = Greeting, Margin = new Thickness(12) };
        AutomationProperties.SetAutomationId(greeting, "greeting");

        var press = new Button { Content = "Press me", Margin = new Thickness(12), Padding = new Thickness(8, 4, 8, 4) };
        AutomationProperties.SetAutomationId(press, "press");
        press.Click += (_, _) => greeting.Text = Pressed;

        var panel = new StackPanel();
        panel.Children.Add(greeting);
        panel.Children.Add(press);

        var window = new Window
        {
            Title = "Walkthrough",
            Width = 360,
            Height = 200,
            Content = panel,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };

        new Application { ShutdownMode = ShutdownMode.OnMainWindowClose }.Run(window);
    }
}
