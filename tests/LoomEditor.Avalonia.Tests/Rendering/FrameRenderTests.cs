using LoomEditor.Avalonia.Rendering;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Rendering;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Tests.Rendering;

// Monaco has no test for the drawing of the lines (it renders HTML): all tests are new.
public class FrameRenderTests
{
    [Fact]
    public async Task Frame_TwoLines_DrawsTextPixelsInEachLineBox()
    {
        await HeadlessSession.RunAsync(() =>
        {
            using AvaloniaTextMeasurer measurer = TestFont.CreateMeasurer();
            var model = new TextModel("MMMM\nWWWW");
            using var viewModel = new ViewModel(
                model,
                measurer.FontInfo,
                new EditorLayoutOptions(),
                new ViewLayoutOptions(measurer.FontInfo.LineHeight, ScrollBeyondLastLine: false),
                _ => new NoopDisposable(),
                TimeProvider.System);
            viewModel.SetViewportSize(300, 100);
            using var renderer = new TextViewRenderer(measurer);
            using var frame = new FrameBitmap(300, 100);

            frame.Draw(measurer, surface =>
                renderer.Render(surface, viewModel, [Selection.Collapsed(new Position(1, 1))], hasFocus: false, cursorVisible: false));

            (byte B, byte G, byte R, byte A) background = frame.PixelAt((int)viewModel.LayoutInfo.ContentLeft + 100, 90);
            double lineHeight = measurer.FontInfo.LineHeight;
            int left = (int)viewModel.LayoutInfo.ContentLeft;
            int right = left + (int)(measurer.FontInfo.SpaceWidth * 4);
            Assert.True(frame.HasInk(left, 0, right, (int)lineHeight, background), "no ink in line 1");
            Assert.True(frame.HasInk(left, (int)lineHeight, right, (int)(2 * lineHeight), background), "no ink in line 2");
            Assert.False(frame.HasInk(left, (int)(2 * lineHeight), right, 100, background), "ink below line 2");
        });
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
