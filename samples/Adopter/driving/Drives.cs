using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;

namespace Adopter.Driving;

/// <summary>
/// What an adopting project's driving half consists of.
/// <para>
/// WW228. It is four calls, and that is the point of it being here: a repository adopting this reads
/// the file it declares itself in, loads the cases it wrote, launches what their fixtures name and
/// hands back the verdict. Everything a harness would have decided for itself — how long to wait,
/// how many attempts an act gets, what a missing read-back does to the exit code — is not here
/// because it is not a property of the project.
/// </para>
/// <para>
/// It compiles and nothing runs it, which is deliberate. What it proves is the shape: one package
/// reference reaching the engine, and a project living underneath the application's own without
/// being swallowed by it. Running a case needs a desk, and a sample that needed one would be a
/// sample this repository's own gate could not build.
/// </para>
/// </summary>
public static class Drives
{
    /// <summary>
    /// Run every case this repository declares, and answer what the run concluded.
    /// </summary>
    /// <param name="repository">Where to look for the declaration and the cases.</param>
    /// <param name="asked">What to run. <see cref="Selection.All"/> for all of it.</param>
    public static SuiteVerdict Everything(string repository, Selection asked)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repository);
        ArgumentNullException.ThrowIfNull(asked);

        var project = ProjectDeclaration.Find(repository);
        var declared = ScenarioFile.Across(ScenarioFile.LoadAll(Path.Combine(repository, "cases")));

        // The register is disposed here, so nothing this run started outlives it — and whatever
        // would not stop is named rather than cleaned up in silence.
        using var register = ProcessRegister.For(project);
        return Suite.Launch(declared, asked, register, project);
    }
}
