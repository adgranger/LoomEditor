// Ported from microsoft/vscode: src/vs/editor/contrib/wordHighlighter/browser/textualHighlightProvider.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Search;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Features.WordHighlighter;

/// <summary>The highlights of a word without any language knowledge: its occurrences as a whole word.</summary>
/// <remarks>
/// Not ported: <c>provideMultiDocumentHighlights</c> (no other models) and the language selector (no provider registry yet).
/// Threading: UI thread only (it reads the model).
/// </remarks>
internal static class TextualDocumentHighlightProvider
{
    /// <summary>Monaco: <c>provideDocumentHighlights</c>; synchronous (no cancellation).</summary>
    public static List<DocumentHighlight> ProvideDocumentHighlights(TextModel model, Position position)
    {
        ArgumentNullException.ThrowIfNull(model);

        var result = new List<DocumentHighlight>();
        if (model.GetWordAtPosition(position) is not { } word)
        {
            return result;
        }

        foreach (FindMatch match in model.FindMatches(word.Word, true, false, true, WordHelper.UsualWordSeparators, false))
        {
            result.Add(new DocumentHighlight(match.Range, DocumentHighlightKind.Text));
        }

        return result;
    }
}
