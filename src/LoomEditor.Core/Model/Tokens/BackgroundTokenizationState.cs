// Ported from microsoft/vscode: src/vs/editor/common/tokenizationTextModelPart.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Tokens;

/// <summary>The state of the background tokenization (Monaco: <c>BackgroundTokenizationState</c>).</summary>
/// <remarks>Threading: a value type.</remarks>
internal enum BackgroundTokenizationState
{
    /// <summary>Some lines still have to be tokenized.</summary>
    InProgress = 1,

    /// <summary>A full tokenization was done; it never goes back to <see cref="InProgress"/>.</summary>
    Completed = 2,
}
