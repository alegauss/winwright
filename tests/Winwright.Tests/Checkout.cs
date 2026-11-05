using System.Collections.ObjectModel;

using Xunit;

namespace Winwright.Tests;

/// <summary>One member of one source file, as a sweep over the checkout reads it.</summary>
/// <param name="Owner">The file it is in, without its extension, which is its type in this tree.</param>
/// <param name="Name">The member's own name.</param>
/// <param name="Body">Its declaration and every line under it, as code, joined by newlines.</param>
/// <param name="IsPublic">Whether an adopter can call it.</param>
internal sealed record SourceMember(string Owner, string Name, string Body, bool IsPublic)
{
    /// <summary>
    /// The type that declares it, where the walk read one. WW462, and it is not always the file:
    /// <c>Throughout.cs</c> declares <c>RegionThroughout</c>, and a call to it is written with the
    /// type on it. Empty where nothing above the member declared a type, and then the file stands in.
    /// </summary>
    public string Declaring { get; init; } = "";

    /// <summary>How anything outside its file spells a call to it.</summary>
    public string Named => $"{(Declaring.Length > 0 ? Declaring : Owner)}.{Name}";
}

/// <summary>
/// One member that reaches a primitive, directly or through another member. WW462.
/// </summary>
/// <param name="Named">The member, as <c>Owner.Member</c>.</param>
/// <param name="IsPublic">Whether an adopter can call it, which is what a catalogue of verbs holds.</param>
internal sealed record SourceReach(string Named, bool IsPublic);

/// <summary>
/// The checkout this suite is running out of: where it is, and what source files are in it.
/// <para>
/// WW193. Every case that reads a file walks up from <c>AppContext.BaseDirectory</c> looking for the
/// solution file, and that loop was written out eighteen times across sixteen files — spelled with
/// three different variable names, which is how a reader misses that they are the same four lines.
/// Four of them go on to enumerate <c>*.cs</c> and skip <c>bin</c> and <c>obj</c>.
/// </para>
/// <para>
/// Extracted because of how the first one went. <c>Deadlines</c> shipped recursing into <c>bin</c>
/// and <c>obj</c> — thousands of files instead of two hundred, inside a suite whose other cases are
/// waiting on five-second deadlines — and the guest went red twice with two different timing
/// failures before the cause was found. The run went 2m50s to 3m53s and back. That was one copy
/// getting one exclusion wrong, and it was also a correctness fault: a stale copy under <c>bin</c>
/// is an entry in a catalogue for a file nobody has.
/// </para>
/// <para>
/// The walk and never the question. Each catalogue keeps its own rule about what it is looking for;
/// a shared answer would be the opposite of what this is for.
/// </para>
/// </summary>
internal static class Checkout
{
    /// <summary>What marks the root. The solution, because that is what a checkout has one of.</summary>
    internal const string Marker = "Winwright.slnx";

    /// <summary>Where the repository is, walked up from where the suite is running.</summary>
    internal static string Root => root.Value;

