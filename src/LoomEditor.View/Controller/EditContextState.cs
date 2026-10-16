// Ported from microsoft/vscode: src/vs/editor/browser/controller/editContext/native/nativeEditContext.ts and nativeEditContextUtils.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Controller;

/// <summary>An update of the text the IME sees (Monaco: <c>ITextUpdateEvent</c>).</summary>
/// <param name="UpdateRangeStart">The start of the replaced range, in the text given to the IME.</param>
/// <param name="UpdateRangeEnd">The end (exclusive) of the replaced range.</param>
/// <param name="Text">The new text of that range.</param>
/// <param name="SelectionStart">The start of the IME selection after the update.</param>
/// <param name="SelectionEnd">The end of the IME selection after the update.</param>
internal readonly record struct TextUpdate(int UpdateRangeStart, int UpdateRangeEnd, string Text, int SelectionStart, int SelectionEnd);

/// <summary>What an IME update means for the cursor (Monaco: <c>ITypeData</c>).</summary>
/// <param name="Text">The text to type.</param>
/// <param name="ReplacePrevCharCnt">How many characters before the cursor it replaces.</param>
/// <param name="ReplaceNextCharCnt">How many characters after the cursor it replaces.</param>
/// <param name="PositionDelta">How many columns the cursor moves from the end of the text.</param>
internal readonly record struct TypeData(string Text, int ReplacePrevCharCnt, int ReplaceNextCharCnt, int PositionDelta);

/// <summary>The text and the selection offsets given to the IME.</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed record EditContextState(string Text, int SelectionStartOffset, int SelectionEndOffset, Selection PrimarySelection)
{
    /// <summary>
    /// The lines of the primary selection, with the selection offsets inside them, or <see langword="null"/> when the
    /// selection is not valid in the model (Monaco: <c>_getNewEditContextState</c>).
    /// </summary>
    public static EditContextState? GetNewEditContextState(TextModel model, Selection primarySelection)
    {
        ArgumentNullException.ThrowIfNull(model);
        Range selectionRange = primarySelection.Range;
        if (!model.IsValidRange(selectionRange))
        {
            return null;
        }

        int startLine = selectionRange.StartLineNumber;
        int endLine = selectionRange.EndLineNumber;
        int endColumnOfEndLineNumber = model.GetLineMaxColumn(endLine);
        var rangeOfText = new Range(startLine, 1, endLine, endColumnOfEndLineNumber);
        string text = model.GetValueInRange(rangeOfText, EndOfLinePreference.TextDefined);
        int selectionStartOffset = selectionRange.StartColumn - 1;
        int selectionEndOffset = text.Length + selectionRange.EndColumn - endColumnOfEndLineNumber;
        return new EditContextState(text, selectionStartOffset, selectionEndOffset, primarySelection);
    }
}
