using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using LoomEditor.Avalonia.Controls.Markdown;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.View.Markdown;

namespace LoomEditor.Avalonia.Tests.Controls;

// The twin of the WinUI builder: no upstream test, all tests are new.
public class MarkdownElementBuilderTests
{
    private static readonly SolidColorBrush Link = new(Colors.Blue);

    private static IReadOnlyList<Control> Build(string markdown)
    {
        var theme = new MarkdownTheme { Foreground = Brushes.White, LinkBrush = Link, QuoteBar = Brushes.Gray, Separator = Brushes.Gray, CodeBackground = Brushes.Black };
        return new MarkdownElementBuilder(theme).Build(MarkdownParser.Parse(markdown), 13, new FontFamily(TestFont.Family));
    }

    private static List<Run> Runs(Control row) => ((SelectableTextBlock)row).Inlines!.OfType<Run>().ToList();

    [Fact]
    public async Task Markdown_BoldItalicCode_BuildsInlines()
    {
        await HeadlessSession.RunAsync(() =>
        {
            Control row = Assert.Single(Build("plain **bold** *italic* `code`"));

            List<Run> runs = Runs(row);
            Assert.Equal(FontWeight.SemiBold, runs.Single(r => r.Text == "bold").FontWeight);
            Assert.Equal(FontStyle.Italic, runs.Single(r => r.Text == "italic").FontStyle);
            Assert.Equal(new FontFamily(TestFont.Family), runs.Single(r => r.Text == "code").FontFamily);
            Assert.Equal(FontWeight.Normal, runs.First(r => r.Text!.StartsWith("plain", StringComparison.Ordinal)).FontWeight);
        });
    }

    [Fact]
    public async Task Markdown_CodeBlock_BuildsBorder()
    {
        await HeadlessSession.RunAsync(() =>
        {
            Control row = Assert.Single(Build("```\nvar x = 1;\n```"));

            Border border = Assert.IsType<Border>(row);
            Assert.Same(Brushes.Black, border.Background);
            var text = Assert.IsType<SelectableTextBlock>(border.Child);
            Assert.Contains("var x = 1;", Assert.Single(text.Inlines!.OfType<Run>()).Text, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Markdown_Link_IsStyledAsLink()
    {
        await HeadlessSession.RunAsync(() =>
        {
            Control row = Assert.Single(Build("see [docs](https://example.com) now"));

            Run link = Runs(row).Single(r => r.Text == "docs");
            Assert.Same(Link, link.Foreground);
            Assert.NotNull(link.TextDecorations);
            Assert.NotEmpty(link.TextDecorations);
        });
    }

    [Fact]
    public async Task Markdown_HeadingAndList_BuildRows()
    {
        await HeadlessSession.RunAsync(() =>
        {
            IReadOnlyList<Control> rows = Build("# Title\n\n- one\n- two");

            Assert.Equal(2, rows.Count);
            Assert.Equal(FontWeight.Bold, Runs(rows[0]).Single().FontWeight);
            Assert.True(((SelectableTextBlock)rows[0]).FontSize > 13);
            StackPanel list = Assert.IsType<StackPanel>(rows[1]);
            Assert.Equal(2, list.Children.Count);
            Assert.Equal("• ", ((SelectableTextBlock)list.Children[0]).Inlines!.OfType<Run>().First().Text);
        });
    }
}