    /// <summary>A path inside it, joined the way the platform spells one.</summary>
    /// <param name="parts">The segments under the root, e.g. <c>docs</c> then <c>ROADMAP.md</c>.</param>
    internal static string At(params string[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        return Path.Combine([Root, .. parts]);
    }

    /// <summary>The engine's sources, which is one of the two trees anything here reads.</summary>
    internal static string Engine => At("src");

    /// <summary>This suite's own, which is the other.</summary>
    internal static string Suite => At("tests");

    /// <summary>
    /// The programs this repository builds beside the engine: the runner's arms, the roll call, the
    /// dump reader, the typing rig. WW437.
    /// <para>
    /// This project's code and not an adopter's, which is the line <see cref="Samples" /> draws — and
    /// it was the tree no whole-repository sweep walked. Found by writing a catalogue entry for a
    /// thread <c>Winwright.Blame</c> parks on purpose and being told the file sleeps nowhere: the
    /// sweep never offered it, so the catalogue could not see it from either side.
    /// </para>
    /// </summary>
    internal static string Tools => At("tools");

    /// <summary>
    /// An adopter's repository kept inside this one, which is a tree this checkout carries and code
    /// this project does not own. WW423.
    /// <para>
    /// WW228 built <c>samples/Adopter</c> to prove the adoption a paragraph used to describe: its
    /// projects reference the engine through a package, the way somebody else's repository does, and
    /// its whole value is being the thing this project is not. Nothing said so, so a sweep from the
    /// root read it as this project's own — WW408's first draft counted its driving half as a library
    /// of this repository's, which is the reading exactly inverted.
    /// </para>
    /// <para>
    /// What a sweep does on meeting it follows from which question the sweep asks. One about this
    /// project's code — the catalogues, the shipped members, the projects this repository builds —
    /// never reads it, and does not by construction: those walk <see cref="Engine" />,
    /// <see cref="Suite" /> and <c>tools</c>. One about the files this checkout carries reads it like
    /// any other, because an adopter's file damaged on this desk is damage in this repository —
    /// which is why the encoding walk holds it. And the rules an adopter really is subject to are the
    /// short list Block J's criterion states: one package reference and no path into the engine's
    /// source, which <c>PackagedTests</c> and <c>SeparationTests</c> hold it to by name.
    /// </para>
    /// </summary>
    internal static string Samples => At("samples");

    /// <summary>
    /// The halves an adopter writes code against, which is what a sweep over shipped code means.
    /// WW408.
    /// <para>
    /// Every reflection sweep here begins by saying which code it is about, and each said it
    /// differently — so each author argued the bound from scratch at the moment they had least to go
    /// on. WW390 spent a paragraph on it; the sweep beside it reads one assembly and narrows to a
    /// namespace. A catalogue covering less than a reader assumes is worse than none, and nothing
    /// said what a reader should assume.
    /// </para>
    /// <para>
    /// The fact is small and stable. Two projects here are libraries: a missing annotation, an
    /// unhandled refusal or a renamed member in one of them is a defect in a repository this project
    /// does not own. The rest are programs, whose methods have one caller each in the same file.
    /// </para>
    /// <para>
    /// A list rather than a derivation, because a sweep needs assemblies and the checkout has
    /// project files — and read back against those project files by a case, which is what makes it
    /// a claim rather than two names somebody typed. The fixture is deliberately not among them:
    /// it is a program, and the suite references it without loading its assembly, because an
    /// application under test is launched from its own output rather than read from beside the
    /// harness.
    /// </para>
    /// </summary>
    internal static IReadOnlyList<System.Reflection.Assembly> Shipped { get; } =
        new ReadOnlyCollection<System.Reflection.Assembly>(
        [
            typeof(Winwright.Acting.Act).Assembly,
            typeof(Winwright.InApp.Renders).Assembly,
        ]);

    /// <summary>
    /// Every project this repository builds, and whether it builds something a person runs. WW408.
    /// <para>
    /// Read off the project file and not off a list, so the answer is the build's own: a project
    /// declaring an output type produces a program, and one that declares none is a library. Every
    /// project here follows it today, which is why the reading is worth having — the run that adds a
    /// third library under <c>src</c> is the run that has to decide whether it is shipped.
    /// </para>
    /// <para>
    /// <c>src</c> and <c>tools</c> and nothing else, which the first draft got wrong and a case said
    /// so: <c>samples/Adopter</c> holds an adopter's own projects, referencing this engine through a
    /// package the way somebody else's repository does. They are the thing shipped code is shipped
    /// to, so counting one as a library of this project's is the reading exactly inverted.
    /// </para>
    /// </summary>
    internal static IReadOnlyList<(string Named, bool IsProgram)> Projects() =>
        new ReadOnlyCollection<(string, bool)>(
            new[] { Engine, Tools }
                .SelectMany(one => Directory.EnumerateFiles(one, "*.csproj", SearchOption.AllDirectories))
                .Where(Written)
                .Select(one => (
                    Path.GetFileNameWithoutExtension(one),
                    File.ReadAllText(one).Contains("<OutputType>", StringComparison.Ordinal)))
                .OrderBy(one => one.Item1, StringComparer.Ordinal)
                .ToList());

    /// <summary>
    /// Every tree this project's own code is in, for a catalogue whose question is about the whole
    /// repository.
    /// <para>
    /// Three of them, and it was two. WW437: the name said everything and the list said
    /// <see cref="Engine" /> and <see cref="Suite" />, so a catalogue of how threads are parked could
    /// not see the one this repository parks most deliberately — the thread
    /// <c>Winwright.Blame</c> holds so that a hang dump has a hang in it. A sweep that never offers a
    /// file reports a clean pass over the files it did offer, which is the reading this project
    /// refuses everywhere else.
    /// </para>
    /// <para>
    /// The adopter's tree stays out, and by name: <see cref="Samples" /> is code this project does
    /// not own, and a catalogue of this repository's habits has nothing to say about it.
    /// </para>
    /// <para>
    /// Computed rather than initialised, and that is not a preference. A static field initialiser
    /// here runs before the one holding the walk, which is declared lower down — so this answered a
    /// null reference in every case that reads a file, and the guest said so.
    /// </para>
    /// </summary>
    internal static IReadOnlyList<string> Everything =>
        new ReadOnlyCollection<string>([Engine, Suite, Tools]);

    /// <summary>
    /// Every C# source under those trees, and never what a build left beside them.
    /// </summary>
    /// <param name="trees">Which trees to walk.</param>
    /// <param name="except">
    /// A file name to leave out — a catalogue that spells the thing it searches for would otherwise
    /// find it in itself, and report the naming as a use.
    /// </param>
    internal static IEnumerable<string> Sources(IEnumerable<string> trees, string? except = null)
    {
        ArgumentNullException.ThrowIfNull(trees);

        return trees
            .SelectMany(one => Directory.EnumerateFiles(one, "*.cs", SearchOption.AllDirectories))
            .Where(Written)
            .Where(one => except is null || !string.Equals(Path.GetFileName(one), except, StringComparison.Ordinal));
    }

    /// <summary>The same over one tree, which is what most of them want.</summary>
    /// <param name="tree">The tree to walk.</param>
    /// <param name="except">A file name to leave out.</param>
    internal static IEnumerable<string> SourcesIn(string tree, string? except = null) =>
        Sources([tree], except);

    /// <summary>
    /// One line of source with its quoted text taken out, so a call named as data is not read as a
    /// call made.
    /// <para>
    /// WW191 found this in one scanner: a case asserting that <c>NotificationArea.OpenOverflow(</c>
    /// is among the calls a catalogue sweeps for was reported as a case that opens the overflow. The
    /// fragment was in a string, which is the one place in a source file where a call is a subject
    /// rather than an act.
    /// </para>
    /// <para>
    /// WW197 found it again in a second scanner, on the same file, and that is why it is here rather
    /// than in either. A catalogue of calls holds every call it knows about as text, so any sweep
    /// reading it raw finds all of them at once.
    /// </para>
    /// <para>
    /// A line whose quotes do not pair is left whole. A raw literal opens on one line and shuts on
    /// another, and stripping from an unmatched quote to the end of the line would delete real code
    /// — which turns a call that was made into one that appears not to be, and a sweep goes quiet
    /// about it. Reading too much is a red somebody answers; reading too little is not.
    /// </para>
    /// </summary>
    /// <param name="line">The line as the file spells it.</param>
    internal static string Code(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        return Uncommented(Unquoted(line));
    }

    /// <summary>
    /// The line with what a person wrote about it taken off and its strings kept.
    /// <para>
    /// Found by WW197 on the doc comment of <see cref="Code" />, which names a call in a <c>see</c>
    /// tag to explain what it does — and a sweep reading comments reported three helpers that touch
    /// nothing as reaching for it. Prose about a call is the other place a call is a subject rather
    /// than an act, and every catalogue here explains itself in prose.
    /// </para>
    /// <para>
    /// Stripped after the quotes and never before, so a <c>//</c> inside a string has already gone
    /// and cannot take the rest of a real line with it.
    /// </para>
    /// <para>
    /// WW202. <see cref="Code" /> is what a sweep for a call wants, and it is the wrong reading for
    /// a sweep looking for a file pattern: <c>"*.cs"</c> is a string, and stripping strings deletes
    /// the very thing being looked for. Both are offered rather than one being made to do, because
    /// a sweep that had to choose between reading comments and reading nothing would choose wrong.
    /// </para>
    /// </summary>
    /// <param name="line">The line as the file spells it.</param>
    internal static string Spoken(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        return Uncommented(line);
    }

    private static string Uncommented(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("//", StringComparison.Ordinal) || trimmed.StartsWith('*'))
            return "";

        var at = line.IndexOf("//", StringComparison.Ordinal);
        return at < 0 ? line : line[..at];
    }

