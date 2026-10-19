namespace LoomEditor.TextMate.Oniguruma;

/// <summary>
/// One capture of a match, in UTF-16 code units. An unmatched group is <c>(-1, -1)</c>, as in vscode-oniguruma.
/// </summary>
/// <param name="Start">Start offset (<c>-1</c> for an unmatched group).</param>
/// <param name="End">End offset (<c>-1</c> for an unmatched group).</param>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct OnigCaptureIndex(int Start, int End)
{
    /// <summary>Length of the capture (<c>0</c> for an unmatched group).</summary>
    public int Length => End - Start;
}

/// <summary>The result of a successful search: group 0 is the whole match, then one entry per group.</summary>
/// <remarks>Threading: immutable after creation.</remarks>
internal sealed class OnigMatch
{
    /// <summary>Creates a match.</summary>
    public OnigMatch(OnigCaptureIndex[] captureIndices)
    {
        CaptureIndices = captureIndices;
    }

    /// <summary>The captures, group 0 first.</summary>
    public OnigCaptureIndex[] CaptureIndices { get; }
}
