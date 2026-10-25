// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestController.ts (the model events, triggerSuggest)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Features;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Suggest;

/// <summary>
/// Connects the suggest model, the list state and the presenter: opens the list when the model suggests, closes it when the session ends or the
/// editor loses focus, moves the selection, resolves the selected item and accepts an item (see <c>Accept</c>).
/// Not ported: the loading state, <c>selectionMode</c>, memories, alternatives.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class SuggestController : IDisposable
{
    private readonly IEditorContext _context;
    private readonly ViewModel? _viewModel;
    private readonly List<Action> _pendingAsyncEdits = [];
    private readonly CommitCharacterController _commitCharacters;
    private CancellationTokenSource? _resolveToken;
    private bool _disposed;

    /// <summary>Starts listening; a context that is not an <see cref="EditorContext"/> never shows anything.</summary>
    public SuggestController(IEditorContext context, SuggestModelOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        _context = context;
        _viewModel = context.GetViewModel();
        Model = new SuggestModel(context, options);
        List = new SuggestListState();
        _commitCharacters = new CommitCharacterController(_viewModel, this, options.AcceptSuggestionOnCommitCharacter);

        Model.DidSuggest += OnDidSuggest;
        Model.DidCancel += OnDidCancel;
        List.SelectionChanged += OnSelectionChanged;
        if (_viewModel is not null)
        {
            _viewModel.FocusChanged += OnFocusChanged;
        }
    }

    /// <summary>The completion session.</summary>
    public SuggestModel Model { get; }

    /// <summary>What the list shows.</summary>
    public SuggestListState List { get; }

    /// <summary>Whether the list is on screen with items.</summary>
    public bool IsVisible => _context.GetSuggestPresenter().IsVisible && !List.IsEmpty;

    /// <summary>Stops listening, ends the session and disposes the model.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Model.DidSuggest -= OnDidSuggest;
        Model.DidCancel -= OnDidCancel;
        List.SelectionChanged -= OnSelectionChanged;
        if (_viewModel is not null)
        {
            _viewModel.FocusChanged -= OnFocusChanged;
        }

        CancelResolve();
        foreach (Action cancel in _pendingAsyncEdits.ToArray())
        {
            cancel();
        }

        _commitCharacters.Dispose();
        Model.Dispose();
    }

    /// <summary>Starts a manual session; with an open list it retriggers (Monaco: <c>triggerSuggest</c>).</summary>
    public void Trigger() => Model.Trigger(new SuggestTriggerOptions(Auto: false));

    /// <summary>Ends the session.</summary>
    public void Cancel() => Model.Cancel();

    /// <summary>Selects the next item (wraps).</summary>
    public bool SelectNext() => List.SelectNext();

    /// <summary>Selects the previous item (wraps).</summary>
    public bool SelectPrevious() => List.SelectPrevious();

    /// <summary>Selects the next page.</summary>
    public bool SelectNextPage() => List.SelectNextPage();

    /// <summary>Selects the previous page.</summary>
    public bool SelectPreviousPage() => List.SelectPreviousPage();

    /// <summary>Selects the first item.</summary>
    public bool SelectFirst() => List.SelectFirst();

    /// <summary>Selects the last item.</summary>
    public bool SelectLast() => List.SelectLast();

    /// <summary>Accepts the selected item, if any.</summary>
    public bool AcceptSelected() => List.SelectedItem is { } item && Accept(item);

    private void OnDidSuggest(object? sender, SuggestEventArgs e)
    {
        if (e.TriggerOptions.Shy)
        {
            return;
        }

        IReadOnlyList<SuggestItem> items = e.CompletionModel.Items;
        int index = SuggestSelector.SelectInitialIndex(items);

        // selecting an item can pump out selection/cursor change events which can cancel suggest halfway
        if (Model.State == SuggestState.Idle)
        {
            return;
        }

        List.Reset(items, index, e.IsFrozen);
        ISuggestPresenter presenter = _context.GetSuggestPresenter();
        if (items.Count == 0)
        {
            presenter.Hide();
        }
        else
        {
            presenter.Show(List, List.SelectedItem!.EditStart);
        }

        _commitCharacters.Refresh();
    }

    private void OnDidCancel(object? sender, SuggestCancelEventArgs e)
    {
        if (!e.Retrigger)
        {
            List.Clear();
            _context.GetSuggestPresenter().Hide();
            CancelResolve();
        }
    }

    private void OnFocusChanged(object? sender, EventArgs e)
    {
        if (_viewModel is { HasFocus: false })
        {
            Model.Cancel();
        }
    }

    private void OnSelectionChanged(object? sender, SuggestItem? item)
    {
        CancelResolve();
        if (item is null)
        {
            return;
        }

        var source = new CancellationTokenSource();
        _resolveToken = source;
        CancellationToken token = source.Token;
        Task<CompletionItem?>? task = item.BeginResolve(token);
        if (task is not null)
        {
            UiContinuation.Then(
                task,
                _context.GetUiDispatcher(),
                resolved =>
                {
                    item.CompleteResolve(resolved);
                    List.NotifyItemUpdated();
                },
                token: token);
        }
    }

    private void CancelResolve()
    {
        _resolveToken?.Cancel();
        _resolveToken?.Dispose();
        _resolveToken = null;
    }
}
