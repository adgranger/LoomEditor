// Ported from microsoft/vscode: src/vs/editor/common/languages/nullTokenize.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

/// <summary>Monaco's <c>NullState</c>: a state that equals only itself.</summary>
/// <remarks>Threading: immutable singleton.</remarks>
internal sealed class NullState : IState
{
    /// <summary>The only instance.</summary>
    public static readonly NullState Instance = new();

    private NullState()
    {
    }

    /// <inheritdoc />
    public IState Clone() => this;

    /// <inheritdoc />
    public bool Equals(IState? other) => ReferenceEquals(this, other);
}

/// <summary>Monaco's <c>nullTokenizeEncoded</c> (the string <c>nullTokenize</c> is not ported).</summary>
/// <remarks>Threading: stateless.</remarks>
internal static class NullTokenize
{
    /// <summary><c>nullTokenizeEncoded</c>: one token for the whole line, with the default colors.</summary>
    public static EncodedTokenizationResult NullTokenizeEncoded(int languageId, IState? state)
    {
        uint[] tokens = new uint[2];
        tokens[0] = 0;
        tokens[1] =
            ((uint)languageId << MetadataConsts.LanguageIdOffset)
            | ((uint)StandardTokenType.Other << MetadataConsts.TokenTypeOffset)
            | ((uint)FontStyle.None << MetadataConsts.FontStyleOffset)
            | ((uint)ColorId.DefaultForeground << MetadataConsts.ForegroundOffset)
            | ((uint)ColorId.DefaultBackground << MetadataConsts.BackgroundOffset);

        return new EncodedTokenizationResult(tokens, state ?? NullState.Instance);
    }
}
