// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursor.ts (CompositionLineState, CompositionState) and cursorTypeOperations.ts (CompositionOutcome)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

/// <summary>What a composition did on one line: the text that went away and the text that was inserted (Monaco: <c>CompositionOutcome</c>).</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed record CompositionOutcome(
    string DeletedText,
    int DeletedSelectionStart,
    int DeletedSelectionEnd,
    string InsertedText,
    int InsertedSelectionStart,
    int InsertedSelectionEnd,
    Range InsertedTextRange);

/// <summary>A line and its selection as they were when a composition started or ended (Monaco: <c>CompositionLineState</c>).</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed record CompositionLineState(string Text, int LineNumber, int StartSelectionOffset, int EndSelectionOffset);

/// <summary>
/// The lines under the cursors when a composition started, used to deduce at its end what the composition inserted
/// (Monaco: <c>CompositionState</c>).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class CompositionState
{
    private readonly List<CompositionLineState>? _original;

    /// <summary>Captures the lines of <paramref name="selections"/>.</summary>
    public CompositionState(TextModel textModel, IReadOnlyList<Selection> selections)
    {
        _original = Capture(textModel, selections);
    }

    /// <summary>
    /// Returns the inserted text during this composition, one outcome per selection, or <see langword="null"/> when it
    /// cannot be deduced (a selection spans several lines, or the number of selections changed).
    /// </summary>
    public List<CompositionOutcome>? DeduceOutcome(TextModel textModel, IReadOnlyList<Selection> selections)
    {
        if (_original is null)
        {
            return null;
        }

        List<CompositionLineState>? current = Capture(textModel, selections);
        if (current is null || _original.Count != current.Count)
        {
            return null;
        }

        var result = new List<CompositionOutcome>(_original.Count);
        for (int i = 0; i < _original.Count; i++)
        {
            result.Add(DeduceOutcome(_original[i], current[i]));
        }

        return result;
    }

    private static List<CompositionLineState>? Capture(TextModel textModel, IReadOnlyList<Selection> selections)
    {
        var result = new List<CompositionLineState>(selections.Count);
        foreach (Selection selection in selections)
        {
            Range range = selection.Range;
            if (range.StartLineNumber != range.EndLineNumber)
            {
                return null;
            }

            int lineNumber = range.StartLineNumber;
            result.Add(new CompositionLineState(textModel.GetLineContent(lineNumber), lineNumber, range.StartColumn - 1, range.EndColumn - 1));
        }

        return result;
    }

    private static CompositionOutcome DeduceOutcome(CompositionLineState original, CompositionLineState current)
    {
        int commonPrefix = Math.Min(
            Math.Min(original.StartSelectionOffset, current.StartSelectionOffset),
            Strings.CommonPrefixLength(original.Text, current.Text));
        int commonSuffix = Math.Min(
            Math.Min(original.Text.Length - original.EndSelectionOffset, current.Text.Length - current.EndSelectionOffset),
            Strings.CommonSuffixLength(original.Text, current.Text));
        string deletedText = original.Text.Substring(commonPrefix, original.Text.Length - commonSuffix - commonPrefix);
        int insertedTextStartOffset = commonPrefix;
        int insertedTextEndOffset = current.Text.Length - commonSuffix;
        string insertedText = current.Text.Substring(insertedTextStartOffset, insertedTextEndOffset - insertedTextStartOffset);
        var insertedTextRange = new Range(current.LineNumber, insertedTextStartOffset + 1, current.LineNumber, insertedTextEndOffset + 1);
        return new CompositionOutcome(
            deletedText,
            original.StartSelectionOffset - commonPrefix,
            original.EndSelectionOffset - commonPrefix,
            insertedText,
            current.StartSelectionOffset - commonPrefix,
            current.EndSelectionOffset - commonPrefix,
            insertedTextRange);
    }
}
