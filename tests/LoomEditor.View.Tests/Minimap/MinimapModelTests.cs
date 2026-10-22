using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Themes;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;
using LoomEditor.View.Minimap;

namespace LoomEditor.View.Tests.Minimap;

// All the tests are new: Monaco has none for the minimap model.
public class MinimapModelTests
{
    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private sealed class RecordingTarget : IMinimapRenderTarget
    {
        public List<string> Calls { get; } = [];

        public void OnDidChangeOptions() => Calls.Add(nameof(OnDidChangeOptions));

        public void OnFlushed() => Calls.Add(nameof(OnFlushed));

        public void OnLinesChanged(int changeFromLineNumber, int changeCount) => Calls.Add(nameof(OnLinesChanged));

        public void OnLinesDeleted(int deleteFromLineNumber, int deleteToLineNumber) => Calls.Add(nameof(OnLinesDeleted));

        public void OnLinesInserted(int insertFromLineNumber, int insertToLineNumber) => Calls.Add(nameof(OnLinesInserted));

        public void OnTokensChanged(IReadOnlyList<ViewLineRange> ranges) => Calls.Add(nameof(OnTokensChanged));

        public void OnSelectionChanged() => Calls.Add(nameof(OnSelectionChanged));

        public void OnDecorationsChanged() => Calls.Add(nameof(OnDecorationsChanged));

        public void OnThemeChanged() => Calls.Add(nameof(OnThemeChanged));
    }

    private static ModelDecorationOptions MinimapDecoration() => ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData
    {
        Description = "minimap",
        Minimap = new ModelDecorationMinimapOptions(DecorationColor.FromValue("#ff0000"), null, MinimapPosition.Inline),
    });

    private static void WithMinimap(int lineCount, Action<MinimapModel, ViewModel, RecordingTarget> callback)
    {
        var model = new TextModel(string.Join('\n', Enumerable.Range(1, lineCount).Select(i => $"line {i}")));
        FontInfo fontInfo = FontInfo.Default;
        using var viewModel = new ViewModel(
            model, fontInfo, new EditorLayoutOptions { Minimap = new EditorMinimapOptions { Size = MinimapSize.Fill } }, new ViewLayoutOptions(fontInfo.LineHeight),
            _ => new NoopDisposable(), TimeProvider.System);
        viewModel.SetViewportSize(800, 200);
        var target = new RecordingTarget();
        using var minimap = new MinimapModel(
            viewModel, target, EditorTheme.Dark, (scale, family) => MinimapCharRendererFactory.Create(scale, family, null));
        callback(minimap, viewModel, target);
    }

    [Fact]
    public void GetLineCount_Sampling_IsTheSampleCount()
    {
        WithMinimap(5000, (minimap, viewModel, _) =>
        {
            Assert.True(minimap.IsSampling);
            Assert.Equal(5000, minimap.GetRealLineCount());
            Assert.True(minimap.GetLineCount() < 5000);
        });
    }

    [Fact]
    public void GetLineCount_NotSampling_IsTheViewLineCount()
    {
        WithMinimap(3, (minimap, viewModel, _) =>
        {
            Assert.False(minimap.IsSampling);
            Assert.Equal(3, minimap.GetLineCount());
            Assert.Equal("line 2", minimap.GetLineContent(2));
        });
    }

    [Fact]
    public void GetMinimapLinesRenderingData_Sampling_ReadsTheSampledLines()
    {
        WithMinimap(5000, (minimap, viewModel, _) =>
        {
            ViewLineData?[] data = minimap.GetMinimapLinesRenderingData(1, 3, [true, false, true]);

            Assert.Equal(minimap.GetLineContent(1), data[0]!.Content);
            Assert.Null(data[1]);
            Assert.Equal(minimap.GetLineContent(3), data[2]!.Content);
            Assert.NotEqual("line 3", data[2]!.Content);
        });
    }

    [Fact]
    public void GetMinimapDecorationsInViewport_Sampling_MapsToMinimapLines()
    {
        WithMinimap(5000, (minimap, viewModel, _) =>
        {
            viewModel.Model.ChangeDecorations(0, accessor => accessor.AddDecoration(new Range(2500, 2, 2500, 4), MinimapDecoration()));

            IReadOnlyList<ViewModelDecoration> result = minimap.GetMinimapDecorationsInViewport(1, minimap.GetLineCount());

            ViewModelDecoration decoration = Assert.Single(result);
            int minimapLine = minimap.ViewLineToMinimapLine(2500);
            Assert.Equal(new Range(minimapLine, 2, minimapLine, 4), decoration.Range);
            Assert.NotEqual(2500, minimapLine);
        });
    }

    [Fact]
    public void ContentChange_FlushesTheRenderTarget()
    {
        WithMinimap(5000, (minimap, viewModel, target) =>
        {
            viewModel.Type("x");

            Assert.Contains(nameof(RecordingTarget.OnFlushed), target.Calls);
            Assert.Contains(nameof(RecordingTarget.OnSelectionChanged), target.Calls);
        });
    }

    [Fact]
    public void UpdateOptions_SameOptions_ReturnsFalse()
    {
        WithMinimap(5000, (minimap, viewModel, target) =>
        {
            Assert.False(minimap.UpdateOptions(EditorTheme.Dark));
            Assert.Empty(target.Calls);
        });
    }

    [Fact]
    public void UpdateOptions_NewTheme_ReturnsTrueAndNotifiesTheTarget()
    {
        WithMinimap(5000, (minimap, viewModel, target) =>
        {
            Assert.True(minimap.UpdateOptions(EditorTheme.Light));
            Assert.Contains(nameof(RecordingTarget.OnThemeChanged), target.Calls);
            Assert.Contains(nameof(RecordingTarget.OnDidChangeOptions), target.Calls);
        });
    }
}
