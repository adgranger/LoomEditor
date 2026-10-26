// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Suggest;

/// <summary>A session ended (Monaco: <c>onDidCancel</c>).</summary>
/// <param name="Retrigger">A new session replaces this one.</param>
/// <remarks>Threading: immutable; raised on the UI thread.</remarks>
internal sealed class SuggestCancelEventArgs(bool Retrigger) : EventArgs
{
    /// <summary>A new session replaces this one.</summary>
    public bool Retrigger { get; } = Retrigger;
}

/// <summary>A session started (Monaco: <c>onDidTrigger</c>).</summary>
/// <param name="Auto">Started by typing.</param>
/// <param name="Shy">The list is not shown right away.</param>
/// <param name="Position">The cursor position at the start.</param>
/// <remarks>Threading: immutable; raised on the UI thread.</remarks>
internal sealed class SuggestTriggerEventArgs(bool Auto, bool Shy, Position Position) : EventArgs
{
    /// <summary>Started by typing.</summary>
    public bool Auto { get; } = Auto;

    /// <summary>The list is not shown right away.</summary>
    public bool Shy { get; } = Shy;

    /// <summary>The cursor position at the start.</summary>
    public Position Position { get; } = Position;
}

/// <summary>The list of the session changed (Monaco: <c>onDidSuggest</c>).</summary>
/// <param name="CompletionModel">The filtered list.</param>
/// <param name="TriggerOptions">The options of the context that produced this event.</param>
/// <param name="IsFrozen">The list is the previous one: nothing matches the typed text but the session was manual.</param>
/// <remarks>Threading: immutable; raised on the UI thread.</remarks>
internal sealed class SuggestEventArgs(CompletionModel CompletionModel, SuggestTriggerOptions TriggerOptions, bool IsFrozen) : EventArgs
{
    /// <summary>The filtered list.</summary>
    public CompletionModel CompletionModel { get; } = CompletionModel;

    /// <summary>The options of the context that produced this event.</summary>
    public SuggestTriggerOptions TriggerOptions { get; } = TriggerOptions;

    /// <summary>The list is the previous one: nothing matches the typed text but the session was manual.</summary>
    public bool IsFrozen { get; } = IsFrozen;
}
