// Ported from microsoft/vscode: src/vs/editor/contrib/gotoSymbol/browser/goToSymbol.ts (getLocationLinks, getDefinitionsAtPosition)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Features.GotoDefinition;

/// <summary>
/// Asks the definition providers of a language for their links. Not ported: <c>shouldIncludeLocationLink</c> (there are no schemes).
/// </summary>
/// <remarks>Threading: called on the UI thread; the returned task completes on any thread.</remarks>
internal static class DefinitionProviders
{
    /// <summary>
    /// All the providers are asked at once; a faulted, cancelled or throwing provider and a null list give nothing.
    /// The result is the concatenation, in provider order, of the non-null links.
    /// </summary>
    public static Task<IReadOnlyList<LocationLink>> GetDefinitionsAtPosition(
        LanguageFeatureRegistry<IDefinitionProvider> registry,
        TextModel model,
        string languageId,
        Position position,
        CancellationToken token)
    {
        IReadOnlyList<IDefinitionProvider> providers = registry.Ordered(languageId);
        if (providers.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<LocationLink>>([]);
        }

        var requests = new List<Task<IReadOnlyList<LocationLink>?>>(providers.Count);
        foreach (IDefinitionProvider provider in providers)
        {
            Task<IReadOnlyList<LocationLink>?> request;
            try
            {
                request = provider.ProvideDefinitionAsync(model, position, token);
            }
            catch (Exception)
            {
                request = Task.FromResult<IReadOnlyList<LocationLink>?>(null);
            }

            requests.Add(request.ContinueWith(
                static t => t.IsCompletedSuccessfully ? t.Result : null,
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default));
        }

        return Task.WhenAll(requests).ContinueWith(
            static t => (IReadOnlyList<LocationLink>)[.. t.Result.Where(list => list is not null).SelectMany(list => list!).Where(link => link is not null)],
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);
    }
}
