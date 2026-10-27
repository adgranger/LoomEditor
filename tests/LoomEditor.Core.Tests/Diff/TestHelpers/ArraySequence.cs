using LoomEditor.Core.Diff.Algorithms;

namespace LoomEditor.Core.Tests.Diff.TestHelpers;

/// <summary>An <see cref="ISequence"/> over the chars of a string or over an int array.</summary>
internal sealed class ArraySequence(int[] elements) : ISequence
{
    public ArraySequence(string text)
        : this([.. text.Select(c => (int)c)])
    {
    }

    public int Length => elements.Length;

    public int GetElement(int offset) => elements[offset];

    public int GetBoundaryScore(int length) => 0;

    public bool IsStronglyEqual(int offset1, int offset2) => elements[offset1] == elements[offset2];
}
