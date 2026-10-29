using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Comments;

/// <summary>Turns a suggestion block into the edit that replaces a model line.</summary>
/// <remarks>Threading: reads a <see cref="TextModel"/>: UI thread.</remarks>
internal static class SuggestionEdit
{
    /// <summary>The edit replacing line <paramref name="lineNumber"/>, or null when the line does not exist.</summary>
    public static TextEdit? Create(TextModel model, int lineNumber, SuggestionBlock block)
    {
        if (lineNumber < 1 || lineNumber > model.LineCount)
        {
            return null;
        }

        int maxColumn = model.GetLineMaxColumn(lineNumber);
        if (!block.IsDeletion)
        {
            return new TextEdit(new Range(lineNumber, 1, lineNumber, maxColumn), string.Join(model.Eol, block.Lines));
        }

        if (lineNumber < model.LineCount)
        {
            return new TextEdit(new Range(lineNumber, 1, lineNumber + 1, 1), string.Empty);
        }

        if (lineNumber > 1)
        {
            return new TextEdit(new Range(lineNumber - 1, model.GetLineMaxColumn(lineNumber - 1), lineNumber, maxColumn), string.Empty);
        }

        return new TextEdit(new Range(1, 1, 1, maxColumn), string.Empty);
    }

    /// <summary>True when the line no longer exists or its content differs from <paramref name="expectedLineContent"/>.</summary>
    public static bool IsStale(TextModel model, int lineNumber, string expectedLineContent) =>
        lineNumber < 1 || lineNumber > model.LineCount
        || !string.Equals(model.GetLineContent(lineNumber), expectedLineContent, StringComparison.Ordinal);

    /// <summary>True when applying the suggestion would not change the model.</summary>
    public static bool IsNoOp(TextModel model, int lineNumber, SuggestionBlock block) =>
        Create(model, lineNumber, block) is not { } edit
        || model.GetValueInRange(edit.Range) == edit.Text.Replace("\r\n", "\n").Replace("\n", model.Eol);
}
