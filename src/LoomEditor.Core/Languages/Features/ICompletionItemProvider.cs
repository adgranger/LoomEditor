// Ported from microsoft/vscode: src/vs/editor/common/languages.ts (CompletionItemProvider)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Languages.Features;

/// <summary>Provides completion items for a model.</summary>
/// <remarks>
/// Threading: called on the UI thread; the returned <see cref="Task"/> may complete on any thread. A result for a cancelled
/// token is ignored by the caller. The provider must not touch the model after the task completed.
/// </remarks>
public interface ICompletionItemProvider
{
    /// <summary>Characters that trigger a request when typed; null for none.</summary>
    IReadOnlyList<string>? TriggerCharacters { get; }

    /// <summary>Provides the items at <paramref name="position"/>; null for none.</summary>
    Task<CompletionList?> ProvideCompletionItemsAsync(TextModel model, Position position, CompletionContext context, CancellationToken token);

    /// <summary>True when <see cref="ResolveCompletionItemAsync"/> does something.</summary>
    bool CanResolve => false;

    /// <summary>Fills in the lazily computed members of <paramref name="item"/>; the result is merged with <see cref="CompletionItem.MergeFrom"/>.</summary>
    Task<CompletionItem?> ResolveCompletionItemAsync(CompletionItem item, CancellationToken token) => Task.FromResult<CompletionItem?>(null);
}
