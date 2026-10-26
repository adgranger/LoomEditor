// Ported from microsoft/vscode: src/vs/editor/common/languages.ts (CompletionItemTag)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Languages.Features;

/// <summary>Extra information about a completion item.</summary>
/// <remarks>Threading: enum, safe to share between threads.</remarks>
public enum CompletionItemTag
{
    /// <summary>The item is deprecated.</summary>
    Deprecated = 1,
}
