// Ported from microsoft/vscode: src/vs/editor/contrib/anchorSelect/browser/anchorSelect.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Accessibility;
using LoomEditor.View.Commands;

namespace LoomEditor.View.Features.SelectionAnchor;

/// <summary>
/// The selection anchor (Monaco: <c>SelectionAnchorController</c> and its four actions): one empty model decoration
/// that follows the edits; the context key <c>selectionAnchorSet</c> is <see cref="EditorContext.SelectionAnchorSet"/>.
/// </summary>
/// <remarks>
/// Behavior differences: no hover message; the alert is an announcement; the controller lives as long as the editor, so
/// there is no model-change reset.
/// Threading: UI thread only.
/// </remarks>
public sealed class SelectionAnchorFeature : IEditorFeature
{
    private static readonly ModelDecorationOptions AnchorDecoration = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "selection-anchor",
        Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
        ClassName = "selection-anchor",
    });

    private readonly List<IDisposable> _registrations = [];
    private EditorContext? _editor;
    private string? _decorationId;

    /// <inheritdoc />
    public string Id => "loom.selectionAnchor";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context is not EditorContext editor)
        {
            return;
        }

        _editor = editor;
        void Register(string id, Action run, Func<bool>? canRun = null) =>
            _registrations.Add(context.Commands.Register(new EditorCommand(id, run, canRun)));

        // Monaco: the other three have the precondition `selectionAnchorSet`.
        Func<bool> anchorSet = () => _decorationId is not null;
        Register(SelectionAnchorCommands.SetSelectionAnchor, Set);
        Register(SelectionAnchorCommands.GoToSelectionAnchor, GoTo, anchorSet);
        Register(SelectionAnchorCommands.SelectFromAnchorToCursor, SelectFromAnchorToCursor, anchorSet);
        Register(SelectionAnchorCommands.CancelSelectionAnchor, Cancel, anchorSet);
    }

    private void Set()
    {
        EditorContext editor = _editor!;
        Position position = editor.Selection.Position;
        string? previous = _decorationId;
        _decorationId = editor.Model.ChangeDecorations(editor.Id, accessor =>
        {
            if (previous is not null)
            {
                accessor.RemoveDecoration(previous);
            }

            return accessor.AddDecoration(new Range(position.LineNumber, position.Column, position.LineNumber, position.Column), AnchorDecoration);
        });
        editor.SelectionAnchorSet = true;
        editor.Announcer.Announce(AccessibilityMessages.AnchorSet(position.LineNumber, position.Column));
    }

    private void GoTo()
    {
        EditorContext editor = _editor!;
        if (_decorationId is not null && editor.Model.GetDecorationRange(_decorationId) is { } anchor)
        {
            editor.Selection = Selection.Collapsed(anchor.Start);
        }
    }

    private void SelectFromAnchorToCursor()
    {
        EditorContext editor = _editor!;
        if (_decorationId is not null && editor.Model.GetDecorationRange(_decorationId) is { } start)
        {
            editor.Selection = new Selection(start.Start, editor.Selection.Position);
            Cancel();
        }
    }

    private void Cancel()
    {
        if (_decorationId is null || _editor is null)
        {
            return;
        }

        string id = _decorationId;
        _editor.Model.ChangeDecorations(_editor.Id, accessor => accessor.RemoveDecoration(id));
        _decorationId = null;
        _editor.SelectionAnchorSet = false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Cancel();
        foreach (IDisposable registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();
        _editor = null;
    }
}
