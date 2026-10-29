// Ported from microsoft/vscode: src/vs/editor/contrib/dnd/browser/dnd.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Text;
using LoomEditor.View.Controller;
using LoomEditor.View.Cursors;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.DragAndDrop;

/// <summary>
/// Follows a drag of the selection started by the mouse operation: shows the drop target, switches the mouse style between
/// move and copy, and runs <see cref="DragAndDropCommand"/> on drop.
/// </summary>
/// <remarks>
/// Behavior differences: the trigger modifier is Ctrl only (no macOS Alt); the drop target is a solid 2 px bar.
/// Threading: UI thread only.
/// </remarks>
internal sealed class DragAndDropController : IDisposable
{
    private static readonly ModelDecorationOptions TargetDecoration = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "dnd-target",
        ClassName = "dnd-target",
    });

    private readonly EditorContext _editor;
    private readonly ViewUserInputEvents _events;
    private readonly EditorDecorationsCollection _decorations;
    private Selection? _dragSelection;
    private bool _mouseDown;
    private bool _modifierPressed;

    public DragAndDropController(EditorContext editor)
    {
        _editor = editor;
        _events = editor.UserInputEvents;
        _decorations = editor.CreateDecorationsCollection();
        _events.MouseDown += OnMouseDown;
        _events.MouseUp += OnMouseUp;
        _events.MouseDrag += OnMouseDrag;
        _events.MouseDrop += OnMouseDrop;
        _events.MouseDropCanceled += OnMouseDropCanceled;
        _events.KeyDown += OnKeyDown;
        _events.KeyUp += OnKeyUp;
        editor.ViewModel.FocusChanged += OnFocusChanged;
    }

    private bool Disabled => !_editor.ViewModel.CursorOptions.DragAndDrop || _editor.ViewModel.CursorOptions.ColumnSelection;

    // Monaco: onEditorBlur (widget and text blur).
    private void OnFocusChanged(object? sender, EventArgs e)
    {
        if (!_editor.ViewModel.HasFocus)
        {
            RemoveDecoration();
            _dragSelection = null;
            _mouseDown = false;
            _modifierPressed = false;
        }
    }

    private void OnKeyDown(object? sender, EditorKeyEventArgs e)
    {
        if (Disabled || !e.CtrlKey)
        {
            return;
        }

        _modifierPressed = true;
        if (_mouseDown)
        {
            _editor.MouseStyle = EditorMouseStyle.Copy;
        }
    }

    private void OnKeyUp(object? sender, EditorKeyEventArgs e)
    {
        if (Disabled)
        {
            return;
        }

        if (e.CtrlKey)
        {
            _modifierPressed = false;
        }

        if (_mouseDown && e.IsCtrlKey)
        {
            _editor.MouseStyle = EditorMouseStyle.Default;
        }
    }

    private void OnMouseDown(object? sender, EditorMouseEventArgs e) => _mouseDown = true;

    private void OnMouseUp(object? sender, EditorMouseEventArgs e)
    {
        _mouseDown = false;
        // Whenever users release the mouse, the drag and drop operation should finish and the cursor should revert to text.
        _editor.MouseStyle = EditorMouseStyle.Text;
    }

    private void OnMouseDrag(object? sender, EditorMouseEventArgs e)
    {
        MouseTarget target = e.Target;
        if (_dragSelection is null)
        {
            Selection[] possible = _editor.Selections
                .Where(selection => target.Position is { } position && selection.Range.Contains(position))
                .ToArray();
            if (possible.Length != 1)
            {
                return;
            }

            _dragSelection = possible[0];
        }

        _editor.MouseStyle = e.Event.CtrlKey ? EditorMouseStyle.Copy : EditorMouseStyle.Default;

        if (target.Position is { } dragPosition)
        {
            if (_dragSelection.Value.Range.Contains(dragPosition))
            {
                RemoveDecoration();
            }
            else
            {
                ShowAt(dragPosition);
            }
        }
    }

    private void OnMouseDropCanceled(object? sender, EventArgs e)
    {
        _editor.MouseStyle = EditorMouseStyle.Text;
        RemoveDecoration();
        _dragSelection = null;
        _mouseDown = false;
    }

    private void OnMouseDrop(object? sender, EditorPartialMouseEventArgs e)
    {
        if (e.Target is { Position: { } position } target && (HitContent(target) || HitMargin(target)))
        {
            var newCursorPosition = new Position(position.LineNumber, position.Column);
            if (_dragSelection is not { } dragSelection)
            {
                SelectDropPosition(e, newCursorPosition);
            }
            else
            {
                Range range = dragSelection.Range;
                bool copy = e.Event.CtrlKey || _modifierPressed;
                // We allow users to paste content beside the selection.
                if (!range.Contains(newCursorPosition) || (copy && (range.End == newCursorPosition || range.Start == newCursorPosition)))
                {
                    _editor.ViewModel.Model.PushStackElement();
                    _editor.ViewModel.ExecuteCommand(new DragAndDropCommand(dragSelection, newCursorPosition, copy), DragAndDropCommands.Source);
                    _editor.ViewModel.Model.PushStackElement();
                }
            }
        }

        _editor.MouseStyle = EditorMouseStyle.Text;
        RemoveDecoration();
        _dragSelection = null;
        _mouseDown = false;
    }

    // The click inside a selection (no drag selection): collapse it, or extend with Shift.
    private void SelectDropPosition(EditorPartialMouseEventArgs e, Position newCursorPosition)
    {
        List<Selection> newSelections = [];
        if (e.Event.ShiftKey)
        {
            Selection primary = _editor.Selection;
            newSelections.Add(new Selection(
                primary.SelectionStart.LineNumber, primary.SelectionStart.Column, newCursorPosition.LineNumber, newCursorPosition.Column));
        }
        else
        {
            foreach (Selection selection in _editor.Selections)
            {
                newSelections.Add(selection.Range.Contains(newCursorPosition) ? Selection.Collapsed(newCursorPosition) : selection);
            }
        }

        // `mouse` as the source and an explicit reason, to behave like any other mouse operation.
        _editor.ViewModel.SetSelections("mouse", newSelections, CursorChangeReason.Explicit);
    }

    private void ShowAt(Position position)
    {
        _decorations.Set([new ModelDeltaDecoration(Range.Collapsed(position), TargetDecoration)]);
        // Monaco: revealPosition(position, ScrollType.Immediate).
        Range viewRange = _editor.ViewModel.CoordinatesConverter.ConvertModelRangeToViewRange(Range.Collapsed(position));
        _editor.ViewModel.RevealRange(DragAndDropCommands.Source, true, viewRange, VerticalRevealType.Simple, ScrollType.Immediate);
    }

    private void RemoveDecoration() => _decorations.Clear();

    private static bool HitContent(MouseTarget target) => target.Type is MouseTargetType.ContentText or MouseTargetType.ContentEmpty;

    private static bool HitMargin(MouseTarget target) =>
        target.Type is MouseTargetType.GutterGlyphMargin or MouseTargetType.GutterLineNumbers or MouseTargetType.GutterLineDecorations;

    public void Dispose()
    {
        _events.MouseDown -= OnMouseDown;
        _events.MouseUp -= OnMouseUp;
        _events.MouseDrag -= OnMouseDrag;
        _events.MouseDrop -= OnMouseDrop;
        _events.MouseDropCanceled -= OnMouseDropCanceled;
        _events.KeyDown -= OnKeyDown;
        _events.KeyUp -= OnKeyUp;
        _editor.ViewModel.FocusChanged -= OnFocusChanged;
        RemoveDecoration();
        _dragSelection = null;
        _mouseDown = false;
        _modifierPressed = false;
    }
}
