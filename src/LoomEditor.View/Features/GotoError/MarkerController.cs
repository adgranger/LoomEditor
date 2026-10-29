// Ported from microsoft/vscode: src/vs/editor/contrib/gotoError/browser/gotoError.ts (MarkerController)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Markers;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.GotoError;

/// <summary>
/// Moves the caret to the next or previous problem and announces it; the session lasts until <see cref="Close"/> (Monaco:
/// <c>MarkerController</c>).
/// </summary>
/// <remarks>
/// Behavior differences: no marker widget (the editor side of <c>MarkerNavigationWidget.showAtMarker</c> is done here: caret, reveal,
/// announcement), a single file, no related information; the editor is not focused (the keys need the focus already). Threading: UI thread only.
/// </remarks>
internal sealed class MarkerController : IDisposable
{
    private readonly EditorContext _editor;
    private MarkerList? _list;
    private bool _following;

    /// <summary>Creates the controller of <paramref name="editor"/>.</summary>
    public MarkerController(EditorContext editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        _editor = editor;
    }

    /// <summary>Goes to the next (<paramref name="next"/>) or previous problem; nothing happens without problems.</summary>
    public void Navigate(bool next)
    {
        TextModel model = _editor.Model;
        bool created = _list is null;
        MarkerList list = _list ??= new MarkerList(_editor.Markers);
        list.Move(next, model, _editor.Selection.Position);
        if (list.Selected is not { } selected)
        {
            if (created)
            {
                Close();
            }

            return;
        }

        if (created)
        {
            StartSession();
        }

        ShowAtMarker(selected);
    }

    /// <summary>Ends the session.</summary>
    public void Close()
    {
        StopSession();
        _list?.Dispose();
        _list = null;
        _editor.MarkersNavigationVisible = false;
    }

    /// <summary>Ends the session.</summary>
    public void Dispose() => Close();

    private void StartSession()
    {
        _editor.MarkersNavigationVisible = true;
        // Follow the cursor; a change of the markers resets the index inside the list itself (no widget to update).
        _editor.SelectionChanged += OnSelectionChanged;
        _editor.Model.ContentChanged += OnContentChanged;
        _following = true;
    }

    private void StopSession()
    {
        if (_following)
        {
            _editor.SelectionChanged -= OnSelectionChanged;
            _editor.Model.ContentChanged -= OnContentChanged;
            _following = false;
        }
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        Position position = _editor.Selection.Position;
        if (_list is { } list && !(list.Selected is { } selected && RangeOf(selected.Marker).Contains(position)))
        {
            list.ResetIndex();
        }
    }

    // A replaced text ends the session (Monaco: onDidChangeModel).
    private void OnContentChanged(object? sender, ModelContentChangedEventArgs e)
    {
        if (e.IsFlush)
        {
            Close();
        }
    }

    private static Range RangeOf(Marker marker) => new(marker.StartLineNumber, marker.StartColumn, marker.EndLineNumber, marker.EndColumn);

    // The editor side of MarkerNavigationWidget.showAtMarker and ZoneWidget._showImpl.
    private void ShowAtMarker(MarkerCoordinate coordinate)
    {
        TextModel model = _editor.Model;
        Marker marker = coordinate.Marker;
        Range range = RangeOf(marker);
        Position caret = _editor.Selection.Position;
        Position position = range.Contains(caret) ? caret : range.Start;

        ViewModel viewModel = _editor.ViewModel;
        viewModel.SetSelections("marker.navigate", [Selection.Collapsed(position)]);
        Range viewRange = viewModel.CoordinatesConverter.ConvertModelRangeToViewRange(Range.Collapsed(position));
        viewModel.RevealRange("marker.navigate", true, viewRange, VerticalRevealType.CenterIfOutsideViewport, ScrollType.Smooth);

        string? lineContent = marker.StartLineNumber >= 1 && marker.StartLineNumber <= model.LineCount ? model.GetLineContent(marker.StartLineNumber) : null;
        _editor.Announcer.Announce(Accessibility.AccessibilityMessages.MarkerNavigation(
            lineContent, marker.Severity, marker.Message, coordinate.Index, coordinate.Total, marker.StartLineNumber, marker.StartColumn));
    }
}
