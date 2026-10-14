using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering.Tests.TestHelpers;

/// <summary>A text model, a view model with a fake font, and a renderer, ready to draw into a recording surface.</summary>
/// <remarks>Threading: not thread-safe; one test, one thread.</remarks>
internal sealed class ViewHarness : IDisposable
{
    public ViewHarness(string text, int width = 500, int height = 200)
    {
        Model = new TextModel(text);
        Measurer = new FakeTextMeasurer();
        ViewModel = new ViewModel(
            Model,
            Measurer.FontInfo,
            new EditorLayoutOptions(),
            new ViewLayoutOptions(Measurer.FontInfo.LineHeight, ScrollBeyondLastLine: false),
            _ => new NoopDisposable(),
            TimeProvider.System);
        ViewModel.SetViewportSize(width, height);
        Renderer = new TextViewRenderer(Measurer);
    }

    public TextModel Model { get; }

    public FakeTextMeasurer Measurer { get; }

    public ViewModel ViewModel { get; }

    public TextViewRenderer Renderer { get; }

    public EditorLayoutInfo LayoutInfo => ViewModel.LayoutInfo;

    public RecordingRenderSurface Render(Selection? selection = null)
    {
        var surface = new RecordingRenderSurface();
        Renderer.Render(surface, ViewModel, selection ?? Selection.Collapsed(new Position(1, 1)));
        return surface;
    }

    public void ScrollTo(double? scrollTop = null, double? scrollLeft = null) =>
        ViewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(scrollLeft, scrollTop), ScrollType.Immediate);

    public void Dispose()
    {
        Renderer.Dispose();
        ViewModel.Dispose();
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
