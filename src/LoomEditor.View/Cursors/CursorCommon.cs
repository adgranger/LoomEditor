// Ported from microsoft/vscode: src/vs/editor/common/cursorCommon.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Cursors;

/// <summary>Small helpers shared by the cursor operations (Monaco: the free functions of <c>cursorCommon.ts</c>).</summary>
/// <remarks>Threading: stateless; thread-safe.</remarks>
internal static class CursorCommon
{
    /// <summary>True for <c>'</c>, <c>"</c> and <c>`</c> (Monaco: <c>isQuote</c>).</summary>
    public static bool IsQuote(char ch) => ch is '\'' or '"' or '`';

    /// <summary>True for <c>'</c>, <c>"</c> and <c>`</c> (Monaco: <c>isQuote</c>, string overload).</summary>
    public static bool IsQuote(string ch) => ch is "'" or "\"" or "`";
}
