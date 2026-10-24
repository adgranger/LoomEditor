// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggest.ts (CompletionOptions, reduced)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Features;

namespace LoomEditor.View.Suggest;

/// <summary>
/// Options of <see cref="SuggestItems.ProvideAsync"/> (Monaco: <c>CompletionOptions</c>).
/// Not ported: <c>snippetSortOrder</c>, <c>kindFilter</c> and the snippet provider.
/// </summary>
/// <param name="ShowDeprecated">Whether items tagged deprecated are kept.</param>
/// <param name="ProviderFilter">When not empty, only these providers are asked.</param>
/// <param name="ProviderItemsToReuse">Providers whose previous items are reused instead of asking them again.</param>
/// <remarks>Threading: immutable record; the collections must not be changed while a request runs.</remarks>
internal sealed record SuggestItemOptions(
    bool ShowDeprecated = true,
    IReadOnlySet<ICompletionItemProvider>? ProviderFilter = null,
    IReadOnlyDictionary<ICompletionItemProvider, IReadOnlyList<SuggestItem>>? ProviderItemsToReuse = null)
{
    /// <summary>Show deprecated items, ask every provider.</summary>
    public static readonly SuggestItemOptions Default = new();
}
