// Ported from microsoft/vscode-textmate: src/debug.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Grammar;

/// <summary><c>DebugFlags</c> and <c>UseOnigurumaFindOptions</c> of vscode-textmate.</summary>
/// <remarks>
/// <para>
/// Upstream turns debug mode on with the <c>VSCODE_TEXTMATE_DEBUG</c> environment variable; in LoomEditor it is always
/// off (no location metadata, ADR 0008 decision 3), so the debug-only code paths are not ported.
/// </para>
/// <para>Threading: constants.</para>
/// </remarks>
internal static class DebugFlags
{
    /// <summary><c>DebugFlags.InDebugMode</c>: always <see langword="false"/>.</summary>
    public const bool InDebugMode = false;

    /// <summary>
    /// <c>UseOnigurumaFindOptions</c>: <see langword="false"/>, so the scanner is always called with
    /// <see cref="Oniguruma.FindOption.None"/> and anchors are resolved in the regex sources instead.
    /// </summary>
    public const bool UseOnigurumaFindOptions = false;
}
