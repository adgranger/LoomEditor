// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/test/browser/completionModel.test.ts (createSuggestItem)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;
using LoomEditor.View.Suggest;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Suggest.TestHelpers;

internal static class TestSuggestItems
{
    /// <summary>
    /// Added to every column: Monaco's tests build ranges that start before column 1 (position column 1, overwriteBefore 3), which
    /// <see cref="Range"/> rejects. The completion model only uses differences of columns, so the model's column is shifted by the same amount.
    /// </summary>
    public const int ColumnShift = 10;

    /// <summary>Monaco: <c>createSuggestItem</c>. The range is <c>[position.Column - overwriteBefore, position.Column]</c> on the position's line (columns shifted by <see cref="ColumnShift"/>).</summary>
    public static SuggestItem CreateSuggestItem(
        string label,
        int overwriteBefore,
        CompletionItemKind kind = CompletionItemKind.Property,
        bool incomplete = false,
        Position? position = null,
        string? sortText = null,
        string? filterText = null,
        string? labelDetail = null)
    {
        Position basePos = position ?? new Position(1, 1);
        var pos = new Position(basePos.LineNumber, basePos.Column + ColumnShift);
        var suggestion = new CompletionItem(label, label, kind)
        {
            SortText = sortText,
            FilterText = filterText,
            LabelDetail = labelDetail,
            Range = CompletionRange.FromRange(new Range(pos.LineNumber, pos.Column - overwriteBefore, pos.LineNumber, pos.Column)),
        };
        return new SuggestItem(pos, suggestion, new CompletionList([suggestion], incomplete), new FakeCompletionProvider());
    }
}
