// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursor.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Edits;

namespace LoomEditor.View.Cursors;

internal static partial class CommandExecutor
{
    // Monaco: _getLoserCursorMap. The result is a set of the major identifiers (Monaco: an object keyed by their strings).
    private static HashSet<int> GetLoserCursorMap(List<IdentifiedEditOperation> rawOperations)
    {
        // This is destructive on the list
        var operations = new List<IdentifiedEditOperation>(rawOperations);

        // Sort operations with last one first. Note the minus! JavaScript's sort is stable and so is OrderBy.
        operations = operations
            .OrderBy(static op => op, Comparer<IdentifiedEditOperation>.Create(static (a, b) => -Range.CompareUsingEnds(a.Range, b.Range)))
            .ToList();

        // Operations can not overlap!
        var loserCursors = new HashSet<int>();

        for (int i = 1; i < operations.Count; i++)
        {
            IdentifiedEditOperation previousOp = operations[i - 1];
            IdentifiedEditOperation currentOp = operations[i];

            if (previousOp.Range.Start < currentOp.Range.End)
            {
                int loserMajor;

                if (previousOp.Identifier!.Value.Major > currentOp.Identifier!.Value.Major)
                {
                    // previousOp loses the battle
                    loserMajor = previousOp.Identifier.Value.Major;
                }
                else
                {
                    loserMajor = currentOp.Identifier.Value.Major;
                }

                loserCursors.Add(loserMajor);

                for (int j = 0; j < operations.Count; j++)
                {
                    if (operations[j].Identifier!.Value.Major == loserMajor)
                    {
                        operations.RemoveAt(j);
                        if (j < i)
                        {
                            i--;
                        }

                        j--;
                    }
                }

                if (i > 0)
                {
                    i--;
                }
            }
        }

        return loserCursors;
    }
}
