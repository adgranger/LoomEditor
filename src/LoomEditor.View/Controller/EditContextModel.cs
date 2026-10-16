// Ported from microsoft/vscode: src/vs/editor/browser/controller/editContext/native/nativeEditContext.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Controller;

/// <summary>
/// Keeps the text and selection last given to the IME, and turns the updates the IME makes into cursor calls
/// (Monaco: the state and the <c>textupdate</c> handling of <c>NativeEditContext</c>).
/// </summary>
/// <remarks>
/// Only the lines of the primary selection are exposed to the IME; with several cursors, <c>CompositionType</c> still
/// applies to all of them (same as Monaco). The text of an empty range is the empty string: Monaco's <c>?? ' '</c> never
/// applies because its state always has a text.
/// Threading: UI thread only; keeps the pending high surrogate between calls.
/// </remarks>
internal sealed class EditContextModel
{
    private string _previousText = string.Empty;
    private (int Start, int End) _previousSelection = (0, 0);
    private string? _highSurrogate;

    /// <summary>The primary selection last given to the IME.</summary>
    public Selection PrimarySelection { get; private set; }

    /// <summary>The text last given to the IME (what <c>TextRequested</c> answers).</summary>
    public string Text => _previousText;

    /// <summary>The selection offsets last given to the IME (what <c>SelectionRequested</c> answers).</summary>
    public (int Start, int End) Selection => _previousSelection;

    /// <summary>
    /// Refreshes the text and selection from the model (Monaco: <c>_updateEditContext</c>) and tells what changed, so
    /// that the control notifies the IME only when needed.
    /// </summary>
    public (bool TextChanged, bool SelectionChanged) Update(TextModel model, Selection primarySelection)
    {
        EditContextState? state = EditContextState.GetNewEditContextState(model, primarySelection);
        if (state is null)
        {
            return (false, false);
        }

        bool textChanged = state.Text != _previousText;
        if (textChanged)
        {
            _previousText = state.Text;
        }

        bool selectionChanged = state.SelectionStartOffset != _previousSelection.Start || state.SelectionEndOffset != _previousSelection.End;
        PrimarySelection = state.PrimarySelection;
        _previousSelection = (state.SelectionStartOffset, state.SelectionEndOffset);
        return (textChanged, selectionChanged);
    }

    /// <summary>
    /// Turns an update made by the IME into what to type, or <see langword="null"/> while a high surrogate waits for
    /// its low surrogate (Monaco: the <c>textupdate</c> listener and <c>_emitTypeEvent</c>).
    /// </summary>
    public TypeData? OnTextUpdate(TextUpdate e)
    {
        string text = e.Text;
        if (text.Length == 1)
        {
            char ch = text[0];
            if (Strings.IsHighSurrogate(ch))
            {
                _highSurrogate = text;
                return null;
            }

            if (Strings.IsLowSurrogate(ch) && !string.IsNullOrEmpty(_highSurrogate))
            {
                var merged = new TextUpdate(e.UpdateRangeStart - 1, e.UpdateRangeEnd - 1, _highSurrogate + text, e.SelectionStart, e.SelectionEnd);
                _highSurrogate = null;
                return EmitTypeEvent(merged);
            }
        }

        return EmitTypeEvent(e);
    }

    /// <summary>Runs what the IME typed on the cursors (Monaco: <c>_onType</c>).</summary>
    public static void Apply(ViewModel viewModel, TypeData data)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        if (data.ReplacePrevCharCnt != 0 || data.ReplaceNextCharCnt != 0 || data.PositionDelta != 0)
        {
            viewModel.CompositionType(data.Text, data.ReplacePrevCharCnt, data.ReplaceNextCharCnt, data.PositionDelta, "keyboard");
        }
        else
        {
            viewModel.Type(data.Text, "keyboard");
        }
    }

    private TypeData EmitTypeEvent(TextUpdate e)
    {
        int selectionEndOffset = _previousSelection.End;
        int selectionStartOffset = _previousSelection.Start;
        _previousSelection = (e.SelectionStart, e.SelectionEnd);

        int replaceNextCharCnt = 0;
        int replacePrevCharCnt = 0;
        if (e.UpdateRangeEnd > selectionEndOffset)
        {
            replaceNextCharCnt = e.UpdateRangeEnd - selectionEndOffset;
        }

        if (e.UpdateRangeStart < selectionStartOffset)
        {
            replacePrevCharCnt = selectionStartOffset - e.UpdateRangeStart;
        }

        string text = string.Empty;
        if (selectionStartOffset < e.UpdateRangeStart)
        {
            text += Substring(_previousText, selectionStartOffset, e.UpdateRangeStart);
        }

        text += e.Text;
        if (selectionEndOffset > e.UpdateRangeEnd)
        {
            text += Substring(_previousText, e.UpdateRangeEnd, selectionEndOffset);
        }

        int positionDelta = 0;
        if (e.SelectionStart == e.SelectionEnd && selectionStartOffset == selectionEndOffset)
        {
            positionDelta = e.SelectionStart - (e.UpdateRangeStart + e.Text.Length);
        }

        return new TypeData(text, replacePrevCharCnt, replaceNextCharCnt, positionDelta);
    }

    // JavaScript's substring(start, end): the arguments are clamped to the string and swapped when start > end.
    private static string Substring(string value, int start, int end)
    {
        start = Math.Clamp(start, 0, value.Length);
        end = Math.Clamp(end, 0, value.Length);
        if (start > end)
        {
            (start, end) = (end, start);
        }

        return value.Substring(start, end - start);
    }
}
