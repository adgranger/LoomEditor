// Ported from microsoft/vscode: src/vs/editor/common/core/wordHelper.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Text;

/// <summary>Word inside a model (Monaco: <c>IWordAtPosition</c>).</summary>
/// <param name="Word">The word.</param>
/// <param name="StartColumn">The column where the word starts.</param>
/// <param name="EndColumn">The column where the word ends.</param>
internal readonly record struct WordAtPosition(string Word, int StartColumn, int EndColumn);

/// <summary>Constants of Monaco's <c>wordHelper.ts</c> (the word regular expressions are ported in phase 5).</summary>
/// <remarks>Threading: constants only; thread-safe.</remarks>
internal static class WordHelper
{
    /// <summary>The default value of the <c>wordSeparators</c> editor option (Monaco: <c>USUAL_WORD_SEPARATORS</c>).</summary>
    public const string UsualWordSeparators = "`~!@#$%^&*()-=+[{]}\\|;:'\",.<>/?";
}
