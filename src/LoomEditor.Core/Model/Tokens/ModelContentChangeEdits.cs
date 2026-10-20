// Ported from microsoft/vscode: src/vs/editor/common/model/textModelStringEdit.ts (linesLengthEditFromModelContentChange)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Model.Tokens;

/// <summary>Turns the content changes of a model into edits of its line array.</summary>
/// <remarks>Threading: pure and thread-safe.</remarks>
internal static class ModelContentChangeEdits
{
    /// <summary>
    /// <c>linesLengthEditFromModelContentChange</c>: one <see cref="LengthEdit"/> over line indices (a change replaces
    /// the whole lines it touches by the lines of its text). <paramref name="changes"/> are in the order of
    /// <see cref="ModelContentChangedEventArgs.Changes"/> (end of the document first).
    /// </summary>
    public static LengthEdit LinesLengthEditFromModelContentChange(IReadOnlyList<ModelContentChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var lengthEdits = new List<LengthEdit>(changes.Count);
        for (int i = changes.Count - 1; i >= 0; i--)
        {
            ModelContentChange c = changes[i];
            lengthEdits.Add(LengthEdit.Replace(
                // Expand the edit range to include the entire line
                new OffsetRange(c.Range.StartLineNumber - 1, c.Range.EndLineNumber),
                EolCounter.Count(c.Text).Count + 1));
        }

        return LengthEdit.Compose(lengthEdits);
    }
}
