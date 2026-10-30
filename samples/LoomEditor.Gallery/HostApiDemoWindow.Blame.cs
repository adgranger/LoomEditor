using LoomEditor.Themes;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.Gallery;

// The fake blame column: one pooled TextBlock per visible line, kept aligned from the editor's scroll and layout events.
public sealed partial class HostApiDemoWindow
{
    private readonly List<Border> _blamePool = [];

    private void InitializeBlame()
    {
        BlameCanvas.Clip = new RectangleGeometry();
        Editor.ScrollChanged += (_, _) => RedrawBlame();
        Editor.LayoutChanged += (_, _) => RedrawBlame();
        BlameCanvas.SizeChanged += (_, _) => RedrawBlame();
    }

    // shortcut: the line count comes from the text (O(text) per redraw, no public LineCount), and a hidden line is detected as "less than one
    // line height below the previous line", so a wrapped line right above a hidden range may be overdrawn on its last row. Upgrade with a host line count.
    private void RedrawBlame()
    {
        double height = BlameCanvas.ActualHeight;
        ((RectangleGeometry)BlameCanvas.Clip).Rect = new Windows.Foundation.Rect(0, 0, 220, height);
        (Windows.UI.Color back, Windows.UI.Color fore) = ThemeColors();
        BlameCanvas.Background = new SolidColorBrush(back);

        int lineCount = Editor.Text.Count(c => c == '\n') + 1;
        double lineHeight = Editor.LineHeight;
        double Top(int line) => Editor.GetTopForLineNumber(line, true) - Editor.ScrollTop;
        bool Hidden(int line) => line > 1 && Top(line) - Top(line - 1) < lineHeight - 0.5;

        // The last line whose top is at or above the viewport top, moved up to a visible line.
        int first = 1, high = lineCount;
        while (first < high)
        {
            int mid = (first + high + 1) / 2;
            if (Top(mid) <= 0)
            {
                first = mid;
            }
            else
            {
                high = mid - 1;
            }
        }

        while (Hidden(first))
        {
            first--;
        }

        // Visible rows from there; the first row past the bottom closes the last drawn one.
        var rows = new List<(int Line, double Top)>();
        for (int line = first; line <= lineCount; line++)
        {
            if (line != first && Hidden(line))
            {
                continue;
            }

            double top = Top(line);
            rows.Add((line, top));
            if (top >= height)
            {
                break;
            }
        }

        int used = 0;
        for (int i = 0; i < rows.Count && rows[i].Top < height; i++)
        {
            double rowHeight = i + 1 < rows.Count ? rows[i + 1].Top - rows[i].Top : lineHeight;
            if (rowHeight <= 0)
            {
                continue;
            }

            (string commit, string author, int age) = HostApiSamples.Blame(rows[i].Line);
            Border block = Block(used++);
            var text = (TextBlock)block.Child;
            text.Text = $"{commit} {author}";
            text.FontSize = Math.Max(6, lineHeight * 0.7);
            text.Foreground = new SolidColorBrush(fore);
            block.Height = rowHeight;
            block.Width = 220;
            block.Background = new SolidColorBrush(Shade(back, age));
            Canvas.SetTop(block, rows[i].Top);
        }

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

    // Three shades by age: under a week, under three months, older.
    private static Windows.UI.Color Shade(Windows.UI.Color back, int ageDays)
    {
        int delta = ageDays < 7 ? 40 : ageDays < 90 ? 20 : 0;
        return Windows.UI.Color.FromArgb(255, Mix(back.R, delta), Mix(back.G, delta), Mix(back.B, delta));
    }

    private static byte Mix(byte value, int delta) => (byte)(value >= 128 ? value - delta : value + delta);

    // The default token rule (rule 0) carries the foreground and the background of the theme.
    private (Windows.UI.Color Back, Windows.UI.Color Fore) ThemeColors()
    {
        bool dark = Editor.Theme.Type is ThemeType.Dark or ThemeType.HighContrastDark;
        TokenColorRule? rule = Editor.Theme.TokenColors.Count > 0 ? Editor.Theme.TokenColors[0] : null;
        return (ToColor(rule?.Background, dark ? Colors.Black : Colors.White), ToColor(rule?.Foreground, dark ? Colors.LightGray : Colors.DimGray));
    }

    private static Windows.UI.Color ToColor(ThemeColor? color, Windows.UI.Color fallback) =>
        color is { } c ? Windows.UI.Color.FromArgb(c.A, c.R, c.G, c.B) : fallback;
}
