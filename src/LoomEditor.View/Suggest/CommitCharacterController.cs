// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestCommitCharacters.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Layout;

namespace LoomEditor.View.Suggest;

/// <summary>
/// Typing a commit character of the selected item accepts the item first; the typed character is then inserted by the normal typing code.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class CommitCharacterController : IDisposable
{
    private readonly ViewModel? _viewModel;
    private readonly SuggestController _controller;
    private readonly bool _enabled;
    private (HashSet<char> Characters, SuggestItem Item)? _active;

    /// <summary>Starts listening to <paramref name="controller"/> and to the typing of <paramref name="viewModel"/>.</summary>
    public CommitCharacterController(ViewModel? viewModel, SuggestController controller, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(controller);
        _viewModel = viewModel;
        _controller = controller;
        _enabled = enabled;

        controller.Model.DidSuggest += OnDidSuggest;
        controller.Model.DidCancel += OnDidCancel;
        controller.List.SelectionChanged += OnSelectionChanged;
        if (viewModel is not null)
        {
            viewModel.WillType += OnWillType;
        }
    }

    /// <summary>Stops listening.</summary>
    public void Dispose()
    {
        _controller.Model.DidSuggest -= OnDidSuggest;
        _controller.Model.DidCancel -= OnDidCancel;
        _controller.List.SelectionChanged -= OnSelectionChanged;
        if (_viewModel is not null)
        {
            _viewModel.WillType -= OnWillType;
        }

        Reset();
    }

    /// <summary>
    /// Takes the selected item of the list again: a reset of the list that keeps the same item does not raise its selection event.
    /// Called by the controller after each list it handled.
    /// </summary>
    internal void Refresh() => OnItem(_controller.List.SelectedItem);

    private void OnDidSuggest(object? sender, SuggestEventArgs e)
    {
        if (e.CompletionModel.Items.Count == 0)
        {
            Reset();
        }
    }

    private void OnDidCancel(object? sender, SuggestCancelEventArgs e) => Reset();

    private void OnSelectionChanged(object? sender, SuggestItem? item) => OnItem(item);

    private void OnItem(SuggestItem? item)
    {
        if (item?.Completion.CommitCharacters is not { Count: > 0 } characters)
        {
            Reset();
            return;
        }

        if (_active is { } active && ReferenceEquals(active.Item, item))
        {
            return;
        }

        _active = ([.. characters], item);
    }

    private void OnWillType(object? sender, string text)
    {
        if (_active is not { } active
            || _controller.List.IsFrozen
            || _controller.Model.State == SuggestState.Idle
            || text.Length == 0
            || !_enabled)
        {
            return;
        }

        if (active.Characters.Contains(text[^1]))
        {
            _controller.Accept(active.Item, noAfterUndoStop: true);
        }
    }

    private void Reset() => _active = null;
}
