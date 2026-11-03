using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using LoomEditor.Themes;
using Editor = global::LoomEditor.Avalonia.Controls.LoomEditor;

namespace LoomEditor.Avalonia.Tests.TestHelpers;

/// <summary>Shows an editor in a headless 400 x 200 window with the test font and reads rendered frames back.</summary>
/// <remarks>Threading: UI thread of the headless session only.</remarks>
internal static class EditorWindow
{
    public const int Width = 400;
    public const int Height = 200;

    public static Window Open(Editor editor)
    {
        editor.FontFamily = new(TestFont.Family);
        var window = new Window { Width = Width, Height = Height, Content = editor };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    /// <summary>The pixels of the next rendered frame, ARGB, row by row.</summary>
    public static uint[] Capture(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        WriteableBitmap frame = window.CaptureRenderedFrame()!;
        using var buffer = frame.Lock();
        var pixels = new int[frame.PixelSize.Width * frame.PixelSize.Height];
        System.Runtime.InteropServices.Marshal.Copy(buffer.Address, pixels, 0, pixels.Length);
        return Array.ConvertAll(pixels, pixel => (uint)pixel);
    }

    /// <summary>The number of pixels of the rectangle that are not the dark theme background.</summary>
    public static int InkCount(uint[] pixels, int left, int top, int right, int bottom)
    {
        EditorTheme theme = EditorTheme.Dark;
        uint background = ((uint)theme.Background.A << 24) | ((uint)theme.Background.R << 16) | ((uint)theme.Background.G << 8) | theme.Background.B;
        int count = 0;
        for (int y = top; y < bottom; y++)
        {
            for (int x = left; x < right; x++)
            {
                if (pixels[(y * Width) + x] != background)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
