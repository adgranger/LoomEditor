// Ported from microsoft/vscode-textmate: src/onigLib.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Oniguruma;

/// <summary>
/// <c>IOnigCaptureIndex</c>: one capture of a match, in UTF-16 code units. An unmatched group is <c>(-1, -1)</c>.
/// </summary>
/// <param name="Start">Start offset (<c>-1</c> for an unmatched group).</param>
/// <param name="End">End offset (<c>-1</c> for an unmatched group).</param>
/// <remarks>
/// <para>
/// vscode-oniguruma reads an unmatched group's <c>-1</c> as an unsigned value and maps it to the string length (or
/// keeps <c>4294967295</c>); either way its <c>length</c> is 0, as here.
/// </para>
/// <para>Threading: immutable.</para>
/// </remarks>
internal readonly record struct OnigCaptureIndex(int Start, int End)
{
    /// <summary><c>length</c>: <c>End - Start</c> (<c>0</c> for an unmatched group).</summary>
    public int Length => End - Start;
}

/// <summary><c>IOnigMatch</c>: the pattern that matched and its captures.</summary>
/// <remarks>Threading: immutable.</remarks>
internal interface IOnigMatch
{
    /// <summary>Index of the pattern that matched, in the scanner's list.</summary>
    int Index { get; }

    /// <summary>The captures, group 0 (the whole match) first.</summary>
    IReadOnlyList<OnigCaptureIndex> CaptureIndices { get; }
}

/// <summary>The <see cref="IOnigMatch"/> returned by <see cref="OnigScanner"/>.</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed class OnigMatch : IOnigMatch
{
    /// <summary>Creates a match.</summary>
    public OnigMatch(int index, IReadOnlyList<OnigCaptureIndex> captureIndices)
    {
        Index = index;
        CaptureIndices = captureIndices;
    }

    /// <inheritdoc />
    public int Index { get; }

    /// <inheritdoc />
    public IReadOnlyList<OnigCaptureIndex> CaptureIndices { get; }
}
