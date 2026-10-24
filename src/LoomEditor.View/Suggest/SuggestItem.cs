// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggest.ts (CompletionItem)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Filters;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Suggest;

/// <summary>
/// One completion proposal with its validated edit ranges, lower-cased texts and the scoring state of the completion model
/// (Monaco: <c>CompletionItem</c> of <c>suggest.ts</c>, renamed so it does not clash with the language feature type).
/// The completion must have a <see cref="CompletionItem.Range"/> (the caller fills the default one).
/// </summary>
/// <remarks>Threading: UI thread only (<see cref="BeginResolve"/> may be called from there and its cancellation callback may run anywhere).</remarks>
internal sealed class SuggestItem
{
    private readonly object _resolveLock = new();
    private Task<CompletionItem?>? _resolve;

    /// <summary>Creates the item of <paramref name="completion"/> returned by <paramref name="provider"/> at <paramref name="position"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="completion"/> has no range.</exception>
    public SuggestItem(Position position, CompletionItem completion, CompletionList container, ICompletionItemProvider provider)
    {
        ArgumentNullException.ThrowIfNull(completion);
        if (completion.Range is not { } range)
        {
            throw new ArgumentException("The completion item must have a range.", nameof(completion));
        }

        Position = position;
        Completion = completion;
        Container = container;
        Provider = provider;

        Range insert = range.Insert;
        Range replace = range.Replace;
        EditStart = new Position(insert.StartLineNumber, insert.StartColumn);
        EditInsertEnd = new Position(insert.EndLineNumber, insert.EndColumn);
        EditReplaceEnd = new Position(replace.EndLineNumber, replace.EndColumn);

        TextLabel = completion.Label;
        LabelLow = TextLabel.ToLowerInvariant();
        SortTextLow = string.IsNullOrEmpty(completion.SortText) ? null : completion.SortText.ToLowerInvariant();
        FilterTextLow = string.IsNullOrEmpty(completion.FilterText) ? null : completion.FilterText.ToLowerInvariant();

        IsInvalid = string.IsNullOrEmpty(TextLabel)
            || insert.StartLineNumber != insert.EndLineNumber
            || replace.StartLineNumber != replace.EndLineNumber
            || insert.StartLineNumber != position.LineNumber
            || replace.StartLineNumber != position.LineNumber
            || insert.StartColumn != replace.StartColumn;

        IsResolved = !provider.CanResolve;
    }

    /// <summary>The position the completion was requested at.</summary>
    public Position Position { get; }

    /// <summary>The provider's item.</summary>
    public CompletionItem Completion { get; }

    /// <summary>The list <see cref="Completion"/> came from.</summary>
    public CompletionList Container { get; }

    /// <summary>The provider that returned the item.</summary>
    public ICompletionItemProvider Provider { get; }

    /// <summary>Start of the insert (and replace) range.</summary>
    public Position EditStart { get; }

    /// <summary>End of the insert range.</summary>
    public Position EditInsertEnd { get; }

    /// <summary>End of the replace range.</summary>
    public Position EditReplaceEnd { get; }

    /// <summary>The label.</summary>
    public string TextLabel { get; }

    /// <summary>The lower-cased label.</summary>
    public string LabelLow { get; }

    /// <summary>The lower-cased sort text; null when there is none or it is empty.</summary>
    public string? SortTextLow { get; }

    /// <summary>The lower-cased filter text; null when there is none or it is empty.</summary>
    public string? FilterTextLow { get; }

    /// <summary>Whether the ranges or the label make the item unusable.</summary>
    public bool IsInvalid { get; }

    /// <summary>Whether the provider cannot resolve the item or it was resolved already.</summary>
    public bool IsResolved { get; private set; }

    /// <summary>The fuzzy score (Monaco: <c>score</c>); <see cref="FuzzyScore.Default"/> is shared and must not be mutated in place.</summary>
    public int[] Score { get; set; } = FuzzyScore.Default;

    /// <summary>The distance to the cursor (Monaco: <c>distance</c>).</summary>
    public int Distance { get; set; }

    /// <summary>The index in the unfiltered list (Monaco: <c>idx</c>).</summary>
    public int Idx { get; set; }

    /// <summary>The word the item was scored against (Monaco: <c>word</c>).</summary>
    public string? Word { get; set; }

    /// <summary>
    /// Starts resolving the item once and returns the task; null when it is already resolved. A second caller gets the same task.
    /// A cancelled <paramref name="token"/> (before the task finished) or a faulted or cancelled task clears the cache so a later call restarts.
    /// Merge the result with <see cref="CompleteResolve"/> on the UI thread.
    /// </summary>
    public Task<CompletionItem?>? BeginResolve(CancellationToken token)
    {
        if (IsResolved)
        {
            return null;
        }

        lock (_resolveLock)
        {
            if (_resolve is not null)
            {
                return _resolve;
            }

            Task<CompletionItem?> task;
            try
            {
                task = Provider.ResolveCompletionItemAsync(Completion, token);
            }
            catch (Exception e)
            {
                task = Task.FromException<CompletionItem?>(e);
            }

            _resolve = task;
            CancellationTokenRegistration registration = token.Register(() => Clear(task, onlyIfNotSucceeded: true));
            task.ContinueWith(
                t =>
                {
                    registration.Dispose();
                    if (!t.IsCompletedSuccessfully)
                    {
                        Clear(t, onlyIfNotSucceeded: false);
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return task;
        }
    }

    /// <summary>Merges the resolved item into <see cref="Completion"/> and marks the item resolved.</summary>
    public void CompleteResolve(CompletionItem? resolved)
    {
        if (resolved is not null)
        {
            Completion.MergeFrom(resolved);
        }

        IsResolved = true;
        lock (_resolveLock)
        {
            _resolve = null;
        }
    }

    private void Clear(Task<CompletionItem?> task, bool onlyIfNotSucceeded)
    {
        lock (_resolveLock)
        {
            if (ReferenceEquals(_resolve, task) && !(onlyIfNotSucceeded && task.IsCompletedSuccessfully))
            {
                _resolve = null;
            }
        }
    }
}
