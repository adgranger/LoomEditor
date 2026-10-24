// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/test/browser/suggestModel.test.ts (suite 'SuggestModel - Context')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Suggest;

namespace LoomEditor.View.Tests.Suggest;

public class SuggestModelContextTests
{
    // Not ported: 'shouldAutoTrigger at embedded language boundaries' (needs embedded languages).
    // Monaco: 'Context - shouldAutoTrigger'
    [Theory]
    [InlineData(3, true)] // end of word, Das|
    [InlineData(4, false)] // no word Das |
    [InlineData(1, true)] // typing a single character before a word: D|as
    [InlineData(55, false)] // number, 1861|
    public void ShouldAutoTrigger_MonacoCases(int offset, bool expected)
    {
        var model = new TextModel("Das Pferd frisst keinen Gurkensalat - Philipp Reis 1861.\nWer hat's erfunden?");
        Position position = model.GetPositionAt(offset);

        Assert.Equal(expected, LineContext.ShouldAutoTrigger(model, position));
    }

    [Fact]
    public void ShouldAutoTrigger_CursorInTheMiddleOfAWord_IsFalse()
    {
        var model = new TextModel("abcdef");

        Assert.False(LineContext.ShouldAutoTrigger(model, new Position(1, 4)));
    }

    [Fact]
    public void Constructor_ReadsTheLineBeforeTheCursorAndTheLeadingWord()
    {
        var model = new TextModel("one two\nfoo.bar");
        var options = new SuggestTriggerOptions(Auto: true);

        var ctx = new LineContext(model, new Position(2, 6), options);

        Assert.Equal(2, ctx.LineNumber);
        Assert.Equal(6, ctx.Column);
        Assert.Equal("foo.b", ctx.LeadingLineContent);
        Assert.Equal("b", ctx.LeadingWord.Word);
        Assert.Same(options, ctx.TriggerOptions);
    }
}
