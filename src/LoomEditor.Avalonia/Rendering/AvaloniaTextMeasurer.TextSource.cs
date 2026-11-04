using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;

namespace LoomEditor.Avalonia.Rendering;

internal sealed partial class AvaloniaTextMeasurer
{
    /// <summary>
    /// The paragraph of one line: no wrapping, the editor's tab width and letter spacing.
    /// <c>GenericTextParagraphProperties</c> is sealed and takes no tab width, hence this class.
    /// </summary>
    /// <remarks>Threading: immutable.</remarks>
    private sealed class LineParagraphProperties(TextRunProperties defaultProperties, double incrementalTab, double letterSpacing)
        : TextParagraphProperties
    {
        public override FlowDirection FlowDirection => FlowDirection.LeftToRight;

        public override TextAlignment TextAlignment => TextAlignment.Left;

        public override double LineHeight => double.NaN;

        public override bool FirstLineInParagraph => true;

        public override TextRunProperties DefaultTextRunProperties => defaultProperties;

        public override TextWrapping TextWrapping => TextWrapping.NoWrap;

        public override double Indent => 0;

        public override double DefaultIncrementalTab => incrementalTab;

        public override double LetterSpacing => letterSpacing;
    }

    /// <summary>
    /// The text of one line, split at the style overrides (Avalonia's own <c>FormattedTextSource</c> is internal) and at
    /// the tabs. The overrides are in order and never overlap; the text between them gets the default properties.
    /// </summary>
    /// <remarks>Threading: UI thread only; <see cref="Length"/> and the tab widths are set while the line is laid out.</remarks>
    private sealed class LineTextSource(string text, TextRunProperties defaultProperties, IReadOnlyList<ValueSpan<TextRunProperties>> overrides)
        : ITextSource
    {
        private readonly Dictionary<int, double> _tabWidths = [];

        /// <summary>The number of characters given to the formatter: less than the text while the text before a tab is measured.</summary>
        public int Length { get; set; } = text.Length;

        public void SetTabWidth(int index, double width) => _tabWidths[index] = width;

        public TextRun? GetTextRun(int textSourceIndex)
        {
            if (textSourceIndex >= Length)
            {
                return new TextEndOfParagraph();
            }

            int end = Length;
            TextRunProperties properties = defaultProperties;
            foreach (ValueSpan<TextRunProperties> span in overrides)
            {
                if (span.Start + span.Length <= textSourceIndex)
                {
                    continue;
                }

                if (span.Start <= textSourceIndex)
                {
                    end = Math.Min(end, span.Start + span.Length);
                    properties = span.Value;
                }
                else
                {
                    end = Math.Min(end, span.Start);
                }

                break;
            }

            if (text[textSourceIndex] == '\t')
            {
                return new TabRun(_tabWidths.GetValueOrDefault(textSourceIndex), properties);
            }

            int tab = text.IndexOf('\t', textSourceIndex, end - textSourceIndex);
            return new TextCharacters(text.AsMemory(textSourceIndex, (tab < 0 ? end : tab) - textSourceIndex), properties);
        }
    }

    /// <summary>A tab: one character of blank space up to the next tab stop.</summary>
    /// <remarks>Threading: immutable.</remarks>
    private sealed class TabRun(double width, TextRunProperties properties) : DrawableTextRun
    {
        public override Size Size => new(width, 0);

        public override double Baseline => 0;

        public override TextRunProperties Properties => properties;

        public override void Draw(DrawingContext drawingContext, Point origin)
        {
            // A tab has no ink.
        }
    }
}
