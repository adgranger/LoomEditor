// Ported from microsoft/vscode: src/vs/editor/common/tokens/lineTokens.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

/// <summary>
/// The tokens of one line, as the view needs them (Monaco: <c>IViewLineTokens</c>). Not ported: <c>getClassName</c> and
/// <c>getInlineStyle</c> (CSS); <see cref="GetMetadata"/>, <see cref="GetForeground"/> and <see cref="GetPresentation"/>
/// are what the renderer uses instead.
/// </summary>
/// <remarks>Threading: implementations are immutable.</remarks>
internal interface IViewLineTokens
{
    /// <summary>The codec used to decode language ids.</summary>
    ILanguageIdCodec LanguageIdCodec { get; }

    /// <summary><c>equals</c>.</summary>
    bool Equals(IViewLineTokens other);

    /// <summary><c>getCount</c>.</summary>
    int GetCount();

    /// <summary><c>getStandardTokenType</c>.</summary>
    StandardTokenType GetStandardTokenType(int tokenIndex);

    /// <summary><c>getForeground</c>: a color id.</summary>
    int GetForeground(int tokenIndex);

    /// <summary><c>getEndOffset</c>.</summary>
    int GetEndOffset(int tokenIndex);

    /// <summary><c>getPresentation</c>.</summary>
    TokenPresentation GetPresentation(int tokenIndex);

    /// <summary><c>findTokenIndexAtOffset</c>.</summary>
    int FindTokenIndexAtOffset(int offset);

    /// <summary><c>getLineContent</c>.</summary>
    string GetLineContent();

    /// <summary><c>getMetadata</c>.</summary>
    uint GetMetadata(int tokenIndex);

    /// <summary><c>getLanguageId</c>.</summary>
    string GetLanguageId(int tokenIndex);

    /// <summary><c>getTokenText</c>.</summary>
    string GetTokenText(int tokenIndex);

    /// <summary><c>forEach</c>.</summary>
    void ForEach(Action<int> callback);
}
