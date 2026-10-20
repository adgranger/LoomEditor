// Ported from microsoft/vscode: src/vs/editor/common/model/mirrorTextModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model;

/// <summary>
/// The content changes of a model, copied for another thread (Monaco: <c>IModelChangedEvent</c>, without
/// <c>isUndoing</c> and <c>isRedoing</c>). Built on the UI thread from <see cref="ModelContentChangedEventArgs"/>.
/// </summary>
/// <param name="Changes">The changes, in the order to apply them.</param>
/// <param name="Eol">The (new) end-of-line sequence; empty when unknown.</param>
/// <param name="VersionId">The new version id the model has transitioned to.</param>
/// <remarks>Threading: immutable (the list must not be changed after creation).</remarks>
internal sealed record MirrorModelChangedEvent(IReadOnlyList<ModelContentChange> Changes, string Eol, int VersionId);

/// <summary>
/// A light copy of a document that applies content changes line by line (Monaco: <c>MirrorTextModel</c>), the model of
/// the tokenization worker. Differences: no <c>URI</c> (nothing reads it), lines are <see cref="List{T}"/>.
/// </summary>
/// <remarks>Threading: not thread-safe; used by the one thread that owns it (the worker's).</remarks>
internal class MirrorTextModel
{
    private string? _cachedTextValue;

    /// <summary>The lines, without line breaks.</summary>
    protected List<string> _lines;

    /// <summary>The end-of-line sequence joining the lines.</summary>
    protected string _eol;

    /// <summary>The version id of the last applied event.</summary>
    protected int _versionId;

    /// <summary>The length of every line plus an end-of-line sequence; <see langword="null"/> until needed.</summary>
    protected PrefixSumComputer? _lineStarts;

    /// <summary>Creates a mirror of <paramref name="lines"/> (not copied).</summary>
    public MirrorTextModel(List<string> lines, string eol, int versionId)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(eol);
        _lines = lines;
        _eol = eol;
        _versionId = versionId;
    }

    /// <summary><c>version</c>.</summary>
    public int Version => _versionId;

    /// <summary><c>dispose</c>.</summary>
    public void Dispose() => _lines.Clear();

    /// <summary><c>getText</c>.</summary>
    public string GetText() => _cachedTextValue ??= string.Join(_eol, _lines);

    /// <summary><c>onEvents</c>.</summary>
    public void OnEvents(MirrorModelChangedEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (e.Eol.Length > 0 && e.Eol != _eol)
        {
            _eol = e.Eol;
            _lineStarts = null;
        }

        // Update my lines
        foreach (ModelContentChange change in e.Changes)
        {
            AcceptDeleteRange(change.Range);
            AcceptInsertText(new Position(change.Range.StartLineNumber, change.Range.StartColumn), change.Text);
        }

        _versionId = e.VersionId;
        _cachedTextValue = null;
    }

    /// <summary><c>_ensureLineStarts</c>.</summary>
    protected void EnsureLineStarts()
    {
        if (_lineStarts is not null)
        {
            return;
        }

        int eolLength = _eol.Length;
        var lineStartValues = new uint[_lines.Count];
        for (int i = 0; i < lineStartValues.Length; i++)
        {
            lineStartValues[i] = (uint)(_lines[i].Length + eolLength);
        }

        _lineStarts = new PrefixSumComputer(lineStartValues);
    }

    // All changes to a line's text go through this method.
    private void SetLineText(int lineIndex, string newValue)
    {
        _lines[lineIndex] = newValue;
        _lineStarts?.SetValue(lineIndex, newValue.Length + _eol.Length);
    }

    private void AcceptDeleteRange(Range range)
    {
        if (range.StartLineNumber == range.EndLineNumber)
        {
            if (range.StartColumn == range.EndColumn)
            {
                // Nothing to delete
                return;
            }

            // Delete text on the affected line
            string line = _lines[range.StartLineNumber - 1];
            SetLineText(range.StartLineNumber - 1, line[..(range.StartColumn - 1)] + line[(range.EndColumn - 1)..]);
            return;
        }

        // Take remaining text on last line and append it to remaining text on first line
        SetLineText(
            range.StartLineNumber - 1,
            _lines[range.StartLineNumber - 1][..(range.StartColumn - 1)] + _lines[range.EndLineNumber - 1][(range.EndColumn - 1)..]);

        // Delete middle lines
        _lines.RemoveRange(range.StartLineNumber, range.EndLineNumber - range.StartLineNumber);
        _lineStarts?.RemoveValues(range.StartLineNumber, range.EndLineNumber - range.StartLineNumber);
    }

    private void AcceptInsertText(Position position, string insertText)
    {
        if (insertText.Length == 0)
        {
            // Nothing to insert
            return;
        }

        string[] insertLines = Strings.SplitLines(insertText);
        string current = _lines[position.LineNumber - 1];
        if (insertLines.Length == 1)
        {
            // Inserting text on one line
            SetLineText(position.LineNumber - 1, current[..(position.Column - 1)] + insertLines[0] + current[(position.Column - 1)..]);
            return;
        }

        // Append overflowing text from first line to the end of text to insert
        insertLines[^1] += current[(position.Column - 1)..];

        // Delete overflowing text from first line and insert text on first line
        SetLineText(position.LineNumber - 1, current[..(position.Column - 1)] + insertLines[0]);

        // Insert new lines & store lengths
        var newLengths = new uint[insertLines.Length - 1];
        for (int i = 1; i < insertLines.Length; i++)
        {
            _lines.Insert(position.LineNumber + i - 1, insertLines[i]);
            newLengths[i - 1] = (uint)(insertLines[i].Length + _eol.Length);
        }

        _lineStarts?.InsertValues(position.LineNumber, newLengths);
    }
}
