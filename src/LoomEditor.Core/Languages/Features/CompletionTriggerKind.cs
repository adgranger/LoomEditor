// Ported from microsoft/vscode: src/vs/editor/common/languages.ts (CompletionTriggerKind)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Languages.Features;

/// <summary>How a completion request was triggered.</summary>
/// <remarks>Threading: enum, safe to share between threads.</remarks>
public enum CompletionTriggerKind
{
    /// <summary>Explicit request (Ctrl+Space) or typing a word character.</summary>
    Invoke = 0,
    /// <summary>One of the provider's trigger characters was typed.</summary>
    TriggerCharacter = 1,
    /// <summary>The previous list was incomplete and more typing requires a new one.</summary>
    TriggerForIncompleteCompletions = 2,
}
