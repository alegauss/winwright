using System.Text.Json.Nodes;

using Winwright.Processes;
using Winwright.Projects;
using Winwright.Scenarios;
using Winwright.Windowing;

namespace Winwright.Mcp;

/// <summary>
/// Running a selection, as a tool answers it.
/// <para>
/// WW222. <c>winwright_check</c> answers whether a file would load, which was WW66's saving and is
/// not the question a session has — that one is <em>did it pass</em>. Without this the tool chain got
/// an agent to a correct case file and then handed the run back to a shell: build, <c>dotnet
/// test</c>, read a trx. A guard that closes one door and leaves the next one open.
/// </para>
/// <para>
/// The desk is read before anything launches and is passed in rather than taken here, because a desk
/// that cannot observe is the answer this has to be able to give and
/// <see cref="Desk.Blocked(Verdicts.Precondition)"/> is the seam that makes giving it provable. A
/// machine with no interactive session, no display or no automation is not a red: nothing about the
/// application was observed, so nothing about it is being reported.
/// </para>
/// </summary>
public static class Running
{
    /// <summary>What this tool takes, which is a selection and where to find what it selects from.</summary>
    public static JsonObject Schema() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["project"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = $"the {ProjectDeclaration.FileName} the executable, the waits and the "
                    + "refusals come from — the file itself, or a directory to find it from",
            },
            ["cases"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = $"the directory the {ScenarioFile.Extension} files are under, walked recursively",
            },
            ["case"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "run only the case of this name; a name nothing declares refuses the run "
                    + "rather than passing over no cases",
            },
            ["tag"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "run only the cases carrying this tag",
            },
            ["sharing"] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "lend one window to the cases that only read it, where the fixture says "
                    + "it may be lent",
            },
        },
        ["required"] = new JsonArray { "project", "cases" },
        ["additionalProperties"] = false,
    };

    /// <summary>
    /// Run what <paramref name="arguments"/> selects.
    /// </summary>
    /// <param name="arguments">The call's arguments, already against <see cref="Schema"/>.</param>
    /// <param name="desk">What this machine turned out to be able to observe.</param>
    /// <returns>
    /// The verdict, as a reading. <see cref="Answer.Refused"/> says the tool could not do what was
    /// asked — a desk that cannot observe, a declaration that will not load, a selector matching
    /// nothing. It does <em>not</em> say the run failed: a run that happened and came back red is
    /// this tool having done its job, and the verdict says which of the three it was.
    /// </returns>
    public static Answer Over(JsonObject arguments, Desk desk)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(desk);

        if (!desk.CanObserve)
        {
            // The condition and the reading of it, in that order: the name is what a case's 'needs'
            // would refer to, and the absence is what actually happened on this machine. A reader
            // handed only the second has to guess which of the six it was.
            var absent = desk.FirstAbsent!;
            return new Answer(
                $"nothing ran, and nothing about the application was observed: this desk lacks "
                    + $"{absent.Name} — {absent.Absence}. That is a hole and not a failure: the run has "
                    + "no verdict to report rather than a red one.",
                Refused: true);
        }

        if (Text(arguments, "project") is not { } where)
            return new Answer($"no {ProjectDeclaration.FileName} was named, so nothing says what to launch", Refused: true);

        if (Text(arguments, "cases") is not { } under)
            return new Answer("no directory of cases was named, so there is nothing to select from", Refused: true);

        try
        {
            var project = Directory.Exists(where)
                ? ProjectDeclaration.Find(where)
                : ProjectDeclaration.Load(where);

            var declared = ScenarioFile.Across(ScenarioFile.LoadAll(under));
            var asked = Selection.Of(
                Text(arguments, "case") is { } named ? [named] : null,
                Text(arguments, "tag") is { } tagged ? [tagged] : null);

            // The register is disposed here and not by the caller, for the reason it exists: a run
            // answering over a stdio pipe has no later moment, and a leftover process is what locks
            // the next build.
            using var register = ProcessRegister.For(project);
            var verdict = Suite.Launch(
                declared,
                asked,
                register,
                project,
                arguments["sharing"]?.GetValue<bool>() ?? false);

            var lines = new List<string>(verdict.Render()) { "", $"exit code {verdict.ExitCode}" };

            // Beside the verdict rather than inside it: a leftover process is a fact about this desk
            // and never a defect in the code under test, and it is what the reader wants in front of
            // them when the next run behaves oddly.
            lines.Add(register.StopAll().Count == 0
                ? ProcessSummary.Sentence(register.Survivors)
                : $"{ProcessSummary.Sentence(register.Survivors)} {string.Join("; ", register.Survivors)}");

            return new Answer(string.Join('\n', lines));
        }
        catch (ScenarioRefusedException refused)
        {
            return new Answer($"{refused.Subject}: {refused.Because}", Refused: true);
        }
        catch (DeclarationMissingException missing)
        {
            return new Answer(missing.Message, Refused: true);
        }
    }

    /// <summary>One text argument, trimmed, or null where it is absent or empty.</summary>
    private static string? Text(JsonObject arguments, string key) =>
        arguments[key]?.GetValue<string>() is { } said && said.Trim().Length > 0 ? said.Trim() : null;
}
