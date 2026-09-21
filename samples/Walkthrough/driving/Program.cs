using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Verdicts;

namespace Walkthrough.Run;

/// <summary>
/// The four calls an adopting project writes, run for real so the documentation area can capture
/// what they answered. WW493.
/// <para>
/// It is the same four <c>samples/Adopter</c> states and does not run: read the declaration by
/// walking up from where the run starts, load the cases, hold the processes in a register that
/// disposes them, and hand back the verdict. Everything a runner would have decided for itself -
/// how long to wait, how many attempts an act gets, what a missing read-back does to the exit
/// code - is not here because it is not a property of the run.
/// </para>
/// </summary>
public static class Program
{
    /// <summary>
    /// Run every case this sample declares and print what came back.
    /// </summary>
    /// <param name="args">The repository to run in, or nothing for this file's own directory.</param>
    public static int Main(string[] args)
    {
        var repository = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();

        var project = ProjectDeclaration.Find(repository);
        var declared = ScenarioFile.Across(ScenarioFile.LoadAll(Path.Combine(project.Root, "cases")));

        // Disposed here, so nothing this run started outlives it - and whatever would not stop is
        // named rather than cleaned up in silence.
        using var register = ProcessRegister.For(project);
        var verdict = Suite.Launch(declared, Selection.All, register, project);

        Console.WriteLine(verdict.Sentence());

        // The line above is the claim; this is what it rests on. A reader told an assertion did
        // not hold wants to know which one and what it read instead, and a reader told one never
        // ran wants the precondition that was absent — both before they open anything.
        foreach (var ran in verdict.Ran)
        {
            Console.WriteLine();
            Console.WriteLine($"  {ran.Declared.Name}");
            foreach (var line in VerdictSummary.Render(ran.Verdict).Split('\n'))
                Console.WriteLine($"  {line.TrimEnd()}");
        }

        return verdict.ExitCode;
    }
}
