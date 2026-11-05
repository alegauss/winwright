using Xunit;

namespace Winwright.Tests;

/// <summary>
/// Every test that creates a window creates it in <em>this</em> process, so two of them running at
/// once would each see the other's windows and read them as their own subject. Sharing a
/// collection is xUnit's way of saying they run one at a time.
/// </summary>
[CollectionDefinition(Serial, DisableParallelization = true)]
public sealed class WindowFixture
{
    /// <summary>The collection name, so the attribute is never spelled twice.</summary>
    public const string Serial = "windows in this process";
}
