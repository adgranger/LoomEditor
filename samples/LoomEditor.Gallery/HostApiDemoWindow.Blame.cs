using LoomEditor.Themes;

namespace LoomEditor.Gallery;

// The fake blame column: one pooled TextBlock per visible line, kept aligned from the editor's scroll and layout events.
public sealed partial class HostApiDemoWindow
{
    // shortcut: the line count comes from the text (O(text) per redraw, no public LineCount), and a hidden line is detected as "less than one
    // line height below the previous line", so a wrapped line right above a hidden range may be overdrawn on its last row. Upgrade with a host line count.
    private void RedrawBlame()
    {
        double height = BlameHeight;
        ClipBlame(height);
        (ThemeColor back, ThemeColor fore) = ThemeColors();
        SetBlameBackground(back);

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
            ShowBlameRow(used++, $"{commit} {author}", Math.Max(6, lineHeight * 0.7), fore, rowHeight, Shade(back, age), rows[i].Top);
        }

        HideBlameRows(used);
    }

    // Three shades by age: under a week, under three months, older.
    private static ThemeColor Shade(ThemeColor back, int ageDays)
    {
        int delta = ageDays < 7 ? 40 : ageDays < 90 ? 20 : 0;
        return ThemeColor.FromRgb(Mix(back.R, delta), Mix(back.G, delta), Mix(back.B, delta));
    }

    private static byte Mix(byte value, int delta) => (byte)(value >= 128 ? value - delta : value + delta);

    // The default token rule (rule 0) carries the foreground and the background of the theme.
    private (ThemeColor Back, ThemeColor Fore) ThemeColors()
    {
        bool dark = Editor.Theme.Type is ThemeType.Dark or ThemeType.HighContrastDark;
        TokenColorRule? rule = Editor.Theme.TokenColors.Count > 0 ? Editor.Theme.TokenColors[0] : null;
        return (
            rule?.Background ?? (dark ? ThemeColor.FromRgb(0, 0, 0) : ThemeColor.FromRgb(255, 255, 255)),
            rule?.Foreground ?? (dark ? ThemeColor.FromRgb(211, 211, 211) : ThemeColor.FromRgb(105, 105, 105)));
    }
}
