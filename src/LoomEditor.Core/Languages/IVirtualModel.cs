// Ported from microsoft/vscode: src/vs/editor/common/languages/autoIndent.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Languages;

/// <summary>
/// The part of a model read by the auto-indentation (Monaco: <c>IVirtualModel</c>; its <c>tokenization</c>
/// sub-object is inlined).
/// </summary>
/// <remarks>Threading: an implementation is used from the thread of the editor.</remarks>
internal interface IVirtualModel
{
    /// <summary><c>tokenization.getLineTokens</c>.</summary>
    IViewLineTokens GetLineTokens(int lineNumber);

    /// <summary><c>tokenization.getLanguageId</c>.</summary>
    string GetLanguageId();

    /// <summary><c>tokenization.getLanguageIdAtPosition</c>.</summary>
    string GetLanguageIdAtPosition(int lineNumber, int column);

    /// <summary><c>tokenization.forceTokenization</c> (optional in Monaco: an implementation without it does nothing).</summary>
    void ForceTokenization(int lineNumber);

    /// <summary><c>getLineContent</c>.</summary>
    string GetLineContent(int lineNumber);
}
