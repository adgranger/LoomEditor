// Ported from microsoft/vscode: src/vs/editor/common/languages.ts (CompletionContext)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Languages.Features;

/// <summary>Why a completion request happens.</summary>
/// <remarks>Threading: immutable value, safe to share between threads.</remarks>
/// <param name="TriggerKind">How the request was triggered.</param>
/// <param name="TriggerCharacter">The typed character when <paramref name="TriggerKind"/> is <see cref="CompletionTriggerKind.TriggerCharacter"/>.</param>
public readonly record struct CompletionContext(CompletionTriggerKind TriggerKind, string? TriggerCharacter = null);
