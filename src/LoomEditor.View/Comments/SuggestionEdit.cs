using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Comments;

/// <summary>Turns a suggestion block into the edit that replaces a range of whole model lines.</summary>
/// <remarks>Threading: reads a <see cref="TextModel"/>: UI thread.</remarks>
internal static class SuggestionEdit
{
    /// <summary>
    /// The contents of lines <paramref name="startLineNumber"/> to <paramref name="endLineNumber"/>, inclusive, joined with
    /// <c>"\n"</c> whatever the model's end of line sequence is. Callers clamp the range to the model.
    /// </summary>
    public static string GetRangeText(TextModel model, int startLineNumber, int endLineNumber)
    {
        var lines = new string[endLineNumber - startLineNumber + 1];
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = model.GetLineContent(startLineNumber + i);
        }

        return string.Join('\n', lines);
    }

    /// <summary>The edit replacing lines <paramref name="startLineNumber"/> to <paramref name="endLineNumber"/>, inclusive, or null when the range does not exist.</summary>
    public static TextEdit? Create(TextModel model, int startLineNumber, int endLineNumber, SuggestionBlock block)
    {
        if (startLineNumber < 1 || endLineNumber > model.LineCount || startLineNumber > endLineNumber)
        {
            return null;
        }

        int endMaxColumn = model.GetLineMaxColumn(endLineNumber);
        if (!block.IsDeletion)
        {
            return new TextEdit(new Range(startLineNumber, 1, endLineNumber, endMaxColumn), string.Join(model.Eol, block.Lines));
        }

        if (endLineNumber < model.LineCount)
        {
            return new TextEdit(new Range(startLineNumber, 1, endLineNumber + 1, 1), string.Empty);
        }

        if (startLineNumber > 1)
        {
            return new TextEdit(
                new Range(startLineNumber - 1, model.GetLineMaxColumn(startLineNumber - 1), endLineNumber, endMaxColumn), string.Empty);
        }

        return new TextEdit(new Range(1, 1, endLineNumber, endMaxColumn), string.Empty);
    }

    /// <summary>The edit replacing line <paramref name="lineNumber"/>, or null when the line does not exist.</summary>
    public static TextEdit? Create(TextModel model, int lineNumber, SuggestionBlock block) =>
        Create(model, lineNumber, lineNumber, block);

    /// <summary>True when lines <paramref name="startLineNumber"/> to <paramref name="endLineNumber"/>, inclusive, no longer exist or their text differs from <paramref name="expectedRangeText"/>.</summary>
    public static bool IsStale(TextModel model, int startLineNumber, int endLineNumber, string expectedRangeText) =>
        startLineNumber < 1 || endLineNumber > model.LineCount || startLineNumber > endLineNumber
        || !string.Equals(GetRangeText(model, startLineNumber, endLineNumber), expectedRangeText, StringComparison.Ordinal);

    /// <summary>True when the line no longer exists or its content differs from <paramref name="expectedLineContent"/>.</summary>
    public static bool IsStale(TextModel model, int lineNumber, string expectedLineContent) =>
        IsStale(model, lineNumber, lineNumber, expectedLineContent);

    /// <summary>True when applying the suggestion to lines <paramref name="startLineNumber"/> to <paramref name="endLineNumber"/>, inclusive, would not change the model.</summary>
    public static bool IsNoOp(TextModel model, int startLineNumber, int endLineNumber, SuggestionBlock block) =>
        Create(model, startLineNumber, endLineNumber, block) is not { } edit
        || model.GetValueInRange(edit.Range) == edit.Text.Replace("\r\n", "\n").Replace("\n", model.Eol);

    /// <summary>True when applying the suggestion would not change the model.</summary>
    public static bool IsNoOp(TextModel model, int lineNumber, SuggestionBlock block) =>
        IsNoOp(model, lineNumber, lineNumber, block);
}
