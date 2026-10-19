// Ported from microsoft/vscode-textmate: src/onigLib.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Oniguruma;

/// <summary>
/// <c>OnigString</c> (the interface): a subject string. <c>disposeOnigString</c> is <see cref="IDisposable.Dispose"/>.
/// </summary>
/// <remarks>Threading: immutable.</remarks>
internal interface IOnigString : IDisposable
{
    /// <summary><c>content</c>.</summary>
    string Content { get; }
}
