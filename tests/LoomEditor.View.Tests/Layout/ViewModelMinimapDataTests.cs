using LoomEditor.Core.Decorations;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

// All the tests are new: Monaco has none for the minimap data of the view model.
public class ViewModelMinimapDataTests
{
    private static ModelDecorationOptions MinimapOptions() => ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData
    {
        Description = "minimap",
        Minimap = new ModelDecorationMinimapOptions(DecorationColor.FromValue("#ff0000"), null, MinimapPosition.Inline),
    });

    private static TextModel CreateBracketsModel(string text)
    {
        var codec = new LanguageIdCodec();
        codec.Register("brackets");
        var configurations = new LanguageConfigurationRegistry();
        configurations.Register("brackets", new LanguageConfiguration { Brackets = [new("(", ")"), new("[", "]")] });
        var model = new TextModel(text, DefaultEndOfLine.LF, null, new TokenizationRegistry(), codec, null, configurations);
        model.Tokenization.SetLanguageId("brackets");
        return model;
    }

    [Fact]
    public void GetMinimapDecorationsInRange_DoesNotReturnBracketColors()
    {
        TestCodeEditor.WithTestCodeEditor(CreateBracketsModel("(a)\n[b]"), editor =>
        {
            var range = new Range(1, 1, 2, 4);

            static bool IsBracketColor(ViewModelDecoration d) => d.Options.Description == "BracketPairColorization";
            Assert.Contains(editor.ViewModel.GetDecorationsInViewport(range), IsBracketColor);
            Assert.DoesNotContain(editor.ViewModel.GetMinimapDecorationsInRange(range), IsBracketColor);
        });
    }

    [Fact]
    public void GetMinimapDecorationsInRange_ReturnsDecorationsWithMinimapOptions()
    {
        TestCodeEditor.WithTestCodeEditor("hello\nworld", editor =>
        {
            editor.Model.ChangeDecorations(0, accessor => accessor.AddDecoration(new Range(2, 1, 2, 4), MinimapOptions()));

            IReadOnlyList<ViewModelDecoration> result = editor.ViewModel.GetMinimapDecorationsInRange(new Range(1, 1, 2, 6));

            ViewModelDecoration decoration = Assert.Single(result, d => d.Options.Minimap is not null);
            Assert.Equal(new Range(2, 1, 2, 4), decoration.Range);
            Assert.NotNull(decoration.Options.Minimap);
        });
    }

    [Fact]
    public void GetMinimapLinesRenderingData_HasTheTabSizeAndTheNeededLines()
    {
        TestCodeEditor.WithTestCodeEditor("one\ntwo\nthree", editor =>
        {
            MinimapLinesRenderingData data = editor.ViewModel.GetMinimapLinesRenderingData(1, 3, [true, false, true]);

            Assert.Equal(editor.Model.Options.TabSize, data.TabSize);
            Assert.Equal(3, data.Data.Length);
            Assert.Equal("one", data.Data[0]!.Content);
            Assert.Null(data.Data[1]);
            Assert.Equal("three", data.Data[2]!.Content);
        });
    }
}
