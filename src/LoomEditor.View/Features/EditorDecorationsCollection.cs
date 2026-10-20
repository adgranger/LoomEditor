// Ported from microsoft/vscode: src/vs/editor/browser/widget/codeEditor/codeEditorWidget.ts (EditorDecorationsCollection)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Events;

namespace LoomEditor.View.Features;

/// <summary>
/// A set of decorations that one feature owns (Monaco: <c>editor.createDecorationsCollection()</c>): it replaces
/// or appends decorations as a whole, and can tell when other code changed decorations.
/// </summary>
/// <remarks>
/// Behavior difference: <see cref="OnDidChange"/> listens to <see cref="TextModel.DecorationsChanged"/> directly;
/// Monaco listens to the editor's <c>onDidChangeModelDecorations</c>, which the view model re-emits (same event, no
/// view model in between). <c>hasModel()</c> checks are dropped: an editor always has its model.
/// Threading: UI thread only.
/// </remarks>
internal sealed class EditorDecorationsCollection
{
    private readonly TextModel _model;
    private readonly int _ownerId;
    private string[] _decorationIds = [];
    private bool _isChangingDecorations;

    public EditorDecorationsCollection(TextModel model, int ownerId, IReadOnlyList<ModelDeltaDecoration>? decorations = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        _model = model;
        _ownerId = ownerId;
        if (decorations is { Count: > 0 })
        {
            Set(decorations);
        }
    }

    /// <summary>The number of decorations in the collection (Monaco: <c>length</c>).</summary>
    public int Count => _decorationIds.Length;

    /// <summary>
    /// Calls <paramref name="listener"/> when decorations change, except when the collection itself is changing
    /// them. Dispose the result to stop listening.
    /// </summary>
    public IDisposable OnDidChange(Action<ModelDecorationsChangedEventArgs> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        void Handler(object? sender, ModelDecorationsChangedEventArgs e)
        {
            if (_isChangingDecorations)
            {
                return;
            }

            listener(e);
        }

        _model.DecorationsChanged += Handler;
        return new Subscription(() => _model.DecorationsChanged -= Handler);
    }

    /// <summary>The current range of the decoration at <paramref name="index"/>, or <see langword="null"/> when there is none.</summary>
    public Range? GetRange(int index)
    {
        if (index < 0 || index >= _decorationIds.Length)
        {
            return null;
        }

        return _model.GetDecorationRange(_decorationIds[index]);
    }

    /// <summary>The current ranges of the decorations, in the order they were added.</summary>
    public IReadOnlyList<Range> GetRanges()
    {
        var result = new List<Range>();
        foreach (string decorationId in _decorationIds)
        {
            if (_model.GetDecorationRange(decorationId) is { } range)
            {
                result.Add(range);
            }
        }

        return result;
    }

    /// <summary>Whether <paramref name="decoration"/> belongs to the collection.</summary>
    public bool Has(ModelDecoration decoration) => Array.IndexOf(_decorationIds, decoration.Id) >= 0;

    /// <summary>Removes every decoration of the collection.</summary>
    public void Clear()
    {
        if (_decorationIds.Length == 0)
        {
            // nothing to do
            return;
        }

        Set([]);
    }

    /// <summary>Replaces the decorations of the collection and returns the new ids.</summary>
    public string[] Set(IReadOnlyList<ModelDeltaDecoration> newDecorations)
    {
        ArgumentNullException.ThrowIfNull(newDecorations);
        try
        {
            _isChangingDecorations = true;
            _model.ChangeDecorations(_ownerId, accessor => _decorationIds = accessor.DeltaDecorations(_decorationIds, newDecorations));
        }
        finally
        {
            _isChangingDecorations = false;
        }

        return _decorationIds;
    }

    /// <summary>Adds decorations to the collection and returns the ids of the new ones.</summary>
    public string[] Append(IReadOnlyList<ModelDeltaDecoration> newDecorations)
    {
        ArgumentNullException.ThrowIfNull(newDecorations);
        string[] newDecorationIds = [];
        try
        {
            _isChangingDecorations = true;
            _model.ChangeDecorations(_ownerId, accessor =>
            {
                newDecorationIds = accessor.DeltaDecorations([], newDecorations);
                _decorationIds = [.. _decorationIds, .. newDecorationIds];
            });
        }
        finally
        {
            _isChangingDecorations = false;
        }

        return newDecorationIds;
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;

        public void Dispose()
        {
            _unsubscribe?.Invoke();
            _unsubscribe = null;
        }
    }
}
