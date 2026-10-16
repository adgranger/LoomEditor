// Ported from microsoft/vscode: src/vs/editor/browser/controller/editContext/clipboardUtils.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Controller;

/// <summary>What a copy puts on the clipboard (Monaco: <c>ClipboardDataToCopy</c>, without the HTML part).</summary>
/// <param name="IsFromEmptySelection">True when one empty selection copied its whole line.</param>
/// <param name="IsBlock">True when the text comes from a column (block) selection.</param>
/// <param name="SourceRanges">The model ranges that were copied.</param>
/// <param name="MulticursorText">One text per cursor when there are several, otherwise <see langword="null"/>.</param>
/// <param name="Text">The text for the clipboard.</param>
internal sealed record ClipboardDataToCopy(
    bool IsFromEmptySelection,
    bool IsBlock,
    IReadOnlyList<Range> SourceRanges,
    IReadOnlyList<string>? MulticursorText,
    string Text);

/// <summary>What the editor remembers about its last copy, to paste it smartly (Monaco: <c>ClipboardStoredMetadata</c>).</summary>
/// <param name="Version">The format version (always 1).</param>
/// <param name="Id">A unique id for the copy.</param>
/// <param name="IsFromEmptySelection">True when one empty selection copied its whole line.</param>
/// <param name="IsBlock">True when the text comes from a column selection.</param>
/// <param name="MulticursorText">One text per cursor when there were several.</param>
/// <param name="Mode">The language of the copied text; always <see langword="null"/> in phase 3 (no rich copy).</param>
internal sealed record ClipboardStoredMetadata(
    int Version,
    string? Id,
    bool IsFromEmptySelection,
    bool IsBlock,
    IReadOnlyList<string>? MulticursorText,
    string? Mode);

/// <summary>
/// Every time the editor writes to the clipboard it records the metadata here; every time it reads, if the text matches
/// the last written text, the metadata comes back (Monaco: <c>InMemoryClipboardMetadataManager</c>).
/// </summary>
/// <remarks>Threading: UI thread only. <see cref="Instance"/> is process-wide, like in Monaco, so that a copy in one editor can be pasted in another.</remarks>
internal sealed class InMemoryClipboardMetadataManager
{
    private (string LastCopiedValue, ClipboardStoredMetadata Data)? _lastState;

    /// <summary>The shared instance.</summary>
    public static InMemoryClipboardMetadataManager Instance { get; } = new();

    /// <summary>Remembers the metadata of the text that was just copied.</summary>
    public void Set(string lastCopiedValue, ClipboardStoredMetadata data) => _lastState = (lastCopiedValue, data);

    /// <summary>The metadata of the last copy when <paramref name="pastedText"/> is that copy; otherwise <see langword="null"/>, and the state is forgotten.</summary>
    public ClipboardStoredMetadata? Get(string pastedText)
    {
        if (_lastState is { } state && state.LastCopiedValue == pastedText)
        {
            // match!
            return state.Data;
        }

        _lastState = null;
        return null;
    }
}

/// <summary>Computes what a copy puts on the clipboard (Monaco: the functions of <c>clipboardUtils.ts</c>).</summary>
/// <remarks>
/// Phase 3 cut point (ADR 0006, decision 12): no rich (HTML) copy, so <c>copyWithSyntaxHighlighting</c> and
/// <c>getRichTextToCopy</c> are not ported and the mode is always <see langword="null"/>. The Firefox quirk is a browser matter.
/// Threading: UI thread only.
/// </remarks>
internal static class ClipboardUtils
{
    /// <summary>Computes the data to copy and remembers its metadata (Monaco: <c>generateDataToCopyAndStoreInMemory</c>).</summary>
    public static (ClipboardDataToCopy DataToCopy, ClipboardStoredMetadata Metadata) GenerateDataToCopyAndStoreInMemory(ViewModel viewModel)
    {
        (ClipboardDataToCopy dataToCopy, ClipboardStoredMetadata metadata) = GenerateDataToCopy(viewModel);
        InMemoryClipboardMetadataManager.Instance.Set(dataToCopy.Text, metadata);
        return (dataToCopy, metadata);
    }

    private static (ClipboardDataToCopy DataToCopy, ClipboardStoredMetadata Metadata) GenerateDataToCopy(ViewModel viewModel)
    {
        bool emptySelectionClipboard = viewModel.CursorOptions.EmptySelectionClipboard;
        List<Selection> selections = viewModel.GetSelections();
        ClipboardDataToCopy dataToCopy = GetDataToCopy(viewModel, selections.ConvertAll(s => s.Range), emptySelectionClipboard);
        var metadata = new ClipboardStoredMetadata(
            1,
            Guid.NewGuid().ToString(),
            dataToCopy.IsFromEmptySelection,
            dataToCopy.IsBlock,
            dataToCopy.MulticursorText,
            null);
        return (dataToCopy, metadata);
    }

    private static ClipboardDataToCopy GetDataToCopy(ViewModel viewModel, List<Range> modelSelections, bool emptySelectionClipboard)
    {
        bool isBlock = viewModel.GetCursorColumnSelectData().IsReal;

        // Windows: Monaco passes forceCRLF = isWindows (ADR 0006, decision 10).
        (IReadOnlyList<Range> sourceRanges, string? sourceText, IReadOnlyList<string>? sourceTexts) =
            viewModel.GetPlainTextToCopy(modelSelections, emptySelectionClipboard, true);
        string newLineCharacter = viewModel.Model.Eol;

        bool isFromEmptySelection = emptySelectionClipboard && modelSelections.Count == 1 && modelSelections[0].IsEmpty;
        string text = sourceTexts is not null ? string.Join(newLineCharacter, sourceTexts) : sourceText!;

        return new ClipboardDataToCopy(isFromEmptySelection, isBlock, sourceRanges, sourceTexts, text);
    }
}
