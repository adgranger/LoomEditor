using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Controller;

namespace LoomEditor.View.Tests.Controller;

// No Monaco test covers this logic: nativeEditContext.ts is only tested through the DOM.
public class CompositionFormatTests
{
    private static (EditContextModel Model, TextModel TextModel) OnLine(string text, int lineNumber)
    {
        // The IME text is the line `lineNumber`; its offsets start at the start of that line.
        var textModel = new TextModel(text);
        var editContext = new EditContextModel();
        editContext.Update(textModel, new Selection(lineNumber, 1, lineNumber, 1));
        return (editContext, textModel);
    }

    [Fact]
    public void ToCompositionFormats_FormatsOnLineTwo_GiveModelRangesOnThatLine()
    {
        (EditContextModel editContext, TextModel model) = OnLine("abc\ndefghi\nxyz", 2);

        IReadOnlyList<CompositionFormat> formats = editContext.ToCompositionFormats(
            model, [(0, 2, "thin"), (2, 5, "thick")]);

        Assert.Equal(
            [
                new CompositionFormat(new Range(2, 1, 2, 3), CompositionUnderline.Secondary),
                new CompositionFormat(new Range(2, 3, 2, 6), CompositionUnderline.Primary),
            ],
            formats);
    }

    [Theory]
    [InlineData("THICK", (int)CompositionUnderline.Primary)]
    [InlineData("Thin", (int)CompositionUnderline.Secondary)]
    [InlineData("none", (int)CompositionUnderline.None)]
    [InlineData("", (int)CompositionUnderline.None)]
    public void ToCompositionFormats_Thickness_IsCaseInsensitiveAndUnknownMeansNone(string thickness, int expected)
    {
        (EditContextModel editContext, TextModel model) = OnLine("abc", 1);

        CompositionFormat format = Assert.Single(editContext.ToCompositionFormats(model, [(0, 1, thickness)]));

        Assert.Equal((CompositionUnderline)expected, format.Underline);
    }
}
