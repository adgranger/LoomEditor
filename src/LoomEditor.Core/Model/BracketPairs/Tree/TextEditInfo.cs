// Ported from microsoft/vscode: src/vs/editor/common/model/bracketPairsTextModelPart/bracketPairsTree/beforeEditPositionMapper.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.BracketPairs.Tree;

/// <summary>
/// A text edit in <see cref="Length"/> coordinates: the text in <c>[StartOffset, EndOffset)</c> is replaced by a text
/// of length <see cref="NewLength"/> (Monaco: <c>TextEditInfo</c>).
/// </summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
internal class TextEditInfo
{
    /// <summary>Creates an edit.</summary>
    public TextEditInfo(Length startOffset, Length endOffset, Length newLength)
    {
        StartOffset = startOffset;
        EndOffset = endOffset;
        NewLength = newLength;
    }

    /// <summary>The start of the replaced text, in the old text.</summary>
    public Length StartOffset { get; }

    /// <summary>The end (exclusive) of the replaced text, in the old text.</summary>
    public Length EndOffset { get; }

    /// <summary>The length of the new text.</summary>
    public Length NewLength { get; }

    /// <summary>
    /// Converts the changes of a model content change event to edits sorted in ascending order
    /// (Monaco: <c>TextEditInfo.fromModelContentChanges</c>).
    /// </summary>
    /// <param name="changes">The changes, in the order of the event (descending, as the model gives them).</param>
    public static List<TextEditInfo> FromModelContentChanges(IReadOnlyList<ModelContentChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);

        // Must be sorted in ascending order
        var edits = new List<TextEditInfo>(changes.Count);
        foreach (var change in changes)
        {
            var range = change.Range;
            edits.Add(new TextEditInfo(
                Length.FromPosition(range.Start),
                Length.FromPosition(range.End),
                Length.OfString(change.Text)));
        }

        edits.Reverse();
        return edits;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"[{StartOffset.ToObj()}...{EndOffset.ToObj()}) -> {NewLength.ToObj()}";
    }
}
