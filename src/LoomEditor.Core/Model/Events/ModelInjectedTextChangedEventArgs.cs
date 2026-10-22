// Ported from microsoft/vscode: src/vs/editor/common/textModelEvents.ts (ModelInjectedTextChangedEvent)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Events;

/// <summary>Describes a change in injected text (Monaco: <c>ModelInjectedTextChangedEvent</c>).</summary>
/// <param name="changes">One <see cref="ModelRawLineChanged"/> per affected line.</param>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed class ModelInjectedTextChangedEventArgs(IReadOnlyList<ModelRawLineChanged> changes) : EventArgs
{
    public IReadOnlyList<ModelRawLineChanged> Changes { get; } = changes;
}
