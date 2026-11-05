using System.Windows;

namespace Winwright.Fixture;

/// <summary>
/// The fixture application: its resources, and nothing else.
/// <para>
/// It takes nothing from the machine it runs on — no account, no settings file, no environment
/// beyond a desktop to draw on. A fixture that needed any of those would be one more thing to set
/// up by hand, which is the cost this exists to remove.
/// </para>
/// <para>
/// Written in code rather than as an <c>App.xaml</c>, because an application definition generates
/// its own entry point and this project needs its own. The flags are read in <see cref="Program"/>
/// before any of this exists.
/// </para>
/// </summary>
public sealed class App : Application
{
    /// <summary>Merge the theme, which is every colour and font this fixture ever draws with.</summary>
    public App() => Resources.MergedDictionaries.Add(
        new ResourceDictionary { Source = new Uri("Theme.xaml", UriKind.Relative) });
}