    /// <summary>The line with its quoted text taken off.</summary>
    private static string Unquoted(string line)
    {
        var quotes = line.Count(one => one == '"');
        if (quotes == 0 || quotes % 2 != 0)
            return line;

        var kept = new System.Text.StringBuilder(line.Length);
        var inside = false;
        foreach (var letter in line)
        {
            if (letter == '"')
            {
                inside = !inside;
                continue;
            }

            if (!inside)
                kept.Append(letter);
        }

        return kept.ToString();
    }

    /// <summary>
    /// The type a line declares, where the line is a top-level declaration.
    /// <para>
    /// A declaration and never a mention: prose about a window class is not a type, and a sweep that
    /// took one quietly renamed every member in the file after it.
    /// </para>
    /// </summary>
    /// <param name="line">The line, already read as code.</param>
    internal static string? Owner(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        if (!line.StartsWith("public ", StringComparison.Ordinal)
            && !line.StartsWith("internal ", StringComparison.Ordinal))
            return null;

        var at = line.IndexOf("class ", StringComparison.Ordinal);
        if (at < 0)
            return null;

        var rest = line[(at + 6)..].Trim();
        var end = rest.IndexOfAny([' ', ':', '(', '{', '<']);
        return end < 0 ? rest : rest[..end];
    }

