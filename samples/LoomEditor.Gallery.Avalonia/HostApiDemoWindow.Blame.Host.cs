using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LoomEditor.Themes;

namespace LoomEditor.Gallery;

public sealed partial class HostApiDemoWindow
{
    private readonly List<Border> _blamePool = [];

    private double BlameHeight => BlameCanvas.Bounds.Height;

    private void InitializeBlame()
    {
        Editor.ScrollChanged += (_, _) => RedrawBlame();
        Editor.LayoutChanged += (_, _) => RedrawBlame();
        BlameCanvas.SizeChanged += (_, _) => RedrawBlame();
    }

    // The canvas clips to its bounds: nothing to do.
    private static void ClipBlame(double height)
    {
    }

    private void SetBlameBackground(ThemeColor color) => BlameCanvas.Background = Brush(color);

    private void ShowBlameRow(int index, string label, double fontSize, ThemeColor fore, double rowHeight, ThemeColor back, double top)
    {
        Border block = Block(index);
        var text = (TextBlock)block.Child!;
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
            _blamePool[i].IsVisible = false;
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

        _blamePool[index].IsVisible = true;
        return _blamePool[index];
    }

    private static SolidColorBrush Brush(ThemeColor c) => new(Color.FromArgb(c.A, c.R, c.G, c.B));
}
