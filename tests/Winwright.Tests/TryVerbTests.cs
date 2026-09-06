using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Xunit;

namespace Winwright.Tests;

/// <summary>
/// WW390. A verb that answers a bool and hands the value back through an <c>out</c> has to say when
/// that value is there, or every caller writes a bang.
/// <para>
/// Two of them exist and both got their annotation by somebody noticing the other. WW364 annotated
/// <c>Locator.TryParse</c> because four bangs had accumulated in <c>StepDeclaration</c> and WW351
/// had just added one; WW377 annotated <c>Chord.TryParse</c> because WW364's shipping made the
/// omission visible one verb over. Each repair cost a task, and the argument for it arrived as a
/// bang somebody had already written, months later, in a file about something else.
/// </para>
/// <para>
/// Derived and not curated, which is what this project reaches for whenever two lists could drift:
/// the population is every exported method that answers a <c>bool</c> and writes to a nullable
/// <c>out</c>, found by reflection, so a third one written tomorrow is in it the moment it compiles.
/// A method that means something else says so in <see cref="MeansSomethingElse" />, which is the
/// shape <c>MayAnswerYesOrNo</c> already has for a neighbouring rule.
/// </para>
/// <para>
/// Two assemblies and not four, and the bound is stated rather than assumed — a catalogue covering
/// less than a reader thinks it does is worse than none. The engine and the in-app half are what
/// somebody else writes code against, so a missing annotation there is a bang in a repository this
/// project does not own. The fixture and the tools are programs: their methods have one caller each,
/// in the same file, and a bang there is a line its author can see.
/// </para>
/// </summary>
public sealed class TryVerbTests
{
    /// <summary>
    /// The halves a consumer writes code against. WW390, and the whole of what this rule covers.
    /// <para>
    /// WW408: pointed at rather than named. The paragraph above argued this bound from scratch
    /// because there was nowhere to argue it once, and the sweep beside this one reached a different
    /// answer for the same question. <c>Checkout.Shipped</c> is the answer now, and a case holds it
    /// against the project files — so a rule meaning something narrower says so against a list
    /// rather than in place of one.
    /// </para>
    /// </summary>
    private static IReadOnlyList<Assembly> Shipped() => Checkout.Shipped;

    /// <summary>
    /// Every exported verb answering a bool that hands something back through an <c>out</c>.
    /// <para>
    /// Declared only, so a method is judged where it is written rather than once per type that
    /// inherits it, and the compiler's own operators are left out: a record's <c>==</c> answers a
    /// bool and is nobody's verb.
    /// </para>
    /// </summary>
    private static IReadOnlyList<MethodInfo> Answering() =>
    [
        .. Shipped()
            .SelectMany(one => one.GetExportedTypes())
            .SelectMany(one => one.GetMethods(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(one => one.ReturnType == typeof(bool) && !one.IsSpecialName)
            .Where(one => one.GetParameters().Any(other => other.IsOut)),
    ];

    /// <summary>
    /// Verbs whose nullable <c>out</c> is not a value the answer promises, with why.
    /// <para>
    /// Empty, and that is the reading rather than an absence: every verb of this shape in these two
    /// assemblies promises its value when it answers true, which is what makes the rule derivable at
    /// all. The list is kept so the day one does not is the day somebody writes down why — the
    /// argument <c>SourceSweep</c>'s own empty bucket makes.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, string> MeansSomethingElse = new(StringComparer.Ordinal);

    [Fact]
    public void Every_verb_that_answers_a_bool_says_when_its_out_is_there()
    {
        // The rule WW364 and WW377 each arrived at separately. A nullable `out` with nothing said
        // about it is a compiler that cannot know, so every caller writes `!` — and a bang is a
        // claim nobody checked, spelled in the file that consumes the verb rather than in the one
        // that decides it.
        var nullability = new NullabilityInfoContext();
        var silent = new List<string>();

        foreach (var verb in Answering())
        {
            foreach (var handed in verb.GetParameters().Where(one => one.IsOut))
            {
                // Declared nullable and not merely a reference type: `out Locator locator` promises
                // one already, and asking it to say so as well would be a rule about nothing.
                // NullabilityInfoContext reads what was written rather than what the flow attributes
                // add, so an annotated parameter still reads as nullable here — which is the whole
                // reason the attribute is what this looks for.
                var about = nullability.Create(handed);
                if (about.ReadState != NullabilityState.Nullable)
                    continue;

                if (handed.GetCustomAttribute<NotNullWhenAttribute>() is not null
                    || handed.GetCustomAttribute<MaybeNullWhenAttribute>() is not null)
                {
                    continue;
                }

                var named = $"{verb.DeclaringType?.Name}.{verb.Name}";
                if (!MeansSomethingElse.ContainsKey(named))
                    silent.Add($"{named}(out {handed.Name})");
            }
        }

        Assert.True(
            silent.Count == 0,
            $"{string.Join(", ", silent)} answer(s) a bool and hand back something nullable without "
                + "saying when it is there, so every caller spells a bang: annotate with "
                + "[NotNullWhen] or say why it means something else");
    }

    [Fact]
    public void The_verbs_were_read_off_the_assemblies_and_not_off_a_list_kept_here()
    {
        // The control, and this rule needs one more than most: its population is two methods, so it
        // passes the day it is written and every day after — including the day the query stops
        // matching. A sweep that found nothing would agree with itself forever.
        var found = Answering().Select(one => $"{one.DeclaringType?.Name}.{one.Name}").ToList();

        Assert.Contains("Locator.TryParse", found);
        Assert.Contains("Chord.TryParse", found);

        // And what the list excuses is a verb that exists, for the reason every catalogue here says
        // so: an entry outliving what it excused is an excuse for nothing.
        Assert.All(MeansSomethingElse.Keys, one => Assert.Contains(one, found));

        // The other half of the control, and the one that matters more: the rule above passes on
        // these two because of their attributes and not because the reading cannot see them. A
        // context that read an annotated parameter as not-null would skip every verb in the
        // population, and the sweep would agree with itself forever while a third went unannotated.
        var nullability = new NullabilityInfoContext();
        var parsed = Assert.Single(Answering(), one => $"{one.DeclaringType?.Name}.{one.Name}" == "Locator.TryParse");
        var handed = Assert.Single(parsed.GetParameters(), one => one.Name == "locator");

        Assert.Equal(NullabilityState.Nullable, nullability.Create(handed).ReadState);
        Assert.NotNull(handed.GetCustomAttribute<NotNullWhenAttribute>());
    }
}
