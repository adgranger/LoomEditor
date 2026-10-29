using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Layout.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

// Monaco: the word wrap tests of viewModelImpl.test.ts and cursor wrapping tests come with P6-22; these are new tests.
public class ViewModelWordWrapTests
{
    private const int LineHeight = 20;

    private readonly FakeTimeProvider _clock = new();
    private readonly FakeFrameScheduler _frames = new();

    private static readonly FontInfo Font = new(1, "Consolas", "normal", 14, LineHeight, 0, EditorFontOptions.LigaturesOff, EditorFontOptions.VariationsOff, true, 8, 16, true, 8, 8, 8, 8);

    private ViewModel CreateViewModel(string text, bool wordWrap = true, int width = 400)
    {
        var layoutOptions = new EditorLayoutOptions
        {
            Minimap = new() { Enabled = false },
            Wrapping = new EditorWrappingOptions { WordWrap = wordWrap ? WordWrap.On : WordWrap.Off },
        };
        var viewModel = new ViewModel(new TextModel(text), Font, layoutOptions, new ViewLayoutOptions(LineHeight, ScrollBeyondLastLine: false), _frames.Schedule, _clock);
        viewModel.SetViewportSize(width, 100);
        return viewModel;
    }

    private static string LongLine(int words) => string.Join(' ', Enumerable.Repeat("word", words));

    [Fact]
    public void WordWrapOn_LongLine_HasSeveralViewLines()
    {
        using ViewModel viewModel = CreateViewModel(LongLine(60));

        Assert.True(viewModel.LayoutInfo.IsViewportWrapping);
        Assert.True(viewModel.GetLineCount() > viewModel.Model.LineCount);
    }

    [Fact]
    public void Resize_ChangesTheWrappingColumn_RebuildsTheViewLines()
    {
        using ViewModel viewModel = CreateViewModel(LongLine(60));
        int before = viewModel.GetLineCount();

        viewModel.SetViewportSize(200, 100);

        Assert.True(viewModel.GetLineCount() > before);
    }

    [Fact]
    public void WordWrapOff_ToOn_KeepsTheFirstVisibleModelLine()
    {
        string text = string.Join('\n', Enumerable.Range(1, 40).Select(_ => LongLine(20)));
        using ViewModel viewModel = CreateViewModel(text, wordWrap: false);
        Assert.Equal(40, viewModel.GetLineCount());
        viewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(ScrollTop: 10 * LineHeight), ScrollType.Immediate);
        int firstModelLine = viewModel.CoordinatesConverter
            .ConvertViewPositionToModelPosition(new Position(viewModel.ViewLayout.GetLinesViewportData().StartLineNumber, 1)).LineNumber;

        viewModel.SetOptions(
            viewModel.LayoutOptions with { Wrapping = new EditorWrappingOptions { WordWrap = WordWrap.On } },
            viewModel.ViewLayoutOptions);

        Assert.True(viewModel.GetLineCount() > 40);
        int startModelLine = viewModel.CoordinatesConverter
            .ConvertViewPositionToModelPosition(new Position(viewModel.ViewLayout.GetLinesViewportData().StartLineNumber, 1)).LineNumber;
        Assert.Equal(firstModelLine, startModelLine);
    }

    [Fact]
    public void TypingInAWrappedLine_UpdatesItsViewLines()
    {
        using ViewModel viewModel = CreateViewModel(LongLine(20));
        int before = viewModel.GetLineCount();

        viewModel.Model.ApplyEdits([TextEdit.Insert(new Position(1, 1), LongLine(40) + " ")]);

        Assert.Equal(1, viewModel.Model.LineCount);
        Assert.True(viewModel.GetLineCount() > before);
    }

    [Fact]
    public void PastingSeveralLines_EachGetsItsLineBreaks()
    {
        using ViewModel viewModel = CreateViewModel("a");

        viewModel.Model.ApplyEdits([TextEdit.Insert(new Position(1, 2), "\n" + LongLine(60) + "\n" + LongLine(60))]);

        Assert.Equal(3, viewModel.Model.LineCount);
        Assert.True(viewModel.GetLineCount() > 3 + 2);
        Assert.True(viewModel.GetLineCount() > viewModel.Model.LineCount * 2);
    }

    [Fact]
    public void ComputeContentWidth_ViewportWrapping_IsTheMaxLineWidth()
    {
        using ViewModel viewModel = CreateViewModel("a");

        viewModel.ViewLayout.SetMaxLineWidth(300);

        Assert.True(viewModel.LayoutInfo.IsViewportWrapping);
        Assert.Equal(300, viewModel.ViewLayout.GetContentWidth());
    }
}
