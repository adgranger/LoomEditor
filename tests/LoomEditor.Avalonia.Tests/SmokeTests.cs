using Avalonia.Controls;
using Avalonia.Headless;
using LoomEditor.Avalonia.Tests.TestHelpers;

namespace LoomEditor.Avalonia.Tests;

public class SmokeTests
{
    [Fact]
    public async Task HeadlessWindow_CapturesAFrame()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var window = new Window { Width = 200, Height = 100, Content = new TextBlock { Text = "LoomEditor" } };
            window.Show();

            var frame = window.CaptureRenderedFrame();

            Assert.NotNull(frame);
            Assert.Equal(200, frame.PixelSize.Width);
            Assert.Equal(100, frame.PixelSize.Height);
        });
    }
}
