using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Gallery;

/// <summary>
/// The actions of the text model demo. Each one is a single undo step.
/// </summary>
/// <remarks>Threading: call on the UI thread only, like the <see cref="TextModel"/> they work on.</remarks>
internal static class ModelDemoActions
{
    /// <summary>Inserts a comment line at the very beginning of the text.</summary>
    public static void InsertHeader(TextModel model)
    {
        Push(model, new[] { new TextEdit(new Range(1, 1, 1, 1), "// Edited with TextModel\n") });
    }

    /// <summary>Appends a new line at the end of the last line.</summary>
    public static void AppendLine(TextModel model)
    {
        int lastLine = model.LineCount;
        var end = new Position(lastLine, model.GetLineMaxColumn(lastLine));
        Push(model, new[] { new TextEdit(new Range(end.LineNumber, end.Column, end.LineNumber, end.Column), "\n// line appended at " + lastLine) });
    }

    /// <summary>Deletes the first line, including its end of line. Does nothing when there is a single line.</summary>
    public static void DeleteFirstLine(TextModel model)
    {
        if (model.LineCount < 2)
        {
            return;
        }

        Push(model, new[] { new TextEdit(new Range(1, 1, 2, 1), string.Empty) });
    }

    /// <summary>Replaces every "Hello" by "Howdy" in a single batch of edits.</summary>
    /// <returns>The number of replacements.</returns>
    public static int ReplaceHello(TextModel model)
    {
        const string Search = "Hello";
        const string Replacement = "Howdy";

        var edits = new List<TextEdit>();
        for (int line = 1; line <= model.LineCount; line++)
        {
            string content = model.GetLineContent(line);
            int index = content.IndexOf(Search, StringComparison.Ordinal);
            while (index >= 0)
            {
                edits.Add(new TextEdit(new Range(line, index + 1, line, index + 1 + Search.Length), Replacement));
                index = content.IndexOf(Search, index + Search.Length, StringComparison.Ordinal);
            }
        }

        if (edits.Count > 0)
        {
            Push(model, edits);
        }

        return edits.Count;
    }

    /// <summary>Changes the end of line sequence, as one undo step.</summary>
    public static void SetEol(TextModel model, EndOfLineSequence eol)
    {
        model.PushStackElement();
        model.PushEol(eol);
    }

    private static readonly ModelDecorationOptions InjectedHint = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "gallery-injected-hint",
        After = new InjectedTextOptions { Content = ": int", InlineClassName = "gallery-injected-hint" },
    });

    /// <summary>Injects a dim ": int" after <paramref name="position"/> (a decoration, not text: the model is unchanged).</summary>
    /// <returns>The id of the decoration, for <see cref="ClearInjectedText"/>.</returns>
    public static string InjectText(TextModel model, Position position)
    {
        var range = new Range(position.LineNumber, position.Column, position.LineNumber, position.Column);
        return model.ChangeDecorations(0, accessor => accessor.AddDecoration(range, InjectedHint));
    }

    /// <summary>Removes the decorations <see cref="InjectText"/> added.</summary>
    public static void ClearInjectedText(TextModel model, IReadOnlyList<string> decorationIds)
    {
        model.ChangeDecorations(0, accessor => accessor.DeltaDecorations(decorationIds, []));
    }

    /// <summary>Formats the state of the model for the status line.</summary>
    public static string FormatStatus(TextModel model)
    {
        return $"Lines: {model.LineCount} · Length: {model.GetValueLength()} · EOL: {(model.EndOfLineSequence == EndOfLineSequence.LF ? "LF" : "CRLF")}"
            + $" · Version: {model.VersionId} · Alt. version: {model.AlternativeVersionId}"
            + $" · Undo: {(model.CanUndo ? "yes" : "no")} · Redo: {(model.CanRedo ? "yes" : "no")}";
    }

    private static void Push(TextModel model, IReadOnlyList<TextEdit> edits)
    {
        model.PushStackElement();
        model.PushEditOperations(null, edits, null);
    }
}
