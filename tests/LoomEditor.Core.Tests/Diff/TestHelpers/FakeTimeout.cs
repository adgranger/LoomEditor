using LoomEditor.Core.Diff.Algorithms;

namespace LoomEditor.Core.Tests.Diff.TestHelpers;

/// <summary>A timeout that turns invalid after a given number of <see cref="IsValid"/> calls.</summary>
internal sealed class FakeTimeout(int validCalls) : ITimeout
{
    private int _calls;

    public bool IsValid() => _calls++ < validCalls;
}
