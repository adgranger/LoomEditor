// Ported from microsoft/vscode: src/vs/editor/common/encodedTokenAttributes.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

/// <summary>The two fixed language ids (Monaco: <c>LanguageId</c>, an "open ended enum at runtime": ids are plain integers).</summary>
/// <remarks>Threading: constants.</remarks>
internal static class LanguageId
{
    /// <summary>The null language.</summary>
    public const int Null = 0;

    /// <summary>Plain text.</summary>
    public const int PlainText = 1;
}
