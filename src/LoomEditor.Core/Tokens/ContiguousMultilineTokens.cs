// Ported from microsoft/vscode: src/vs/editor/common/tokens/contiguousMultilineTokens.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Tokens;

/// <summary>
/// Tokens over a contiguous range of lines: <c>tokens[index]</c> holds the <c>[endOffset, metadata]</c> pairs of line
/// <c>StartLineNumber + index</c> (Monaco: <c>ContiguousMultilineTokens</c>). Not ported: <c>deserialize</c>,
/// <c>serializeSize</c>, <c>serialize</c> (ADR 0008 decision 5: objects, not bytes) and <c>getLineRange</c>
/// (use <see cref="StartLineNumber"/> / <see cref="EndLineNumber"/>).
/// </summary>
/// <remarks>
/// Threading: a block is mutated only by its owner thread; once posted to the UI thread, the worker never touches it
/// again.
/// </remarks>
internal sealed class ContiguousMultilineTokens
{
    private List<uint[]?> _tokens;

    /// <summary>Creates a block starting at <paramref name="startLineNumber"/>.</summary>
    public ContiguousMultilineTokens(int startLineNumber, List<uint[]?> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        StartLineNumber = startLineNumber;
        _tokens = tokens;
    }

    /// <summary>(Inclusive) start line number for these tokens.</summary>
    public int StartLineNumber { get; private set; }

    /// <summary>(Inclusive) end line number for these tokens.</summary>
    public int EndLineNumber => StartLineNumber + _tokens.Count - 1;

    /// <summary><c>getLineTokens</c>: the <c>[endOffset, metadata]</c> pairs of a line of the block.</summary>
    public uint[]? GetLineTokens(int lineNumber) => _tokens[lineNumber - StartLineNumber];

    /// <summary><c>appendLineTokens</c>.</summary>
    public void AppendLineTokens(uint[] lineTokens) => _tokens.Add(lineTokens);

    /// <summary>
    /// <c>applyEdit</c>: moves the block to the version of the text after replacing <paramref name="range"/> by
    /// <paramref name="text"/>.
    /// </summary>
    public void ApplyEdit(Range range, string text)
    {
        EolCount eol = EolCounter.Count(text);
        AcceptDeleteRange(range);
        AcceptInsertText(new Position(range.StartLineNumber, range.StartColumn), eol.Count, eol.FirstLineLength);
    }

    private void AcceptDeleteRange(Range range)
    {
        if (range.StartLineNumber == range.EndLineNumber && range.StartColumn == range.EndColumn)
        {
            // Nothing to delete
            return;
        }

        int firstLineIndex = range.StartLineNumber - StartLineNumber;
        int lastLineIndex = range.EndLineNumber - StartLineNumber;

        if (lastLineIndex < 0)
        {
            // this deletion occurs entirely before this block, so we only need to adjust line numbers
            int deletedLinesCount = lastLineIndex - firstLineIndex;
            StartLineNumber -= deletedLinesCount;
            return;
        }

        if (firstLineIndex >= _tokens.Count)
        {
            // this deletion occurs entirely after this block, so there is nothing to do
            return;
        }

        if (firstLineIndex < 0 && lastLineIndex >= _tokens.Count)
        {
            // this deletion completely encompasses this block
            StartLineNumber = 0;
            _tokens = [];
            return;
        }

        if (firstLineIndex == lastLineIndex)
        {
            // a delete on a single line
            _tokens[firstLineIndex] = ContiguousTokensEditing.Delete(_tokens[firstLineIndex], range.StartColumn - 1, range.EndColumn - 1);
            return;
        }

        if (firstLineIndex >= 0)
        {
            // The first line survives
            _tokens[firstLineIndex] = ContiguousTokensEditing.DeleteEnding(_tokens[firstLineIndex], range.StartColumn - 1);

            if (lastLineIndex < _tokens.Count)
            {
                // The last line survives
                uint[]? lastLineTokens = ContiguousTokensEditing.DeleteBeginning(_tokens[lastLineIndex], range.EndColumn - 1);

                // Take remaining text on last line and append it to remaining text on first line
                _tokens[firstLineIndex] = ContiguousTokensEditing.Append(_tokens[firstLineIndex], lastLineTokens);

                // Delete middle lines
                _tokens.RemoveRange(firstLineIndex + 1, lastLineIndex - firstLineIndex);
            }
            else
            {
                // The last line does not survive

                // Take remaining text on last line and append it to remaining text on first line
                _tokens[firstLineIndex] = ContiguousTokensEditing.Append(_tokens[firstLineIndex], null);

                // Delete lines
                _tokens = _tokens.GetRange(0, firstLineIndex + 1);
            }
        }
        else
        {
            // The first line does not survive
            StartLineNumber -= -firstLineIndex;

            // Remove beginning from last line
            _tokens[lastLineIndex] = ContiguousTokensEditing.DeleteBeginning(_tokens[lastLineIndex], range.EndColumn - 1);

            // Delete lines
            _tokens = _tokens.GetRange(lastLineIndex, _tokens.Count - lastLineIndex);
        }
    }

    private void AcceptInsertText(Position position, int eolCount, int firstLineLength)
    {
        if (eolCount == 0 && firstLineLength == 0)
        {
            // Nothing to insert
            return;
        }

        int lineIndex = position.LineNumber - StartLineNumber;

        if (lineIndex < 0)
        {
            // this insertion occurs before this block, so we only need to adjust line numbers
            StartLineNumber += eolCount;
            return;
        }

        if (lineIndex >= _tokens.Count)
        {
            // this insertion occurs after this block, so there is nothing to do
            return;
        }

        if (eolCount == 0)
        {
            // Inserting text on one line
            _tokens[lineIndex] = ContiguousTokensEditing.Insert(_tokens[lineIndex], position.Column - 1, firstLineLength);
            return;
        }

        _tokens[lineIndex] = ContiguousTokensEditing.DeleteEnding(_tokens[lineIndex], position.Column - 1);
        _tokens[lineIndex] = ContiguousTokensEditing.Insert(_tokens[lineIndex], position.Column - 1, firstLineLength);

        InsertLines(position.LineNumber, eolCount);
    }

    private void InsertLines(int insertIndex, int insertCount)
    {
        if (insertCount == 0)
        {
            return;
        }

        // Monaco passes a line number as an index (kept as is); its arrayInsert appends when the index is past the end.
        _tokens.InsertRange(Math.Min(insertIndex, _tokens.Count), new uint[]?[insertCount]);
    }
}
