using System.Collections.ObjectModel;

using Winwright.Projects;

namespace Winwright.Concordance;

/// <summary>
/// The copies an invocation named, or the reason it named none that can be read.
/// <para>
/// Its own type rather than lines inside <see cref="Program"/>, because the whole risk of a gate
/// like this is in the arguments: a flag that was silently dropped leaves a reading of two copies
/// where three were meant, and two copies that agree is a green. What the arguments turned into is
/// therefore something a test can hold, and not something only a process exit code reports.
/// </para>
/// </summary>
/// <param name="Copies">The copies named, in the order they were named.</param>
/// <param name="Complaint">Why this invocation cannot be read at all. Empty where it can.</param>
public sealed record Roster(IReadOnlyList<EngineCopy> Copies, string Complaint = "")
{
    /// <summary>The package id assumed where an invocation names none.</summary>
    public const string DefaultPackage = "Winwright";

    /// <summary>Whether the copies here can be read against each other.</summary>
    public bool Readable => Complaint.Length == 0;

    /// <summary>What every invocation is answered with where it could not be read.</summary>
    public static string Usage =>
        """
        usage: Winwright.Concordance [--package <id>] --declared <csproj|props> --packed <nupkg|dir>
                                     --pinned <consuming csproj> --running

          --declared  the version a source tree declares, out of its first <Version>
          --packed    the version a build actually produced, out of the nuspec inside the package
          --pinned    the version a consuming project asks for, out of its PackageReference
          --running   the version of the engine assembly this process loaded
          --package   the package id the --packed and --pinned flags after it are about
                      (default: Winwright; it is read left to right, so it can change mid-line)

        At least two copies, because one copy agrees with itself on every machine there is.
        Exit: 0 they agree, 1 they do not or one cannot be pinned, 2 this line could not be read.
        """;

    /// <summary>
    /// Read a command line into the copies it names.
    /// <para>
    /// An argument this does not know is refused rather than skipped. A skipped flag is a copy that
    /// silently left the reading, and a reading with a copy missing still exits zero — which is the
    /// shape of the defect this whole project was started over, arriving through a typo.
    /// </para>
    /// </summary>
    /// <param name="args">The command line, without the executable.</param>
    public static Roster From(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var copies = new List<EngineCopy>();
        var package = DefaultPackage;

        for (var index = 0; index < args.Count; index++)
        {
            var flag = args[index]?.Trim() ?? "";
            if (flag == "--running")
            {
                copies.Add(Engine.Running());
                continue;
            }

            if (flag is not ("--declared" or "--packed" or "--pinned" or "--package"))
                return Refusing($"'{args[index]}' is not one of the flags this reads");

            if (index + 1 >= args.Count || string.IsNullOrWhiteSpace(args[index + 1]))
                return Refusing($"{flag} was given nothing to read");

            var value = args[++index].Trim();
            switch (flag)
            {
                case "--package":
                    package = value;
                    break;
                case "--declared":
                    copies.Add(Engine.Declared($"the tree ({Path.GetFileName(value)})", value));
                    break;
                case "--packed":
                    copies.Add(Engine.Packed($"the {package} package in {value}", value, package));
                    break;
                default:
                    copies.Add(Engine.Pinned(
                        $"{Path.GetFileName(value)}'s reference to {package}", value, package));
                    break;
            }
        }

        // Refused here and not left to Agreement.Between's exception, so a line that names one copy
        // is answered with what is wrong with the line rather than with a stack trace.
        return copies.Count < 2
            ? Refusing(
                $"{copies.Count} copy was named, and agreement needs at least two: "
                    + "one copy agrees with itself on every machine there is")
            : new Roster(new ReadOnlyCollection<EngineCopy>(copies));
    }

    /// <summary>The reading these copies make, which is refused unless this roster is readable.</summary>
    /// <exception cref="InvalidOperationException">Where this roster could not be read.</exception>
    public Agreement Read() => Readable
        ? Agreement.Between(Copies)
        : throw new InvalidOperationException(Complaint);

    private static Roster Refusing(string complaint) => new([], complaint);
}
