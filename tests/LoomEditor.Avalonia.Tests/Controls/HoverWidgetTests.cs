using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LoomEditor.Avalonia.Controls.Hover;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Features.Hover;
using Editor = global::LoomEditor.Avalonia.Controls.LoomEditor;

namespace LoomEditor.Avalonia.Tests.Controls;

// The twin of the WinUI control: no upstream test, all tests are new.
public class HoverWidgetTests
{
    private sealed class FixedHoverProvider(string markdown) : IHoverProvider
    {
        public Task<Hover?> ProvideHoverAsync(TextModel model, Position position, CancellationToken token) =>
            Task.FromResult<Hover?>(new Hover([new MarkdownString(markdown)]));
    }

    private static async Task<bool> WaitUntil(Func<bool> condition)
    {
        for (int i = 0; i < 100 && !condition(); i++)
        {
            await Task.Delay(20);
        }

        return condition();
    }

    private static async Task<(Editor Editor, Window Window, HoverWidget Widget)> ShowHover()
    {
        var editor = new Editor { Text = "hello world\nsecond line", MinimapEnabled = false };
        editor.LanguageFeatures.HoverProviders.Register("*", new FixedHoverProvider("**docs** for `hello`"));
        Window window = EditorWindow.Open(editor);
        editor.Focus();
        EditorWindow.Capture(window);
        editor.SetSelections([Selection.Collapsed(new Position(1, 2))]);
        HoverWidget widget = editor.GetVisualDescendants().OfType<HoverWidget>().Single();

        editor.Commands.Execute(HoverCommands.ShowHover);
        Assert.True(await WaitUntil(() => widget.IsVisible), "the hover did not show");
        return (editor, window, widget);
    }

    [Fact]
    public async Task Hover_ProviderResult_ShowsWidgetNearPosition()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (Editor editor, Window window, HoverWidget widget) = await ShowHover();
            Dispatcher.UIThread.RunJobs();

            var row = Assert.IsType<SelectableTextBlock>(widget.Rows[0]);
            Assert.Contains(row.Inlines!.OfType<Run>(), run => run.Text == "docs");
            // Under the first line when there is room.
            Assert.True(Canvas.GetTop(widget) >= editor.LineHeight, $"top {Canvas.GetTop(widget)}");
            Assert.True(Canvas.GetLeft(widget) >= editor.ViewModel.LayoutInfo.ContentLeft);
            window.Close();
        });
    }

    [Fact]
    public async Task Hover_Hide_HidesWidget()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (Editor editor, Window window, HoverWidget widget) = await ShowHover();

            editor.Commands.Execute(HoverCommands.HideHover);

            Assert.False(widget.IsVisible);
            window.Close();
        });
    }
}
