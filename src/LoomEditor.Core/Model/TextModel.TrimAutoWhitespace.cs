// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model;

// Trim auto-inserted whitespace: the lines that got whitespace from an auto-whitespace edit (e.g. the indentation
// that Enter inserts) are emptied by the next PushEditOperations, if no incoming edit conflicts with it.
public sealed partial class TextModel
{
    // Monaco: _trimAutoWhitespaceLines
    private IReadOnlyList<int>? _trimAutoWhitespaceLines;

    // Monaco: _pushEditOperations, the "if (this._options.trimAutoWhitespace && this._trimAutoWhitespaceLines)" block
    private void AddTrimAutoWhitespaceEdits(IReadOnlyList<Selection>? beforeCursorState, List<IdentifiedEditOperation> editOperations)
    {
        // Go through each saved line number and insert a trim whitespace edit
        // if it is safe to do so (no conflicts with other edits).
        IReadOnlyList<int> trimLines = _trimAutoWhitespaceLines!;

        var incomingEdits = new List<(Range Range, string? Text)>(editOperations.Count);
        foreach (IdentifiedEditOperation op in editOperations)
        {
            ValidAnnotatedEditOperation valid = ValidateEditOperation(op);
            incomingEdits.Add((ValidateRange(valid.Range), valid.Text));
        }

        // Sometimes, auto-formatters change ranges automatically which can cause undesired auto whitespace trimming near the cursor
        // We'll use the following heuristic: if the edits occur near the cursor, then it's ok to trim auto whitespace
        bool editsAreNearCursors = true;
        if (beforeCursorState is not null)
        {
            foreach (Selection sel in beforeCursorState)
            {
                bool foundEditNearSel = false;
                foreach ((Range editRange, _) in incomingEdits)
                {
                    bool selIsAbove = editRange.StartLineNumber > sel.Range.EndLineNumber;
                    bool selIsBelow = sel.Range.StartLineNumber > editRange.EndLineNumber;
                    if (!selIsAbove && !selIsBelow)
                    {
                        foundEditNearSel = true;
                        break;
                    }
                }

                if (!foundEditNearSel)
                {
                    editsAreNearCursors = false;
                    break;
                }
            }
        }

        if (editsAreNearCursors)
        {
            foreach (int trimLineNumber in trimLines)
            {
                int maxLineColumn = GetLineMaxColumn(trimLineNumber);

                bool allowTrimLine = true;
                foreach ((Range editRange, string? editText) in incomingEdits)
                {
                    if (trimLineNumber < editRange.StartLineNumber || trimLineNumber > editRange.EndLineNumber)
                    {
                        // `trimLine` is completely outside this edit
                        continue;
                    }

                    // At this point:
                    //   editRange.StartLineNumber <= trimLine <= editRange.EndLineNumber

                    if (trimLineNumber == editRange.StartLineNumber && editRange.StartColumn == maxLineColumn
                        && editRange.IsEmpty && editText is { Length: > 0 } && editText[0] == '\n')
                    {
                        // This edit inserts a new line (and maybe other text) after `trimLine`
                        continue;
                    }

                    if (trimLineNumber == editRange.StartLineNumber && editRange.StartColumn == 1
                        && editRange.IsEmpty && editText is { Length: > 0 } && editText[^1] == '\n')
                    {
                        // This edit inserts a new line (and maybe other text) before `trimLine`
                        continue;
                    }

                    // Looks like we can't trim this line as it would interfere with an incoming edit
                    allowTrimLine = false;
                    break;
                }

                if (allowTrimLine)
                {
                    var trimRange = new Range(trimLineNumber, 1, trimLineNumber, maxLineColumn);
                    editOperations.Add(new IdentifiedEditOperation(trimRange, null));
                }
            }
        }

        _trimAutoWhitespaceLines = null;
    }
}
