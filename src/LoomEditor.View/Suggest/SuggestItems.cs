// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggest.ts (provideSuggestionItems)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Suggest;

/// <summary>
/// Asks the completion providers of a language for their items (Monaco: <c>provideSuggestionItems</c>).
/// Not ported: snippets, clipboard, durations, the kind filter.
/// </summary>
/// <remarks>Threading: <see cref="ProvideAsync"/> is called on the UI thread; the returned task completes on any thread.</remarks>
internal static class SuggestItems
{
    /// <summary>
    /// Providers are asked group by group (most specific first); the first group that returns items wins.
    /// The model is read synchronously before the first provider call. A cancelled token gives a canceled task.
    /// </summary>
    public static Task<IReadOnlyList<SuggestItem>> ProvideAsync(
        LanguageFeatureRegistry<ICompletionItemProvider> registry,
        TextModel model,
        string languageId,
        Position position,
        SuggestItemOptions options,
        CompletionContext context,
        CancellationToken token)
    {
        position = model.ValidatePosition(position);
        WordAtPosition? word = model.GetWordAtPosition(position);
        Range defaultReplace = word is { } w
            ? new Range(position.LineNumber, w.StartColumn, position.LineNumber, w.EndColumn)
            : Range.Collapsed(position);
        var defaultRange = new CompletionRange(defaultReplace.WithEnd(position), defaultReplace);

        IReadOnlyList<IReadOnlyList<ICompletionItemProvider>> groups = registry.OrderedGroups(languageId);
        var items = new List<SuggestItem>();
        var tcs = new TaskCompletionSource<IReadOnlyList<SuggestItem>>(TaskCreationOptions.RunContinuationsAsynchronously);

        void Finish()
        {
            if (token.IsCancellationRequested)
            {
                tcs.TrySetCanceled(token);
                return;
            }

            // OrderBy is stable, like Monaco's Array.prototype.sort.
            tcs.TrySetResult(items.OrderBy(x => x, SuggestItemComparer.Default).ToList());
        }

        void RunGroup(int index)
        {
            if (token.IsCancellationRequested || index >= groups.Count)
            {
                Finish();
                return;
            }

            var didAddResult = false;
            var requests = new List<(ICompletionItemProvider Provider, Task<CompletionList?> Task)>();
            foreach (ICompletionItemProvider provider in groups[index])
            {
                if (options.ProviderItemsToReuse is { } reuse && reuse.TryGetValue(provider, out IReadOnlyList<SuggestItem>? reused))
                {
                    items.AddRange(reused);
                    didAddResult |= reused.Count > 0;
                    continue;
                }

                if (options.ProviderFilter is { Count: > 0 } filter && !filter.Contains(provider))
                {
                    continue;
                }

                Task<CompletionList?> task;
                try
                {
                    task = provider.ProvideCompletionItemsAsync(model, position, context, token);
                }
                catch (Exception e)
                {
                    task = Task.FromException<CompletionList?>(e);
                }

                requests.Add((provider, task));
            }

            Task.WhenAll(requests.Select(r => r.Task)).ContinueWith(
                _ =>
                {
                    try
                    {
                        foreach ((ICompletionItemProvider provider, Task<CompletionList?> task) in requests)
                        {
                            // A faulted or canceled provider is ignored (Monaco: onUnexpectedExternalError).
                            if (!task.IsCompletedSuccessfully || task.Result is not { } list)
                            {
                                continue;
                            }

                            foreach (CompletionItem suggestion in list.Suggestions)
                            {
                                if (!options.ShowDeprecated && suggestion.Tags?.Contains(CompletionItemTag.Deprecated) == true)
                                {
                                    continue;
                                }

                                suggestion.Range ??= defaultRange;
                                suggestion.SortText = string.IsNullOrEmpty(suggestion.SortText) ? suggestion.Label : suggestion.SortText;
                                items.Add(new SuggestItem(position, suggestion, list, provider));
                                didAddResult = true;
                            }
                        }

                        if (didAddResult || token.IsCancellationRequested)
                        {
                            Finish();
                        }
                        else
                        {
                            RunGroup(index + 1);
                        }
                    }
                    catch (Exception e)
                    {
                        tcs.TrySetException(e);
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
        }

        RunGroup(0);
        return tcs.Task;
    }
}