    /// <summary>
    /// The member a line declares, at the one indentation a member of a class sits at.
    /// <para>
    /// WW207. This existed four times and had already split two against two, on a bug that was
    /// measured rather than imagined: a member returning a tuple opens a bracket before its own name
    /// — <c>private (string Surfaces, string Geometry) Driven()</c> — so a reader taking the first
    /// bracket finds no name at all and the member is invisible to it. Two sweeps went quiet about a
    /// method they were pointed straight at, an hour apart, before it was written down.
    /// </para>
    /// <para>
    /// The last bracket an identifier opens, and nothing past an arrow, which is a body rather than
    /// a signature. Each sweep keeps its own question — this answers only which member a line is in,
    /// which is what WW193 said about the file walk and is the same argument one level down.
    /// </para>
    /// </summary>
    /// <param name="line">The line, already read as code.</param>
    internal static string? Member(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        if (!line.StartsWith("    public ", StringComparison.Ordinal)
            && !line.StartsWith("    private ", StringComparison.Ordinal)
            && !line.StartsWith("    internal ", StringComparison.Ordinal))
            return null;

        var arrow = line.IndexOf("=>", StringComparison.Ordinal);
        var signature = arrow < 0 ? line : line[..arrow];

        var named = "";
        for (var at = 1; at < signature.Length; at++)
        {
            if (signature[at] != '(')
                continue;

            // An assignment before the bracket makes this a field initialised by a call rather than
            // a member declared with parameters. All four copies read
            // `private readonly string root = Temp();` as a member called Temp, and every line after
            // it belonged to a member that does not exist — which is the sort of thing that stays
            // harmless until the line after one of them is the line a sweep is looking for.
            if (signature[..at].Contains('='))
                break;

            var began = at;
            while (began > 0 && (char.IsLetterOrDigit(signature[began - 1]) || signature[began - 1] == '_'))
                began--;

            if (began < at)
                named = signature[began..at];
        }

        return named.Length == 0 ? null : named;
    }

