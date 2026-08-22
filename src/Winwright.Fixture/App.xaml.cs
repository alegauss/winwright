using System.Windows;

namespace Winwright.Fixture;

/// <summary>
/// The fixture application.
/// <para>
/// It takes nothing from the machine it runs on: no account, no settings file, no environment
/// beyond a desktop to draw on. A fixture that needed any of those would be one more thing to set
/// up by hand, which is the cost this exists to remove.
/// </para>
/// </summary>
public partial class App : Application
{
}
