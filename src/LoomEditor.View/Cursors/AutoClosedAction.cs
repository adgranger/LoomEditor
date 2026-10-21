// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursor.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

/// <summary>The characters the editor closed automatically, tracked with two decorations each (Monaco: <c>AutoClosedAction</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class AutoClosedAction(TextModel model, string[] autoClosedCharactersDecorations, string[] autoClosedEnclosingDecorations) : IDisposable
{
    private string[] _autoClosedCharactersDecorations = autoClosedCharactersDecorations;
    private string[] _autoClosedEnclosingDecorations = autoClosedEnclosingDecorations;

    /// <summary>Monaco: <c>getAllAutoClosedCharacters</c>.</summary>
    public static List<Range> GetAllAutoClosedCharacters(IEnumerable<AutoClosedAction> autoClosedActions)
    {
        var autoClosedCharacters = new List<Range>();
        foreach (AutoClosedAction autoClosedAction in autoClosedActions)
        {
            autoClosedCharacters.AddRange(autoClosedAction.GetAutoClosedCharactersRanges());
        }

        return autoClosedCharacters;
    }

    /// <summary>Removes the decorations.</summary>
    public void Dispose() =>
        model.ChangeDecorations(0, accessor =>
        {
            _autoClosedCharactersDecorations = accessor.DeltaDecorations(_autoClosedCharactersDecorations, []);
            _autoClosedEnclosingDecorations = accessor.DeltaDecorations(_autoClosedEnclosingDecorations, []);
        });

    public List<Range> GetAutoClosedCharactersRanges()
    {
        var result = new List<Range>();
        foreach (string decoration in _autoClosedCharactersDecorations)
        {
            if (model.GetDecorationRange(decoration) is { } decorationRange)
            {
                result.Add(decorationRange);
            }
        }

        return result;
    }

    /// <summary>Whether every selection is still strictly inside its pair and no pair spans lines.</summary>
    public bool IsValid(IReadOnlyList<Selection> selections)
    {
        var enclosingRanges = new List<Range>();
        foreach (string decoration in _autoClosedEnclosingDecorations)
        {
            if (model.GetDecorationRange(decoration) is { } decorationRange)
            {
                enclosingRanges.Add(decorationRange);
                if (decorationRange.StartLineNumber != decorationRange.EndLineNumber)
                {
                    // Stop tracking if the range becomes multiline...
                    return false;
                }
            }
        }

        // Monaco sorts both arrays in place; copies are sorted here, with a stable sort.
        Comparer<Range> comparer = Comparer<Range>.Create(Range.CompareUsingStarts);
        Range[] sortedEnclosingRanges = [.. enclosingRanges.OrderBy(r => r, comparer)];
        Range[] sortedSelections = [.. selections.Select(s => s.Range).OrderBy(r => r, comparer)];

        for (int i = 0; i < sortedSelections.Length; i++)
        {
            if (i >= sortedEnclosingRanges.Length)
            {
                return false;
            }

            if (!sortedEnclosingRanges[i].StrictContains(sortedSelections[i]))
            {
                return false;
            }
        }

        return true;
    }
}
