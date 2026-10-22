using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Features.WordHighlighter;

namespace LoomEditor.View.Tests.Features.WordHighlighter;

// New tests: wordHighlighter.test.ts tests the registry of providers (phase 8); its textual expectations are reused here.
public class TextualDocumentHighlightProviderTests
{
    // Monaco: 'DocumentHighlightProviders' › 'uses textual fallback only when no other provider matches'
    [Fact]
    public void TextualProvider_WordWordWord_ReturnsFourTextHighlights()
    {
        var model = new TextModel("word word word\nword");

        List<DocumentHighlight> highlights = TextualDocumentHighlightProvider.ProvideDocumentHighlights(model, new Position(1, 2));

        Assert.Equal(
            [
                new DocumentHighlight(new Range(1, 1, 1, 5), DocumentHighlightKind.Text),
                new DocumentHighlight(new Range(1, 6, 1, 10), DocumentHighlightKind.Text),
                new DocumentHighlight(new Range(1, 11, 1, 15), DocumentHighlightKind.Text),
                new DocumentHighlight(new Range(2, 1, 2, 5), DocumentHighlightKind.Text),
            ],
            highlights);
    }

    [Fact]
    public void TextualProvider_NotOnAWord_ReturnsEmpty()
    {
        var model = new TextModel("word  word");

        Assert.Empty(TextualDocumentHighlightProvider.ProvideDocumentHighlights(model, new Position(1, 6)));
    }

    [Fact]
    public void TextualProvider_WholeWordsOnly_SkipsLongerWords()
    {
        var model = new TextModel("word words word");

        Assert.Equal(2, TextualDocumentHighlightProvider.ProvideDocumentHighlights(model, new Position(1, 1)).Count);
    }
}
