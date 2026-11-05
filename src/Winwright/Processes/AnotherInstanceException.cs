namespace Winwright.Processes;

/// <summary>
/// Another windowed instance of the application under test is open, so the run stopped before it
/// could photograph the wrong one. It refuses rather than reporting a hole, because the failure
/// this exists for did not skip anything: it returned a picture of another instance's Settings
/// window when Statistics had been asked for, printed the size it captured, and exited zero.
/// </summary>
public sealed class AnotherInstanceException : Exception
{
    /// <param name="executable">The application under test.</param>
    /// <param name="others">The instances in the way, each with the windows it is showing.</param>
    public AnotherInstanceException(string executable, IReadOnlyList<OtherInstance> others)
        : base($"{executable} is already showing a window in {Listed(others)}; "
            + $"pass {InstanceCheck.OverrideName} to drive it anyway")
    {
        Executable = executable;
        Others = others;
    }

    /// <summary>The application under test.</summary>
    public string Executable { get; }

    /// <summary>The instances in the way.</summary>
    public IReadOnlyList<OtherInstance> Others { get; }

    private static string Listed(IReadOnlyList<OtherInstance> others) =>
        others.Count == 1
            ? others[0].ToString()
            : string.Join("; ", others.Select(other => other.ToString()));
}
