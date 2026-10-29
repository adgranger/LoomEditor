using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Layout.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

// Monaco: the injected text tests of viewModelImpl.test.ts and cursor.test.ts come with P6-22; these are new tests.
public class ViewModelInjectedTextTests
{
    private const int LineHeight = 20;
    private const int EditorId = 7;

    private readonly FakeTimeProvider _clock = new();
    private readonly FakeFrameScheduler _frames = new();

    private static readonly FontInfo Font = new(1, "Consolas", "normal", 14, LineHeight, 0, EditorFontOptions.LigaturesOff, EditorFontOptions.VariationsOff, true, 8, 16, true, 8, 8, 8, 8);

    private ViewModel CreateViewModel(string text, bool wordWrap)
    {
        var layoutOptions = new EditorLayoutOptions
        {
            Wrapping = new EditorWrappingOptions { WordWrap = wordWrap ? WordWrap.On : WordWrap.Off },
        };
        var viewModel = new ViewModel(
            new TextModel(text), Font, layoutOptions, new ViewLayoutOptions(LineHeight, ScrollBeyondLastLine: false), _frames.Schedule, _clock, EditorId);
        viewModel.SetViewportSize(400, 100);
        return viewModel;
    }

    private static string Inject(ViewModel viewModel, int ownerId, Range range, InjectedTextOptions? before = null, InjectedTextOptions? after = null)
    {
        var options = ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData { Description = "test", Before = before, After = after });
        return viewModel.Model.DeltaDecorations(ownerId, [], [new ModelDeltaDecoration(range, options)])[0];
    }

    [Fact]
    public void AddingInjectedText_ThatWraps_AddsViewLines()
    {
        using ViewModel viewModel = CreateViewModel("abc", wordWrap: true);
        Assert.Equal(1, viewModel.GetLineCount());

        Inject(viewModel, EditorId, new Range(1, 1, 1, 2), after: new InjectedTextOptions { Content = string.Join(' ', Enumerable.Repeat("word", 60)) });

        Assert.True(viewModel.GetLineCount() > 1);
    }

    [Fact]
    public void RemovingInjectedText_RemovesTheViewLines()
    {
        using ViewModel viewModel = CreateViewModel("abc", wordWrap: true);
        string id = Inject(viewModel, EditorId, new Range(1, 1, 1, 2), after: new InjectedTextOptions { Content = string.Join(' ', Enumerable.Repeat("word", 60)) });
        Assert.True(viewModel.GetLineCount() > 1);

        viewModel.Model.DeltaDecorations(EditorId, [id], []);

        Assert.Equal(1, viewModel.GetLineCount());
    }

    [Fact]
    public void InjectedTextBeforeTheCursor_MovesItsViewColumn()
    {
        using ViewModel viewModel = CreateViewModel("abcdefgh", wordWrap: false);
        viewModel.SetSelections("test", [Selection.Collapsed(new Position(1, 5))]);

        Inject(viewModel, EditorId, new Range(1, 3, 1, 4), before: new InjectedTextOptions { Content = "XYZ" });

        Assert.Equal("abXYZcdefgh", viewModel.GetLineContent(1));
        Assert.Equal(new Position(1, 5), viewModel.GetPosition());
        Assert.Equal(new Position(1, 8), viewModel.GetViewSelections()[0].Position);
    }

    [Fact]
    public void InjectedTextOfAnotherEditor_DoesNotChangeTheViewLines()
    {
        using ViewModel viewModel = CreateViewModel("abc", wordWrap: true);

        Inject(viewModel, EditorId + 1, new Range(1, 1, 1, 2), after: new InjectedTextOptions { Content = string.Join(' ', Enumerable.Repeat("word", 60)) });

        Assert.Equal(1, viewModel.GetLineCount());
        Assert.Equal("abc", viewModel.GetLineContent(1));
    }

    [Fact]
    public void GetInjectedTextAt_ReturnsTheOptions()
    {
        using ViewModel viewModel = CreateViewModel("abcdef", wordWrap: false);
        Inject(viewModel, EditorId, new Range(1, 1, 1, 3), after: new InjectedTextOptions { Content = "XY" });

        Assert.Equal("XY", viewModel.GetInjectedTextAt(new Position(1, 4))!.Options.Content);
        Assert.Null(viewModel.GetInjectedTextAt(new Position(1, 2)));
    }
}
