// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Suggest;

/// <summary>The line before the cursor when a session starts or the cursor moves (Monaco: <c>LineContext</c> of <c>suggestModel.ts</c>).</summary>
/// <remarks>Threading: immutable; UI thread only (it reads the model).</remarks>
internal sealed class LineContext
{
    /// <summary>Reads the context of <paramref name="position"/> in <paramref name="model"/>.</summary>
    public LineContext(TextModel model, Position position, SuggestTriggerOptions triggerOptions)
    {
        string line = model.GetLineContent(position.LineNumber);
        LeadingLineContent = line[..Math.Min(position.Column - 1, line.Length)];
        LeadingWord = model.GetWordUntilPosition(position);
        LineNumber = position.LineNumber;
        Column = position.Column;
        TriggerOptions = triggerOptions;
    }

    /// <summary>The line number.</summary>
    public int LineNumber { get; }

    /// <summary>The column of the cursor.</summary>
    public int Column { get; }

    /// <summary>The text of the line before the cursor.</summary>
    public string LeadingLineContent { get; }

    /// <summary>The part of the word at the cursor that is before the cursor.</summary>
    public WordAtPosition LeadingWord { get; }

    /// <summary>The options of the session.</summary>
    public SuggestTriggerOptions TriggerOptions { get; }

    /// <summary>
    /// Whether typing at <paramref name="position"/> starts a session: the cursor is at the end of a word, or right after its first character,
    /// and the word is not a number. Monaco also tokenizes the line when cheap (<c>tokenizeIfCheap</c>): not ported, the word comes from the model.
    /// </summary>
    public static bool ShouldAutoTrigger(TextModel model, Position position)
    {
        if (model.GetWordAtPosition(position) is not { } word)
        {
            return false;
        }

        // after typing a single character before a word
        if (word.EndColumn != position.Column && word.StartColumn + 1 != position.Column)
        {
            return false;
        }

        // Monaco: !isNaN(Number(word))
        return !double.TryParse(word.Word, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }
}
