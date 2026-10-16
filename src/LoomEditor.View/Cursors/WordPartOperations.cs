// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorWordOperations.ts (class WordPartOperations)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

/// <summary>Word moves and deletes by "word part": the pieces of <c>camelCase</c> and <c>snake_case</c> identifiers.</summary>
/// <remarks>
/// Monaco's <c>WordPartOperations extends WordOperations</c>; static classes cannot inherit in C#, so it calls
/// <see cref="WordOperations"/>. Threading: pure functions; thread-safe as long as the model is not modified concurrently.
/// </remarks>
internal static class WordPartOperations
{
    // Monaco: candidates[2] / candidates[0] can be undefined; the callers then fall back (see DeleteWordPartLeft in the feature).
    public static Range? DeleteWordPartLeft(DeleteWordContext ctx)
    {
        var model = new TextModelCursorSimpleModel(ctx.Model);
        List<Range> candidates = EnforceDefined(
            WordOperations.DeleteWordLeft(ctx, WordNavigationType.WordStart),
            WordOperations.DeleteWordLeft(ctx, WordNavigationType.WordEnd),
            WordOperations.DeleteWordPartLeft(model, ctx.Selection));
        candidates = candidates.OrderBy(r => r, Comparer<Range>.Create(Range.CompareUsingEnds)).ToList();
        return candidates.Count > 2 ? candidates[2] : null;
    }

    public static Range? DeleteWordPartRight(DeleteWordContext ctx)
    {
        var model = new TextModelCursorSimpleModel(ctx.Model);
        List<Range> candidates = EnforceDefined(
            WordOperations.DeleteWordRight(ctx, WordNavigationType.WordStart),
            WordOperations.DeleteWordRight(ctx, WordNavigationType.WordEnd),
            WordOperations.DeleteWordPartRight(model, ctx.Selection));
        candidates = candidates.OrderBy(r => r, Comparer<Range>.Create(Range.CompareUsingStarts)).ToList();
        return candidates.Count > 0 ? candidates[0] : null;
    }

    public static Position MoveWordPartLeft(WordCharacterClassifier wordSeparators, ICursorSimpleModel model, Position position, bool hasMulticursor)
    {
        List<Position> candidates =
        [
            WordOperations.MoveWordLeft(wordSeparators, model, position, WordNavigationType.WordStart, hasMulticursor),
            WordOperations.MoveWordLeft(wordSeparators, model, position, WordNavigationType.WordEnd, hasMulticursor),
            WordOperations.MoveWordPartLeft(model, position),
        ];
        candidates = candidates.Order().ToList();
        return candidates[2];
    }

    public static Position MoveWordPartRight(WordCharacterClassifier wordSeparators, ICursorSimpleModel model, Position position)
    {
        List<Position> candidates =
        [
            WordOperations.MoveWordRight(wordSeparators, model, position, WordNavigationType.WordStart),
            WordOperations.MoveWordRight(wordSeparators, model, position, WordNavigationType.WordEnd),
            WordOperations.MoveWordPartRight(model, position),
        ];
        candidates = candidates.Order().ToList();
        return candidates[0];
    }

    private static List<Range> EnforceDefined(Range? first, Range? second, Range third)
    {
        var result = new List<Range>(3);
        if (first is { } a)
        {
            result.Add(a);
        }

        if (second is { } b)
        {
            result.Add(b);
        }

        result.Add(third);
        return result;
    }
}