    /// <summary>
    /// Every member of one file, with the lines under it and whether an adopter can call it.
    /// <para>
    /// WW210, and the same argument WW207 made about <see cref="Member" /> one level up. Walking a
    /// file member by member — open on a declaration, collect until the next — was written twice,
    /// and the second copy is the one that has to hold: a sweep whose answer depends on which
    /// file it read first is not a reading, and that is a bug about the walk rather than about
    /// either question. The questions stay apart. This answers only where each member begins and
    /// ends.
    /// </para>
    /// </summary>
    /// <param name="file">The source to read.</param>
    /// <param name="reading">
    /// How to read each line: <see cref="Code" /> by default, and <see cref="Spoken" /> for a sweep
    /// whose subject is inside a string.
    /// <para>
    /// WW212 needed the second and got the first, which is the same trap WW202 named one level up.
    /// A sweep for the flag a member tests is looking for <c>Has("names")</c>, and the reading that
    /// drops strings leaves <c>Has()</c> — so every surface in the fixture read as gated by nothing,
    /// which was a claim about this walk arriving as a claim about the fixture.
    /// </para>
    /// </param>
    internal static IReadOnlyList<SourceMember> Members(string file, Func<string, string>? reading = null)
    {
        ArgumentNullException.ThrowIfNull(file);

        var read = reading ?? Code;
        var owner = Path.GetFileNameWithoutExtension(file);
        var found = new List<SourceMember>();
        var named = "";
        var declaring = "";
        var visible = false;
        var body = new List<string>();

        foreach (var line in File.ReadLines(file).Select(read))
        {
            // WW462. Read before the member, because a declaration closes the one above it: the
            // type a member belongs to is the last one declared over it, and a file declares
            // several. Nothing else changes — `Owner` is still the file, which is what the sweeps
            // that attribute a member to its file already read.
            if (Declares(line) is { } type)
            {
                Close();
                declaring = type;
                continue;
            }

            if (Member(line) is { } next)
            {
                Close();
                named = next;
                visible = line.StartsWith("    public ", StringComparison.Ordinal);
                body.Add(line);
            }
            else if (named.Length > 0)
            {
                body.Add(line);
            }
        }

        Close();
        return new ReadOnlyCollection<SourceMember>(found);

        void Close()
        {
            if (named.Length > 0)
            {
                found.Add(
                    new SourceMember(owner, named, string.Join('\n', body), visible)
                    {
                        Declaring = declaring,
                    });
            }

            named = "";
            visible = false;
            body = [];
        }
    }

    /// <summary>
    /// The type a top-level line declares, of any of the four kinds. WW462.
    /// <para>
    /// Beside <see cref="Owner" /> rather than inside it: that one answers a <c>class</c> and is
    /// read by two sweeps that attribute a member to a class, and widening it would change what
    /// they see. This is the same question asked for the qualified name a call is written with,
    /// where a record counts exactly as much — <c>RegionThroughout</c> is one, and
    /// <c>RegionThroughout.Around(</c> is how every caller spells it.
    /// </para>
    /// </summary>
    /// <param name="line">The line, already read as code.</param>
    private static string? Declares(string line)
    {
        if (!line.StartsWith("public ", StringComparison.Ordinal)
            && !line.StartsWith("internal ", StringComparison.Ordinal))
            return null;

        foreach (var kind in new[] { "class ", "record ", "struct ", "interface " })
        {
            var at = line.IndexOf(kind, StringComparison.Ordinal);
            if (at < 0)
                continue;

            var rest = line[(at + kind.Length)..].Trim();

            // `record struct Foo` names the kind twice, so the word after the first is not the type.
            if (rest.StartsWith("struct ", StringComparison.Ordinal) || rest.StartsWith("class ", StringComparison.Ordinal))
                rest = rest[(rest.IndexOf(' ', StringComparison.Ordinal) + 1)..].Trim();

            var end = rest.IndexOfAny([' ', ':', '(', '{', '<']);
            var name = end < 0 ? rest : rest[..end];
            return name.Length == 0 ? null : name;
        }

        return null;
    }

    /// <summary>
    /// Whether a path is a source somebody wrote rather than a copy a build made. Matched on the
    /// separators either side, so a directory called <c>binding</c> is not mistaken for output.
    /// </summary>
    /// <param name="path">The file.</param>
    internal static bool Written(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var separator = Path.DirectorySeparatorChar;
        return !path.Contains($"{separator}bin{separator}", StringComparison.Ordinal)
            && !path.Contains($"{separator}obj{separator}", StringComparison.Ordinal);
    }

