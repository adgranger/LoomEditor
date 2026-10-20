using LoomEditor.Core.Model;

namespace LoomEditor.Core.Tests.Model;

/// <summary>Tests of the model options (<see cref="TextModel.Options"/>, <see cref="TextModel.UpdateOptions"/>) and the indent helpers. New tests.</summary>
public class TextModelOptionsTests
{
    [Fact]
    public void Options_NewModel_HasMonacoDefaults()
    {
        var model = new TextModel("x");

        Assert.Equal(4, model.Options.TabSize);
        Assert.Equal(4, model.Options.IndentSize);
        Assert.True(model.Options.InsertSpaces);
        Assert.True(model.Options.TrimAutoWhitespace);
        Assert.False(model.Options.IndentSizeIsTabSize);
    }

    [Fact]
    public void UpdateOptions_TabSize_RaisesOptionsChangedOnce()
    {
        var model = new TextModel("x");
        var events = new List<ModelOptionsChangedEventArgs>();
        model.OptionsChanged += (_, e) => events.Add(e);

        model.UpdateOptions(new TextModelUpdateOptions(TabSize: 8));
        model.UpdateOptions(new TextModelUpdateOptions(TabSize: 8));

        Assert.Equal(8, model.Options.TabSize);
        ModelOptionsChangedEventArgs e = Assert.Single(events);
        Assert.True(e.TabSize);
        Assert.False(e.IndentSize);
        Assert.False(e.InsertSpaces);
        Assert.False(e.TrimAutoWhitespace);
    }

    [Fact]
    public void UpdateOptions_IndentSizeIsTabSize_FollowsLaterTabSizeChanges()
    {
        var model = new TextModel("x");

        model.UpdateOptions(new TextModelUpdateOptions(IndentSizeIsTabSize: true));
        model.UpdateOptions(new TextModelUpdateOptions(TabSize: 2));

        Assert.True(model.Options.IndentSizeIsTabSize);
        Assert.Equal(2, model.Options.IndentSize);
    }

    [Fact]
    public void UpdateOptions_ExplicitIndentSize_IsKeptWhenTabSizeChanges()
    {
        var model = new TextModel("x");

        model.UpdateOptions(new TextModelUpdateOptions(IndentSize: 3));
        model.UpdateOptions(new TextModelUpdateOptions(TabSize: 8));

        Assert.False(model.Options.IndentSizeIsTabSize);
        Assert.Equal(3, model.Options.IndentSize);
    }

    [Fact]
    public void UpdateOptions_InsertSpacesAndTrimAutoWhitespace_AreReported()
    {
        var model = new TextModel("x");
        ModelOptionsChangedEventArgs? received = null;
        model.OptionsChanged += (_, e) => received = e;

        model.UpdateOptions(new TextModelUpdateOptions(InsertSpaces: false, TrimAutoWhitespace: false));

        Assert.NotNull(received);
        Assert.False(received.TabSize);
        Assert.True(received.InsertSpaces);
        Assert.True(received.TrimAutoWhitespace);
        Assert.False(model.Options.InsertSpaces);
        Assert.False(model.Options.TrimAutoWhitespace);
    }

    [Fact]
    public void UpdateOptions_TabSizeZero_IsClampedToOne()
    {
        var model = new TextModel("x");

        model.UpdateOptions(new TextModelUpdateOptions(TabSize: 0));

        Assert.Equal(1, model.Options.TabSize);
    }

    [Fact]
    public void Constructor_WithOptions_UsesThem()
    {
        var model = new TextModel("x", options: new TextModelOptions(tabSize: 2, insertSpaces: false, defaultEol: DefaultEndOfLine.CRLF));

        Assert.Equal(2, model.Options.TabSize);
        Assert.True(model.Options.IndentSizeIsTabSize);
        Assert.False(model.Options.InsertSpaces);
        Assert.Equal(DefaultEndOfLine.CRLF, model.Options.DefaultEol);
        Assert.Equal("\r\n", model.Eol);
    }

    [Fact]
    public void Equals_SameValues_AreEqual()
    {
        Assert.Equal(new TextModelOptions(tabSize: 2), new TextModelOptions(tabSize: 2));
        Assert.Equal(new TextModelOptions(tabSize: 2).GetHashCode(), new TextModelOptions(tabSize: 2).GetHashCode());
        Assert.NotEqual(new TextModelOptions(tabSize: 4), TextModelOptions.Default);
    }

    [Theory]
    [InlineData("\t  x", 4)]
    [InlineData("", 1)]
    [InlineData("   ", 4)]
    [InlineData("x  ", 1)]
    public void GetLineIndentColumn_ReturnsTheColumnAfterTheLeadingWhitespace(string line, int expected)
    {
        var model = new TextModel(line);

        Assert.Equal(expected, model.GetLineIndentColumn(1));
    }

    // Monaco: 'issue #168836: updating tabSize should also update indentSize when indentSize is set to "tabSize"'
    [Fact]
    public void Issue168836_UpdatingTabSizeAlsoUpdatesIndentSizeWhenItFollowsTheTabSize()
    {
        var m = new TextModel("some text", options: new TextModelOptions(tabSize: 2, indentSize: null));
        Assert.Equal(2, m.Options.TabSize);
        Assert.Equal(2, m.Options.IndentSize);
        Assert.True(m.Options.IndentSizeIsTabSize);
        m.UpdateOptions(new TextModelUpdateOptions(TabSize: 4));
        Assert.Equal(4, m.Options.TabSize);
        Assert.Equal(4, m.Options.IndentSize);
        Assert.True(m.Options.IndentSizeIsTabSize);
    }
}
