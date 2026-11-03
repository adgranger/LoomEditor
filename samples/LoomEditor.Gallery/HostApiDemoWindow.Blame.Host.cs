using LoomEditor.Themes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.Gallery;

public sealed partial class HostApiDemoWindow
{
    private readonly List<Border> _blamePool = [];

    private double BlameHeight => BlameCanvas.ActualHeight;

    private void InitializeBlame()
    {
        BlameCanvas.Clip = new RectangleGeometry();
        Editor.ScrollChanged += (_, _) => RedrawBlame();
        Editor.LayoutChanged += (_, _) => RedrawBlame();
        BlameCanvas.SizeChanged += (_, _) => RedrawBlame();
    }

    private void ClipBlame(double height) => ((RectangleGeometry)BlameCanvas.Clip).Rect = new Windows.Foundation.Rect(0, 0, 220, height);

    private void SetBlameBackground(ThemeColor color) => BlameCanvas.Background = Brush(color);

    private void ShowBlameRow(int index, string label, double fontSize, ThemeColor fore, double rowHeight, ThemeColor back, double top)
    {
        Border block = Block(index);
        var text = (TextBlock)block.Child;
        text.Text = label;
        text.FontSize = fontSize;
        text.Foreground = Brush(fore);
        block.Height = rowHeight;
        block.Width = 220;
        block.Background = Brush(back);
        Canvas.SetTop(block, top);
    }

    private void HideBlameRows(int used)
    {
        for (int i = used; i < _blamePool.Count; i++)
        {
            _blamePool[i].Visibility = Visibility.Collapsed;
        }
    }

    private Border Block(int index)
    {
        if (index == _blamePool.Count)
        {
            var block = new Border { IsHitTestVisible = false, Child = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center } };
            _blamePool.Add(block);
            BlameCanvas.Children.Add(block);
        }

        _blamePool[index].Visibility = Visibility.Visible;
        return _blamePool[index];
    }

    private static SolidColorBrush Brush(ThemeColor c) => new(Windows.UI.Color.FromArgb(c.A, c.R, c.G, c.B));
}