    /// <summary>
    /// Every member of a tree that reaches one of <paramref name="primitives"/>, directly or through
    /// another member, all the way down and across files. WW462.
    /// <para>
    /// The walk and never the question, which is WW193's rule and WW210's one level down: what a
    /// sweep is looking for stays its own, and this answers only what reaches it. Two sweeps asked
    /// the same question of the same sources and only one of them crossed files —
    /// <c>Synthesising</c> did and <c>DeskVerbs</c> did not — so <c>Menu.Enter</c> was a verb that
    /// reaches the foreground in one reading and not in the other. Written once, the two cannot
    /// disagree about that again.
    /// </para>
    /// <para>
    /// An edge is taken on <c>Owner.Member(</c> anywhere, and on a bare <c>Member(</c> only inside
    /// the file that declares it: a call to another type is written with the type on it and a call
    /// inside a file is not. A bare name matched across the whole engine would let any private helper
    /// called <c>Run</c> stand in for <c>Pointer.Run</c>, and the sweep would report verbs that reach
    /// nothing — which is the scoping <c>DeskVerbs</c> kept its walk inside one file for.
    /// </para>
    /// </summary>
    /// <param name="tree">The tree to walk.</param>
    /// <param name="primitives">What a member has to reach, matched in its body.</param>
    /// <param name="except">A file to leave out — the primitives' own declarations, usually.</param>
    internal static IReadOnlyList<SourceReach> Reaching(
        string tree, IEnumerable<string> primitives, string? except = null)
    {
        ArgumentNullException.ThrowIfNull(primitives);

        var marks = primitives.ToList();

        // Grouped and not indexed, because a name in a file can be two overloads. Keeping the last
        // one threw the rest away, so an overload that touched the desk was invisible whenever a
        // quieter one was declared below it — WW210, measured on `TopLevelWindows.OfProcess`.
        var members = SourcesIn(tree, except)
            .SelectMany(one => Members(one))
            .GroupBy(one => one.Named, StringComparer.Ordinal)
            .ToDictionary(
                one => one.Key,
                one => (
                    Owner: one.First().Owner,
                    Body: string.Join('\n', one.Select(each => each.Body)),
                    IsPublic: one.Any(each => each.IsPublic)),
                StringComparer.Ordinal);

        var touching = members
            .Where(one => marks.Any(mark => one.Value.Body.Contains(mark, StringComparison.Ordinal)))
            .Select(one => one.Key)
            .ToHashSet(StringComparer.Ordinal);

        for (var grew = true; grew;)
        {
            grew = false;
            foreach (var one in members.Where(one => !touching.Contains(one.Key)).ToList())
            {
                if (!touching.Any(deep => Calls(one.Value.Body, one.Value.Owner, deep)))
                    continue;

                touching.Add(one.Key);
                grew = true;
            }
        }

        return new ReadOnlyCollection<SourceReach>(
            touching
                .OrderBy(one => one, StringComparer.Ordinal)
                .Select(one => new SourceReach(one, members[one].IsPublic))
                .ToList());
    }

    /// <summary>Whether a body calls <paramref name="named"/>, which is spelled two ways. WW462.</summary>
    /// <param name="body">The calling member's lines, as code.</param>
    /// <param name="owner">The file the calling member is in.</param>
    /// <param name="named">The called member, as <c>Owner.Member</c>.</param>
    private static bool Calls(string body, string owner, string named)
    {
        var dot = named.IndexOf('.', StringComparison.Ordinal);
        var type = named[..dot];
        var member = named[(dot + 1)..];

        return body.Contains($"{type}.{member}(", StringComparison.Ordinal)
            || (string.Equals(type, owner, StringComparison.Ordinal)
                && body.Contains($"{member}(", StringComparison.Ordinal));
    }

    private static readonly Lazy<string> root = new(Walk);

    private static string Walk()
    {
        var walking = new DirectoryInfo(AppContext.BaseDirectory);
        while (walking is not null && !File.Exists(Path.Combine(walking.FullName, Marker)))
            walking = walking.Parent;

        Assert.NotNull(walking);
        return walking.FullName;
    }
}
