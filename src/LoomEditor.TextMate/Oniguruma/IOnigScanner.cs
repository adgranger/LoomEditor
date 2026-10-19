// Ported from microsoft/vscode-textmate: src/onigLib.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Oniguruma;

/// <summary><c>OnigScanner</c> (the interface): finds the earliest match of a list of patterns.</summary>
/// <remarks>Threading: single-threaded; used and disposed by the thread that created it.</remarks>
internal interface IOnigScanner : IDisposable
{
    /// <summary><c>findNextMatchSync</c> on a string created by the same library.</summary>
    /// <returns>The match, or <see langword="null"/>.</returns>
    IOnigMatch? FindNextMatchSync(IOnigString str, int startPosition, FindOption options = FindOption.None);

    /// <summary><c>findNextMatchSync</c> on a plain string (a temporary <see cref="IOnigString"/>).</summary>
    /// <returns>The match, or <see langword="null"/>.</returns>
    IOnigMatch? FindNextMatchSync(string str, int startPosition, FindOption options = FindOption.None);
}
