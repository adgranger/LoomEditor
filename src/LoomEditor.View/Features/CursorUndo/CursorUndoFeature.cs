// Ported from microsoft/vscode: src/vs/editor/contrib/cursorUndo/browser/cursorUndo.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.CursorUndo;

/// <summary>
/// Cursor undo and redo (Monaco: <c>CursorUndoRedoController</c> and its two actions): remembers the previous cursor
/// positions and scroll positions of the editor and restores them without touching the text.
/// </summary>
/// <remarks>
/// Behavior difference: the editor's model never changes, so only a content change clears the stacks.
/// Threading: UI thread only.
/// </remarks>
public sealed class CursorUndoFeature : IEditorFeature
{
    private const int MaxStackSize = 50;

    private readonly List<IDisposable> _registrations = [];
    private List<StackElement> _undoStack = [];
    private List<StackElement> _redoStack = [];
    private bool _isCursorUndoRedo;
    private IEditorContext? _context;
    private ViewModel? _viewModel;

    /// <inheritdoc />
    public string Id => "loom.cursorUndo";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.GetViewModel() is not { } viewModel)
        {
            return;
        }

        _context = context;
        _viewModel = viewModel;
        context.Model.ContentChanged += OnContentChanged;
        viewModel.EventDispatcher.OnEvent += OnViewModelEvent;
        _registrations.Add(context.Commands.Register(new EditorCommand(CursorUndoCommands.CursorUndo, CursorUndo)));
        _registrations.Add(context.Commands.Register(new EditorCommand(CursorUndoCommands.CursorRedo, CursorRedo)));
    }

    private void OnContentChanged(object? sender, ModelContentChangedEventArgs e) => Clear();

    private void Clear()
    {
        _undoStack = [];
        _redoStack = [];
    }

    private void OnViewModelEvent(object? sender, OutgoingViewModelEvent e)
    {
        if (e is not CursorStateChangedEvent { OldSelections: { } oldSelections } cursorEvent
            || _isCursorUndoRedo
            || cursorEvent.OldModelVersionId != cursorEvent.ModelVersionId)
        {
            return;
        }

        var previous = new StackElement(oldSelections, ScrollTop(), ScrollLeft());
        if (_undoStack.Count > 0 && _undoStack[^1].SameSelections(previous))
        {
            return;
        }

        _undoStack.Add(previous);
        _redoStack = [];
        if (_undoStack.Count > MaxStackSize)
        {
            // keep the cursor undo stack bounded
            _undoStack.RemoveAt(0);
        }
    }

    private double ScrollTop() => _viewModel!.ViewLayout.GetCurrentScrollTop();

    private double ScrollLeft() => _viewModel!.ViewLayout.GetCurrentScrollLeft();

    private StackElement Current() => new(_viewModel!.GetSelections(), ScrollTop(), ScrollLeft());

    private void CursorUndo()
    {
        if (_undoStack.Count == 0)
        {
            return;
        }

        _redoStack.Add(Current());
        ApplyState(Pop(_undoStack));
    }

    private void CursorRedo()
    {
        if (_redoStack.Count == 0)
        {
            return;
        }

        _undoStack.Add(Current());
        ApplyState(Pop(_redoStack));
    }

    private static StackElement Pop(List<StackElement> stack)
    {
        StackElement last = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        return last;
    }

    private void ApplyState(StackElement element)
    {
        _isCursorUndoRedo = true;
        try
        {
            _viewModel!.SetSelections(null, element.Selections);
            _viewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(element.ScrollLeft, element.ScrollTop), ScrollType.Immediate);
        }
        finally
        {
            _isCursorUndoRedo = false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_context is not null && _viewModel is not null)
        {
            _context.Model.ContentChanged -= OnContentChanged;
            _viewModel.EventDispatcher.OnEvent -= OnViewModelEvent;
        }

        foreach (IDisposable registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();
        Clear();
        _context = null;
        _viewModel = null;
    }

    // Monaco: StackElement with its CursorState; the selections compare one by one (start, end and direction).
    private sealed record StackElement(IReadOnlyList<Selection> Selections, double ScrollTop, double ScrollLeft)
    {
        public bool SameSelections(StackElement other) => Selections.SequenceEqual(other.Selections);
    }
}
