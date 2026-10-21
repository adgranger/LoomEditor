namespace LoomEditor.View.Features.BracketMatching;

/// <summary>Which brackets <see cref="BracketMatchingFeature"/> highlights (Monaco: the <c>matchBrackets</c> option).</summary>
/// <remarks>Threading: an enum; thread-safe.</remarks>
internal enum MatchBracketsMode
{
    /// <summary>The bracket next to the cursor, or else the pair that encloses it.</summary>
    Always,

    /// <summary>Only the bracket next to the cursor.</summary>
    Near,

    /// <summary>Nothing.</summary>
    Never,
}
